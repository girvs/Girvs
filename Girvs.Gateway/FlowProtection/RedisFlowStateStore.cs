using System.Globalization;
using Girvs.Cache.CacheImps;
using StackExchange.Redis;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 基于 Redis 的流程票据状态存储实现。
/// </summary>
/// <param name="redis">Redis 连接包装器，用于获取可执行脚本的数据库实例。</param>
/// <remarks>
/// <para>
/// 全部状态迁移（创建、预留、提交、释放、删除）都以 Lua 脚本一次执行完成，
/// 借助 Redis 单线程执行脚本的特性获得“比较并交换”语义，避免并发请求同时通过同一顺位的校验。
/// </para>
/// <para>
/// 每张票据对应一个 Redis Hash（键前缀见 <see cref="KeyPrefix"/>），字段结构固定为：
/// <c>flowId</c> 归属流程、<c>businessId</c> 绑定的业务标识、<c>nextIndex</c> 下一个允许顺位、
/// <c>variant</c> 锁定的流程组合键、<c>status</c> 状态（<c>processing</c> 处理中 / <c>active</c> 可继续 /
/// <c>completed</c> 已完成）、<c>attemptId</c> 当前处理锁凭据、
/// <c>lockUntilUnixMs</c> 处理锁到期时刻（Unix 毫秒）、<c>createdAtUnixMs</c> 创建时刻（Unix 毫秒）。
/// </para>
/// <para>
/// 时间一律取自 Redis 服务端的 <c>TIME</c> 命令而非网关本地时钟，避免多实例时钟漂移造成处理锁
/// 提前失效或永不失效。
/// </para>
/// </remarks>
public sealed class RedisFlowStateStore(IRedisConnectionWrapper redis) : IFlowStateStore
{
    /// <summary>
    /// 票据键前缀，与票据值拼接后作为 Redis 键；把前缀独立成常量便于运维按模式检索与清理。
    /// </summary>
    private const string KeyPrefix = "flow:ticket:";

    /// <summary>
    /// 创建票据并占用首步的原子脚本。
    /// </summary>
    /// <remarks>
    /// <para>键与参数排布：</para>
    /// <list type="bullet">
    /// <item><description><c>KEYS[1]</c>：票据键（前缀 + 票据值）。</description></item>
    /// <item><description><c>ARGV[1]</c>：流程标识，写入 <c>flowId</c> 字段。</description></item>
    /// <item><description><c>ARGV[2]</c>：业务标识，写入 <c>businessId</c> 字段（可为空串，表示暂未绑定）。</description></item>
    /// <item><description><c>ARGV[3]</c>：首步顺位，写入 <c>nextIndex</c> 字段。</description></item>
    /// <item><description><c>ARGV[4]</c>：本次处理锁凭据，写入 <c>attemptId</c> 字段。</description></item>
    /// <item><description><c>ARGV[5]</c>：票据存活时长（秒），用于 <c>PEXPIRE</c>。</description></item>
    /// <item><description><c>ARGV[6]</c>：处理锁时长（秒），与当前 Unix 毫秒相加得到 <c>lockUntilUnixMs</c>。</description></item>
    /// <item><description><c>ARGV[7]</c>：流程组合键，写入 <c>variant</c> 字段；线性流程为空串。</description></item>
    /// </list>
    /// <para>
    /// 返回值固定为三元组 <c>{ 状态码, 下一个允许顺位, 是否完成 }</c>：
    /// 成功为 <c>{'OK', ARGV[3], '0'}</c>；票据已存在（含已完成但仍在保留期内的票据）返回
    /// <c>{'FLOW_IN_PROGRESS'}</c>，由上层按“重复开启”拒绝。
    /// </para>
    /// <para>
    /// 首步即进入 <c>processing</c> 并直接持锁：本次请求与其它并发请求的互斥由同一把锁保证，
    /// 无需额外的“先建后锁”两步操作。
    /// </para>
    /// </remarks>
    private const string CreateAndReserveScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 1 then
            return {'FLOW_IN_PROGRESS'}
        end

        local t = redis.call('TIME')
        local now = (t[1] * 1000) + math.floor(t[2] / 1000)
        local lockUntil = now + (tonumber(ARGV[6]) * 1000)

        redis.call('HSET', KEYS[1],
            'flowId', ARGV[1],
            'businessId', ARGV[2],
            'nextIndex', ARGV[3],
            'variant', ARGV[7],
            'status', 'processing',
            'attemptId', ARGV[4],
            'lockUntilUnixMs', tostring(lockUntil),
            'createdAtUnixMs', tostring(now))
        redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[5]) * 1000)
        return {'OK', ARGV[3], '0'}
        """;

    /// <summary>
    /// 推进既有票据到下一步的原子脚本，同时兼容线性流程与参数化流程。
    /// </summary>
    /// <remarks>
    /// <para>键与参数排布：</para>
    /// <list type="bullet">
    /// <item><description><c>KEYS[1]</c>：票据键。</description></item>
    /// <item><description><c>ARGV[1]</c>：流程标识，必须与票据 <c>flowId</c> 字段一致。</description></item>
    /// <item><description><c>ARGV[2]</c>：业务标识，必须与票据已绑定的值一致（票据未绑定时不校验）。</description></item>
    /// <item><description><c>ARGV[3]</c>：线性流程的期望顺位；给出组合候选时会被候选顺位取代。</description></item>
    /// <item><description><c>ARGV[4]</c>：本次处理锁凭据，成功后写入 <c>attemptId</c>。</description></item>
    /// <item><description><c>ARGV[5]</c>：处理锁时长（秒）。</description></item>
    /// <item><description><c>ARGV[6]</c>：调用方携带的前置参数（已归一为组合键）；空串表示未携带。</description></item>
    /// <item><description><c>ARGV[7]</c>：候选组合数量 <c>N</c>；线性流程传 0。</description></item>
    /// <item><description>
    /// <c>ARGV[8 + 2i]</c> 与 <c>ARGV[9 + 2i]</c>（<c>i</c> 从 0 到 <c>N-1</c>）：
    /// 第 <c>i</c> 个候选的（组合键, 该接口在该组合中的顺位）配对。
    /// </description></item>
    /// </list>
    /// <para>
    /// 校验优先级（顺序即拒绝优先级，先命中先返回）：
    /// 票据不存在 → 已完成 → 流程标识不符 → 业务标识不符 → 组合相关校验（未锁定组合、前置参数冲突、
    /// 候选未命中）→ 顺位不符 → 处理锁未过期或状态非法。
    /// </para>
    /// <para>
    /// 返回值为 <c>{ 状态码, 期望顺位, '0', 命中的组合键 }</c>；失败时只返回单元素
    /// <c>{ 状态码 }</c>。第四项为空串表示线性流程，上层据此判定是否需要按组合还原顺位。
    /// </para>
    /// </remarks>
    private const string ReserveScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return {'FLOW_NOT_FOUND'}
        end

        local status = redis.call('HGET', KEYS[1], 'status')
        if status == 'completed' then
            return {'FLOW_COMPLETED'}
        end

        if redis.call('HGET', KEYS[1], 'flowId') ~= ARGV[1] then
            return {'FLOW_STEP_NOT_ALLOWED'}
        end

        local storedBusinessId = redis.call('HGET', KEYS[1], 'businessId') or ''
        if storedBusinessId ~= '' and storedBusinessId ~= ARGV[2] then
            return {'FLOW_BUSINESS_MISMATCH'}
        end

        local expectedIndexText = ARGV[3]
        local matchedVariant = ''
        local candidateCount = tonumber(ARGV[7]) or 0
        if candidateCount > 0 then
            local lockedVariant = redis.call('HGET', KEYS[1], 'variant') or ''
            if lockedVariant == '' then
                return {'FLOW_STEP_NOT_ALLOWED'}
            end

            if ARGV[6] ~= '' and ARGV[6] ~= lockedVariant then
                return {'FLOW_SELECTOR_MISMATCH'}
            end

            local found = false
            for i = 0, candidateCount - 1 do
                local variantKey = ARGV[8 + (i * 2)]
                if variantKey == lockedVariant then
                    expectedIndexText = ARGV[9 + (i * 2)]
                    matchedVariant = variantKey
                    found = true
                    break
                end
            end

            if not found then
                return {'FLOW_STEP_NOT_ALLOWED'}
            end
        end

        if redis.call('HGET', KEYS[1], 'nextIndex') ~= expectedIndexText then
            return {'FLOW_STEP_NOT_ALLOWED'}
        end

        local t = redis.call('TIME')
        local now = (t[1] * 1000) + math.floor(t[2] / 1000)
        if status == 'processing' then
            local lockUntil = tonumber(redis.call('HGET', KEYS[1], 'lockUntilUnixMs') or '0')
            if lockUntil > now then
                return {'FLOW_IN_PROGRESS'}
            end
        elseif status ~= 'active' then
            return {'FLOW_IN_PROGRESS'}
        end

        local newLockUntil = now + (tonumber(ARGV[5]) * 1000)
        redis.call('HSET', KEYS[1],
            'status', 'processing',
            'attemptId', ARGV[4],
            'lockUntilUnixMs', tostring(newLockUntil))
        return {'OK', expectedIndexText, '0', matchedVariant}
        """;

    /// <summary>
    /// 提交当前步骤并推进顺位的原子脚本。
    /// </summary>
    /// <remarks>
    /// <para>键与参数排布：</para>
    /// <list type="bullet">
    /// <item><description><c>KEYS[1]</c>：票据键。</description></item>
    /// <item><description><c>ARGV[1]</c>：处理锁凭据，必须与 <c>attemptId</c> 字段一致。</description></item>
    /// <item><description><c>ARGV[2]</c>：提交前的当前顺位，必须与 <c>nextIndex</c> 字段一致。</description></item>
    /// <item><description><c>ARGV[3]</c>：是否为最后一步，<c>'1'</c> 表示是、<c>'0'</c> 表示否。</description></item>
    /// <item><description><c>ARGV[4]</c>：下游回传的业务标识；空串表示本次不绑定也不比对。</description></item>
    /// <item><description><c>ARGV[5]</c>：完成态保留时长（秒），仅在最后一步时用于重置过期时间。</description></item>
    /// </list>
    /// <para>
    /// 状态迁移：先清空 <c>attemptId</c> 与 <c>lockUntilUnixMs</c> 释放处理锁，再按是否为最后一步
    /// 把 <c>status</c> 置为 <c>completed</c> 或 <c>active</c>，并把 <c>nextIndex</c> 加一。
    /// </para>
    /// <para>
    /// 返回值为 <c>{ 状态码, 新顺位, 是否完成 }</c>：最后一步为 <c>{'OK', nextIndex, '1'}</c>
    /// 并同时把过期时间改为完成态时长；中间步为 <c>{'OK', nextIndex, '0'}</c>，票据存活时长保持不变。
    /// 已完成、非持锁状态或凭据不符返回 <c>FLOW_COMPLETED</c> / <c>FLOW_IN_PROGRESS</c>，
    /// 顺位不符返回 <c>FLOW_STEP_NOT_ALLOWED</c>，业务标识冲突返回 <c>FLOW_BUSINESS_MISMATCH</c>。
    /// </para>
    /// <para>
    /// 下游回传的业务标识绑定只在此处发生：票据若尚未绑定（空串）则写入下游回传值，已绑定则必须一致，
    /// 从而保证下游在首步成功后才确定业务标识时也能正确落库。
    /// </para>
    /// </remarks>
    private const string CommitScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return {'FLOW_NOT_FOUND'}
        end

        local status = redis.call('HGET', KEYS[1], 'status')
        if status == 'completed' then
            return {'FLOW_COMPLETED'}
        end

        if status ~= 'processing' or redis.call('HGET', KEYS[1], 'attemptId') ~= ARGV[1] then
            return {'FLOW_IN_PROGRESS'}
        end

        if redis.call('HGET', KEYS[1], 'nextIndex') ~= ARGV[2] then
            return {'FLOW_STEP_NOT_ALLOWED'}
        end

        if ARGV[4] ~= '' then
            local storedBusinessId = redis.call('HGET', KEYS[1], 'businessId') or ''
            if storedBusinessId == '' then
                redis.call('HSET', KEYS[1], 'businessId', ARGV[4])
            elseif storedBusinessId ~= ARGV[4] then
                return {'FLOW_BUSINESS_MISMATCH'}
            end
        end

        local nextIndex = tostring(tonumber(ARGV[2]) + 1)
        redis.call('HSET', KEYS[1], 'attemptId', '', 'lockUntilUnixMs', '0')
        if ARGV[3] == '1' then
            redis.call('HSET', KEYS[1], 'status', 'completed', 'nextIndex', nextIndex)
            redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[5]) * 1000)
            return {'OK', nextIndex, '1'}
        end

        redis.call('HSET', KEYS[1], 'status', 'active', 'nextIndex', nextIndex)
        return {'OK', nextIndex, '0'}
        """;

    /// <summary>
    /// 释放处理锁并让票据回到可继续状态的原子脚本。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>KEYS[1]</c> 为票据键，<c>ARGV[1]</c> 为处理锁凭据；凭据必须与 <c>attemptId</c> 字段一致，
    /// 否则说明调用方已不是当前持锁者（例如锁曾被超时剥夺），返回 <c>FLOW_IN_PROGRESS</c> 而不做任何修改。
    /// </para>
    /// <para>
    /// 状态迁移：<c>status</c> 置回 <c>active</c>，清空 <c>attemptId</c> 与 <c>lockUntilUnixMs</c>；
    /// <c>nextIndex</c> 保持不变，因此该步骤可被重新尝试。
    /// </para>
    /// <para>
    /// 返回 <c>{ 'OK', 当前顺位, '0' }</c>；票据不存在返回 <c>FLOW_NOT_FOUND</c>，
    /// 已完成返回 <c>FLOW_COMPLETED</c>（完成态不允许回退）。
    /// </para>
    /// </remarks>
    private const string ReleaseScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return {'FLOW_NOT_FOUND'}
        end

        if redis.call('HGET', KEYS[1], 'status') == 'completed' then
            return {'FLOW_COMPLETED'}
        end

        if redis.call('HGET', KEYS[1], 'attemptId') ~= ARGV[1] then
            return {'FLOW_IN_PROGRESS'}
        end

        redis.call('HSET', KEYS[1],
            'status', 'active',
            'attemptId', '',
            'lockUntilUnixMs', '0')
        return {'OK', redis.call('HGET', KEYS[1], 'nextIndex'), '0'}
        """;

    /// <summary>
    /// 删除整张票据的原子脚本。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>KEYS[1]</c> 为票据键，<c>ARGV[1]</c> 为处理锁凭据。凭据必须与 <c>attemptId</c> 一致，
    /// 防止已被超时剥夺的旧尝试删掉属于新持锁者的票据。
    /// </para>
    /// <para>
    /// 与释放不同，这里不检查完成态：删除是撤销语义，成功即移除整个 Hash，
    /// 返回 <c>{ 'OK', '', '0' }</c>（第二项为空串，表示不再有顺位语义）。
    /// 票据不存在返回 <c>FLOW_NOT_FOUND</c>，凭据不符返回 <c>FLOW_IN_PROGRESS</c>。
    /// </para>
    /// </remarks>
    private const string DeleteScript =
        """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return {'FLOW_NOT_FOUND'}
        end

        if redis.call('HGET', KEYS[1], 'attemptId') ~= ARGV[1] then
            return {'FLOW_IN_PROGRESS'}
        end

        redis.call('DEL', KEYS[1])
        return {'OK', '', '0'}
        """;

    /// <summary>
    /// 执行 <see cref="CreateAndReserveScript"/>，创建票据并占用首步。
    /// </summary>
    /// <param name="request">预留请求，需要票据、流程标识、首步顺位、存活时长与处理锁时长。</param>
    /// <param name="cancellationToken">取消标记；当前未下传到底层脚本调用，保留以符合接口契约。</param>
    /// <returns>解析后的存储结果：成功时含首步顺位，票据已存在时为“处理中”失败。</returns>
    /// <remarks>
    /// <c>ARGV</c> 顺序与脚本注释一致：流程标识、业务标识、首步顺位、处理锁凭据、
    /// 票据存活秒数、处理锁秒数、组合键。数值统一用不变文化格式化，避免不同区域设置产生千分位等干扰。
    /// </remarks>
    public async Task<FlowStoreResult> CreateAndReserveAsync(
        FlowReserveRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(
            CreateAndReserveScript,
            [Key(request.Ticket)],
            [
                request.FlowId,
                request.BusinessId,
                InvariantText(request.ExpectedIndex),
                request.AttemptId,
                InvariantText(request.FlowTtlSeconds),
                InvariantText(request.LockTtlSeconds),
                request.VariantKey,
            ]
        );

        return MapResult(result);
    }

    /// <summary>
    /// 执行 <see cref="ReserveScript"/>，为既有票据占用下一步。
    /// </summary>
    /// <param name="request">
    /// 预留请求：线性流程给出期望顺位（<see cref="FlowReserveRequest.ExpectedIndex"/>）；
    /// 参数化流程给出候选组合顺位（<see cref="FlowReserveRequest.CandidateVariantSteps"/>）。
    /// </param>
    /// <param name="cancellationToken">取消标记；当前未下传到底层脚本调用，保留以符合接口契约。</param>
    /// <returns>解析后的存储结果：成功时含命中的组合键（线性流程为 <see langword="null" />）与下一个顺位。</returns>
    /// <remarks>
    /// <para>
    /// 参数数组按“前 7 项定长 + 每候选 2 项”排布：下标 0 到 6 依次为流程标识、业务标识、期望顺位、
    /// 处理锁凭据、处理锁秒数、前置参数组合键、候选数量；随后每对 (组合键, 顺位) 依次写入
    /// 下标 7/8、9/10……，与脚本中 <c>ARGV[8 + i*2]</c>、<c>ARGV[9 + i*2]</c> 的读取方式严格对应。
    /// </para>
    /// <para>
    /// 线性流程的候选数量为 0，脚本因此跳过组合相关校验，直接比对期望顺位。
    /// </para>
    /// </remarks>
    public async Task<FlowStoreResult> ReserveAsync(
        FlowReserveRequest request,
        CancellationToken cancellationToken
    )
    {
        var candidateVariantSteps = request.CandidateVariantSteps ?? [];
        var args = new RedisValue[7 + (candidateVariantSteps.Count * 2)];
        args[0] = request.FlowId;
        args[1] = request.BusinessId;
        args[2] = InvariantText(request.ExpectedIndex);
        args[3] = request.AttemptId;
        args[4] = InvariantText(request.LockTtlSeconds);
        args[5] = request.SelectorVariantKey ?? string.Empty;
        args[6] = InvariantText(candidateVariantSteps.Count);
        for (var i = 0; i < candidateVariantSteps.Count; i++)
        {
            var candidate = candidateVariantSteps[i];
            args[7 + (i * 2)] = candidate.VariantKey;
            args[8 + (i * 2)] = InvariantText(candidate.StepIndex);
        }

        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(ReserveScript, [Key(request.Ticket)], args);

        return MapResult(result);
    }

    /// <summary>
    /// 执行 <see cref="CommitScript"/>，提交当前步骤并推进顺位。
    /// </summary>
    /// <param name="request">提交请求，需处理锁凭据、当前顺位、是否最后一步、业务标识与完成态时长。</param>
    /// <param name="cancellationToken">取消标记；当前未下传到底层脚本调用，保留以符合接口契约。</param>
    /// <returns>解析后的存储结果：成功时含推进后的顺位与完成标记。</returns>
    /// <remarks>
    /// 布尔值转换为脚本约定的 <c>'1'</c>/<c>'0'</c>；业务标识为空时传空串，表示本次不绑定也不比对。
    /// </remarks>
    public async Task<FlowStoreResult> CommitAsync(
        FlowCommitRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(
            CommitScript,
            [Key(request.Ticket)],
            [
                request.AttemptId,
                InvariantText(request.ExpectedIndex),
                request.IsLastStep ? "1" : "0",
                request.BusinessIdFromResponse ?? string.Empty,
                InvariantText(request.CompletedTtlSeconds),
            ]
        );

        return MapResult(result);
    }

    /// <summary>
    /// 执行 <see cref="ReleaseScript"/>，释放处理锁并保留票据与当前顺位。
    /// </summary>
    /// <param name="request">释放请求，需票据与处理锁凭据。</param>
    /// <param name="cancellationToken">取消标记；当前未下传到底层脚本调用，保留以符合接口契约。</param>
    /// <returns>解析后的存储结果：成功时含未变化的当前顺位。</returns>
    public async Task<FlowStoreResult> ReleaseAsync(
        FlowReleaseRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(ReleaseScript, [Key(request.Ticket)], [request.AttemptId]);

        return MapResult(result);
    }

    /// <summary>
    /// 执行 <see cref="DeleteScript"/>，删除整张票据。
    /// </summary>
    /// <param name="request">删除请求，需票据与处理锁凭据。</param>
    /// <param name="cancellationToken">取消标记；当前未下传到底层脚本调用，保留以符合接口契约。</param>
    /// <returns>解析后的存储结果：成功时不带顺位语义；票据不存在时为未找到失败。</returns>
    public async Task<FlowStoreResult> DeleteAsync(
        FlowReleaseRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(DeleteScript, [Key(request.Ticket)], [request.AttemptId]);

        return MapResult(result);
    }

    /// <summary>
    /// 把 Lua 脚本的返回值数组解析为存储结果。
    /// </summary>
    /// <param name="redisResult">脚本返回的 Redis 结果，约定为数组。</param>
    /// <returns>成功时含顺位、完成标记与可选组合键；失败时含映射后的错误码。</returns>
    /// <remarks>
    /// <para>
    /// 数组布局约定：首项恒为状态码；状态码为 <c>OK</c> 时，后续项依次为
    /// （下一个顺位, 是否完成, 可选的命中组合键）。
    /// </para>
    /// <para>
    /// 由于不同脚本返回的元组长度不同（删除操作只返回三元组、线性流程不返回组合键），
    /// 这里对每一项都按长度做存在性判断，而不是假定固定长度：
    /// 顺位无法解析为整数时置为 <see langword="null" />，组合键为空串时归一为 <see langword="null" />。
    /// </para>
    /// </remarks>
    private static FlowStoreResult MapResult(RedisResult redisResult)
    {
        var values = (RedisResult[])redisResult;
        var code = (string)values[0];
        if (code == "OK")
        {
            var nextIndexText = values.Length > 1 ? (string)values[1] : string.Empty;
            var nextIndex = int.TryParse(
                nextIndexText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed
            )
                ? parsed
                : (int?)null;
            var completed = values.Length > 2 && (string)values[2] == "1";
            var variantKey = values.Length > 3 ? (string)values[3] : null;
            return FlowStoreResult.Success(nextIndex, completed, string.IsNullOrEmpty(variantKey) ? null : variantKey);
        }

        return FlowStoreResult.Failed(ToErrorCode(code));
    }

    /// <summary>
    /// 把脚本返回的状态码字符串映射为内部错误码。
    /// </summary>
    /// <param name="code">脚本返回的状态码。</param>
    /// <returns>对应的内部错误码；未识别的取值按“步骤不允许”处理。</returns>
    /// <remarks>
    /// 未知状态码一律落到拒绝侧，保证脚本演进或配置漂移时也不会误放行。
    /// </remarks>
    private static FlowErrorCode ToErrorCode(string code)
    {
        return code switch
        {
            "FLOW_NOT_FOUND" => FlowErrorCode.FlowNotFound,
            "FLOW_EXPIRED" => FlowErrorCode.FlowExpired,
            "FLOW_BUSINESS_MISMATCH" => FlowErrorCode.FlowBusinessMismatch,
            "FLOW_STEP_NOT_ALLOWED" => FlowErrorCode.FlowStepNotAllowed,
            "FLOW_IN_PROGRESS" => FlowErrorCode.FlowInProgress,
            "FLOW_COMPLETED" => FlowErrorCode.FlowCompleted,
            "FLOW_SELECTOR_INVALID" => FlowErrorCode.FlowSelectorInvalid,
            "FLOW_SELECTOR_MISMATCH" => FlowErrorCode.FlowSelectorMismatch,
            _ => FlowErrorCode.FlowStepNotAllowed,
        };
    }

    /// <summary>
    /// 以不变文化格式化整数，供脚本参数使用。
    /// </summary>
    /// <param name="value">待格式化的整数。</param>
    /// <returns>不带任何区域性符号的数字字符串。</returns>
    /// <remarks>
    /// 脚本用 <c>tonumber</c> 与字符串直接比较，若受区域设置影响出现千分位或本地化数字，
    /// 会导致 <c>nextIndex</c> 比对失败，因此必须固定文化。
    /// </remarks>
    private static string InvariantText(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 由票据值构造完整的 Redis 键。
    /// </summary>
    /// <param name="ticket">流程票据。</param>
    /// <returns>前缀与票据拼接后的键。</returns>
    private static RedisKey Key(string ticket)
    {
        return $"{KeyPrefix}{ticket}";
    }
}

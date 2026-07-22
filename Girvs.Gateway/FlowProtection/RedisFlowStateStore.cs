using Girvs.Cache.CacheImps;
using StackExchange.Redis;

namespace Girvs.Gateway.FlowProtection;

public sealed class RedisFlowStateStore(IRedisConnectionWrapper redis) : IFlowStateStore
{
    private const string KeyPrefix = "flow:ticket:";

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
            'status', 'processing',
            'attemptId', ARGV[4],
            'lockUntilUnixMs', tostring(lockUntil),
            'createdAtUnixMs', tostring(now))
        redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[5]) * 1000)
        return {'OK', ARGV[3], '0'}
        """;

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

        if redis.call('HGET', KEYS[1], 'nextIndex') ~= ARGV[3] then
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
        return {'OK', ARGV[3], '0'}
        """;

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
                request.ExpectedIndex.ToString(),
                request.AttemptId,
                request.FlowTtlSeconds.ToString(),
                request.LockTtlSeconds.ToString(),
            ]
        );

        return MapResult(result);
    }

    public async Task<FlowStoreResult> ReserveAsync(
        FlowReserveRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(
            ReserveScript,
            [Key(request.Ticket)],
            [
                request.FlowId,
                request.BusinessId,
                request.ExpectedIndex.ToString(),
                request.AttemptId,
                request.LockTtlSeconds.ToString(),
            ]
        );

        return MapResult(result);
    }

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
                request.ExpectedIndex.ToString(),
                request.IsLastStep ? "1" : "0",
                request.BusinessIdFromResponse ?? string.Empty,
                request.CompletedTtlSeconds.ToString(),
            ]
        );

        return MapResult(result);
    }

    public async Task<FlowStoreResult> ReleaseAsync(
        FlowReleaseRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(ReleaseScript, [Key(request.Ticket)], [request.AttemptId]);

        return MapResult(result);
    }

    public async Task<FlowStoreResult> DeleteAsync(
        FlowReleaseRequest request,
        CancellationToken cancellationToken
    )
    {
        var db = await redis.GetDatabaseAsync();
        var result = await db.ScriptEvaluateAsync(DeleteScript, [Key(request.Ticket)], [request.AttemptId]);

        return MapResult(result);
    }

    private static FlowStoreResult MapResult(RedisResult redisResult)
    {
        var values = (RedisResult[])redisResult;
        var code = (string)values[0];
        if (code == "OK")
        {
            var nextIndexText = values.Length > 1 ? (string)values[1] : string.Empty;
            var nextIndex = int.TryParse(nextIndexText, out var parsed) ? parsed : (int?)null;
            var completed = values.Length > 2 && (string)values[2] == "1";
            return FlowStoreResult.Success(nextIndex, completed);
        }

        return FlowStoreResult.Failed(ToErrorCode(code));
    }

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
            _ => FlowErrorCode.FlowStepNotAllowed,
        };
    }

    private static RedisKey Key(string ticket)
    {
        return $"{KeyPrefix}{ticket}";
    }
}

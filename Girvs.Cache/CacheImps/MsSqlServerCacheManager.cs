using System.Data;
using System.Data.SqlClient;
using Girvs.Cache.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.Options;

namespace Girvs.Cache.CacheImps;

/// <summary>
/// Represents a MsSql server distributed cache
/// </summary>
public partial class MsSqlServerCacheManager : DistributedCacheManager
{
    #region Fields

    protected readonly DistributedCacheConfig _distributedCacheConfig;
    private readonly SqlServerCacheOptions _sqlServerCacheOptions;

    #endregion

    #region Ctor

    public MsSqlServerCacheManager(
        AppSettings appSettings,
        IDistributedCache distributedCache,
        ICacheKeyManager cacheKeyManager,
        IConcurrentCollection<object> concurrentCollection,
        IOptions<SqlServerCacheOptions> sqlServerCacheOptions
    )
        : base(appSettings, distributedCache, cacheKeyManager, concurrentCollection)
    {
        _distributedCacheConfig = appSettings.Get<CacheConfig>().DistributedCacheConfig;
        _sqlServerCacheOptions = sqlServerCacheOptions.Value;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// 按 T-SQL 规则用方括号引用标识符，内部右括号需双写转义
    /// </summary>
    private static string QuoteIdentifier(string identifier)
    {
        return "[" + identifier.Replace("]", "]]") + "]";
    }

    /// <summary>
    /// 已按 SQL Server 规则转义的表名，避免配置值中带特殊字符时拼接出错误语句
    /// </summary>
    private string QualifiedTableName
    {
        get
        {
            var schema = string.IsNullOrWhiteSpace(_distributedCacheConfig.SchemaName)
                ? "dbo"
                : _distributedCacheConfig.SchemaName;
            var table = string.IsNullOrWhiteSpace(_distributedCacheConfig.TableName)
                ? "DistributedCache"
                : _distributedCacheConfig.TableName;

            return $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";
        }
    }

    /// <summary>
    /// Performs SQL command
    /// </summary>
    /// <param name="command">SQL command</param>
    /// <param name="parameters">Parameters for SQL command</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task PerformActionAsync(
        SqlCommand command,
        params SqlParameter[] parameters
    )
    {
        // 连接由本方法负责释放，命令由调用方通过 using 释放
        await using var conn = new SqlConnection(_sqlServerCacheOptions.ConnectionString);

        await conn.OpenAsync();
        command.Connection = conn;
        if (parameters is { Length: > 0 })
            command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters)
    {
        prefix = PrepareKeyPrefix(prefix, prefixParameters);

        using var command = new SqlCommand(
            $"DELETE FROM {QualifiedTableName} WHERE Id LIKE @Prefix + '%'"
        );

        await PerformActionAsync(
            command,
            new SqlParameter("Prefix", SqlDbType.NVarChar) { Value = prefix }
        );

        RemoveByPrefixInstanceData(prefix);
    }

    /// <summary>
    /// Clear all cache data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task ClearAsync()
    {
        using var command = new SqlCommand($"TRUNCATE TABLE {QualifiedTableName}");

        await PerformActionAsync(command);

        ClearInstanceData();
    }

    #endregion
}

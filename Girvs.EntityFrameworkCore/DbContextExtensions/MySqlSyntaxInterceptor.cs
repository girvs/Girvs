using System.Data.Common;
using System.Text.RegularExpressions;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public partial class MySqlSyntaxInterceptor : DbCommandInterceptor
{
    private void FixSql(DbCommand command)
    {
        // 逻辑：如果 SQL 包含波浪号且没加反引号，则强行修正
        if (command != null && command.CommandText.Contains("~") && !command.CommandText.Contains("`"))
        {
            // 正则匹配：找到 IX_ 开头且以 ~ 结尾的标识符，给它戴上反引号
            command.CommandText = MyRegex().Replace(command.CommandText, "`$1`");
        }
    }

    // 重写同步执行方法
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        FixSql(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    // 重写异步执行方法（Migration 自动迁移通常走这里）
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        FixSql(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    [GeneratedRegex(@"(IX_[^\s,`]+~)", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex MyRegex();
}
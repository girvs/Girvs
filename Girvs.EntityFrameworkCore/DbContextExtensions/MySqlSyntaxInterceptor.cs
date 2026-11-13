using System.Data.Common;
using System.Text.RegularExpressions;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public partial class MySqlSyntaxInterceptor : DbCommandInterceptor
{
    private void FixSql(DbCommand command)
    {
        var commandText = command.CommandText;

        // 逻辑：如果 SQL 包含波浪号且没加反引号，则强行修正。
        // 先用 char 重载做快速判断，避免每次为 Contains 产生额外查找开销。
        if (!commandText.Contains('~') || commandText.Contains('`')) return;

        // 正则匹配：找到 IX_ 开头且以 ~ 结尾的标识符，给它戴上反引号
        command.CommandText = MyRegex().Replace(commandText, "`$1`");
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

    // 标识符与波浪号均为 ASCII，使用 CultureInvariant 避免依赖当前线程区域设置。
    [GeneratedRegex(@"(IX_[^\s,`]+~)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
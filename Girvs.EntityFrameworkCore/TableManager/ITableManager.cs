using Girvs.BusinessBasis;

namespace Girvs.EntityFrameworkCore.TableManager;

public interface ITableManager : IManager
{
    Task<List<string>> GetEntityAllTableNames(DbContext dbContext, string schema, string entityTableName);
}
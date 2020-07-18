using Girvs.Domain.Models;

namespace Girvs.Domain.Managers
{
    public interface IQuery<TEntity> where TEntity : BaseEntity, new()
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int RecordCount { get; set; }
        string[] QueryFields { get; set; }
    }
}
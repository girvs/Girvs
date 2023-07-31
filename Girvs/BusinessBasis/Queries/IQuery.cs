namespace Girvs.BusinessBasis.Queries;

public interface IQuery<TEntity>  where TEntity : Entity 
{
    int PageIndex { get; set; }
    int PageSize { get; set; }
    int RecordCount { get; set; }
    string[] QueryFields { get; set; }
}
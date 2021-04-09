namespace Girvs.Domian.Queries
{
    public interface IQuery<TEntity>
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int RecordCount { get; set; }
        string[] QueryFields { get; set; }
    }
}
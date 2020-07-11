namespace Girvs.Domain.Managers
{
    public interface IQuery
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int RecordCount { get; set; }
        string[] QueryFields { get; set; }
    }
}
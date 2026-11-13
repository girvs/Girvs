namespace Girvs.BusinessBasis.Dto;

public interface IQueryDto
{
    int PageIndex { get; set; }
    int PageSize { get; set; }
    int RecordCount { get; set; }
    string[] QueryFields { get; set; }
    // 整数上取整，避免 decimal 除法与装箱。
    int PageCount => (RecordCount + PageSize - 1) / PageSize;
    string OrderBy { get; set; }
}

public abstract class QueryDtoBase<TDto> : IQueryDto where TDto : IDto, new()
{
    [Required] [Range(0, int.MaxValue)] public int PageIndex { get; set; } = 0;

    [Required] [Range(1, int.MaxValue)] public int PageSize { get; set; } = 20;
    public int RecordCount { get; set; }
    public string[] QueryFields { get; set; } = { };
    public List<TDto> Result { get; set; }

    private string _orderBy = string.Empty;

    public string OrderBy
    {
        get => _orderBy;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _orderBy = typeof(TDto).GetTypeQueryFieldByPropertyName(value);
            }
        }
    }
}
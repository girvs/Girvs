namespace Girvs.Infrastructure;

public class ExceptionResult
{
    public string Title { get; set; }
    public string Link { get; set; }
    public dynamic Errors { get; set; }
    public string TraceId { get; set; }
    public int Status { get; set; }
    public string StackTrace { get; set; }
}

namespace Girvs.Domain.Models
{
    public abstract class IncludeInitField : AggregateRoot
    {
        public bool IsInitData { get; set; }
    }
}
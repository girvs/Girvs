namespace Girvs.Domain.Models
{
    public interface IIncludeCreatorId<TUserKey> 
    {
        public TUserKey CreatorId { get; set; }
    }
}
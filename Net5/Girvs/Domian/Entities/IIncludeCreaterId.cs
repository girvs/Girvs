namespace Girvs.Domian.Entities
{
    public interface IIncludeCreatorId<TUserKey> 
    {
        public TUserKey CreatorId { get; set; }
    }
}
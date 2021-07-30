namespace Girvs.BusinessBasis.Entities
{
    public interface IIncludeCreatorId<TUserKey> 
    {
        public TUserKey CreatorId { get; set; }
    }
}
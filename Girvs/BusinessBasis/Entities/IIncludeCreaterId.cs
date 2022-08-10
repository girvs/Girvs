namespace Girvs.BusinessBasis.Entities;

public interface IIncludeCreatorId<TUserKey>
{
    TUserKey CreatorId { get; set; }
}
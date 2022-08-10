namespace Girvs.AutoMapper.Extensions;

public static class ICollectionExtensions
{
    public static T MapTo<T>(this IEnumerable list)
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper.Map<T>(list);
    }
}
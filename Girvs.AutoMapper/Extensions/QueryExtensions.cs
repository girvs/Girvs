namespace Girvs.AutoMapper.Extensions;

public static class QueryExtensions
{
    public static TDto MapToQueryDto<TDto, TEntity>(this IQuery<TEntity> query) where TEntity : Entity, new() =>
        EngineContext.Current.Resolve<IMapper>().Map<TDto>(query);
}
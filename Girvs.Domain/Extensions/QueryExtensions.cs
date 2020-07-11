using AutoMapper;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;

namespace Girvs.Domain.Extensions
{
    public static class QueryExtensions
    {
        public static TModel MapToModel<TModel>(this IQuery query)
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TModel>(query);
        }

        public static TDto MapToDto<TDto>(this IQuery query)
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TDto>(query);
        }
    }
}
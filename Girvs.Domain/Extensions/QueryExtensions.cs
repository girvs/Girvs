using AutoMapper;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;

namespace Girvs.Domain.Extensions
{
    public static class QueryExtensions
    {
        public static TDto MapToDto<TDto, TEntity>(this IQuery<TEntity> query) where TEntity : BaseEntity, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TDto>(query);
        }
    }
}
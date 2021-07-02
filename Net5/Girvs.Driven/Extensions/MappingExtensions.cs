using AutoMapper;
using Girvs.BusinessBasis.Dto;
using Girvs.BusinessBasis.Entities;
using Girvs.Driven.Commands;
using Girvs.Infrastructure;

namespace Girvs.Driven.Extensions
{
    public static class MappingExtensions
    {
        public static TEntity MapToEntity<TEntity>(this Command command) where TEntity : BaseEntity, new()
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<TEntity>(command);
            }
            return new TEntity();
        }
        
        public static TDto MapToDto<TDto>(this Command command) where TDto : IDto, new()
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<TDto>(command);
            }

            return new TDto();
        }

        public static TCommand MapToCommand<TCommand>(this IDto dto) where TCommand : Command
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TCommand>(dto);
        }
        
        public static TCommand MapToCommand<TCommand>(this BaseEntity entity) where TCommand : Command
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TCommand>(entity);
        }
    }
}
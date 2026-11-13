namespace Girvs.Driven.Extensions;

public static class MappingExtensions
{
    extension(Command command)
    {
        public TEntity MapToEntity<TEntity>() where TEntity : Entity, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<TEntity>(command);
            }
            return new TEntity();
        }

        public TDto MapToDto<TDto>() where TDto : IDto, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<TDto>(command);
            }

            return new TDto();
        }
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
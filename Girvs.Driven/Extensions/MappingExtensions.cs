namespace Girvs.Driven.Extensions;

public static class MappingExtensions
{
    public static TEntity MapToEntity<TEntity>(this Command command)
        where TEntity : Entity, new()
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper != null ? mapper.Map<TEntity>(command) : new TEntity();
    }

    public static TDto MapToDto<TDto>(this Command command)
        where TDto : IDto, new()
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper != null ? mapper.Map<TDto>(command) : new TDto();
    }

    public static TCommand MapToCommand<TCommand>(this IDto dto)
        where TCommand : Command
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper.Map<TCommand>(dto);
    }

    public static TCommand MapToCommand<TCommand>(this BaseEntity entity)
        where TCommand : Command
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper.Map<TCommand>(entity);
    }
}

namespace Girvs.AutoMapper.Extensions;

public static class BaseEntityExtensions
{
    public static TDto MapToDto<TDto>(this Entity entity) where TDto : IDto, new()
    {
        var mapper = EngineContext.Current.Resolve<IMapper>();
        return mapper.Map<TDto>(entity);
    }

    public static TEntity MergeForm<TEntity>(this Entity entity, TEntity source) where TEntity : Entity, new()
    {
        var ps = entity.GetType().GetProperties();
        foreach (var info in ps)
        {
            var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
            info.SetValue(entity, value);
        }

        return (TEntity)entity;
    }

    public static TEntity MergeForm<TEntity>(this Entity entity, TEntity source, string[] specifyPropertyNames) where TEntity : Entity, new()
    {
        var ps = entity.GetType().GetProperties().Where(x => specifyPropertyNames.Contains(x.Name));
        foreach (var info in ps)
        {
            var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
            info.SetValue(entity, value);
        }

        return (TEntity)entity;
    }
}
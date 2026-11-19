namespace Girvs.AutoMapper.Extensions;

public static class BaseEntityExtensions
{
    extension(Entity entity)
    {
        public TDto MapToDto<TDto>()
            where TDto : IDto, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TDto>(entity);
        }

        public TEntity MergeForm<TEntity>(TEntity source)
            where TEntity : Entity, new()
        {
            var ps = entity.GetType().GetProperties();
            foreach (var info in ps)
            {
                var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
                info.SetValue(entity, value);
            }

            return (TEntity)entity;
        }

        public TEntity MergeForm<TEntity>(TEntity source, string[] specifyPropertyNames)
            where TEntity : Entity, new()
        {
            var ps = entity
                .GetType()
                .GetProperties()
                .Where(x => specifyPropertyNames.Contains(x.Name));
            foreach (var info in ps)
            {
                var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
                info.SetValue(entity, value);
            }

            return (TEntity)entity;
        }
    }
}

using System;
using System.Collections.Generic;
using AutoMapper;
using Girvs.Domain.TypeFinder;

namespace Girvs.Domain.Infrastructure.Mapper
{
    public class DefaultProfile : Profile
    {
        public DefaultProfile()
        {
            CreateDefaultMapping();
        }

        private void CreateDefaultMapping()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var modelTypes = typeFinder.FindClassesOfType<IDto>(true, false);
            CreateModelMapping(modelTypes);

            modelTypes = typeFinder.FindClassesOfType<IQueryDto>(true, false);
            CreateModelMapping(modelTypes);
        }

        private void CreateModelMapping(IEnumerable<Type> types)
        {
            foreach (var modelType in types)
            {
                if (Attribute.GetCustomAttribute(modelType, typeof(AutoMapFromAttribute)) is AutoMapFromAttribute fromEntity)
                {
                    CreateMap(fromEntity.EntityType, modelType);
                }

                if (Attribute.GetCustomAttribute(modelType, typeof(AutoMapToAttribute)) is AutoMapToAttribute toEntity)
                {
                    CreateMap(modelType, toEntity.EntityType);
                }
            }
        }
    }
}
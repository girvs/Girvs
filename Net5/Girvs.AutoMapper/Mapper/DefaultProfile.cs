using System;
using System.Collections.Generic;
using AutoMapper;
using Girvs.BusinessBasis.Dto;
using Girvs.Infrastructure;
using Girvs.TypeFinder;

namespace Girvs.AutoMapper.Mapper
{
    public class DefaultProfile : Profile, IOrderedMapperProfile
    {
        public DefaultProfile()
        {
            CreateDefaultMapping();
        }

        private void CreateDefaultMapping()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var modelTypes = typeFinder.FindOfType<IDto>();
            CreateModelMapping(modelTypes);

            modelTypes = typeFinder.FindOfType<IQueryDto>();
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

        public int Order { get; } = 0;
    }
}
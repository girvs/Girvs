using System.Collections;
using AutoMapper;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;

namespace Girvs.Domain.Driven.Extensions
{
    public static class CommandExtensions
    {
        public static T MapToEntity<T>(this Command command)
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<T>(command);
            }

            return default(T);
        }
        
        
        public static T MapToEntities<T>(this IList entities) where T : IList
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<T>(entities);
            }

            return default(T);
        }
    }
}
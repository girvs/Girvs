using AutoMapper;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;

namespace Girvs.Domain.Extensions
{
    public static class MappingExtensions
    {
        public static T MapTo<T>(this Command command) where T : new()
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<T>(command);
            }

            return new T();
        }
    }
}
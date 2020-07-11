using System.Collections;
using AutoMapper;
using Girvs.Domain.Infrastructure;

namespace Girvs.Domain.Extensions
{
    public static class ListExtension
    {
        public static T MapTo<T>(this IEnumerable list)
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<T>(list);
        }
    }
}
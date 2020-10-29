using System.Collections;
using AutoMapper;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;
using Google.Protobuf;

namespace Girvs.WebFrameWork.Extensions
{
    public static class GrpcMessageExtensions
    {
        public static T MapToCommand<T>(this IMessage message) where T : Command
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<T>(message);
            }

            return null;
        }


        public static T MapToMessage<T>(this BaseEntity entity) where T : IMessage
        {
            IMapper mapper = EngineContext.Current.Resolve<IMapper>();
            if (mapper != null)
            {
                return mapper.Map<T>(entity);
            }

            return default(T);
        }

        public static T MapToMessage<T>(this IList entities) where T : IList
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
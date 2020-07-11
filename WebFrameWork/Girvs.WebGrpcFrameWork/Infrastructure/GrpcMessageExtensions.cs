using AutoMapper;
using Girvs.Domain;
using Girvs.Domain.Extensions;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Google.Protobuf;

namespace Girvs.WebGrpcFrameWork.Infrastructure
{
    public static class GrpcMessageExtensions
    {
        public static TEntity MapToEntity<TEntity>(this IMessage message) where TEntity : BaseEntity, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TEntity>(message);
        }

        public static TEntity MapToQuery<TEntity>(this IMessage message) where TEntity : IQuery, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TEntity>(message);
        }
        
        public static string[] GetActionFields(this IMessage message)
        {
            return DtoExtensions.GetActionFields(message.GetType());
        }

        public static string[] GetActionFields<T>(this IMessage message) where T : IMessage , new()
        {
            return DtoExtensions.GetActionFields(typeof(T));
        }
        
        public static TMessage MapToMessage<TMessage>(this BaseEntity entity) where TMessage : IMessage, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TMessage>(entity);
        }
        
        public static TMessage MapToMessageQuery<TMessage>(this IQuery query) where TMessage : IMessage, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TMessage>(query);
        }
    }
}
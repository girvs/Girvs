using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using AutoMapper.Configuration.Annotations;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;

namespace Girvs.Application.Extensions
{
    public static class DtoExtensions
    {
        public static TCommand MapToCommand<TCommand>(this IDto dto) where TCommand : Command
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TCommand>(dto);
        }
        
        public static TEntity MapTo<TEntity>(this IDto dto) where TEntity : Entity, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TEntity>(dto);
        }

        public static TQuery MapTo<TQuery>(this IQueryDto queryDto)
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TQuery>(queryDto);
        }

        public static string[] GetActionFields(this IDto dto)
        {
            return GetActionFields(dto.GetType());
        }

        public static string[] GetActionFields<T>(this IQueryDto queryDto)
            where T : IDto, new()
        {
            return GetActionFields(typeof(T));
        }

        public static string[] GetActionFields(Type t)
        {
            IList<string> fields = new List<string>();
            var propertyInfos = t.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var ignore = Attribute.GetCustomAttribute(propertyInfo, typeof(IgnoreAttribute)) as IgnoreAttribute;
                if (!(ignore is null) || !CheckPropertyInfoValidity(propertyInfo)) continue;
                var sourceMember =
                    Attribute.GetCustomAttribute(propertyInfo, typeof(SourceMemberAttribute)) as
                        SourceMemberAttribute;
                fields.Add(sourceMember is null ? propertyInfo.Name : sourceMember.Name);
            }

            return fields.ToArray();
        }

        private static bool CheckPropertyInfoValidity(PropertyInfo propertyInfo)
        {
            bool result = true;
            var getMethod = propertyInfo.GetMethod;
            if (getMethod != null)
            {
                result = !getMethod.IsStatic;
            }

            var setMethod = propertyInfo.SetMethod;
            result = setMethod != null && !setMethod.IsStatic;
            return result;
        }

        public static string[] GetActionFields<T>()
        {
            return GetActionFields(typeof(T));
        }
    }
}
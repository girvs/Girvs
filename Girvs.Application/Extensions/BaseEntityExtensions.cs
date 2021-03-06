﻿using System.Linq;
using AutoMapper;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;

namespace Girvs.Application.Extensions
{
    public static class BaseEntityExtensions
    {
        public static TDto MapToDto<TDto>(this Entity entity) where TDto : IDto, new()
        {
            var mapper = EngineContext.Current.Resolve<IMapper>();
            return mapper.Map<TDto>(entity);
        }

        public static T MergeForm<T>(this Entity entity, T source) where T : Entity, new()
        {
            var ps = entity.GetType().GetProperties();
            foreach (var info in ps)
            {
                var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
                info.SetValue(entity, value);
            }

            return (T)entity;
        }

        public static T MergeForm<T>(this Entity entity, T source, string[] specifyPropertyNames) where T : Entity, new()
        {
            var ps = entity.GetType().GetProperties().Where(x => specifyPropertyNames.Contains(x.Name));
            foreach (var info in ps)
            {
                var value = source.GetType().GetProperty(info.Name)?.GetValue(source);
                info.SetValue(entity, value);
            }

            return (T)entity;
        }
    }
}
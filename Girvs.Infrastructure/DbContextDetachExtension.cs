using System;
using System.Collections.Generic;
using System.Linq;
using Girvs.Domain;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure
{
    public static class DbContextDetachAllExtension
    {
        /// <summary>
        /// 取消跟踪DbContext中所有被跟踪的实体
        /// </summary>
        public static void DetachAll(this DbContext dbContext)
        {
            //循环遍历DbContext中所有被跟踪的实体
            while (true)
            {
                //每次循环获取DbContext中一个被跟踪的实体
                var currentEntry = dbContext.ChangeTracker.Entries().FirstOrDefault();

                //currentEntry不为null，就将其State设置为EntityState.Detached，即取消跟踪该实体
                if (currentEntry != null)
                {
                    //设置实体State为EntityState.Detached，取消跟踪该实体，之后dbContext.ChangeTracker.Entries().Count()的值会减1
                    currentEntry.State = EntityState.Detached;
                }
                //currentEntry为null，表示DbContext中已经没有被跟踪的实体了，则跳出循环
                else
                {
                    break;
                }
            }
        }

        public static bool IsWasTrack<T>(this DbContext dbContext, T t) where T : BaseEntity, new()
        {
            return dbContext.ChangeTracker.Entries<T>().Any(x => x.Entity.Id == t.Id);
        }

        public static void DetachById<T>(this DbContext dbContext, params Guid[] idStrings)
            where T : BaseEntity, new()
        {
            if (!idStrings.Any()) return;

            var currentEntries = dbContext.ChangeTracker.Entries<T>().Where(x => idStrings.Contains(x.Entity.Id));
            foreach (var currentEntry in currentEntries)
            {
                currentEntry.State = EntityState.Detached;
            }
        }

        public static void Detach<T>(this DbContext dbContext) where T : BaseEntity
        {
            //循环遍历DbContext中所有被跟踪的实体
            while (true)
            {
                //每次循环获取DbContext中一个被跟踪的实体
                var currentEntry = dbContext.ChangeTracker.Entries<T>().FirstOrDefault();

                //currentEntry不为null，就将其State设置为EntityState.Detached，即取消跟踪该实体
                if (currentEntry != null)
                {
                    //设置实体State为EntityState.Detached，取消跟踪该实体，之后dbContext.ChangeTracker.Entries().Count()的值会减1
                    currentEntry.State = EntityState.Detached;
                }
                //currentEntry为null，表示DbContext中已经没有被跟踪的实体了，则跳出循环
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 取消指定更新的实体
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entity"></param>
        /// <typeparam name="T"></typeparam>
        public static void DetachSpecifyEntity<T>(this DbContext dbContext, T entity) where T : BaseEntity
        {
            //每次循环获取DbContext中一个被跟踪的实体
            var currentEntry = dbContext.ChangeTracker.Entries<T>().FirstOrDefault(x => x.Entity == entity);

            //currentEntry不为null，就将其State设置为EntityState.Detached，即取消跟踪该实体
            if (currentEntry != null)
            {
                //设置实体State为EntityState.Detached，取消跟踪该实体，之后dbContext.ChangeTracker.Entries().Count()的值会减1
                currentEntry.State = EntityState.Detached;
            }
        }

        /// <summary>
        /// 取消指定更新的实体
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <typeparam name="T"></typeparam>
        public static void DetachSpecifyEntities<T>(this DbContext dbContext, List<T> entities) where T : BaseEntity
        {
            foreach (var entity in entities)
            {
                DetachSpecifyEntity(dbContext, entity);
            }
        }
    }
}
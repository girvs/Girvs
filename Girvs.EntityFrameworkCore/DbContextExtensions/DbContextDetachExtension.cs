using System.Collections.Generic;
using System.Linq;
using Girvs.BusinessBasis.Entities;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
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

        public static bool IsWasTrack<TEntity, TKey>(this DbContext dbContext, TEntity t)
            where TEntity : BaseEntity<TKey>
        {
            return dbContext.ChangeTracker.Entries<TEntity>().Any(x => x.Entity.Id.Equals(t.Id));
        }

        public static void DetachById<TEntity, TKey>(this DbContext dbContext, params TKey[] idStrings)
            where TEntity : BaseEntity<TKey>
        {
            if (!idStrings.Any()) return;

            var currentEntries = dbContext.ChangeTracker.Entries<TEntity>().Where(x => idStrings.Contains(x.Entity.Id));
            foreach (var currentEntry in currentEntries)
            {
                currentEntry.State = EntityState.Detached;
            }
        }

        public static void Detach<TEntity, TKey>(this DbContext dbContext) where TEntity : BaseEntity<TKey>
        {
            //循环遍历DbContext中所有被跟踪的实体
            while (true)
            {
                //每次循环获取DbContext中一个被跟踪的实体
                var currentEntry = dbContext.ChangeTracker.Entries<TEntity>().FirstOrDefault();

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
        public static void DetachSpecifyEntity<TEntity, TKey>(this DbContext dbContext, TEntity entity)
            where TEntity : BaseEntity<TKey>
        {
            //每次循环获取DbContext中一个被跟踪的实体
            var currentEntry = dbContext.ChangeTracker.Entries<TEntity>().FirstOrDefault(x => x.Entity == entity);

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
        public static void DetachSpecifyEntities<TEntity, TKey>(this DbContext dbContext, List<TEntity> entities)
            where TEntity : BaseEntity<TKey>
        {
            foreach (var entity in entities)
            {
                DetachSpecifyEntity<TEntity, TKey>(dbContext, entity);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.TableRoutes;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShardingCore;
using ShardingCore.Core.VirtualRoutes.TableRoutes;
using ShardingCore.DIExtensions;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.ReadWriteConfigurations;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class DataProviderServiceExtensions
    {
        /// <summary>
        /// 注册数据库基础对象上下文
        /// </summary>
        public static void AddGirvsObjectContext(this IServiceCollection services)
        {
            var typeFinder = new WebAppTypeFinder();
            var dbContexts = typeFinder.FindOfType(typeof
                (IDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();

            if (!dbContexts.Any()) return;
            var serviceType = typeof(DataProviderServiceExtensions);
            var mi = serviceType.GetMethod(nameof(AddGirvsDbContext));
            if (mi == null) return;
            foreach (var dbContext in dbContexts)
            {
                var dmi = mi.MakeGenericMethod(dbContext);
                var config = GetDataConnectionConfig(dbContext);
                Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = (provider, builder) =>
                {
                    builder.ConfigDbContextOptionsBuilder(config);
                };
                dmi.Invoke(serviceType, new object[] {services, optionsAction});
            }
        }

        public static DataConnectionConfig GetDataConnectionConfig(Type dbContext)
        {
            var girvsDbConfigAttribute = dbContext.GetCustomAttribute<GirvsDbConfigAttribute>();
            if (girvsDbConfigAttribute == null)
            {
                throw new GirvsException($"{dbContext.Name} 未绑定数据库配置");
            }

            var dbConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>();
            return dbConfig.DataConnectionConfigs.FirstOrDefault(x => x.Name == girvsDbConfigAttribute.DbName);
        }

        /// <summary>
        /// 注册数据库连接
        /// </summary>
        /// <param name="services"></param>
        public static void AddGirvsShardingCoreContext(this IServiceCollection services)
        {
            var typeFinder = new WebAppTypeFinder();
            var dbContexts = typeFinder.FindOfType(typeof
                (IDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();


            if (!dbContexts.Any()) return;
            var serviceType = typeof(DataProviderServiceExtensions);
            var mi = serviceType.GetMethod(nameof(AddGirvsShardingConfigure));
            if (mi == null) return;
            foreach (var dmi in dbContexts.Select(dbContext => mi.MakeGenericMethod(dbContext)))
            {
                dmi.Invoke(serviceType, new object[] {services});
            }
        }


        public static void AddGirvsShardingTableRoutes(this ShardingTableOptions op)
        {
            var typeFinder = new WebAppTypeFinder();
            var tableRoutes = typeFinder.FindOfType(typeof
                (IGirvsTableRoute)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();

            if (!tableRoutes.Any()) return;
            var serviceType = typeof(DataProviderServiceExtensions);
            var mi = serviceType.GetMethod(nameof(AddGirvsShardingTableRoute));
            if (mi == null) return;
            foreach (var dmi in tableRoutes.Select(tableRoute => mi.MakeGenericMethod(tableRoute)))
            {
                dmi.Invoke(serviceType, new object[] {op});
            }
        }

        public static void AddGirvsShardingTableRoute<TRoute>(this ShardingTableOptions op)
            where TRoute : IVirtualTableRoute
        {
            op.AddShardingTableRoute<TRoute>();
        }

        public static void AddGirvsShardingConfigure<TContext>(this IServiceCollection services)
            where TContext : DbContext, IShardingDbContext
        {
            var config = GetDataConnectionConfig(typeof(TContext));
            services.AddShardingDbContext<TContext>((conn, optionsBuilder) =>
                {
                    optionsBuilder.ConfigDbContextOptionsBuilder(config, conn);
                })
                .Begin(o =>
                {
                    o.AutoTrackEntity = true;
                    o.CreateShardingTableOnStart = true;
                    o.EnsureCreatedWithOutShardingTable = true;
                })
                .AddShardingTransaction((connection, builder) =>
                {
                    switch (config.UseDataType)
                    {
                        case UseDataType.MsSql:
                            builder.UseSqlServer(connection);
                            break;

                        case UseDataType.MySql:
                            builder.UseMySql(connection, new MySqlServerVersion(config.VersionNumber));
                            break;
                    }

                    var loggerFactory = EngineContext.Current.Resolve<ILoggerFactory>();
                    builder.UseLoggerFactory(loggerFactory);
                })
                .AddDefaultDataSource(config.Name, config.MasterDataConnectionString)
                .AddShardingTableRoute(op => { op.AddGirvsShardingTableRoutes(); })
                .AddReadWriteSeparation(sp => new Dictionary<string, IEnumerable<string>>()
                    {
                        {config.Name, config.ReadDataConnectionString}
                    }, ReadStrategyEnum.Loop, defaultEnable: true, defaultPriority: 10,
                    ReadConnStringGetStrategyEnum.LatestFirstTime)
                .End();
        }

        public static IServiceCollection AddGirvsDbContext<TContext>(this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TContext : DbContext
        {
            return services.AddDbContext<TContext>(optionsAction, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }

        private static void ConfigDbContextOptionsBuilder(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr = null)
        {
            var dataConnectionConfig = config;
            connStr ??= config.MasterDataConnectionString;

            switch (dataConnectionConfig.UseDataType)
            {
                case UseDataType.MsSql:
                    optionsBuilder.UseSqlServerWithLazyLoading(dataConnectionConfig, connStr);
                    break;

                case UseDataType.MySql:
                    optionsBuilder.UseMySqlWithLazyLoading(dataConnectionConfig, connStr);
                    break;

                case UseDataType.SqlLite:
                    optionsBuilder.UseSqlLiteWithLazyLoading(dataConnectionConfig, connStr);
                    break;

                case UseDataType.Oracle:
                    optionsBuilder.UseOracleWithLazyLoading(dataConnectionConfig, connStr);
                    break;
            }

            if (dataConnectionConfig.UseLazyLoading)
            {
                optionsBuilder.UseLazyLoadingProxies();
            }

            if (!dataConnectionConfig.UseDataTracking)
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }

            if (dataConnectionConfig.EnableSensitiveDataLogging)
            {
                var loggerFactory = EngineContext.Current.Resolve<ILoggerFactory>();
                optionsBuilder.UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging();
            }

            // optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
        }
    }
}
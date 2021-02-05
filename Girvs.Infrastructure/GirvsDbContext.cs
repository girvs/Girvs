using System;
using System.Linq;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Enumerations;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;
using Girvs.Infrastructure.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace Girvs.Infrastructure
{
    public abstract class GirvsDbContext : DbContext, IDbContext
    {
        private readonly GirvsConfig _girvsConfig;
        private readonly ILogger<DbContext> _logger;

        protected GirvsDbContext(DbContextOptions options)
            : base(options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _girvsConfig = EngineContext.Current.Resolve<GirvsConfig>() ??
                           throw new ArgumentNullException(nameof(GirvsConfig));
            _logger = EngineContext.Current.Resolve<ILogger<DbContext>>() ??
                      throw new ArgumentNullException("ILogger");
        }

        public abstract string DbConfigName { get; }

        public DataConnectionConfig GetDataConnectionConfig()
        {
            if (string.IsNullOrEmpty(DbConfigName))
            {
                throw new GirvsException("DbContext未绑定指定的数据库名称", 568);
            }

            return _girvsConfig.DataConnectionConfigs.FirstOrDefault(x => x.Name == DbConfigName)
                   ?? throw new GirvsException("DbContext未绑定指定的数据库名称不正确", 568);
        }

        /// <summary>
        /// 数据库的读写操作，默认为读操作，只有在注入IUnitOfWork时，进行重写字符串时才会切换至写数据库
        /// </summary>
        public DataBaseWriteAndRead ReadAndWriteMode { get; set; } = DataBaseWriteAndRead.Read;

        public void SwitchMasterDataBase()
        {
            ReadAndWriteMode = DataBaseWriteAndRead.Write;
            Database.GetDbConnection().ConnectionString = GetDbConnectionString();
            _logger.LogInformation($"切换数据库模式为：{ReadAndWriteMode}，数据库字符串为：{GetDbConnectionString()}");
        }

        public string GetDbConnectionString()
        {
            var dataConnectionConfig = GetDataConnectionConfig();

            if (ReadAndWriteMode == DataBaseWriteAndRead.Write)
                return dataConnectionConfig.MasterDataConnectionString;

            if (!dataConnectionConfig.ReadDataConnectionString.Any())
                return dataConnectionConfig.MasterDataConnectionString;

            if (dataConnectionConfig.ReadDataConnectionString.Count == 1)
            {
                return dataConnectionConfig.ReadDataConnectionString[0];
            }

            var r = new Random();
            var index = r.Next(0, dataConnectionConfig.ReadDataConnectionString.Count);
            return dataConnectionConfig.ReadDataConnectionString[index];
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connStr = GetDbConnectionString();
            _logger.LogInformation(
                $"当前操作数据库模式为：{ReadAndWriteMode}，数据库字符串为：{connStr}");
            var dataConnectionConfig = GetDataConnectionConfig();

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

            optionsBuilder.EnableSensitiveDataLogging();
        }

        public static void OnModelCreatingBaseEntityAndTableKey<TEntity, TKey>(EntityTypeBuilder<TEntity> entity)
            where TEntity : BaseEntity<TKey>, new()
        {
            string tableName = typeof(TEntity).Name.Replace("Entity", "").Replace("Model", "");
            entity.ToTable(tableName).HasKey(x => x.Id);


            foreach (var propertyInfo in typeof(TEntity).GetProperties())
            {
                if (propertyInfo.Name == nameof(IIncludeCreateTime.CreateTime))
                {
                    entity.Property("CreateTime").HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeUpdateTime.UpdateTime))
                {
                    entity.Property("UpdateTime").HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeInitField.IsInitData))
                {
                    entity.Property("IsInitData").HasColumnType("bit");
                }

                if (propertyInfo.Name == "TenantId")
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        entity.Property("TenantId").HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeDeleteField.IsDelete))
                {
                    entity.Property("IsDelete").HasColumnType("bit");
                }

                if (propertyInfo.Name == "CreatorId")
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        entity.Property("TenantId").HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeCreatorName.CreatorName))
                {
                    entity.Property("CreatorName").HasColumnType("nvarchar(40)");
                }
            }
        }
    }
}
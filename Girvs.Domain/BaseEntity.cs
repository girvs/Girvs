using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Girvs.Domain
{
    /// <summary>
    /// 所有实体基类
    /// </summary>
    public abstract class BaseEntity
    {
        public BaseEntity()
        {
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;

            IWebHostEnvironment evn = EngineContext.Current.Resolve<IWebHostEnvironment>();
            if (evn.IsDevelopment())
            {
                try
                {
                    Creator = EngineContext.Current.CurrentClaimSid;
                    TenantId = EngineContext.Current.CurrentClaimTenantId;
                }
                catch
                {
                    Creator = Guid.Empty;
                    TenantId = Guid.Empty;
                }
            }
            else
            {
                Creator = EngineContext.Current.CurrentClaimSid;
                TenantId = EngineContext.Current.CurrentClaimTenantId;
            }
        }

        /// <summary>
        /// 主键值
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MaxLength(36)]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid TenantId { get; set; }

        ///<summary>添加时间</summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public System.DateTime CreateTime { get; set; }

        /// <summary>
        /// 操作人员,记录后台录入人员
        /// </summary>
        [MaxLength(36)]
        public Guid Creator { get; set; }

        ///<summary>更新时间</summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public System.DateTime UpdateTime { get; set; }
    }
}
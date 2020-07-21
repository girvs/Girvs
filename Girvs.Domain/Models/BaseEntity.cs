using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Girvs.Domain.Infrastructure;

namespace Girvs.Domain.Models
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

            var httpContext = EngineContext.Current.HttpContext;
            if (httpContext != null && httpContext.User.Identity.IsAuthenticated)
            {
                Creator = EngineContext.Current.CurrentClaimSid;
                TenantId = EngineContext.Current.CurrentClaimTenantId;
            }
            else
            {
                Creator = Guid.Empty;
                TenantId = Guid.Empty;
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


        /// <summary>
        /// 重写方法 相等运算
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var compareTo = obj as BaseEntity;

            if (ReferenceEquals(this, compareTo)) return true;
            if (ReferenceEquals(null, compareTo)) return false;

            return Id.Equals(compareTo.Id);
        }

        /// <summary>
        /// 重写方法 实体比较 ==
        /// </summary>
        /// <param name="a">领域实体a</param>
        /// <param name="b">领域实体b</param>
        /// <returns></returns>
        public static bool operator ==(BaseEntity a, BaseEntity b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;

            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        /// <summary>
        /// 重写方法 实体比较 !=
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(BaseEntity a, BaseEntity b)
        {
            return !(a == b);
        }

        /// <summary>
        /// 获取哈希
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (GetType().GetHashCode() * 907) + Id.GetHashCode();
        }

        /// <summary>
        /// 输出领域对象的状态
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetType().Name + " [Id=" + Id + "]";
        }
    }
}
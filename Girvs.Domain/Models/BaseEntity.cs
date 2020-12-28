using System;
using Girvs.Domain.Infrastructure;

namespace Girvs.Domain.Models
{
    public abstract class BaseEntity : BaseEntity<Guid>
    {
    }

    /// <summary>
    /// 所有实体基类
    /// </summary>
    public abstract class BaseEntity<TKey> : Entity
    {
        public BaseEntity()
        {
            FormatPrpertyValue();
        }

        private void FormatPrpertyValue()
        {
            if (this is IIncludeCreatorName creatorNameObj)
            {
                creatorNameObj.CreatorName = EngineContext.Current.UserName;
            }

            if (this is IIncludeCreateTime createTimeObj)
            {
                createTimeObj.CreateTime = DateTime.Now;
            }

            if (this is IIncludeUpdateTime updateTimeObj)
            {
                updateTimeObj.UpdateTime = DateTime.Now;
            }

            if (this is IIncludeDeleteField deleteFieldObj)
            {
                deleteFieldObj.IsDelete = false;
            }

            if (this is IIncludeInitField initFieldObj)
            {
                initFieldObj.IsInitData = false;
            }

            var multiTenantPrperty = GetType().GetProperty("IIncludeMultiTenant");
            if (multiTenantPrperty != null)
            {
                multiTenantPrperty.SetValue(this, EngineContext.Current.CurrentClaimTenantId);
            }
            
            var creatorPrperty = GetType().GetProperty("CreatorId");
            if (creatorPrperty != null)
            {
                creatorPrperty.SetValue(this, EngineContext.Current.CurrentClaimSid);
            }
        }
        
        /// <summary>
        /// 主键值
        /// </summary>
        public TKey Id { get; set; }

        /// <summary>
        /// 重写方法 相等运算
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var compareTo = obj as BaseEntity<TKey>;

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
        public static bool operator ==(BaseEntity<TKey> a, BaseEntity<TKey> b)
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
        public static bool operator !=(BaseEntity<TKey> a, BaseEntity<TKey> b)
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
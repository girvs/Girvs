using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;

namespace Girvs.Domain.IRepositories
{
    /// <summary>
    /// 数据库操作基本辅助类
    /// </summary>
    /// <typeparam name="T">实体类型，实体必须继承BaseEntity</typeparam>
    public interface IBaseActionRepository<T> : IRepository<T> where T : BaseEntity
    {
        
    }
}
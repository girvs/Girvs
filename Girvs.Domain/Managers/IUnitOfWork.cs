﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Models;

namespace Girvs.Domain.Managers
{
    /// <summary>
    /// 工作单元，方便多操作事务至业务层
    /// </summary>
    public interface IUnitOfWork :  IDisposable
    {
        //是否提交成功
        Task<bool> Commit(CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// 工作单元，方便多操作事务至业务层
    /// </summary>
    public interface IUnitOfWork<TEntity> : IUnitOfWork, IManager where TEntity : Entity
    {
    }
}
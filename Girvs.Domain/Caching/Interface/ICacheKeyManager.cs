using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using System;

namespace Girvs.Domain.Caching.Interface
{
    public interface ICacheKeyManager<TEntity> : ICacheKey where TEntity : BaseEntity
    {

    }
}
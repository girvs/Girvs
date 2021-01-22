using System;
using System.Threading.Tasks;
using Girvs.Domain.Enumerations;

namespace Girvs.Domain.Managers
{
    public interface IServiceMethodPermissionCompare : IManager
    {
        Task<bool> PermissionCompare(Guid functionId, Permission permission);
    }
}
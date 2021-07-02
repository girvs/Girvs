using System.Collections.Generic;
using System.Threading.Tasks;
using BasicManagement.Domain.Models;
using Girvs.BusinessBasis.Repositories;

namespace BasicManagement.Domain.Repositories
{
    /// <summary>
    /// 系统字典仓储
    /// </summary>
    public interface ISysDictRepository : IRepository<SysDict, int>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeType"></param>
        /// <returns></returns>
        Task<List<SysDict>> GetSysDictsByCodeType(string codeType);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeType"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<SysDict> GetSysDictByCode(string codeType, string code);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<SysDict> GetSysDictByName(string codeType, string name);
    }
}

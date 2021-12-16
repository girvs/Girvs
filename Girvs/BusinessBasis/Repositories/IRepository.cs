using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Queries;

namespace Girvs.BusinessBasis.Repositories
{
    public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : BaseEntity<Guid>
    {
    }

    public interface IRepository<TEntity, TPrimaryKey> where TEntity : BaseEntity<TPrimaryKey>
    {
        Expression<Func<TEntity, bool>> OtherQueryCondition { get; }

        bool CompareTenantId(TEntity entity);

        ///// <summary>
        ///// 设置数据库的读写模式
        ///// </summary>
        ///// <param name="writeAndRead"></param>
        //void SetDataBaseWriteAndRead(DataBaseWriteAndRead writeAndRead);
        /// <summary>
        /// 新增或更新实体，当Id为空时则为新增，不为空代表更新
        /// </summary>
        /// <param name="t">实体</param>
        /// <returns>是否成功</returns>
        Task<TEntity> AddAsync(TEntity t);

        /// <summary>
        /// 添加实体集合
        /// </summary>
        /// <param name="ts">实体集合</param>
        /// <returns>影响的行数</returns>
        Task<List<TEntity>> AddRangeAsync(List<TEntity> ts);

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="t">实体</param>
        /// <param name="fields">需要更新的字段列表</param>
        /// <returns>是否成功</returns>
        Task UpdateAsync(TEntity t, params string[] fields);

        /// <summary>
        /// 更新实体集合
        /// </summary>
        /// <param name="ts">实体集合</param>
        /// <param name="fields">需要更新的字段列表</param>
        /// <returns>多少条记录被响应</returns>
        Task UpdateRangeAsync(List<TEntity> ts, params string[] fields);

        /// <summary>
        /// 根据条件批量更新
        /// </summary>
        /// <param name="fieldValue">更新的字段名和值</param>
        /// <param name="predicate">条件</param>
        /// <typeparam name="TP">字段类型</typeparam>
        /// <returns></returns>
        Task UpdateRangeAsync(
            Expression<Func<TEntity, bool>> predicate,
            params KeyValuePair<string, object>[] fieldValue
        );

        /// <summary>
        /// 删除指定的主键值的实体
        /// </summary>
        /// <param name="t">实体</param>
        /// <returns>是否成功</returns>
        Task DeleteAsync(TEntity t);

        /// <summary>
        /// 根据主值集合删除对实体集
        /// </summary>
        /// <param name="ts">集合</param>
        /// <returns>主键集合</returns>
        Task DeleteRangeAsync(List<TEntity> ts);

        /// <summary>
        /// 根据条件进行批量删除
        /// </summary>
        /// <param name="predicate">条件表达式</param>
        /// <returns></returns>
        Task DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 根据主键值获取相关的实体
        /// </summary>
        /// <param name="id">主健值</param>
        /// <returns>对应的实体</returns>
        Task<TEntity> GetByIdAsync(TPrimaryKey id);

        /// <summary>
        /// 获取所有实体列表集合
        /// </summary>
        /// <param name="fields">需要查询的字段列表</param>
        /// <returns>实体列表集合</returns>
        Task<List<TEntity>> GetAllAsync(params string[] fields);

        /// <summary>
        /// 根据条件所有实体列表集合
        /// </summary>
        /// <param name="fields">需要查询的字段列表</param>
        /// <returns>实体列表集合</returns>
        Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, params string[] fields);

        /// <summary>
        /// 根据查询条件获取集合
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>实体集合</returns>
        Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query);

        /// <summary>
        /// 是否存在指定键的实体
        /// </summary>
        /// <param name="id">主键值</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistEntityAsync(TPrimaryKey id);

        /// <summary>
        /// 根据字段条件判断是否存在实体
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<bool> ExistEntityAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 根据条件获取单条记录集
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 判断当前实体是否被追踪
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> IsWasTrack(TEntity entity);

        /// <summary>
        /// 取消数据追踪
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> DetachById(TPrimaryKey key);

        /// <summary>
        /// 追踪数据
        /// </summary>
        /// <param name="entity"></param>
        object Entry(TEntity entity);
    }
}
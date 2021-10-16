﻿using System;
using System.Linq.Expressions;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Entities;

namespace ZhuoFan.Wb.BasicService.Domain.Models
{
    public class ServiceDataRule : AggregateRoot<Guid>
    {
        /// <summary>
        /// 用户类型（为扩展而用）
        /// </summary>
        public UserType UserType { get; set; }
        
        /// <summary>
        /// 实体说明
        /// </summary>
        public string EntityDesc { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string EntityTypeName { get; set; }
        
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; set; }

        /// <summary>
        /// 字段类型（预留）
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 字段赋值
        /// </summary>
        public string FieldValue { get; set; }

        /// <summary>
        /// 表达式运算符
        /// </summary>
        public ExpressionType ExpressionType { get; set; }
       
    }
}
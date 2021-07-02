using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Girvs.BusinessBasis.Dto
{
    public interface IQueryDto
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        int RecordCount { get; set; }
        string[] QueryFields { get; set; }
        int PageCount => (int)Math.Ceiling(RecordCount / (decimal)PageSize);
    }

    public abstract class QueryDtoBase<T> : IQueryDto where T : IDto, new()
    {
        // protected QueryDtoBase()
        // {
        //     QueryFields = this.GetActionFields<T>();
        // }

        [Required]
        [Range(0,int.MaxValue)]
        public int PageIndex { get; set; } = 0;
        
        [Required]
        [Range(1,int.MaxValue)]
        public int PageSize { get; set; } = 20;
        public int RecordCount { get; set; }
        public string[] QueryFields { get; set; } = { };
        public List<T> Result { get; set; }
    }
}
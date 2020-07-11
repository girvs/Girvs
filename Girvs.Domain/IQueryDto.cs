using System;
using System.Collections.Generic;
using Girvs.Domain.Extensions;

namespace Girvs.Domain
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
        protected QueryDtoBase()
        {
            QueryFields = this.GetActionFields<T>();
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int RecordCount { get; set; }
        public string[] QueryFields { get; set; }
        public List<T> Result { get; set; }
    }
}
using System;

namespace Girvs.Domain.Models
{
    public interface IIncludeUpdateTime
    {
        public DateTime UpdateTime { get; set; }
    }
}
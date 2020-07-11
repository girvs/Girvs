using System;

namespace Girvs.Domain.Managers
{
    public interface IIncludeCreatorAndCreateTime : IIncludeCreator
    {
        public DateTime CreateTime { get; set; }
    }
}


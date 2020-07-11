using System;

namespace Girvs.Domain.Managers
{
    public interface IIncludeUpdaterAndUpdateTime : IIncludeUpdater
    {
        public DateTime UpdateTime { get; set; }
    }
}
using Microsoft.Extensions.Caching.Distributed;

namespace Girvs.Cache.Caching;

public class GirvsDistributedCacheEntryOptions : DistributedCacheEntryOptions
{
    public When When { get; set; } = When.Always;
}
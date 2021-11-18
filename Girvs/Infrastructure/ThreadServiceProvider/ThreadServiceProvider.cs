using System;
using System.Collections.Generic;
using System.Threading;

namespace Girvs.Infrastructure.ThreadServiceProvider
{
    public static class ThreadServiceProvider
    {
        private static AsyncLocal<IServiceProvider> _asyncLocal = new AsyncLocal<IServiceProvider>();

        private static IDictionary<string, IServiceProvider> _serviceProviders =
            new Dictionary<string, IServiceProvider>();



        // public IServiceProvider this[string index] {          get => _serviceProviders[index];
        //     set => _serviceProviders[index] = value; }
    }
}
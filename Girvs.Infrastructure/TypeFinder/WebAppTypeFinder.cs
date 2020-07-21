using System;
using System.Collections.Generic;
using System.Reflection;
using Girvs.Domain.FileProvider;

namespace Girvs.Infrastructure.TypeFinder
{
    /// <summary>
    ///当前正在执行WebAppDomain。仅名称匹配的程序集
    /// </summary>
    public class WebAppTypeFinder : AppDomainTypeFinder
    {
        private bool _binFolderAssembliesLoaded;

        public WebAppTypeFinder(IGirvsFileProvider fileProvider = null) : base(fileProvider)
        {
        }

        /// <summary>
        /// 确保已加载Bin文件中的程序集
        /// </summary>
        public bool EnsureBinFolderAssembliesLoaded { get; set; } = true;

        public virtual string GetBinDirectory()
        {
            return AppContext.BaseDirectory;
        }

        public override IList<Assembly> GetAssemblies()
        {
            if (!EnsureBinFolderAssembliesLoaded || _binFolderAssembliesLoaded)
                return base.GetAssemblies();

            _binFolderAssembliesLoaded = true;
            var binPath = GetBinDirectory();
            LoadMatchingAssemblies(binPath);
            return base.GetAssemblies();
        }
    }
}
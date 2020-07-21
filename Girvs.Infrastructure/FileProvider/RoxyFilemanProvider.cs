using Girvs.Domain.FileProvider;
using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Girvs.Infrastructure.FileProvider
{
    /// <summary>
    /// 代表roxyFileman images文件夹的文件提供程序
    /// </summary>
    public class RoxyFilemanProvider : IFileProvider
    {
        private readonly PhysicalFileProvider _physicalFileProvider;

        public RoxyFilemanProvider(string root)
        {
            _physicalFileProvider = new PhysicalFileProvider(root);
        }

        public RoxyFilemanProvider(string root, ExclusionFilters filters)
        {
            _physicalFileProvider = new PhysicalFileProvider(root, filters);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _physicalFileProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (_physicalFileProvider.GetFileInfo(subpath).Exists)
                return _physicalFileProvider.GetFileInfo(subpath);

            var fileProvider = EngineContext.Current.Resolve<IGirvsFileProvider>() as GirvsFileProvider;
            var roxyFilemanService = EngineContext.Current.Resolve<IRoxyFilemanService>();
            var virtualPath = fileProvider?.GetVirtualPath(fileProvider.GetDirectoryName(_physicalFileProvider.GetFileInfo(subpath).PhysicalPath));
            roxyFilemanService.FlushImagesOnDisk(virtualPath);

            return _physicalFileProvider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return _physicalFileProvider.Watch(filter);
        }
    }
}
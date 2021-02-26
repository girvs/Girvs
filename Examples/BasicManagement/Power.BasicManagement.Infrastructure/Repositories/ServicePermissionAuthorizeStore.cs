using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Domain.FileProvider;
using Girvs.Domain.GirvsAuthorizePermission;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Infrastructure.Repositories
{
    public class ServicePermissionAuthorizeStore : IServicePermissionAuthorizeStore
    {
        private const string SavePathFile = "App_Data/ServicePermissionAuthorize.json";

        private readonly IGirvsFileProvider _fileProvider;
        private static object obj = new object();

        public ServicePermissionAuthorizeStore(IGirvsFileProvider fileProvider)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public Task<List<AuthorizePermissionModel>> GetList()
        {
            var filePath = _fileProvider.MapPath(SavePathFile);
            string content = _fileProvider.ReadAllText(filePath, Encoding.Default);

            var result = JsonSerializer.Deserialize<List<AuthorizePermissionModel>>(content);
            return Task.FromResult(result);
        }

        public async Task CreateOrUpdate(List<AuthorizePermissionModel> list)
        {
            lock (obj)
            {
                var all = GetList().Result;
                foreach (var authorizeModel in list)
                {
                    var existModel = all.FirstOrDefault(x => x.ServiceId == authorizeModel.ServiceId);
                    if (existModel == null)
                    {
                        all.Add(authorizeModel);
                    }
                    else
                    {
                        existModel.ServiceName = authorizeModel.ServiceName;
                        existModel.Permissions = authorizeModel.Permissions;
                    }
                }

                SaveFileContent(all).Wait();
            }
        }

        private Task SaveFileContent(List<AuthorizePermissionModel> list)
        {
            var filePath = _fileProvider.MapPath(SavePathFile);
            string content = JsonSerializer.Serialize(list);
            _fileProvider.WriteAllText(filePath, content, Encoding.Default);
            return Task.CompletedTask;
        }
    }
}
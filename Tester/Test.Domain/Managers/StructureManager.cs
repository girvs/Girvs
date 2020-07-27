using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Domain.FileProvider;
using Test.Domain.Models;

namespace Test.Domain.Managers
{
    public class StructureManager : IStructureManager
    {
        private readonly IGirvsFileProvider _fileProvider;

        public StructureManager(IGirvsFileProvider fileProvider)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public Task<List<Structure>> GetAllListAsync()
        {
            var path = _fileProvider.MapPath("/Structure.json");
            var structureText = _fileProvider.ReadAllText(path, Encoding.UTF8);
            return Task.FromResult(JsonSerializer.Deserialize<List<Structure>>(structureText));
        }
    }
}
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Girvs.BusinessBasis.Entities;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.CodeGenerator.Services;

[DynamicWebApi]
public class CodeGeneratorManager : ICodeGeneratorManager
{
    private const string MimeType = "application/octet-stream";
    private string zipTempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zipTempDir");

    public IEnumerable<string> GetEntityTypes()
    {
        var typeFinder = new WebAppTypeFinder();
        var types = typeFinder.FindOfType<Entity>();
        return types.Select(x => x.FullName);
    }

    [HttpPost]
    public void GeneratorCode(IEnumerable<string> entityTypeNames)
    {
        var typeFinder = new WebAppTypeFinder();
        var types = typeFinder.FindOfType<Entity>();
        var entityTypes = types.Where(x => entityTypeNames.Contains(x.FullName)).FirstOrDefault();

        var fileProvider = CommonHelper.DefaultFileProvider;
        if (fileProvider.DirectoryExists(zipTempDir))
        {
            fileProvider.DeleteDirectory(zipTempDir);
        }

        fileProvider.CreateDirectory(zipTempDir);
        
        var generateTypes = typeFinder.FindOfType<IGenerator>();
        foreach (var generateType in generateTypes)
        {
            var g = Activator.CreateInstance(generateType) as IGenerator;
            var parameterManager = new ParameterManager(g, entityTypes);
            var parseContent = g.Generate(entityTypes,parameterManager.GetBuildeTemplateParameter());
            var csFilePath = fileProvider.Combine(zipTempDir, parameterManager.GetFileSavePath());
            var filePath = fileProvider.GetDirectoryName(csFilePath);
            if (!fileProvider.DirectoryExists(filePath))
            {
                fileProvider.CreateDirectory(filePath);
            }
            fileProvider.WriteAllTextAsync(csFilePath, parseContent, Encoding.UTF8);
        }
    }
}
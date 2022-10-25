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

    // [HttpGet("{entityTypeNames}")]
    // [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    // public Task<FileResult> GeneratorCode(IEnumerable<string> entityTypeNames)
    // {
    //     return Task.Run(() =>
    //     {
    //         var content = GeneratorCodeContent(entityTypeNames);
    //         var zipFileStream = new MemoryStream(content);
    //         var actionresult =
    //             new FileStreamResult(zipFileStream, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(MimeType))
    //             {
    //                 FileDownloadName = "entities.zip",
    //             };
    //         return (FileResult) actionresult;
    //     });
    // }

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


        // var generateTypes = typeFinder.FindOfType<IGenerator>();
        // using var ms = new MemoryStream();
        // using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
        //
        // foreach (var generateType in generateTypes)
        // {
        //     var g = Activator.CreateInstance(generateType) as IGenerator;
        //     var parseContent = g.Generate(entityTypes);
        //     var entry1 = zip.CreateEntry(parseContent.OutputPathFile);
        //     using var entryStream = new StreamWriter(entry1.Open());
        //     entryStream.Write(parseContent.Content);
        //     
        //     break;
        // }


    }
    
    
    
}
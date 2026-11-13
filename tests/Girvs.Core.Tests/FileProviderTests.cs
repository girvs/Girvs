using Girvs.FileProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Girvs.Core.Tests;

public class FileProviderTests : IDisposable
{
    private readonly string _contentRoot;
    private readonly GirvsFileProvider _fileProvider;

    public FileProviderTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "girvs-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_contentRoot);
        _fileProvider = new GirvsFileProvider(new FakeWebHostEnvironment
        {
            ContentRootPath = _contentRoot,
            WebRootPath = null,
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, true);
    }

    [Fact]
    public void MapPath_虚拟路径_映射到内容根目录()
    {
        var mapped = _fileProvider.MapPath("~/appsettings.json");

        Assert.Equal(Path.Combine(_contentRoot, "appsettings.json"), mapped);
    }

    [Fact]
    public void Combine_多段路径_合并为系统路径()
    {
        var combined = _fileProvider.Combine("a", "b", "c.json");

        Assert.Equal(Path.Combine("a", "b", "c.json"), combined);
    }

    [Fact]
    public void FileExists_不存在的文件_返回False()
    {
        Assert.False(_fileProvider.FileExists(Path.Combine(_contentRoot, "missing.json")));
    }

    [Fact]
    public void CreateFile_新文件_创建成功且父目录存在()
    {
        var path = Path.Combine(_contentRoot, "sub", "created.json");

        _fileProvider.CreateFile(path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void DirectoryExists_已创建目录_返回True()
    {
        Assert.True(_fileProvider.DirectoryExists(_contentRoot));
    }

    [Fact]
    public void GetFiles_目录中存在子目录_只返回文件()
    {
        var subDirectory = Path.Combine(_contentRoot, "sub");
        Directory.CreateDirectory(subDirectory);
        File.WriteAllText(Path.Combine(_contentRoot, "a.txt"), "a");
        File.WriteAllText(Path.Combine(subDirectory, "b.txt"), "b");

        var files = _fileProvider.GetFiles(_contentRoot, "*");

        Assert.Contains(files, file => file.EndsWith("a.txt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            files,
            file => string.Equals(file, subDirectory, StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void GetVirtualPath_WebRootPath为空_返回虚拟路径且不抛异常()
    {
        var virtualPath = _fileProvider.GetVirtualPath(
            Path.Combine(_contentRoot, "appsettings.json")
        );

        Assert.StartsWith("~/", virtualPath);
    }

    [Fact]
    public void GetFileInfo_通过接口调用_相对路径可解析()
    {
        File.WriteAllText(Path.Combine(_contentRoot, "file.txt"), "content");

        IFileProvider provider = _fileProvider;
        var fileInfo = provider.GetFileInfo("file.txt");

        Assert.True(fileInfo.Exists);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Girvs.Core.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

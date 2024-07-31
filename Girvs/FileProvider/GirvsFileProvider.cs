namespace Girvs.FileProvider;

public class GirvsFileProvider(IWebHostEnvironment webHostEnvironment)
    : PhysicalFileProvider(
        (
            File.Exists(webHostEnvironment.ContentRootPath)
                ? Path.GetDirectoryName(webHostEnvironment.ContentRootPath)
                : webHostEnvironment.ContentRootPath
        ) ?? string.Empty
    ),
        IGirvsFileProvider
{
    private static void DeleteDirectoryRecursive(string path)
    {
        Directory.Delete(path, true);
        const int maxIterationToWait = 10;
        var curIteration = 0;

        while (Directory.Exists(path))
        {
            curIteration += 1;
            if (curIteration > maxIterationToWait)
                return;
            Thread.Sleep(100);
        }
    }

    protected static bool IsUncPath(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsUnc;

    public virtual string Combine(params string[] paths)
    {
        var path = Path.Combine(
            paths.SelectMany(p => IsUncPath(p) ? new[] { p } : p.Split('\\', '/')).ToArray()
        );

        if (Environment.OSVersion.Platform == PlatformID.Unix && !IsUncPath(path))
            //add leading slash to correctly form path in the UNIX system
            path = "/" + path;

        return path;
    }

    public virtual void CreateDirectory(string path)
    {
        if (!DirectoryExists(path))
            Directory.CreateDirectory(path);
    }

    public virtual void CreateFile(string path)
    {
        if (FileExists(path))
            return;

        var fileInfo = new FileInfo(path);
        CreateDirectory(fileInfo.DirectoryName);

        //we use 'using' to close the file after it's created
        using (File.Create(path)) { }
    }

    public void DeleteDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(path);

        //find more info about directory deletion
        //and why we use this approach at https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true

        foreach (var directory in Directory.GetDirectories(path))
        {
            DeleteDirectory(directory);
        }

        try
        {
            DeleteDirectoryRecursive(path);
        }
        catch (IOException)
        {
            DeleteDirectoryRecursive(path);
        }
        catch (UnauthorizedAccessException)
        {
            DeleteDirectoryRecursive(path);
        }
    }

    public virtual void DeleteFile(string filePath)
    {
        if (!FileExists(filePath))
            return;

        File.Delete(filePath);
    }

    public virtual bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public virtual void DirectoryMove(string sourceDirName, string destDirName)
    {
        Directory.Move(sourceDirName, destDirName);
    }

    public virtual IEnumerable<string> EnumerateFiles(
        string directoryPath,
        string searchPattern,
        bool topDirectoryOnly = true
    )
    {
        return Directory.EnumerateFiles(
            directoryPath,
            searchPattern,
            topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories
        );
    }

    public virtual void FileCopy(string sourceFileName, string destFileName, bool overwrite = false)
    {
        File.Copy(sourceFileName, destFileName, overwrite);
    }

    public virtual bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public virtual long FileLength(string path)
    {
        if (!FileExists(path))
            return -1;

        return new FileInfo(path).Length;
    }

    public virtual void FileMove(string sourceFileName, string destFileName)
    {
        File.Move(sourceFileName, destFileName);
    }

    public virtual string GetAbsolutePath(params string[] paths)
    {
        var allPaths = new List<string>();

        if (paths.Any() && !paths[0].Contains(WebRootPath, StringComparison.InvariantCulture))
            allPaths.Add(WebRootPath);

        allPaths.AddRange(paths);

        return Combine(allPaths.ToArray());
    }

    [SupportedOSPlatform("windows")]
    public virtual DirectorySecurity GetAccessControl(string path) =>
        new DirectoryInfo(path).GetAccessControl();

    public virtual DateTime GetCreationTime(string path) => File.GetCreationTime(path);

    public virtual string[] GetDirectories(
        string path,
        string searchPattern = "",
        bool topDirectoryOnly = true
    )
    {
        if (string.IsNullOrEmpty(searchPattern))
            searchPattern = "*";

        return Directory.GetDirectories(
            path,
            searchPattern,
            topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories
        );
    }

    public virtual string GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public virtual string GetDirectoryNameOnly(string path) => new DirectoryInfo(path).Name;

    public virtual string GetFileExtension(string filePath) => Path.GetExtension(filePath);

    public virtual string GetFileName(string path) => Path.GetFileName(path);

    public virtual string GetFileNameWithoutExtension(string filePath) =>
        Path.GetFileNameWithoutExtension(filePath);

    public virtual string[] GetFiles(
        string directoryPath,
        string searchPattern = "",
        bool topDirectoryOnly = true
    )
    {
        if (string.IsNullOrEmpty(searchPattern))
            searchPattern = "*.*";

        return Directory.GetFileSystemEntries(
            directoryPath,
            searchPattern,
            new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = !topDirectoryOnly,
            }
        );
    }

    public virtual DateTime GetLastAccessTime(string path) => File.GetLastAccessTime(path);

    public virtual DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    public virtual DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public virtual string GetParentDirectory(string directoryPath) =>
        Directory.GetParent(directoryPath)?.FullName;

    public virtual string GetVirtualPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (!IsDirectory(path) && FileExists(path))
            path = new FileInfo(path).DirectoryName;

        path = path
            ?.Replace(WebRootPath, string.Empty)
            .Replace('\\', '/')
            .Trim('/')
            .TrimStart('~', '/');

        return $"~/{path ?? string.Empty}";
    }

    public virtual bool IsDirectory(string path) => DirectoryExists(path);

    public virtual string MapPath(string path)
    {
        path = path.Replace("~/", string.Empty).TrimStart('/');

        //if virtual path has slash on the end, it should be after transform the virtual path to physical path too
        var pathEnd = path.EndsWith('/') ? Path.DirectorySeparatorChar.ToString() : string.Empty;

        return Combine(Root ?? string.Empty, path) + pathEnd;
    }

    public virtual async Task<byte[]> ReadAllBytesAsync(string filePath) =>
        File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : Array.Empty<byte>();

    public virtual async Task<string> ReadAllTextAsync(string path, Encoding encoding)
    {
        await using var fileStream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        using var streamReader = new StreamReader(fileStream, encoding);

        return await streamReader.ReadToEndAsync();
    }

    public virtual string ReadAllText(string path, Encoding encoding)
    {
        using var fileStream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        using var streamReader = new StreamReader(fileStream, encoding);

        return streamReader.ReadToEnd();
    }

    public virtual async Task WriteAllBytesAsync(string filePath, byte[] bytes) =>
        await File.WriteAllBytesAsync(filePath, bytes);

    public virtual async Task WriteAllTextAsync(string path, string contents, Encoding encoding) =>
        await File.WriteAllTextAsync(path, contents, encoding);

    public virtual void WriteAllText(string path, string contents, Encoding encoding)
    {
        File.WriteAllText(path, contents, encoding);
    }

    public new IFileInfo GetFileInfo(string subpath)
    {
        subpath = subpath.Replace(Root, string.Empty);

        return base.GetFileInfo(subpath);
    }

    protected string WebRootPath { get; } =
        File.Exists(webHostEnvironment.WebRootPath)
            ? Path.GetDirectoryName(webHostEnvironment.WebRootPath)
            : webHostEnvironment.WebRootPath;
}

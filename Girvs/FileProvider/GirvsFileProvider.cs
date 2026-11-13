namespace Girvs.FileProvider;

public class GirvsFileProvider : PhysicalFileProvider, IGirvsFileProvider
{
    public GirvsFileProvider(IWebHostEnvironment webHostEnvironment)
        : base(webHostEnvironment.ContentRootPath ?? string.Empty)
    {
        var contentRoot = webHostEnvironment.ContentRootPath ?? string.Empty;
        WebRootPath = string.IsNullOrEmpty(webHostEnvironment.WebRootPath)
            ? (string.IsNullOrEmpty(contentRoot) ? string.Empty : Path.Combine(contentRoot, "wwwroot"))
            : webHostEnvironment.WebRootPath;
    }

    private static void DeleteDirectoryRecursive(string path)
    {
        const int maxAttempts = 10;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch (Exception ex) when (
                attempt < maxAttempts && (ex is IOException || ex is UnauthorizedAccessException)
            )
            {
                Thread.Sleep(100);
            }
        }
    }

    protected static bool IsUncPath(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsUnc;
    }

    public virtual string Combine(params string[] paths)
    {
        // 逐段处理，省去 SelectMany 产生的枚举器与中间数组。
        var segments = new List<string>(paths.Length);
        foreach (var path in paths)
        {
            if (IsUncPath(path) || (path.IndexOf('\\') < 0 && path.IndexOf('/') < 0))
            {
                segments.Add(path);
                continue;
            }

            segments.AddRange(path.Split('\\', '/'));
        }

        var combined = Path.Combine(segments.ToArray());

        if (Environment.OSVersion.Platform == PlatformID.Unix && !IsUncPath(combined))
            //add leading slash to correctly form path in the UNIX system
            combined = "/" + combined;

        return combined;
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
        using (File.Create(path))
        {
        }
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

    public virtual IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern,
        bool topDirectoryOnly = true)
    {
        return Directory.EnumerateFiles(directoryPath, searchPattern,
            topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
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
        var allPaths = new List<string>(paths.Length + 1);

        if (paths.Length > 0
            && !string.IsNullOrEmpty(WebRootPath)
            && !paths[0].Contains(WebRootPath, StringComparison.OrdinalIgnoreCase))
            allPaths.Add(WebRootPath);

        allPaths.AddRange(paths);

        return Combine(allPaths.ToArray());
    }

    [SupportedOSPlatform("windows")]
    public virtual DirectorySecurity GetAccessControl(string path)
    {
        return new DirectoryInfo(path).GetAccessControl();
    }

    public virtual DateTime GetCreationTime(string path)
    {
        return File.GetCreationTime(path);
    }

    public virtual string[] GetDirectories(string path, string searchPattern = "", bool topDirectoryOnly = true)
    {
        if (string.IsNullOrEmpty(searchPattern))
            searchPattern = "*";

        return Directory.GetDirectories(path, searchPattern,
            topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
    }

    public virtual string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public virtual string GetDirectoryNameOnly(string path)
    {
        return new DirectoryInfo(path).Name;
    }

    public virtual string GetFileExtension(string filePath)
    {
        return Path.GetExtension(filePath);
    }

    public virtual string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public virtual string GetFileNameWithoutExtension(string filePath)
    {
        return Path.GetFileNameWithoutExtension(filePath);
    }

    public virtual string[] GetFiles(string directoryPath, string searchPattern = "", bool topDirectoryOnly = true)
    {
        if (string.IsNullOrEmpty(searchPattern))
            searchPattern = "*.*";

        return Directory.GetFiles(directoryPath, searchPattern,
            new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = !topDirectoryOnly,
            });
    }

    public virtual DateTime GetLastAccessTime(string path)
    {
        return File.GetLastAccessTime(path);
    }

    public virtual DateTime GetLastWriteTime(string path)
    {
        return File.GetLastWriteTime(path);
    }

    public virtual DateTime GetLastWriteTimeUtc(string path)
    {
        return File.GetLastWriteTimeUtc(path);
    }

    public virtual string GetParentDirectory(string directoryPath)
    {
        return Directory.GetParent(directoryPath)?.FullName;
    }

    public virtual string GetVirtualPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (!IsDirectory(path) && FileExists(path))
            path = new FileInfo(path).DirectoryName ?? string.Empty;

        if (!string.IsNullOrEmpty(WebRootPath))
        {
            path = path.StartsWith(WebRootPath, StringComparison.OrdinalIgnoreCase)
                ? path[WebRootPath.Length..]
                : path.Replace(WebRootPath, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        path = path.Replace('\\', '/').Trim('/').TrimStart('~', '/');

        return $"~/{path}";
    }

    public virtual bool IsDirectory(string path)
    {
        return DirectoryExists(path);
    }

    public virtual string MapPath(string path)
    {
        path = path.Replace("~/", string.Empty).TrimStart('/');

        //if virtual path has slash on the end, it should be after transform the virtual path to physical path too
        var pathEnd = path.EndsWith('/') ? Path.DirectorySeparatorChar.ToString() : string.Empty;

        return Combine(Root ?? string.Empty, path) + pathEnd;
    }

    public virtual async Task<byte[]> ReadAllBytesAsync(string filePath)
    {
        return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : Array.Empty<byte>();
    }

    public virtual async Task<string> ReadAllTextAsync(string path, Encoding encoding)
    {
        await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream, encoding);

        return await streamReader.ReadToEndAsync();
    }

    public virtual string ReadAllText(string path, Encoding encoding)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream, encoding);

        return streamReader.ReadToEnd();
    }

    public virtual async Task WriteAllBytesAsync(string filePath, byte[] bytes)
    {
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public virtual async Task WriteAllTextAsync(string path, string contents, Encoding encoding)
    {
        await File.WriteAllTextAsync(path, contents, encoding);
    }

    public virtual void WriteAllText(string path, string contents, Encoding encoding)
    {
        File.WriteAllText(path, contents, encoding);
    }

    protected string WebRootPath { get; }
}
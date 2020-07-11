using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Girvs.Domain.FileProvider
{
    public interface IGirvsFileProvider : IFileProvider
    {
        /// <summary>
        /// 合并路径
        /// </summary>
        /// <param name="paths">路径数组</param>
        /// <returns>组合路径</returns>
        string Combine(params string[] paths);

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="path">要创建的目录</param>
        void CreateDirectory(string path);

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="path">要创建的文件的路径和名称</param>
        void CreateFile(string path);

        /// <summary>
        /// 深度优先递归删除，在Windows资源管理器中打开对后代目录的处理。
        /// </summary>
        /// <param name="path">Directory path</param>
        void DeleteDirectory(string path);

        /// <summary>
        /// 删除指定的文件
        /// </summary>
        /// <param name="filePath">要删除的文件名。不支持通配符</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// 判断目录是否存在
        /// </summary>
        /// <param name="path">目录路径</param>
        bool DirectoryExists(string path);

        /// <summary>
        /// 将文件或目录及其内容移动到新位置
        /// </summary>
        /// <param name="sourceDirName">要移动的文件或目录的路径</param>
        /// <param name="destDirName">sourceDirName的新位置的路径。如果sourceDirName是文件，则destDirName也必须是文件名</param>
        void DirectoryMove(string sourceDirName, string destDirName);

        /// <summary>
        /// 返回与其中的搜索模式匹配的可枚举的文件名集合 指定的路径，并有选择地搜索子目录。
        /// </summary>
        /// <param name="directoryPath">要搜索的目录的路径</param>
        /// <param name="searchPattern">与路径中文件名匹配的搜索字符串。这个参数可以包含有效文字路径和通配符（*和？）字符的组合，但不支持正则表达式。
        /// </param>
        /// <param name="topDirectoryOnly">指定是搜索当前目录，还是搜索当前目录和全部子目录 </param>
        /// <returns>列举的文件全名（包括路径）的集合由path指定并与指定搜索模式匹配的目录</returns>
        IEnumerable<string> EnumerateFiles(string directoryPath, string searchPattern, bool topDirectoryOnly = true);

        /// <summary>
        /// 将现有文件复制到新文件。允许覆盖同名文件
        /// </summary>
        /// <param name="sourceFileName">要复制的文件</param>
        /// <param name="destFileName">目标文件的名称,不能是目录</param>
        /// <param name="overwrite">如果可以覆盖目标文件，则为true；否则为false</param>
        void FileCopy(string sourceFileName, string destFileName, bool overwrite = false);

        /// <summary>
        /// 判断指定的文件是否存在
        /// </summary>
        /// <param name="filePath">路径文件名</param>
        /// <returns>如果调用者具有必需的权限并且路径包含现有文件的名称，则为true；否则为false</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// 获取文件的长度（以字节为单位），或者为目录或不存在的文件为-1
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件长度</returns>
        long FileLength(string path);

        /// <summary>
        /// 将指定的文件移动到新位置，并提供指定新文件名的选项
        /// </summary>
        /// <param name="sourceFileName">要移动的文件名。可以包含相对路径或绝对路径</param>
        /// <param name="destFileName">文件的新路径和名称</param>
        void FileMove(string sourceFileName, string destFileName);

        /// <summary>
        /// 返回目录的绝对路径
        /// </summary>
        /// <param name="paths">路径数组</param>
        /// <returns>目录的绝对路径</returns>
        string GetAbsolutePath(params string[] paths);

        /// <summary>
        ///获取一个System.Security.AccessControl.DirectorySecurity对象，该对象封装指定目录的访问控制列表（ACL）条目
        /// </summary>
        /// <param name="path">包含System.Security.AccessControl.DirectorySecurity对象的目录的路径，该对象描述文件的访问控制列表（ACL）信息</param>
        /// <returns>封装由path参数描述的文件的访问控制规则的对象</returns>
        DirectorySecurity GetAccessControl(string path);

        /// <summary>
        /// 返回指定文件或目录的创建日期和时间
        /// </summary>
        /// <param name="path">要获取其创建日期和时间信息的文件或目录</param>
        /// <returns>System.DateTime结构设置为指定文件或目录的创建日期和时间。这个值以当地时间表示</returns>
        DateTime GetCreationTime(string path);

        /// <summary>返回与目录匹配的子目录的名称（包括它们的路径）。指定目录中的指定搜索模式</summary>
        /// <param name="path">要搜索的目录的路径</param>
        /// <param name="searchPattern">匹配路径中子目录名称的搜索字符串。这个参数可以包含有效文字和通配符的组合，但不支持正则表达式。</param>
        /// <param name="topDirectoryOnly">指定是搜索当前目录，还是搜索当前目录和全部子目录</param>
        /// <returns>匹配的子目录的全名（包括路径）的数组指定的条件，如果找不到目录，则为空数组returns>
        string[] GetDirectories(string path, string searchPattern = "", bool topDirectoryOnly = true);

        /// <summary>返回指定路径字符串的目录信息</summary>
        /// <param name="path">文件或目录的路径</param>
        /// <returns>路径的目录信息；如果path表示根目录或为null，则为null。则返回System.String.Empty，如果路径不包含目录信息</returns>
        string GetDirectoryName(string path);

        /// <summary>仅返回指定路径字符串的目录名称</summary>
        /// <param name="path">目录路径</param>
        /// <returns>目录名称</returns>
        string GetDirectoryNameOnly(string path);

        /// <summary>返回指定路径字符串的扩展名</summary>
        /// <param name="filePath">从中获取扩展名的路径字符串</param>
        /// <returns>指定路径的扩展名（包括句点“.”）</returns>
        string GetFileExtension(string filePath);

        /// <summary>返回文件名和指定路径字符串的扩展名</summary>
        /// <param name="path">从中获取文件名和扩展名的路径字符串</param>
        /// <returns>路径中最后一个目录字符之后的字符</returns>
        string GetFileName(string path);

        /// <summary>
        /// 返回指定路径字符串的文件名，不带扩展名
        /// </summary>
        /// <param name="filePath">文件的路径</param>
        /// <returns>文件名，减去最后一个句点（.）及其后的所有字符</returns>
        string GetFileNameWithoutExtension(string filePath);

        /// <summary>
        /// 返回与指定搜索匹配的文件名（包括它们的路径）
        /// 指定目录中的模式，使用一个值确定是否搜索子目录。
        /// </summary>
        /// <param name="directoryPath">要搜索的目录的路径</param>
        /// <param name="searchPattern">
        /// 与路径中文件名匹配的搜索字符串。这个参数
        /// 可以包含有效文字路径和通配符（*和？）字符的组合
        /// ，但不支持正则表达式。
        /// </param>
        /// <param name="topDirectoryOnly">指定是搜索当前目录，还是搜索当前目录和全部子目录</param>
        /// <returns>指定目录中文件的全名（包括路径）的数组与指定的搜索模式匹配，如果没有找到文件，则为空数组。</returns>
        string[] GetFiles(string directoryPath, string searchPattern = "", bool topDirectoryOnly = true);

        /// <summary>
        /// 返回上次访问指定文件或目录的日期和时间
        /// </summary>
        /// <param name="path">要获取访问日期和时间信息的文件或目录</param>
        /// <returns>System.DateTime结构设置为指定文件的日期和时间</returns>
        DateTime GetLastAccessTime(string path);

        /// <summary>
        /// 返回指定文件或目录的最后写入日期和时间
        /// </summary>
        /// <param name="path">要获取其写入日期和时间信息的文件或目录</param>
        /// <returns>System.DateTime结构设置为指定文件或目录的最后写入日期和时间。此值以当地时间表示</returns>
        DateTime GetLastWriteTime(string path);

        /// <summary>
        /// 返回以协调世界时（UTC）表示的日期和时间，以指定的文件或目录为最后写入
        /// </summary>
        /// <param name="path">要获取其写入日期和时间信息的文件或目录</param>
        /// <returns>
        /// System.DateTime结构设置为指定文件或目录的最后写入日期和时间。此值以UTC时间表示
        /// </returns>
        DateTime GetLastWriteTimeUtc(string path);

        /// <summary>
        /// 检索指定路径的父目录
        /// </summary>
        /// <param name="directoryPath">检索父目录的路径</param>
        /// <returns>父目录；如果path是根目录（包括UNC服务器或共享名）的根目录，则为null</returns>
        string GetParentDirectory(string directoryPath);

        /// <summary>
        /// 从物理磁盘路径获取虚拟路径。
        /// </summary>
        /// <param name="path">物理磁盘路径</param>
        /// <returns>虚拟路径。例如。 “~/bin”</returns>
        string GetVirtualPath(string path);

        /// <summary>
        /// 检查路径是否为目录
        /// </summary>
        /// <param name="path">检查路径</param>
        /// <returns>如果路径是目录，则为true，否则为false</returns>
        bool IsDirectory(string path);

        /// <summary>
        /// 将虚拟路径映射到物理磁盘路径。
        /// </summary>
        /// <param name="path">映射路径。例如。 “~/bin”</param>
        /// <returns>物理路径。例如。 “ c：\inetpub\wwwroot\bin”</returns>
        string MapPath(string path);

        /// <summary>
        /// 将文件内容读入字节数组
        /// </summary>
        /// <param name="filePath">读取文件</param>
        /// <returns>包含文件内容的字节数组</returns>
        byte[] ReadAllBytes(string filePath);

        /// <summary>
        /// 打开文件，以指定的编码读取文件的所有行，然后关闭文件。
        /// </summary>
        /// <param name="path">打开文件以供阅读</param>
        /// <param name="encoding">应用于文件内容的编码</param>
        /// <returns>包含文件所有行的字符串</returns>
        string ReadAllText(string path, Encoding encoding);

        /// <summary>
        /// 设置最后一次写入指定文件的日期和时间（以协调世界时（UTC））。
        /// </summary>
        /// <param name="path">要为其设置日期和时间信息的文件</param>
        /// <param name="lastWriteTimeUtc">一个System.DateTime，其中包含要为路径的最后写入日期和时间设置的值。此值以UTC时间表示</param>
        void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);

        /// <summary>
        /// 将指定的字节数组写入文件
        /// </summary>
        /// <param name="filePath">要写入的文件</param>
        /// <param name="bytes">要写入文件的字节</param>
        void WriteAllBytes(string filePath, byte[] bytes);

        /// <summary>
        /// 创建一个新文件，使用指定的编码将指定的字符串写入文件，
        ///，然后关闭文件。如果目标文件已经存在，则将其覆盖。
        /// </summary>
        /// <param name="path">要写入的文件</param>
        /// <param name="contents">写入文件的字符串</param>
        /// <param name="encoding">应用于字符串的编码</param>
        void WriteAllText(string path, string contents, Encoding encoding);
    }
}
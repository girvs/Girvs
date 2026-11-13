# 03 — 文件提供程序修正

Status: completed
Blocked by: 02

## 完成内容

- 构造函数直接使用 `IWebHostEnvironment.ContentRootPath`，修正原先 `File.Exists` 误判目录的问题；`WebRootPath` 为空时回退 `ContentRootPath/wwwroot`。
- `GetFiles` 由 `Directory.GetFileSystemEntries` 改为 `Directory.GetFiles`，不再返回子目录。
- `GetVirtualPath` 增加 `WebRootPath` 空值与大小写保护，避免空引用与错误替换。
- `GetAbsolutePath` 在 `WebRootPath` 为空时不再拼入空根路径。
- 删除不可重写的 `new GetFileInfo` 隐藏方法；基类方法为显式接口实现，无法 `override`。
- `DeleteDirectoryRecursive` 改为有限次重试，保留对文件占用场景的容忍。

## 验证结果

- `FileProviderTests` 8 项通过。
- `Girvs.Core.Tests` 四目标各 51 项通过。

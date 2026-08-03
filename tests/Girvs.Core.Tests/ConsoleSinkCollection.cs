using Xunit;

namespace Girvs.Core.Tests;

/// <summary>
/// Console sink 测试通过 Console.SetOut 修改全局标准输出，
/// 禁用该 collection 内测试的并行执行以防相互污染。
/// </summary>
[CollectionDefinition("ConsoleSink", DisableParallelization = true)]
public sealed class ConsoleSinkCollection;
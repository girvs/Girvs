using System.Collections.Generic;
using Girvs.DynamicWebApi;

namespace Girvs.CodeGenerator.Services;

public interface ICodeGeneratorManager : IAppWebApiService
{
    IEnumerable<string> GetEntityTypes();
    dynamic GeneratorCode(IEnumerable<string> entityTypes);
}
using System.Collections.Generic;
using System.Linq;
using Girvs.BusinessBasis.Entities;
using Girvs.CodeGenerator.Generator.CodeGenerators.Domain.CommandHandlers;
using Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;
using Girvs.TypeFinder;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.CodeGenerator.Services;

[DynamicWebApi]
public class CodeGeneratorManager : ICodeGeneratorManager
{
    public IEnumerable<string> GetEntityTypes()
    {
        var typeFinder = new WebAppTypeFinder();
        var types = typeFinder.FindOfType<Entity>();
        return types.Select(x => x.FullName);
    }

    public dynamic GeneratorCode(IEnumerable<string> entityTypeNames)
    {
        var typeFinder = new WebAppTypeFinder();
        var types = typeFinder.FindOfType<Entity>();
        var entityTypes = types.Where(x => entityTypeNames.Contains(x.FullName)).FirstOrDefault();

        IGenerator g = new CreateCommandGenerator();
        return g.Generate(entityTypes);
    }
}
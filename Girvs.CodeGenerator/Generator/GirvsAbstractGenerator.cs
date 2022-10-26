namespace Girvs.CodeGenerator.Generator;

public abstract class GirvsAbstractGenerator : IGenerator
{
    public abstract string OutputFileName { get; }
    public abstract string GeneratorName { get; }
    public abstract string TemplateResourceName { get; }
    


    protected virtual string GetResourceContent()
    {
        var assembly = typeof(GirvsAbstractGenerator).GetTypeInfo().Assembly;
        var stream = assembly.GetManifestResourceStream(TemplateResourceName);

        if (stream == null) throw new GirvsException("获取模板内容出错");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public string Generate(Type entityType, TemplateParameter templateParameter)
    {
        var templateContent = GetResourceContent();
        var templateEngine = Template.Parse(templateContent);
        var parseContent = templateEngine.Render(Hash.FromAnonymousObject(templateParameter));
        return parseContent;
    }
}
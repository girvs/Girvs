namespace Girvs.CodeGenerator.Generator;

public class TemplateFieldParameter: ILiquidizable
{
    public string FieldName { get; set; }
    
    public string FieldNameTitleCaseName
    {
        get
        {
            if (FieldName.Length > 0)
            {
                return FieldName[..1].ToLower() + FieldName[1..];
            }

            return FieldName;
        }
    }

    public string FieldTypeName { get; set; }

    public bool IsPrimarykey { get; set; }

    public string DbType { get; set; }

    public int MaxLength { get; set; }

    public string Comment { get; set; }

    public bool IsGenericType { get; set; }
    
    public object ToLiquid()
    {
        return new
        {
            FieldName = this.FieldName,
            FieldTypeName = this.FieldTypeName,
            IsPrimarykey = this.IsPrimarykey,
            DbType = this.DbType,
            MaxLength = this.MaxLength,
            Comment = this.Comment
        };
    }
}
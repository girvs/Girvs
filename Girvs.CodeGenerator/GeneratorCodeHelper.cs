// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.IO.Compression;
// using System.Linq;
// using System.Reflection;
// using System.Text;
// using System.Text.RegularExpressions;
// using CADPLIS.Domain.CodeGenerator;
// using DotLiquid;
//
// namespace CADPLIS.CodeGenerator
// {
//
//     /// <summary>
//     /// Automatic code generation
//     /// </summary>
//     public static class GeneratorCodeHelper
//     {
//         #region Db Field Mapping Model
//
//         /// <summary>
//         /// Db Field mapping model
//         /// </summary>
//         private class DbFieldMap
//         {
//             /// <summary>
//             /// Constructor
//             /// </summary>
//             /// <param name="fieldName"></param>
//             /// <param name="mappingName"></param>
//             public DbFieldMap(string fieldName, string mappingName, string description)
//             {
//                 this.FieldName = fieldName;
//                 this.MappingName = mappingName;
//                 this.Description = description;
//             }
//
//             /// <summary>
//             /// table field name
//             /// </summary>
//             public string FieldName { get; set; }
//             /// <summary>
//             /// mapping name
//             /// </summary>
//             public string MappingName { get; set; }
//             /// <summary>
//             /// Description
//             /// </summary>
//             public string Description { get; set; }
//
//         }
//
//         /// <summary>
//         /// Get Db column manual mapping name
//         /// </summary>
//         /// <returns></returns>
//         private static string GetDbColumnManualMappingName(string columnName)
//         {
//             var list = GetDbColumnMappingList();
//             var model = list.Where(m => m.FieldName == columnName).FirstOrDefault();
//             if (model != null && !string.IsNullOrEmpty(model.MappingName))
//             {
//                 return model.MappingName;
//             }
//             return "";
//         }
//
//         /// <summary>
//         /// Get Db column manual mapping description
//         /// </summary>
//         /// <returns></returns>
//         private static string GetDbColumnManualMappingDescription(string columnName)
//         {
//             var list = GetDbColumnMappingList();
//             var model = list.Where(m => m.FieldName == columnName).FirstOrDefault();
//             if (model != null && !string.IsNullOrEmpty(model.Description))
//             {
//                 return model.Description;
//             }
//             return "";
//         }
//
//         #endregion
//
//         #region Utils
//
//         ///<summary>
//         /// Remove prefix string
//         ///</summary>
//         ///<param name="val">original string</param>
//         ///<param name="str">prefix string</param>
//         ///<returns></returns>
//         public static string GetRemovePrefixString(string val, string str)
//         {
//             string strRegex = @"^(" + str + ")";
//             return Regex.Replace(val, strRegex, "");
//         }
//
//         ///<summary>
//         /// Remove suffix string
//         ///</summary>
//         ///<param name="val">original string</param>
//         ///<param name="str">suffix string</param>
//         ///<returns></returns>
//         public static string GetRemoveSuffixString(string val, string str)
//         {
//             string strRegex = @"(" + str + ")" + "$";
//             return Regex.Replace(val, strRegex, "");
//         }
//
//         /// <summary>
//         /// Convert to Pascal style - capitalize the first letter of each word
//         /// </summary>
//         /// <param name="sourceName">fieldName</param>
//         /// <param name="modelName">prefix</param>
//         /// <returns></returns>
//         public static string ConvertToPascal(string sourceName, string modelName = "", string splitChar = "")
//         {
//             if (!string.IsNullOrEmpty(modelName)) return modelName;
//
//             //Get manual mapping name
//             var manualMappingName = GetDbColumnManualMappingName(sourceName);
//             if (manualMappingName != "") return manualMappingName;
//
//             //if (prefix == null) prefix = "";
//             //prefix = prefix.ToLower();
//             var prefix = "";
//             string fieldDelimiter = "_";
//             string result = string.Empty;
//             if (sourceName.Contains(fieldDelimiter))
//             {
//                 sourceName = sourceName.ToLower();
//                 sourceName = GetRemovePrefixString(sourceName, prefix);
//
//                 //All lowercase
//                 string[] array = sourceName.ToLower().Split(fieldDelimiter.ToCharArray());
//                 foreach (var t in array)
//                 {
//                     //title case
//                     result += t.Substring(0, 1).ToUpper() + t.Substring(1) + splitChar;
//                 }
//             }
//             else if (string.IsNullOrWhiteSpace(sourceName))
//             {
//                 result = sourceName;
//             }
//             else if (sourceName.Length == 1)
//             {
//                 result = sourceName.ToUpper();
//             }
//             else
//             {
//                 result = sourceName.Substring(0, 1).ToUpper() + sourceName.Substring(1);
//             }
//             return result;
//         }
//
//         /// <summary>
//         /// Convert to camel style - the first word is lowercase, followed by the first letter of each word
//         /// </summary>
//         /// <param name="sourceName">fieldName</param>
//         /// <param name="modelName">prefix</param>
//         /// <returns></returns>
//         public static string ConvertToCamel(string sourceName, string modelName = "")
//         {
//             sourceName = sourceName.ToLower();
//
//             //Pascal first
//             string result = ConvertToPascal(sourceName, modelName);
//             //initial lowercase
//             if (result.Length == 1)
//             {
//                 result = result.ToLower();
//             }
//             else
//             {
//                 result = result.Substring(0, 1).ToLower() + result.Substring(1);
//             }
//             return result;
//         }
//
//
//         /// <summary>
//         /// Get column property name
//         /// </summary>
//         /// <param name="columnName"></param>
//         /// <param name="columnSettings"></param>
//         /// <returns></returns>
//         private static string GetColumnPropertyName(string columnName, List<DbColumnSetting> columnSettings)
//         {
//             //get property name from column settings
//             var model = columnSettings.Where(m => m.ColumnName == columnName).FirstOrDefault();
//             if (model != null) return model.PropertyName;
//
//             return ConvertToPascal(columnName);
//         }
//
//
//         /// <summary>
//         /// Get column description
//         /// </summary>
//         /// <param name="columnDescription">column description</param>
//         /// <param name="columnName">column name</param>
//         /// <returns></returns>
//         public static string GetColumnDescription(string columnDescription, string columnName, List<DbColumnSetting> columnSettings)
//         {
//             //get description from column settings
//             var model = columnSettings.Where(m => m.ColumnName == columnName).FirstOrDefault();
//             if (model != null) return model.ColumnDesc;
//
//             if (!string.IsNullOrEmpty(columnDescription)) return columnDescription;
//
//             var description = GetDbColumnManualMappingDescription(columnName);
//             if (!string.IsNullOrEmpty(description)) return description;
//
//             return ConvertToPascal(columnName, "", " ");
//         }
//
//         /// <summary>
//         /// Get table column csharp data type
//         /// </summary>
//         /// <param name="dataType"></param>
//         /// <returns></returns>
//         private static string GetColumnDataType(DbColumnInfo field)
//         {
//             var dataType = "string";
//             switch (field.DataType)
//             {
//                 case "nvarchar":
//                 case "varchar":
//                 case "ntext":
//                 case "text":
//                 case "char":
//                     dataType = "string";
//                     break;
//                 case "int":
//                     dataType = "int";
//                     if (field.IsNullable) dataType = "int?";
//                     break;
//                 case "bigint":
//                     dataType = "long";
//                     if (field.IsNullable) dataType = "long?";
//                     break;
//                 case "float":
//                     dataType = "float";
//                     if (field.IsNullable) dataType = "float?";
//                     break;
//                 case "bool":
//                     dataType = "bool";
//                     if (field.IsNullable) dataType = "bool?";
//                     break;
//                 case "bit":
//                     dataType = "bool";
//                     if (field.IsNullable) dataType = "bool?";
//                     break;
//                 case "date":
//                 case "datetime":
//                     dataType = "DateTime";
//                     if (field.IsNullable) dataType = "DateTime?";
//                     break;
//                 case "uniqueidentifier":
//                     dataType = "Guid";
//                     if (field.IsNullable) dataType = "Guid?";
//                     break;
//                 case "decimal":
//                 case "numeric":
//                     dataType = "decimal";
//                     if (field.IsNullable) dataType = "decimal?";
//                     break;
//             }
//             return dataType;
//         }
//
//         /// <summary>
//         /// Get Column value type
//         /// </summary>
//         /// <returns></returns>
//         private static string GetColumnValueType(DbColumnInfo field)
//         {
//             var dataType = "string";
//             switch (field.DataType)
//             {
//                 case "nvarchar":
//                 case "varchar":
//                 case "ntext":
//                 case "text":
//                 case "char":
//                     dataType = "string";
//                     break;
//                 case "int":
//                 case "bigint":
//                 case "float":
//                 case "decimal":
//                 case "numeric":
//                     dataType = "int";
//                     break;
//                 case "bool":
//                 case "bit":
//                     dataType = "bool";
//                     break;
//                 case "date":
//                 case "datetime":
//                     dataType = "DateTime";
//                     break;
//                 case "uniqueidentifier":
//                     dataType = "Guid";
//                     break;
//             }
//             return dataType;
//         }
//
//         /// <summary>
//         /// Is column in manual mappingl list
//         /// </summary>
//         /// <param name="columnName"></param>
//         /// <returns></returns>
//         public static bool IsColumnInManualMappingList(string columnName)
//         {
//             var list = GetDbColumnMappingList();
//             return list.Count(m => m.FieldName == columnName) > 0;
//         }
//
//         #endregion
//
//         #region Namespace path define
//
//         /// <summary>
//         /// Domain layer namespace
//         /// </summary>
//         private static readonly string DOMAIN_NAMESPACE = "CADPLIS.Domain";
//         /// <summary>
//         /// Infrastruct layer namespace
//         /// </summary>
//         private static readonly string INFRASTRUCT_NAMESPACE = "CADPLIS.EntityFrameworkCore";
//         /// <summary>
//         /// Application layer namespace
//         /// </summary>
//         private static readonly string APPLICATION_NAMESPACE = "CADPLIS.Application";
//         /// <summary>
//         /// Application contracts namespace
//         /// </summary>
//         private static readonly string APPLICATION_CONTRACTS_NAMESPACE = "CADPLIS.Application.Contracts";
//         /// <summary>
//         /// UserInterface (Server) layer namespace
//         /// </summary>
//         private static readonly string SERVER_NAMESPACE = "CADPLIS.Server";
//         /// <summary>
//         /// UserInterface (WebSite) layer namespace
//         /// </summary>
//         private static readonly string WEBSITE_NAMESPACE = "CADPLIS.WebSite";
//
//         #endregion
//
//         /// <summary>
//         /// Get table column manual setting mapping list
//         /// </summary>
//         /// <returns></returns>
//         private static List<DbFieldMap> GetDbColumnMappingList()
//         {
//             var list = new List<DbFieldMap>();
//             list.Add(new DbFieldMap("created_date", "CreatedTime", "Created date"));
//             list.Add(new DbFieldMap("creator_user_id", "CreatedBy", "Created By"));
//             list.Add(new DbFieldMap("creator_urid", "CreatedUserRoleId", "Created User Role"));
//             list.Add(new DbFieldMap("last_updated_date", "UpdatedTime", "Updated date"));
//             list.Add(new DbFieldMap("last_updated_user_id", "UpdatedBy", "Last updated By"));
//             list.Add(new DbFieldMap("last_updated_urid", "UpdatedUserRoleId", "Last updated User Role"));
//             return list;
//         }
//
//
//         /// <summary>
//         /// Generate corresponding data from a single table
//         /// </summary>
//         /// <param name="tableName">table name</param>
//         /// <param name="tableDescription">table description</param>
//         /// <param name="columns">table columns</param>
//         /// <param name="columnSettings">table column settings</param>
//         public static byte[] CodeGenerator(string tableName, string tableDescription, List<DbColumnInfo> columns, List<DbColumnSetting> columnSettings, string fileType, string modelName, string moduleName)
//         {
//             // ModelClassName
//             // ModelName
//             // ModelFields  Name Comment
//             var dt = DateTime.Now;
//             byte[] data;
//
//             var modelFields = columns.Select(r => new
//             {
//                 r.DbColumnName,
//                 CsDataType = GetColumnDataType(r),
//                 CsValueType = GetColumnValueType(r),
//                 ColumnName = GetColumnPropertyName(r.DbColumnName, columnSettings),
//                 ColumnDescription = GetColumnDescription(r.ColumnDescription, r.DbColumnName, columnSettings),
//                 r.DataType,
//                 r.DecimalDigits,
//                 r.DefaultValue,
//                 r.IsIdentity,
//                 r.IsNullable,
//                 r.IsPrimarykey,
//                 Length = (r.DataType == "uniqueidentifier") ? 36 : r.Length,
//                 r.PropertyName,
//                 r.PropertyType,
//                 r.Scale,
//                 r.TableId,
//                 r.TableName,
//                 r.Value,
//                 IsShowList = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsShowList) > 0,
//                 IsShowEdit = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsShowEdit) > 0,
//                 IsShowSearch = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsShowSearch) > 0,
//                 IsShowView = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsShowView) > 0,
//                 IsReadonly = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsReadonly) > 0,
//                 IsDropdown = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsDropdown) > 0,
//                 IsFileupload = columnSettings.Count(m => m.ColumnName == r.DbColumnName && m.IsFileupload) > 0,
//                 DropdownType = columnSettings.Where(m => m.ColumnName == r.DbColumnName).FirstOrDefault()?.DropdownType
//             }).ToArray();
//
//             var pkColumn = modelFields.Where(m => m.IsPrimarykey).FirstOrDefault();
//             var pkColumnName = "";
//             var pkDataType = "";
//             if (pkColumn != null)
//             {
//                 pkColumnName = pkColumn.ColumnName;
//                 pkDataType = pkColumn.CsDataType;
//             }
//
//             var obj = new
//             {
//                 TableName = tableName,
//                 ModuleName = moduleName,
//                 ModelName = ConvertToCamel(tableName, modelName),
//                 ModelClassName = ConvertToPascal(tableName, modelName),
//                 ModelDescription = tableDescription,
//                 ModelCreateTime = dt,
//                 PKColumnName = pkColumnName,
//                 PKDataType = pkDataType,
//                 PKIsIdentity = modelFields.Where(m => m.IsIdentity && m.IsPrimarykey).Count() > 0,
//                 RouteId = "{" + pkColumnName + ":" + pkDataType + "}",
//                 Fields = columns.Select(r => ConvertToPascal(r.DbColumnName)).ToArray(),
//                 ModelFields = modelFields,
//                 SearchFields = modelFields.Where(m => m.IsShowSearch).ToArray(),
//                 ListFields = modelFields.Where(m => m.IsShowList).ToArray(),
//                 EditFields = modelFields.Where(m => m.IsShowEdit).ToArray(),
//                 ViewFields = modelFields.Where(m => m.IsShowView).ToArray(),
//                 ReadonlyFields = modelFields.Where(m => m.IsReadonly).ToArray()
//             };
//
//             var assembly = IntrospectionExtensions.GetTypeInfo(typeof(GeneratorCodeHelper)).Assembly;
//             using (MemoryStream ms = new MemoryStream())
//             {
//                 using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create, false))
//                 {
//                     string file;
//                     string result;
//                     Template template;
//
//                     switch (fileType)
//                     {
//                         case "AllFiles":
//                             //Model
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Domain.Model.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(DOMAIN_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + ".cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //IRepository
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Domain.IRepository.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(DOMAIN_NAMESPACE + "/" + obj.ModuleName + "/I" + obj.ModelClassName + "Repository.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //EntityConfiguration
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.EntityConfiguration.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/EntityConfigurations/" + obj.ModelClassName + "EntityTypeConfiguration.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Repository
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.Repository.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Repository.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //EntityConfiguration
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.EntityConfiguration.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/EntityConfigurations/" + obj.ModelClassName + "EntityTypeConfiguration.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Repository
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.Repository.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Repository.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Contracts.Dto
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Contracts.Dto.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_CONTRACTS_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Dto.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Contracts.IAppService
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Contracts.IAppService.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_CONTRACTS_NAMESPACE + "/" + obj.ModuleName + "/I" + obj.ModelClassName + "AppService.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //AppService
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.AppService.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "AppService.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Profile
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Profile.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Profile.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Server.Controller
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.Server.Controller.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(SERVER_NAMESPACE + "/Controllers/" + obj.ModelClassName + "Controller.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.List.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.List.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "List.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.List.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.List.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "List.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.Edit.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.Edit.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "Edit.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.Edit.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.Edit.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "Edit.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.View.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.View.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "View.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.View.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.View.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "View.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             break;
//                         case "DomainFiles":
//                             //Model
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Domain.Model.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(DOMAIN_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + ".cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //IRepository
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Domain.IRepository.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(DOMAIN_NAMESPACE + "/" + obj.ModuleName + "/I" + obj.ModelClassName + "Repository.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             break;
//                         case "InfrastructFiles":
//                             //EntityConfiguration
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.EntityConfiguration.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/EntityConfigurations/" + obj.ModelClassName + "EntityTypeConfiguration.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Repository
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Infrastruct.Repository.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(INFRASTRUCT_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Repository.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             break;
//                         case "ApplicationFiles":
//                             //Contracts.Dto
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Contracts.Dto.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_CONTRACTS_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Dto.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Contracts.IAppService
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Contracts.IAppService.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_CONTRACTS_NAMESPACE + "/" + obj.ModuleName + "/I" + obj.ModelClassName + "AppService.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //AppService
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.AppService.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "AppService.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //Profile
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.Application.Profile.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(APPLICATION_NAMESPACE + "/" + obj.ModuleName + "/" + obj.ModelClassName + "Profile.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             break;
//                         case "UserInterfaceFiles":
//                             //Server.Controller
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.Server.Controller.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(SERVER_NAMESPACE + "/Controllers/" + obj.ModelClassName + "Controller.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.List.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.List.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "List.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.List.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.List.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "List.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.Edit.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.Edit.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "Edit.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.Edit.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.Edit.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "Edit.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.View.razor
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.View.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "View.razor");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             //WetSite.View.razor.cs
//                             using (var reader = new StreamReader(assembly.GetManifestResourceStream("CADPLIS.CodeGenerator.Templates.UserInterface.WebSite.View.code.tpl"), Encoding.UTF8))
//                             {
//                                 file = reader.ReadToEnd();
//                                 template = Template.Parse(file);
//                                 result = template.Render(Hash.FromAnonymousObject(obj));
//                                 ZipArchiveEntry entry1 = zip.CreateEntry(WEBSITE_NAMESPACE + "/Pages/" + obj.ModuleName + "/" + obj.ModelClassName + "View.razor.cs");
//                                 using (StreamWriter entryStream = new StreamWriter(entry1.Open()))
//                                 {
//                                     entryStream.Write(result);
//                                 }
//                             }
//                             break;
//
//                     }
//                 }
//                 data = ms.ToArray();
//             }
//             return data;
//         }
//
//     }
// }

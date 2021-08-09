using Girvs.Driven.Commands;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule
{
    public class CreateOrUpdateServiceDataRuleCommand : Command
    {
        public CreateOrUpdateServiceDataRuleCommand(string serviceName, string moduleName, UserType userType, string dataSource,
            string fieldName, string fieldDesc)
        {
            ServiceName = serviceName;
            ModuleName = moduleName;
            UserType = userType;
            DataSource = dataSource;
            FieldName = fieldName;
            FieldDesc = fieldDesc;
        }

        public override string CommandDesc { get; set; } = "添加服务规则授权";

        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// 用户类型
        /// </summary>
        public UserType UserType { get; private set; }

        /// <summary>
        /// 字段对应的数据来源（即接口名称）
        /// </summary>
        public string DataSource { get; private set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// 字段的说明
        /// </summary>
        public string FieldDesc { get; private set; }
    }
}
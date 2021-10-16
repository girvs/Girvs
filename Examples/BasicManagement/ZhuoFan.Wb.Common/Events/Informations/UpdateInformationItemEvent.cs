using System;
using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Informations
{
    public class UpdateInformationItemEvent : IntegrationEvent
    {
        public UpdateInformationItemEvent()
        {

        }
        public UpdateInformationItemEvent(Guid informationId, string code, string name, int type, int infoType, string rules, string remark)
        {
            InformationId = informationId;
            Code = code;
            Name = name;
            Type = type;
            InfoType = infoType;
            Rules = rules;
            Remark = remark;
        }
        /// <summary>
        /// 主键id
        /// </summary>
        public Guid InformationId { get; set; }

        /// <summary>
        /// 信息项代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public int Type { get; set; }


        /// <summary>
        /// 信息项类型
        /// </summary>
        public int InfoType { get; set; }

        /// <summary>
        /// 配置项
        /// </summary>
        public string Rules { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Remark { get; set; }
    }
}

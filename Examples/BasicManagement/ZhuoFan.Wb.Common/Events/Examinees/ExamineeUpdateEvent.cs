using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Examinees
{
    /// <summary>
    /// 修改报考考生事件
    /// </summary>
    public class ExamineeUpdateEvent : IntegrationEvent
    {
        public ExamineeUpdateEvent()
        {

        }
        public ExamineeUpdateEvent(string name, string gender, string identityCard)
        {
            Name = name;
            Gender = gender;
            IdentityCard = identityCard;
        }
        /// <summary>
        /// 考生姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 考生性别
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// 考生身份证
        /// </summary>
        public string IdentityCard { get; set; }
    }
}

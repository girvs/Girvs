namespace ZhuoFan.Wb.Common.Events.Organizations
{
    public record EditUnitEvent : IntegrationEvent
    {
        public EditUnitEvent()
        {

        }
        public EditUnitEvent(Guid unitId, string unitName, string unitCode)
        {
            UnitId = unitId;
            UnitName = unitName;
            UnitCode = unitCode;
        }
        /// <summary>
        /// 单位Id
        /// </summary>
        public Guid UnitId { get;  set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string UnitName { get;  set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string UnitCode { get;  set; }
    }
}

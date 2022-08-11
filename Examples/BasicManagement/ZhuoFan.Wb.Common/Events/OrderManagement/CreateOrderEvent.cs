namespace ZhuoFan.Wb.Common.Events.OrderManagement
{
    public record CreateOrderEvent : IntegrationEvent
    {
        public CreateOrderEvent()
        {

        }
        public CreateOrderEvent(Guid organizationId, Guid unitId, Guid positionId, Guid tenantId = default, string registrationNo = null, string name = null, string iDCardNo = null, string examinationUnit = null, string examinationPosition = null, decimal amountPayable = 0)
        {
            OrganizationId = organizationId;
            UnitId = unitId;
            PositionId = positionId;
            TenantId = tenantId;
            RegistrationNo = registrationNo;
            Name = name;
            IDCardNo = iDCardNo;
            ExaminationUnit = examinationUnit;
            ExaminationPosition = examinationPosition;
            AmountPayable = amountPayable;
        }
        #region 需要存入数据库的属性
        /// <summary>
        /// 机构Id
        /// </summary>
        public Guid OrganizationId { get; set; }
        /// <summary>
        /// 单位Id
        /// </summary>
        public Guid UnitId { get; set; }

        /// <summary>
        /// 职位Id
        /// </summary>
        public Guid PositionId { get; set; }

        /// <summary>
        /// 租户id
        /// </summary>
        public Guid TenantId { get; set; }


        /// <summary>
        /// 报名序号
        /// </summary>
        public string RegistrationNo { get; set; }
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 身份证号
        /// </summary>
        public string IDCardNo { get; set; }
        /// <summary>
        /// 报考单位
        /// </summary>
        public string ExaminationUnit { get; set; }
        /// <summary>
        /// 报考职位
        /// </summary>
        public string ExaminationPosition { get; set; }
        /// <summary>
        /// 应缴金额
        /// </summary>
        public decimal AmountPayable { get; set; }

        #endregion
    }
}

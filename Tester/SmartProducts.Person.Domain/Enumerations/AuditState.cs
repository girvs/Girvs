namespace SmartProducts.Person.Domain.Enumerations
{
    public enum AuditState
    {
        /// <summary>
        /// 未审核
        /// </summary>
        UnAudit,
        /// <summary>
        /// 审核中
        /// </summary>
        Auditing,
        /// <summary>
        /// 审核不通过
        /// </summary>
        AuditFailed,
        /// <summary>
        /// 审核通过
        /// </summary>
        AuditPass
    }
}

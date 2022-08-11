namespace ZhuoFan.Wb.Common.Events.ExamAreas
{
    /// <summary>
    /// 考区修改事件
    /// 暂时没用
    /// </summary>
    public record EditExamAreaEvent : IntegrationEvent
    {
        /// <summary>
        /// 考区Id
        /// </summary>
        public Guid ExamAreaId { get; set; }

        /// <summary>
        /// 考区代码
        /// </summary>
        public string ExamAreaCode { get; set; }

        /// <summary>
        /// 考区名称
        /// </summary>
        public string ExamAreaName { get; set; }
    }
}

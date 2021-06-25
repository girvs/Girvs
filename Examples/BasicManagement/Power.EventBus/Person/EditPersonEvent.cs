namespace Power.EventBus.Person
{
    public class EditPersonEvent : IntegrationEvent
    {
        public bool IsDelete { get; set; }

        public bool IsAdd { get; set; }

        public bool IsUpdate { get; set; }

        /// <summary>
        /// 员工号
        /// </summary>
        public long EmployeeNo { get; set; }

        /// <summary>
        /// 人员Id
        /// </summary>
        public string PersonId { get; set; }

        /// <summary>
        /// 部门Id
        /// </summary>
        public string DepartmentId { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 身份证
        /// </summary>
        public string IDNo { get; set; }

        /// <summary>
        /// 卡号
        /// </summary>
        public string WorkCard { get; set; }

        /// <summary>
        /// 人脸图片
        /// </summary>
        public string FaceUrl { get; set; }

        public EditPersonEvent()
        { 
        }
    }
}

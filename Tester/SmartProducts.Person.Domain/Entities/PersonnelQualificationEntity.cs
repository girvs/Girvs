using System;
using Girvs.Domain;

namespace SmartProducts.Person.Domain.Entities
{
    /// <summary>
    /// 人员资质管理实体
    /// </summary>
    public class PersonnelQualificationEntity : BaseEntity
    {
        /// <summary>
        /// 资质名称
        /// </summary>
        public string CertificateName { get; set; }
        /// <summary>
        /// 资质证书编号
        /// </summary>
        public string CertificateNO { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime CertificateDeadline { get; set; }

        /// <summary>
        /// 资质图片
        /// </summary>
        public string CertificatePic { get; set; }

        /// <summary>
        /// 所属人员ID
        /// </summary>
        public string PersonId { get; set; }

        public virtual PersonInfoEntity PersonInfo { get; set; }
    }
}

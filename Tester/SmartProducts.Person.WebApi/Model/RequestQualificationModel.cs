using System;
using Hdyt.SmartProducts.Core;
using Hdyt.SmartProducts.Core.Infrastructure.Mapper;
using SmartProducts.Person.Core.Entities;

namespace SmartProducts.Person.WebApi.Model
{
    [AutoMapFrom(typeof(PersonInfoEntity))]
    [AutoMapTo(typeof(PersonInfoEntity))]
    public class RequestAddQualificationModel : IModel
    {
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
    }

    [AutoMapFrom(typeof(PersonInfoEntity))]
    [AutoMapTo(typeof(PersonInfoEntity))]
    public class RequestUpdateQualificationModel : IModel
    {
        public Guid Id { get; set; }
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
    }
}
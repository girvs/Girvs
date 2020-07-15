using System.Collections.Generic;
using Girvs.Domain;
using Girvs.Domain.Models;
using SmartProducts.Person.Domain.Enumerations;

namespace SmartProducts.Person.Domain.Entities
{
    /// <summary>
    /// 人员实体类
    /// </summary>
    public class PersonInfoEntity : BaseEntity
    {
        public PersonInfoEntity()
        {
            PersonnelQualifications = new List<PersonnelQualificationEntity>();
        }
        ///<summary>名称</summary>
        public string Name { get; set; }

        ///<summary>性别</summary>
        public Gender Sex { get; set; }

        ///<summary>政治面貌</summary>
        public PoliticOutlook PoliticOutlook { get; set; }

        ///<summary>教育程度</summary>
        public Education Education { get; set; }

        ///<summary>手机号码</summary>
        public string Mobilephone { get; set; }

        ///<summary>身份证号码</summary>
        public string IDNo { get; set; }

        ///<summary>工作类型</summary>
        public WorkType WorkType { get; set; }

        ///<summary>是否有重大病史</summary>
        public bool MedicalHistory { get; set; }

        ///<summary>工种</summary>
        public int WorkerType { get; set; }

        ///<summary>所属实工单位ID</summary>
        public string ConstructionUnitId { get; set; }

        public string CurrentInAreaId { get; set; }

        /// <summary>
        /// 人员头像ID，对应海康设备中的图像ID
        /// </summary>
        public int HeadPortraitId { get; set; }

        public string WorkCard { get; set; }
        public string QrCodeUrl { get; set; }
        public DataState State { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string PortraitUrl { get; set; }

        /// <summary>
        /// 当前人员相关的资质,导航属性
        /// </summary>
        public virtual List<PersonnelQualificationEntity> PersonnelQualifications { get; set; }
    }
}

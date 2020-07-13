using AutoMapper.Configuration.Annotations;
using Hdyt.SmartProducts.Core;
using Hdyt.SmartProducts.Core.Infrastructure.Mapper;
using SmartProducts.Person.Core.Entities;
using SmartProducts.Person.Core.Enumerations;

namespace SmartProducts.Person.WebApi.Model
{
    [AutoMapFrom(typeof(PersonInfoEntity))]
    [AutoMapTo(typeof(PersonInfoEntity))]
    public class RequestAddPersonModel : IModel
    {
        [Ignore]//忽略，不会查询该字段
        public string Name { get; set; }

        [SourceMember(nameof(PersonInfoEntity.Sex))] //查询别名，对应实体字符串
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
    }

    [AutoMapFrom(typeof(PersonInfoEntity))]
    [AutoMapTo(typeof(PersonInfoEntity))]
    public class ResponsePersonMode : IModel
    {
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
    }
}
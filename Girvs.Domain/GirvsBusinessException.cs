using System;

namespace Girvs.Domain
{
    /// <summary>
    /// 系统业务异常抛出的信息类
    /// </summary>
    public class GirvsBusinessException : Exception
    {
        public GirvsBusinessException(string message) : base(message)
        {

        }
    }
}
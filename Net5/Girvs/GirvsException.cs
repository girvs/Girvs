using System;
using System.Runtime.Serialization;

namespace Girvs
{
    [Serializable]
    public class GirvsException : Exception
    {
        /// <summary>
        /// 初始化Exception类的新实例。
        /// </summary>
        public GirvsException()
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化Exception类的新实例。
        /// </summary>
        public GirvsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化Exception类的新实例。
        /// </summary>
        public GirvsException(string messageFormat, params object[] args)
            : base(string.Format(messageFormat, args))
        {
        }

        /// <summary>
        /// 使用序列化的数据初始化Exception类的新实例。
        /// </summary>
        protected GirvsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和对引起此异常的内部异常的引用来初始化Exception类的新实例。
        /// </summary>
        public GirvsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
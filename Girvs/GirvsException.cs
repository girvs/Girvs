namespace Girvs;

/// <summary>
/// 表示在应用程序执行期间发生的错误
/// </summary>
[Serializable]
public class GirvsException : Exception
{
    public int StatusCode { get; set; }
    public dynamic Error { get; }

    /// <summary>
    /// Initializes a new instance of the Exception class.
    /// </summary>
    public GirvsException(int statusCode = 568, dynamic error = null)
    {
        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the Exception class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="statusCode"></param>
    /// <param name="error"></param>
    public GirvsException(string message, int statusCode = 568, dynamic error = null)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the Exception class with a specified error message.
    /// </summary>
    /// <param name="messageFormat">The exception message format.</param>
    /// <param name="error"></param>
    /// <param name="args">The exception message arguments.</param>
    /// <param name="statusCode"></param>
    public GirvsException(string messageFormat, int statusCode = 568, dynamic error = null, params object[] args)
        : base(string.Format(messageFormat, args))
    {
        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>
    /// Initializes a new instance of the Exception class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    /// <param name="statusCode"></param>
    protected GirvsException(SerializationInfo info, StreamingContext context, int statusCode = 568)
        : base(info, context)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the Exception class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="statusCode"></param>
    /// <param name="error"></param>
    public GirvsException(string message, Exception innerException, int statusCode = 568, dynamic error = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Error = error;
    }
}
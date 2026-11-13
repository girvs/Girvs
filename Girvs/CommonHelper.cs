using System.Collections.Concurrent;

namespace Girvs;

/// <summary>
/// 公共辅助类
/// </summary>
public class CommonHelper
{
    // 属性查找属于静态元数据，缓存后避免每次反射线性搜索成员。
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache = new();

    //we use EmailValidator from FluentValidation. So let's keep them sync - https://github.com/JeremySkinner/FluentValidation/blob/master/src/FluentValidation/Validators/EmailValidator.cs
    private const string EmailExpression =
        @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";

    // 预编译并复用，避免每次调用重新解析表达式；超时防止病态输入拖垮线程。
    private static readonly Regex EmailRegex = new(
        EmailExpression,
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// 字符串复杂度校验（必须包含数字、小写或大写字母、特殊字符、字符数在8-30之间）
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool Complexity(string str)
    {
        // 原实现每次调用都新建 Regex，正则解析与实例化是主要开销。
        return ComplexityRegex.IsMatch(str);
    }

    private static readonly Regex ComplexityRegex = new(
        @"(?=.*[0-9])
          (?=.*[a-zA-Z])
          (?=.*[\W_])",
        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace
        | RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// 确保用户的电子邮件或抛出。
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns></returns>
    public static string EnsureSubscriberEmailOrThrow(string email)
    {
        var output = EnsureNotNull(email);
        output = output.Trim();
        output = EnsureMaximumLength(output, 255);

        if (!IsValidEmail(output))
        {
            throw new GirvsException("Email is not valid.");
        }

        return output;
    }

    /// <summary>
    /// 验证字符串是有效的电子邮件格式
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        email = email.Trim();

        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// 验证该字符串是有效的IP地址
    /// </summary>
    public static bool IsValidIpAddress(string ipAddress)
    {
        // 走 ReadOnlySpan 重载，避免不必要的字符串处理与装箱。
        return !string.IsNullOrEmpty(ipAddress) && IPAddress.TryParse(ipAddress.AsSpan(), out _);
    }

    /// <summary>
    /// 产生随机数字码
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Result string</returns>
    [Obsolete("Obsolete")]
    public static string GenerateRandomDigitCode(int length)
    {
        using var random = new SecureRandomNumberGenerator();
        // 原实现每次迭代都拼接新字符串，复杂度 O(n²)；改为一次性字段缓冲。
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = (char)('0' + random.Next(10));
        return new string(buffer);
    }

    /// <summary>
    /// 返回指定范围内的随机整数
    /// </summary>
    /// <param name="min">Minimum number</param>
    /// <param name="max">Maximum number</param>
    /// <returns>Result</returns>
    [Obsolete("Obsolete")]
    public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
    {
        using var random = new SecureRandomNumberGenerator();
        return random.Next(min, max);
    }

    /// <summary>
    /// 确保字符串不超过最大允许长度
    /// </summary>
    /// <param name="str">Input string</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="postfix">A string to add to the end if the original string was shorten</param>
    /// <returns>Input string if its length is OK; otherwise, truncated input string</returns>
    public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length <= maxLength)
            return str;

        var pLen = postfix?.Length ?? 0;

        var result = str[0..(maxLength - pLen)];
        if (!string.IsNullOrEmpty(postfix))
        {
            result += postfix;
        }

        return result;
    }

    /// <summary>
    /// 确保字符串仅包含数字值
    /// </summary>
    public static string EnsureNumericOnly(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        // 单个缓冲原地写入，省去 Where + ToArray 的两次分配。
        var buffer = new char[str.Length];
        var count = 0;
        foreach (var c in str)
        {
            if (char.IsDigit(c))
                buffer[count++] = c;
        }

        return new string(buffer, 0, count);
    }

    /// <summary>
    /// 确保字符串不为空
    /// </summary>
    public static string EnsureNotNull(string str)
    {
        return str ?? string.Empty;
    }

    /// <summary>
    /// 指示指定的字符串是空字符串还是空字符串
    /// </summary>
    public static bool AreNullOrEmpty(params string[] stringsToValidate)
    {
        return stringsToValidate.Any(string.IsNullOrEmpty);
    }

    /// <summary>
    /// 比较两个数组
    /// </summary>
    public static bool ArraysEqual<T>(T[] a1, T[] a2)
    {
        //also see Enumerable.SequenceEqual(a1, a2);
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Length != a2.Length)
            return false;

        // 直接循环比较，避免 LINQ 索引委托与枚举器分配。
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < a1.Length; i++)
        {
            if (!comparer.Equals(a1[i], a2[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 将对象的属性设置为值。
    /// </summary>
    public static void SetProperty(object instance, string propertyName, object value)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

        var instanceType = instance.GetType();
        var pi = GetCachedProperty(instanceType, propertyName);
        if (pi == null)
            throw new GirvsException("No property '{0}' found on the instance of type '{1}'.", 568, propertyName,
                instanceType);
        if (!pi.CanWrite)
            throw new GirvsException("The property '{0}' on the instance of type '{1}' does not have a setter.",
                568, propertyName, instanceType);
        if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
            value = To(value, pi.PropertyType);
        pi.SetValue(instance, value, Array.Empty<object>());
    }

    //public static Expression<Func<TEntity, bool>> BuildeExpression<TEntity>(string propertyName, object value,OperatorType operatorType)
    //{

    //}

    /// <summary>
    /// 获取对象的属性值。
    /// </summary>
    [CanBeNull]
    public static object GetProperty(object instance, string propertyName)
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

        var instanceType = instance.GetType();
        var pi = GetCachedProperty(instanceType, propertyName);
        if (pi == null)
            throw new GirvsException("No property '{0}' found on the instance of type '{1}'.", 568, propertyName,
                instanceType);
        return pi.GetValue(instance);
    }

    private static PropertyInfo GetCachedProperty(Type type, string propertyName)
    {
        if (PropertyCache.TryGetValue((type, propertyName), out var cached))
            return cached;

        var property = type.GetProperty(propertyName);
        if (property != null)
            PropertyCache[(type, propertyName)] = property;

        return property;
    }

    /// <summary>
    /// 一个值到目的地类型转换。
    /// </summary>
    public static object To(object value, Type destinationType)
    {
        return To(value, destinationType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 一个值到目的地类型转换。
    /// </summary>
    public static object To(object value, Type destinationType, CultureInfo culture)
    {
        if (value == null)
            return null;

        var sourceType = value.GetType();

        var destinationConverter = TypeDescriptor.GetConverter(destinationType);
        if (destinationConverter.CanConvertFrom(value.GetType()))
            return destinationConverter.ConvertFrom(null, culture, value);

        var sourceConverter = TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter.CanConvertTo(destinationType))
            return sourceConverter.ConvertTo(null, culture, value, destinationType);

        if (destinationType.IsEnum && value is int i)
            return Enum.ToObject(destinationType, i);

        if (!destinationType.IsInstanceOfType(value))
            return Convert.ChangeType(value, destinationType, culture);

        return value;
    }

    /// <summary>
    /// 一个值到目的地类型转换。
    /// </summary>
    public static T To<T>(object value)
    {
        //return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        return (T) To(value, typeof(T));
    }

    /// <summary>
    /// 转换枚举为前端
    /// </summary>
    public static string ConvertEnum(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;

        // 原实现逐字符多次 ToString 且字符串累加为 O(n²)；改为单次遍历 + StringBuilder。
        var sb = new StringBuilder(str.Length + 8);
        foreach (var c in str)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ');
            sb.Append(c);
        }

        // 保持原有语义：统一去掉首部空白（首个大写字母不应产生前导空格）。
        return sb.ToString().TrimStart();
    }

    /// <summary>
    /// 多年变化
    /// </summary>
    public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
    {
        var age = endDate.Year - startDate.Year;
        if (startDate > endDate.AddYears(-age))
            age--;
        return age;
    }

    /// <summary>
    /// 获取或设置默认文件提供程序
    /// </summary>
    public static IGirvsFileProvider DefaultFileProvider { get; set; }
}
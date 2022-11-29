namespace Girvs.Extensions;

/// <summary>
/// Extension methods for String class.
/// </summary>
public static class StringExtensions
{
    public static string Base64Encode(this string content)
    {
        if (content.IsNullOrWhiteSpace()) return content;
        var bytes = Encoding.UTF8.GetBytes(content);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 将base64格式，转换utf8
    /// </summary>
    /// <param name="content">解密内容</param>
    /// <returns></returns>
    public static string Base64Decode(this string content)
    {
        if (content.IsNullOrWhiteSpace()) return content;
        var bytes = Convert.FromBase64String(content);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Adds a char to end of given string if it does not ends with the char.
    /// </summary>
    public static string EnsureEndsWith(this string str, char c)
    {
        return EnsureEndsWith(str, c, StringComparison.Ordinal);
    }

    /// <summary>
    /// Adds a char to end of given string if it does not ends with the char.
    /// </summary>
    public static string EnsureEndsWith(this string str, char c, StringComparison comparisonType)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.EndsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return str + c;
    }

    /// <summary>
    /// Adds a char to end of given string if it does not ends with the char.
    /// </summary>
    public static string EnsureEndsWith(this string str, char c, bool ignoreCase, CultureInfo culture)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.EndsWith(c.ToString(culture), ignoreCase, culture))
        {
            return str;
        }

        return str + c;
    }

    /// <summary>
    /// Adds a char to beginning of given string if it does not starts with the char.
    /// </summary>
    public static string EnsureStartsWith(this string str, char c,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.StartsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return c + str;
    }

    /// <summary>
    /// Adds a char to beginning of given string if it does not starts with the char.
    /// </summary>
    public static string EnsureStartsWith(this string str, char c, bool ignoreCase, CultureInfo culture)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.StartsWith(c.ToString(culture), ignoreCase, culture))
        {
            return str;
        }

        return c + str;
    }

    /// <summary>
    /// Indicates whether this string is null or an System.String.Empty string.
    /// </summary>
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// indicates whether this string is null, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
    public static string Left(this string str, int len)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.Length < len)
        {
            throw new ArgumentException("len argument can not be bigger than given string's length!");
        }

        return str.Substring(0, len);
    }

    /// <summary>
    /// Converts line endings in the string to <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string NormalizeLineEndings(this string str)
    {
        return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    /// <summary>
    /// Gets index of nth occurence of a char in a string.
    /// </summary>
    /// <param name="str">source string to be searched</param>
    /// <param name="c">Char to search in <paramref name="str"/></param>
    /// <param name="n">Count of the occurence</param>
    public static int NthIndexOf(this string str, char c, int n)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        var count = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] != c)
            {
                continue;
            }

            if ((++count) == n)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Removes first occurrence of the given postfixes from end of the given string.
    /// Ordering is important. If one of the postFixes is matched, others will not be tested.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="postFixes">one or more postfix.</param>
    /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
    public static string RemovePostFix(this string str, params string[] postFixes)
    {
        if (str == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        if (postFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var postFix in postFixes)
        {
            if (str.EndsWith(postFix))
            {
                return str.Left(str.Length - postFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// Removes first occurrence of the given prefixes from beginning of the given string.
    /// Ordering is important. If one of the preFixes is matched, others will not be tested.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="preFixes">one or more prefix.</param>
    /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
    public static string RemovePreFix(this string str, params string[] preFixes)
    {
        if (str == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        if (preFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var preFix in preFixes)
        {
            if (str.StartsWith(preFix))
            {
                return str.Right(str.Length - preFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// Gets a substring of a string from end of the string.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
    public static string Right(this string str, int len)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (str.Length < len)
        {
            throw new ArgumentException("len argument can not be bigger than given string's length!");
        }

        return str.Substring(str.Length - len, len);
    }

    /// <summary>
    /// Uses string.Split method to split given string by given separator.
    /// </summary>
    public static string[] Split(this string str, string separator)
    {
        return str.Split(new[] {separator}, StringSplitOptions.None);
    }

    /// <summary>
    /// Uses string.Split method to split given string by given separator.
    /// </summary>
    public static string[] Split(this string str, string separator, StringSplitOptions options)
    {
        return str.Split(new[] {separator}, options);
    }

    /// <summary>
    /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string[] SplitToLines(this string str)
    {
        return str.Split(Environment.NewLine);
    }

    /// <summary>
    /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string[] SplitToLines(this string str, StringSplitOptions options)
    {
        return str.Split(Environment.NewLine, options);
    }

    /// <summary>
    /// Converts PascalCase string to camelCase string.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="invariantCulture">Invariant culture</param>
    /// <returns>camelCase of the string</returns>
    public static string ToCamelCase(this string str, bool invariantCulture = true)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return invariantCulture ? str.ToLowerInvariant() : str.ToLower();
        }

        return (invariantCulture ? char.ToLowerInvariant(str[0]) : char.ToLower(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// Converts PascalCase string to camelCase string in specified culture.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="culture">An object that supplies culture-specific casing rules</param>
    /// <returns>camelCase of the string</returns>
    public static string ToCamelCase(this string str, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return str.ToLower(culture);
        }

        return char.ToLower(str[0], culture) + str.Substring(1);
    }

    /// <summary>
    /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
    /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
    /// </summary>
    /// <param name="str">String to convert.</param>
    /// <param name="invariantCulture">Invariant culture</param>
    public static string ToSentenceCase(this string str, bool invariantCulture = false)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return Regex.Replace(
            str,
            "[a-z][A-Z]",
            m => m.Value[0] + " " +
                 (invariantCulture ? char.ToLowerInvariant(m.Value[1]) : char.ToLower(m.Value[1]))
        );
    }

    /// <summary>
    /// Converts given PascalCase/camelCase string to sentence (by splitting words by space).
    /// Example: "ThisIsSampleSentence" is converted to "This is a sample sentence".
    /// </summary>
    /// <param name="str">String to convert.</param>
    /// <param name="culture">An object that supplies culture-specific casing rules.</param>
    public static string ToSentenceCase(this string str, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1], culture));
    }

    /// <summary>
    /// Converts string to enum value.
    /// </summary>
    /// <typeparam name="T">Type of enum</typeparam>
    /// <param name="value">String value to convert</param>
    /// <returns>Returns enum object</returns>
    public static T ToEnum<T>(this string value)
        where T : struct
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return (T) Enum.Parse(typeof(T), value);
    }

    /// <summary>
    /// Converts string to enum value.
    /// </summary>
    /// <typeparam name="T">Type of enum</typeparam>
    /// <param name="value">String value to convert</param>
    /// <param name="ignoreCase">Ignore case</param>
    /// <returns>Returns enum object</returns>
    public static T ToEnum<T>(this string value, bool ignoreCase)
        where T : struct
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return (T) Enum.Parse(typeof(T), value, ignoreCase);
    }

    public static string ToShortMd5(this string str)
    {
        using var md5Hash = MD5.Create();
        var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
        //转换成字符串，并取9到25位
        var sBuilder = BitConverter.ToString(data, 4, 8);
        //BitConverter转换出来的字符串会在每个字符中间产生一个分隔符，需要去除掉
        sBuilder = sBuilder.Replace("-", "");
        return sBuilder.ToString().ToUpper();
    }

    public static string ToMd5(this string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;

        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = md5.ComputeHash(inputBytes);

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes)
        {
            sb.Append(hashByte.ToString("X2"));
        }

        return sb.ToString();
    }

    public static Guid? ToHasGuid(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return (Guid?) null;
        }

        return Guid.Parse(str);
    }

    public static Guid ToGuidDefaultEmpty(this string str)
    {
        var s = Guid.TryParse(str, out var result);
        return s ? result : Guid.Empty;
    }

    // public static T ToEnum<T>(this string str)
    // {
    //     return (T) Enum.Parse(typeof(T), str);
    // }

    public static Guid ToGuid(this string str)
    {
        return string.IsNullOrEmpty(str) ? Guid.Empty : Guid.Parse(str);
    }

    /// <summary>
    /// Converts camelCase string to PascalCase string.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="invariantCulture">Invariant culture</param>
    /// <returns>PascalCase of the string</returns>
    public static string ToPascalCase(this string str, bool invariantCulture = true)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return invariantCulture ? str.ToUpperInvariant() : str.ToUpper();
        }

        return (invariantCulture ? char.ToUpperInvariant(str[0]) : char.ToUpper(str[0])) + str.Substring(1);
    }

    /// <summary>
    /// Converts camelCase string to PascalCase string in specified culture.
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <param name="culture">An object that supplies culture-specific casing rules</param>
    /// <returns>PascalCase of the string</returns>
    public static string ToPascalCase(this string str, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (str.Length == 1)
        {
            return str.ToUpper(culture);
        }

        return char.ToUpper(str[0], culture) + str.Substring(1);
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    public static string Truncate(this string str, int maxLength)
    {
        if (str == null)
        {
            return null;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str.Left(maxLength);
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
    /// It adds given <paramref name="postfix"/> to end of the string if it's truncated.
    /// Returning string can not be longer than maxLength.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    public static string TruncateWithPostfix(this string str, int maxLength, string postfix = "...")
    {
        if (str == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(str) || maxLength == 0)
        {
            return string.Empty;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        if (maxLength <= postfix.Length)
        {
            return postfix.Left(maxLength);
        }

        return str.Left(maxLength - postfix.Length) + postfix;
    }


    public static string ConverterInitialsLowerCase(this string str)
    {
        if (str == null) throw new ArgumentNullException(nameof(str));

        var initialStr = str.Substring(0, 1);
        var leftOver = str.Substring(1);

        return $"{initialStr.ToLower()}{leftOver}";
    }
}
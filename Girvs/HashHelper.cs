namespace Girvs;

/// <summary>
/// Hash 帮助类
/// </summary>
public class HashHelper
{
    /// <summary>
    /// 创建数据哈希
    /// </summary>
    /// <param name="data">用于计算哈希的数据</param>
    /// <param name="hashAlgorithm">哈希算法</param>
    /// <param name="trimByteCount">哈希算法中将使用的字节数；保留0以使用所有数组</param>
    /// <returns>哈希</returns>
    public static string CreateHash(byte[] data, string hashAlgorithm = "SHA1", int trimByteCount = 0)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrEmpty(hashAlgorithm))
            throw new ArgumentNullException(nameof(hashAlgorithm));

        // 直接按区间取跨度，省去截取副本；ToHexString 一次成串，
        // 不再经过 BitConverter + Replace 的两次分配。
        ReadOnlySpan<byte> source = trimByteCount > 0 && data.Length > trimByteCount
            ? data.AsSpan(0, trimByteCount)
            : data.AsSpan();

        // 静态 HashData 线程安全，且省去每次调用创建算法实例的开销。
        var hash = hashAlgorithm.ToUpperInvariant() switch
        {
            "SHA1" or "SHA-1" => SHA1.HashData(source),
            "SHA256" or "SHA-256" => SHA256.HashData(source),
            "SHA384" or "SHA-384" => SHA384.HashData(source),
            "SHA512" or "SHA-512" => SHA512.HashData(source),
            "MD5" => MD5.HashData(source),
            _ => throw new ArgumentException($"Unrecognized hash name: {hashAlgorithm}", nameof(hashAlgorithm)),
        };

        return Convert.ToHexString(hash);
    }
}
using System;
using System.Security.Cryptography;

namespace Girvs
{
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
            if (string.IsNullOrEmpty(hashAlgorithm))
                throw new ArgumentNullException(nameof(hashAlgorithm));

            var algorithm = (HashAlgorithm) CryptoConfig.CreateFromName(hashAlgorithm);
            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash name");

            if (trimByteCount > 0 && data.Length > trimByteCount)
            {
                var newData = new byte[trimByteCount];
                Array.Copy(data, newData, trimByteCount);

                return BitConverter.ToString(algorithm.ComputeHash(newData)).Replace("-", string.Empty);
            }

            return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
        }
    }
}
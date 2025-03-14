using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AE.PID.Core;

public abstract class HashHelper
{
    public static string ComputeMD5Hash(string input)
    {
        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            // 将字节数组转换为16进制字符串
            var sb = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }
    }

    public static string ComputeSHA256Hash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
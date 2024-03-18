using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AE.PID.Server.DTOs.PDMS;

namespace AE.PID.Server.Services;

public static class ApiHelper
{
    private const string SystemId = "visioVault";
    private const string Password = "9F3517D0CADF2569BF9CF2BDB85DCC9B";

    private static string ToMd5(this string plainText)
    {
        byte[] secretBytes;
        try
        {
            using var md5 = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            secretBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        }
        catch (Exception e)
        {
            // Handle the exception or rethrow it if needed.
            throw new Exception("Error computing MD5 hash.", e);
        }

        // Convert the byte array to a hexadecimal string representation.
        var md5Code = BitConverter.ToString(secretBytes).Replace("-", string.Empty).ToLower();

        // If the generated string is not 32 characters long, pad it with leading zeros.
        var tempIndex = 32 - md5Code.Length;
        for (var i = 0; i < tempIndex; i++) md5Code = "0" + md5Code;

        return md5Code;
    }

    public static HeaderDto CreateHeader()
    {
        var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        return new HeaderDto
        {
            SystemId = "visioVault",
            CurrentDateTime = timeStamp,
            MD5 = (SystemId + Password + timeStamp).ToMd5()
        };
    }

    public static FormUrlEncodedContent BuildFormUrlEncodedContent<T>(T data)
    {
        var nameValueCollection = new List<KeyValuePair<string, string>>
            // ReSharper disable once StringLiteralTypo
            { new("datajson", JsonSerializer.Serialize(data)) };
        var content = new FormUrlEncodedContent(nameValueCollection);
        return content;
    }
}
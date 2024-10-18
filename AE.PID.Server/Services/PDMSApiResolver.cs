using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AE.PID.Server.DTOs.PDMS;

namespace AE.PID.Server.Services;

public static class PDMSApiResolver
{
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
            Time = timeStamp,
            MD5 = ("visioVault" + "9F3517D0CADF2569BF9CF2BDB85DCC9B" + timeStamp).ToMd5()
        };
    }

    public static BipHeaderDto CreateBipHeader(string userId, string uuid, BipActions bipActions)
    {
        var bidCode = bipActions switch
        {
            BipActions.SyncProjectFunctionGroups => "BIP1APT02100011",
            BipActions.SyncProjectFunctionZoneMaterials => "BIP1APT02100011",
            _ => throw new ArgumentOutOfRangeException(nameof(bipActions), bipActions, null)
        };

        var time = DateTime.Now;
        return new BipHeaderDto
        {
            UserId = userId,
            UUID = uuid,
            Time = time.ToString("yyyy-MM-dd HH:mm:ss"),
            Id = time.ToString(CultureInfo.CurrentCulture),
            BipCode = bidCode,
            FromSystemCode = "Vosio",
            ToSystemCode = "Weaver"
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
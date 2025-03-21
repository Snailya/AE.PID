// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

if (args.Length < 2)
{
    Console.WriteLine("Usage: Updater <currentVersion> <requestUrl>");

    return;
}

var currentVersion = args[0];
var requestUrl = args[1];

// Download the new version file from remote 
// 获取服务器版本信息
var serverVersionInfo = await GetServerVersionInfoAsync(requestUrl);
if (serverVersionInfo == null)
{
    Console.WriteLine("STATUS: UPDATE_CHECK_FAILED");

    return;
}

// Compare the local version with the remote
if (new Version(serverVersionInfo.Version) <= new Version(currentVersion))
{
    Console.WriteLine("STATUS: NO_UPDATE_AVAILABLE");

    return;
}

// Download the packages if not exist
var tmpDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "tmp");
if (!Directory.Exists(tmpDirectory))
    Directory.CreateDirectory(tmpDirectory);
var localFilePath = Path.Combine(tmpDirectory, "tmp",
    serverVersionInfo.FileName);
if (!File.Exists(localFilePath) || !VerifyFileHash(localFilePath, serverVersionInfo.FileHash))
{
    Console.WriteLine($"DOWNLOADING: {serverVersionInfo.FileName}");

    await DownloadFileAsync(serverVersionInfo.DownloadUrl, localFilePath);
    if (!VerifyFileHash(localFilePath, serverVersionInfo.FileHash))
    {
        Console.WriteLine("STATUS: UPDATE_CHECK_FAILED");

        return;
    }
}

// 输出更新信息（结构化数据）
Console.WriteLine("UPDATE_INFO:");
Console.WriteLine($"VERSION: {serverVersionInfo.Version}");
Console.WriteLine($"DOWNLOAD_URL: {serverVersionInfo.DownloadUrl}");
Console.WriteLine($"RELEASE_NOTES: {serverVersionInfo.ReleaseNotes}");
Console.WriteLine($"FILE_PATH: {localFilePath}");

Console.WriteLine("PROMPT: Do you want to continue? [Y/n]");

var input = Console.Read();

if (char.ToUpper((char)input) == 'Y')
{
    // Execute installer
    InstallUpdate(localFilePath);
    Console.WriteLine("STATUS: UPDATE_DONE");
}
else
{
    Console.WriteLine("STATUS: Abort.");
}

return;

static async Task<VersionInfo?> GetServerVersionInfoAsync(string serverUrl)
{
    using var httpClient = new HttpClient();
    var json = await httpClient.GetStringAsync(serverUrl);

    // 使用 JToken 解析 JSON
    var jToken = JToken.Parse(json);

    // 假设返回的 JSON 是一个对象，而不是数组
    if (jToken is JObject jObject)
        return new VersionInfo
        {
            Version = jObject["version"]?.ToString() ?? string.Empty,
            FileName = jObject["fileName"]?.ToString() ?? string.Empty,
            FileHash = jObject["fileHash"]?.ToString() ?? string.Empty,
            DownloadUrl = jObject["downloadUrl"]?.ToString() ?? string.Empty,
            ReleaseNotes = jObject["releaseNotes"]?.ToString() ?? string.Empty
        };

    return null;
}

static bool VerifyFileHash(string filePath, string expectedHash)
{
    return ComputeHash(filePath) == expectedHash;
}

static async Task DownloadFileAsync(string downloadUrl, string destinationPath)
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    var targetFolder = Path.GetDirectoryName(destinationPath);
    if (targetFolder != null && !Directory.Exists(targetFolder))
        Directory.CreateDirectory(targetFolder);

    using var fileStream = new FileStream(destinationPath, FileMode.Create);
    await response.Content.CopyToAsync(fileStream);
}

static void InstallUpdate(string installerPath)
{
    var processStartInfo = new ProcessStartInfo
    {
        FileName = installerPath,
        CreateNoWindow = true,
        // ensure the administrator privilege as the replacement in the Local folder might return access deny code 5 
        UseShellExecute = false,
        Verb = "runas"
    };

    using var process = Process.Start(processStartInfo);
    process?.WaitForExit();
}

static string ComputeHash(string filePath)
{
    using var stream = File.OpenRead(filePath);
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(stream);
    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
}

public class VersionInfo
{
    /// <summary>
    ///     The version string of the application
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     The url to download the installer
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    ///     The recommended filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     The hash of the file used for check.
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    ///     The changelog
    /// </summary>
    public string ReleaseNotes { get; set; } = string.Empty;
}
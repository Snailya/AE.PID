using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using Refit;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class StencilUpdateService : ApiFactory<IStencilApi>, IEnableLogger
{
    private readonly IConfigurationService _configurationService;
    private readonly string _folder;

    public StencilUpdateService(IConfigurationService configurationService) : base(configurationService)
    {
        _configurationService = configurationService;

        _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            configurationService.RuntimeConfiguration.AppDataFolder,
            "libraries");

        if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
    }

    public async Task<IEnumerable<Stencil>> UpdateAsync()
    {
        // get the local stencil configuration
        var locals = _configurationService.GetCurrentConfiguration().Stencils.ToList();

        try
        {
            // get the server side stencil info
            var servers = (await Api!.GetCurrentSnapshot()).ToList();

            var results = new List<Stencil>();
            foreach (var server in servers)
            {
                var local = locals.SingleOrDefault(x => x.Id == server.Id);
                var result = new Stencil
                {
                    Id = server.Id,
                    Name = server.StencilName,
                    FilePath = local == null || !File.Exists(local.FilePath)
                        ? await DownloadAsync(server.DownloadUrl)
                        : local.FilePath
                };

                results.Add(result);
            }

            // delete the files that are not in the server config
            foreach (var local in locals.Where(local => servers.All(x => x.Id != local.Id)))
                File.Delete(local.FilePath);

            // update configuration
            _configurationService.UpdateProperty(x => x.Stencils, results);

            return results;
        }
        catch (ApiException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
        catch (HttpRequestException e)
        {
            this.Log().Error(e);
            throw new NetworkNotValidException();
        }
    }


    private async Task<string> DownloadAsync(string downloadUrl)
    {
        var client = new HttpClient();
        using var response = await client.GetAsync(downloadUrl);

        var fileName = Path.GetFileName(
            GetFilenameFromContentDisposition(response.Content.Headers));
        if (string.IsNullOrEmpty(fileName)) throw new InvalidOperationException("");

        var filePath = Path.GetFullPath(Path.Combine(_folder, fileName));

        // Otherwise, get the content as a stream
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        await contentStream.CopyToAsync(fileStream);

        return filePath;
    }

    private static string? GetFilenameFromContentDisposition(HttpContentHeaders headers)
    {
        if (headers.ContentDisposition != null)
        {
            var filename = headers.ContentDisposition.FileName;
            var filenameStar = headers.ContentDisposition.FileNameStar;

            // 处理filename*（使用UTF-8编码）
            if (!string.IsNullOrEmpty(filenameStar)) return DecodeFileName(filenameStar);
            // 如果没有filename*，则处理普通的filename
            return filename.Trim('"');
        }

        return null;
    }

    private static string DecodeFileName(string filenameStar)
    {
        // 移除前缀
        if (filenameStar.StartsWith("UTF-8''"))
        {
            var encodedFileName = filenameStar.Substring(7);
            // URL解码
            return Uri.UnescapeDataString(encodedFileName);
        }

        return filenameStar;
    }
}
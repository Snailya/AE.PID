using System.Net.Http;
using Refit;

namespace AE.PID.Visio.Test;

public class IApiTest
{
    private const string BaseAddress = "http://localhost:32768";

    [Fact]
    public async void Test2()
    {
        // 创建 HttpClient
        using var httpClient = new HttpClient();

        // 构建 Multipart 内容
        using var multipartContent = new MultipartFormDataContent();

        // 添加文件内容
        var fileStream = File.OpenRead(@"D:\03-程序\AE-PID\AE.PID.Visio.Test\Test Files\Test.pdf");
        var fileContent = new StreamContent(fileStream);
        multipartContent.Add(fileContent, "file",
            Path.GetFileName(@"D:\03-程序\AE-PID\AE.PID.Visio.Test\Test Files\Test.pdf"));

        // 添加字符串参数
        var userIdContent = new StringContent("1");
        multipartContent.Add(userIdContent, "userId");
        multipartContent.Add(userIdContent, "userId");
        multipartContent.Add(userIdContent, "userId");

        // 发送请求
        var response = await httpClient.PostAsync("http://localhost:32768/api/v3/debug/upload", multipartContent);
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async void Test3()
    {
        var api = RestService.For<IApi>(BaseAddress);
        var fileInfo = new FileInfo(@"C:\Users\lijin\Desktop\Visio\嵌套更新测试.vsdx");
        var fileInfoPart = new FileInfoPart(fileInfo, fileInfo.Name);

        var bytes = File.ReadAllBytes(@"C:\Users\lijin\Desktop\Visio\嵌套更新测试.vsdx");

        var byteArrayPart = new ByteArrayPart(bytes, @"C:\Users\lijin\Desktop\Visio\嵌套更新测试.vsdx");

        var response = await api.Update2(byteArrayPart, []);

        Assert.NotEmpty(response);
    }


    public interface IApi
    {
        [Multipart]
        [Post("/api/v3/debug/upload")]
        Task<string> UploadFileWithModel([AliasAs("File")] FileInfoPart fileInfo, [AliasAs("Name")] string name,
            [AliasAs("Description")] string description);

        [Multipart]
        [Post("/api/v3/debug/upload2")]
        Task<string> UploadFileWithModel2([AliasAs("file")] ByteArrayPart fileInfo,
            [AliasAs("excludes")] string[]? excludes = null);

        [Multipart]
        [Post("/api/v3/debug/upload3")]
        Task<string> Update([AliasAs("file")] ByteArrayPart file, [AliasAs("excludes")] string[]? excludes = null,
            [Query] int status = 1);
        
        [Multipart]
        [Post("/api/v3/documents/update")]
        Task<string> Update2([AliasAs("file")] ByteArrayPart file, [AliasAs("excludes")] string[]? excludes = null,
            [Query] int status = 1);
    }
}
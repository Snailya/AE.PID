namespace AE.PID.Server;

internal static class FileHelper
{
    public static async Task<string> SaveToTmpFile(IFormFile file, string? customFilename = null)
    {
        var filePath = Path.Combine(PathConstants.TmpPath, customFilename ?? Path.GetRandomFileName());

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }
}
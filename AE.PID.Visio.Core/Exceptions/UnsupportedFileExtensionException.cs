namespace AE.PID.Visio.Core.Exceptions;

public class UnsupportedFileExtensionException(string extension) : Exception($"不支持该扩展名: {extension}")
{
}
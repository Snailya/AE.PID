using System;

namespace AE.PID.Client.Core;

public class FileExtensionNotSupportException(string extension) : Exception($"不支持该扩展名: {extension}")
{
}
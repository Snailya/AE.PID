using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AE.PID.Client.Infrastructure.Extensions;

public class DebugExt
{
    public static void Log(string message, object? value = null, [CallerMemberName] string callerName = "")
    {
        // 获取当前线程的 ID
        var threadId = Thread.CurrentThread.ManagedThreadId;

        // 格式化输出信息
        var logMessage = $"[T: {threadId}] [C: {callerName}] [M: {message}] [V: {value}]";

        // 输出到控制台或日志文件
        Debug.WriteLine(logMessage);
    }
}
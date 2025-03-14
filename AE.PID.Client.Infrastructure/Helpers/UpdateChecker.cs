using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class UpdateChecker(IUserInteractionService ui) : IEnableLogger
{
    private static string GetUpdater()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe");
        if (!File.Exists(path)) throw new FileNotFoundException();

        LogHost.Default.Info($"Updater found at {path}.");
        return path;
    }

    public Task<bool> CheckAsync(string currentVersion, string serverUrl)
    {
        return Task.Run(async () =>
        {
            var tcs = new TaskCompletionSource<bool>();

            try
            {
                var fileName = GetUpdater();

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName, // 要启动的应用程序或文档的路径。
                    Arguments = $"{currentVersion} {serverUrl}", // 传递给目标程序的命令行参数。
                    //WorkingDirectory = "", // 设置进程的工作目录（默认为当前程序目录）。
                    WindowStyle = ProcessWindowStyle.Normal, // 控制进程窗口的显示方式。
                    UseShellExecute = false, // 是否使用操作系统外壳程序启动进程。
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true, //是否不创建新窗口（需配合 UseShellExecute = false）
                    ErrorDialog = true // 当进程启动失败时，是否显示错误对话框。
                };

                var version = string.Empty;
                string[]? releaseNotes = null;

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    // 读取标准输出
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var outputLine = await process.StandardOutput.ReadLineAsync();

                        if (string.IsNullOrEmpty(outputLine)) continue;

                        Console.WriteLine(outputLine); // 打印输出到主程序控制台（可选）

                        // 捕获结构化更新信息
                        if (outputLine.StartsWith("STATUS"))
                        {
                            if (!outputLine.Contains("NO_UPDATE_AVAILABLE")) continue;

                            tcs.SetResult(false);
                            break;
                        }

                        if (outputLine.StartsWith("UPDATE_INFO:"))
                        {
                            // ReSharper disable once MethodHasAsyncOverload
                            version = process.StandardOutput.ReadLine()?.Split(':')[1].Trim();
                            // ReSharper disable once MethodHasAsyncOverload
                            process.StandardOutput.ReadLine();
                            // ReSharper disable once MethodHasAsyncOverload
                            releaseNotes = Regex.Replace(process.StandardOutput.ReadLine(), @"(\d+)\.\s*", "$1. ")?.Split(':')[1].Trim().Replace("；", ";")
                                .Split(';');
                            // ReSharper disable once MethodHasAsyncOverload
                            process.StandardOutput.ReadLine();

                            this.Log().Info($"Update available: {version}");

                            tcs.SetResult(true);
                        }
                        else if (outputLine.StartsWith("PROMPT:"))
                        {
                            var formattedReleaseNotes = releaseNotes == null
                                ? string.Empty
                            : string.Join("\n",
                                    releaseNotes);

                            var message = $"""
                                           版本：{currentVersion} -> {version}

                                           更新内容：

                                           {formattedReleaseNotes}

                                           现在安装？

                                           """;
                            if (await ui.SimpleDialog(message, "更新"))
                                await process.StandardInput.WriteLineAsync("Y"); // 自动输入 Y
                            else
                                await process.StandardInput.WriteLineAsync("n"); // 自动输入 n

                            await process.StandardInput.FlushAsync();
                        }
                    }

                    // 读取标准错误
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(errorOutput)) throw new Exception(errorOutput);

                    process.WaitForExit(); // 等待进程退出
                }
            }
            catch (Exception e)
            {
                this.Log().Error(e, "Failed to run updater.");
                throw new ApplicationUpdateFailedException(e.Message);
            }

            return await tcs.Task;
        });
    }
}
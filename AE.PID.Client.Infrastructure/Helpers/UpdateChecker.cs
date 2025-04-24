using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class UpdateChecker(IUserInteractionService ui) : IEnableLogger
{
    private string GetUpdater()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe");
        if (!File.Exists(path))
        {
            this.Log().Error("Updater not found at {Path}", path);
            throw new FileNotFoundException($"Updater not found at {path}");
        }

        this.Log().Info($"Updater found at {path}");
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

                var commandLine = $"\"{startInfo.FileName}\" {startInfo.Arguments}";
                this.Log().Info("[Updater] Starting process: {CommandLine}", commandLine);
                this.Log().Debug("Working directory: {WorkingDir}", startInfo.WorkingDirectory);

                var version = string.Empty;
                string[]? releaseNotes = null;

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    // 添加进程退出事件处理
                    process.EnableRaisingEvents = true;
                    process.Exited += (_, _) =>
                        this.Log().Info("[Updater] Process exited with code: {ExitCode}", process.ExitCode);

                    // 启动进程
                    if (!process.Start())
                    {
                        this.Log().Error("[Updater] Failed to start process");
                        throw new InvalidOperationException("Failed to start updater process");
                    }

                    this.Log().Info("[Updater] Process started with PID: {PID}", process.Id);

                    // 异步读取标准错误
                    var errorTask = process.StandardError.ReadToEndAsync()
                        .ContinueWith(t =>
                        {
                            if (!string.IsNullOrEmpty(t.Result))
                                this.Log().Error("[Updater] stderr: {Error}", t.Result);
                        });

                    // 读取标准输出
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var outputLine = (await process.StandardOutput.ReadLineAsync())?.Trim();
                        if (outputLine == null || string.IsNullOrEmpty(outputLine)) continue;

                        this.Log().Info("[Updater] stdout: {Output}", outputLine);

                        switch (outputLine)
                        {
                            case { } s when s.StartsWith("STATUS"):
                                if (s.Contains("NO_UPDATE_AVAILABLE"))
                                {
                                    this.Log().Info("[Updater] No updates available");
                                    tcs.SetResult(false);
                                }

                                break;
                            case { } s when s.StartsWith("UPDATE_INFO:"):
                                version = (await process.StandardOutput.ReadLineAsync())?.Split(':')[1].Trim()!;
                                await process.StandardOutput.ReadLineAsync(); // 跳过分隔线
                                releaseNotes = Regex.Replace(
                                        await process.StandardOutput.ReadLineAsync() ?? "",
                                        @"(\d+)\.\s*", "$1. ")
                                    ?.Split(':')[1]
                                    .Trim()
                                    .Replace("；", ";")
                                    .Split(';');
                                await process.StandardOutput.ReadLineAsync(); // 跳过分隔线

                                this.Log().Info("[Updater] Found new version: {Version}", version);
                                this.Log().Debug("[Updater] Release notes: {Notes}",
                                    string.Join("\n- ", releaseNotes ?? []));
                                tcs.SetResult(true);
                                break;
                            case { } s when s.StartsWith("PROMPT:"):
                                var message = $"""
                                               版本：{currentVersion} → {version}

                                               更新内容：
                                               {string.Join("；\n", releaseNotes ?? [])}

                                               现在安装？
                                               """;

                                this.Log().Info("[Updater] Showing update dialog to user");
                                var result = await ui.SimpleDialog(message, "更新");

                                this.Log().Info("[Updater] User selected: {Selection}", result ? "Install" : "Skip");
                                await process.StandardInput.WriteLineAsync(result ? "Y" : "n");
                                await process.StandardInput.FlushAsync();
                                break;
                        }
                    }

                    await Task.WhenAll(errorTask);

                    // 等待进程完全退出
                    process.WaitForExit();
                    this.Log().Info("[Updater] Process completed");
                }
            }
            catch (Exception e)
            {
                this.Log().Error(e, "[Updater] Update check failed: {ErrorMessage}", e.Message);
                tcs.TrySetException(new ApplicationUpdateFailedException(e.Message));
                throw;
            }

            return await tcs.Task;
        });
    }
}
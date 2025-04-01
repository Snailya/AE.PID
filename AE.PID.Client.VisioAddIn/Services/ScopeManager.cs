using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

public class ScopeManager : IDisposable, IEnableLogger
{
    private const int ExpiredCheckInterval = 10;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new();
    private readonly Dictionary<Document, (IServiceScope Scope, int RefCount, DateTime? ReleaseTime)> _scopes = new();
    private readonly IServiceProvider _serviceProvider;

    public ScopeManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _cleanupTimer = new Timer(_ => CleanupExpiredScopes(), null,
            TimeSpan.FromMinutes(ExpiredCheckInterval), // 首次检查延迟
            TimeSpan.FromMinutes(ExpiredCheckInterval)); // 后续间隔
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        lock (_lock)
        {
            foreach (var scope in _scopes.Values)
                scope.Scope.Dispose();
            _scopes.Clear();
        }
    }

    /// <summary>
    ///     Get the scope and add the count.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public IServiceScope GetScope(Document document)
    {
        lock (_lock)
        {
            if (!_scopes.TryGetValue(document, out var entry))
            {
                entry = (_serviceProvider.CreateScope(), 0, null);
                _scopes.Add(document, entry);

                // 监听文档关闭事件（Visio 示例）
                document.BeforeDocumentClose += doc =>
                {
                    if (doc == document)
                        ForceReleaseScope(document);
                };
            }

            // 增加引用计数
            _scopes[document] = (entry.Scope, entry.RefCount + 1, null);
            return entry.Scope;
        }
    }

    /// <summary>
    ///     Decrease the count or add dispose event in the future
    /// </summary>
    /// <param name="document"></param>
    public void ReleaseScope(Document document)
    {
        lock (_lock)
        {
            if (!_scopes.TryGetValue(document, out var entry)) return;

            // 减少引用计数，若归零则计划释放
            if (entry.RefCount <= 1)
                _scopes[document] = (entry.Scope, 0, DateTime.UtcNow.AddMinutes(ExpiredCheckInterval)); // 10分钟后释放
            else
                _scopes[document] = (entry.Scope, entry.RefCount - 1, null);
        }
    }

    // 强制立即释放（如文档关闭时）
    private void ForceReleaseScope(Document document)
    {
        lock (_lock)
        {
            if (!_scopes.TryGetValue(document, out var entry)) return;
            _scopes.Remove(document);
            entry.Scope.Dispose();

            Debug.WriteLine($"{document.Name} scope disposed.");
        }
    }

    /// <summary>
    ///     检查并释放过期 Scope
    /// </summary>
    private void CleanupExpiredScopes()
    {
        lock (_lock)
        {
            var toRelease = _scopes
                .Where(kvp => kvp.Value.ReleaseTime.HasValue && DateTime.UtcNow >= kvp.Value.ReleaseTime)
                .ToList();

            foreach (var item in toRelease)
                ForceReleaseScope(item.Key);
        }
    }
}
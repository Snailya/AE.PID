using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

public class DocumentSimplifier
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static Subject<IVPage> ManuallyInvokeTrigger { get; } = new();

    /// <summary>
    ///     Emit a value manually
    /// </summary>
    public static void Invoke(IVPage page)
    {
        ManuallyInvokeTrigger.OnNext(page);
    }

    public static void Simplify(string filePath)
    {
    }
}
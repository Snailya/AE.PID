using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using NLog;
using ReactiveUI;
using Application = Microsoft.Office.Interop.Visio.Application;


namespace AE.PID.Controllers.Services;

/// <summary>
///     Document monitor will monitor the document timeliness when application is idle. Prompt update periodically
/// </summary>
public class DocumentMonitor
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<Document> _checked = [];
    private readonly CompositeDisposable _cleanUp = new();
    private readonly ConfigurationService _configuration;

    public DocumentMonitor(ConfigurationService configuration)
    {
        _configuration = configuration;
        Logger.Info("Document Monitor Service started.");

        // check update when visio is idle, however if the document has been checked, skip it
        Observable.FromEvent<EApplication_VisioIsIdleEventHandler, Application>(
                handler => Globals.ThisAddIn.Application.VisioIsIdle += handler,
                handler => Globals.ThisAddIn.Application.VisioIsIdle -= handler
            )
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Select(app => app.ActiveDocument)
            .DistinctUntilChanged()
            .WhereNotNull()
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing && document.Stat == 0)
            .Where(x => _checked.All(i => i.ID != x.ID) && IsMasterOutOfDate(x))
            // switch back to main thread to prompt user
            .ObserveOn(Globals.ThisAddIn.SynchronizationContext)
            .Subscribe(document =>
                {
                    var dialog = ThisAddIn.AskForUpdate("检测到文档模具与库中模具不一致，是否立即更新文档模具？");
                    if (dialog == DialogResult.No || dialog == DialogResult.Cancel)
                        _checked.Add(document);
                    else
                        VisioHelper.UpdateDocumentStencil(document);
                },
                ex => { Logger.Error(ex, "Document Monitor Service ternimated accidently."); },
                () => { Logger.Error("Document Monitor Service should never complete."); })
            .DisposeWith(_cleanUp);

        // remove the document from checked if it is closed. because it's not able to know if the document is modified out scope
        Observable.FromEvent<EApplication_BeforeDocumentCloseEventHandler, Document>(
                handler => Globals.ThisAddIn.Application.BeforeDocumentClose += handler,
                handler => Globals.ThisAddIn.Application.BeforeDocumentClose -= handler)
            // switch to background thread
            .ObserveOn(ThreadPoolScheduler.Instance)
            .WhereNotNull()
            .Subscribe(document =>
            {
                if (_checked.Contains(document))
                    _checked.Remove(document);
            })
            .DisposeWith(_cleanUp);
    }

    /// <summary>
    ///     Compare the document stencil with library stuffs.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    private bool IsMasterOutOfDate(IVDocument document)
    {
        return document.Masters != null && document.Masters.OfType<IVMaster>().ToList().Any(source =>
            _configuration.LibraryItems.Items.Any(x => x.BaseId == source.BaseID && x.UniqueId != source.UniqueID));
    }
}
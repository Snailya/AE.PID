using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.Tools;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Splat;
using Application = Microsoft.Office.Interop.Visio.Application;

namespace AE.PID.Services;

/// <summary>
///     Document monitor will monitor the document timeliness when application is idle. Prompt update periodically
/// </summary>
public class DocumentMonitor : IEnableLogger
{
    private readonly List<Document> _checked = [];
    private readonly CompositeDisposable _cleanUp = new();
    private readonly ConfigurationService _configuration;

    public DocumentMonitor(ConfigurationService configuration)
    {
        _configuration = configuration;
        this.Log().Info("Document Monitor Service started.");

        // check update when visio is idle, however, if the document has been checked, skip it
        Observable.FromEvent<EApplication_VisioIsIdleEventHandler, Application>(
                handler => Globals.ThisAddIn.Application.VisioIsIdle += handler,
                handler => Globals.ThisAddIn.Application.VisioIsIdle -= handler
            )
            .Select(app => app.ActiveDocument)
            .DistinctUntilChanged()
            .WhereNotNull()
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing && document.Stat == 0)
            .Where(x => _checked.All(i => i.ID != x.ID) && IsMasterOutOfDate(x))
            // switch back to the main thread to prompt user
            .ObserveOn(WindowManager.Dispatcher!)
            .Subscribe(document =>
                {
                    // ask for update
                    var result = WindowManager.ShowDialog("检测到文档模具与库中模具不一致，是否立即更新文档模具？");

                    if (result is MessageBoxResult.No or MessageBoxResult.Cancel)
                        _checked.Add(document);
                    else
                        VisioHelper.UpdateDocumentStencil(document);
                },
                ex => { this.Log().Error(ex, "Document Monitor Service ternimated accidently."); },
                () => { this.Log().Error("Document Monitor Service should never complete."); })
            .DisposeWith(_cleanUp);

        // remove the document from checked if it is closed. because it's not able to know if the document is modified out scope
        Observable.FromEvent<EApplication_BeforeDocumentCloseEventHandler, Document>(
                handler => Globals.ThisAddIn.Application.BeforeDocumentClose += handler,
                handler => Globals.ThisAddIn.Application.BeforeDocumentClose -= handler)
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
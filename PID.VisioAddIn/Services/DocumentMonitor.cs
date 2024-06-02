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

        // check update when visio is idle, however, if the document has been checked, skip it
        Observable.FromEvent<EApplication_VisioIsIdleEventHandler, Application>(
                handler => Globals.ThisAddIn.Application.VisioIsIdle += handler,
                handler => Globals.ThisAddIn.Application.VisioIsIdle -= handler
            )
            .Select(app => app.ActiveDocument)
            .DistinctUntilChanged()
            .WhereNotNull()
            .Where(document => document.Type == VisDocumentTypes.visTypeDrawing && document.Stat == 0 &&
                               _checked.All(i => i.ID != document.ID))
            .Do(document => this.Log().Info($"Try checking the currency of the {document.FullName} masters..."))
            .Where(IsMasterOutOfDate)
            .Do(document => this.Log().Info("Masters are out of date."))
            // switch back to the main thread to prompt user
            .ObserveOn(WindowManager.Dispatcher!)
            .Subscribe(document =>
                {
                    // ask for update
                    var result = WindowManager.ShowDialog(Properties.Resources.MSG_document_masters_update_confirmation);

                    if (result is MessageBoxResult.No or MessageBoxResult.Cancel)
                    {
                        _checked.Add(document);
                        this.Log().Info("Update skipped by user.");
                    }
                    else
                    {
                        VisioHelper.UpdateDocumentStencil(document);
                    }
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
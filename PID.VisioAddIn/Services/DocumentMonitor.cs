using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using AE.PID.Properties;
using AE.PID.Tools;
using AE.PID.Visio.Core;
using AE.PID.Visio.Infrastructure.Services;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Splat;
using Application = Microsoft.Office.Interop.Visio.Application;
using Path = System.IO.Path;

namespace AE.PID.Services;

/// <summary>
///     Document monitor will monitor the document timeliness when application is idle. Prompt update periodically
/// </summary>
public class DocumentMonitor : IEnableLogger
{
    private readonly List<Document> _checked = [];
    private readonly CompositeDisposable _cleanUp = new();
    private readonly IConfigurationService _configuration;
    private readonly ApiFactory _factory;

    public DocumentMonitor(ApiFactory? factory = null, IConfigurationService? configuration = null)
    {
        _factory = factory ?? Locator.Current.GetService<ApiFactory>()!;

        _configuration = configuration ?? Locator.Current.GetService<IConfigurationService>()!;

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
            .Do(document => this.Log().Info($"Masters in {document.Name} are out of date."))
            // switch back to the main thread to prompt user
            .ObserveOn(App.UIScheduler)
            .Subscribe(document =>
                {
                    // ask for update
                    var result = WindowManager.ShowDialog(Resources.MSG_document_masters_update_confirmation);

                    if (result is MessageBoxResult.No or MessageBoxResult.Cancel)
                    {
                        _checked.Add(document);
                        this.Log().Info("Update skipped by user.");
                    }
                    else
                    {
                        _ = Update(document);
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
    public bool IsMasterOutOfDate(IVDocument document)
    {
        return document.Masters != null && document.Masters.OfType<IVMaster>().ToList().Any(source =>
            _configuration.Masters.Items.Any(x => x.BaseId == source.BaseID && x.UniqueId != source.UniqueID));
    }

    public async Task Update(Document document)
    {
        if (_configuration.UseServerSideUpdate)
            await UseServerSideUpdate(document);
        else
            VisioHelper.UseLocalUpdate(document);
    }


    /// <summary>
    ///     Update the document by transfer the file to server.
    /// </summary>
    /// <param name="document"></param>
    private async Task UseServerSideUpdate(Document document)
    {
        // remove hidden information to reduce size
        document.RemoveHiddenInformation((int)VisRemoveHiddenInfoItems.visRHIMasters);

        var filePath = string.Empty;
        // store the file path otherwise it will lose after the document close
        document!.BeforeDocumentClose += v => { filePath = document.FullName; };
        document.Close();

        if (string.IsNullOrEmpty(filePath)) return;

        // convert the file to byte-array content and sent as byte-array
        // because there is an encrypted system on end user, so directly transfer the file to server will not be able to read in the server side
        var packageBytes = File.ReadAllBytes(filePath);
        var content = new ByteArrayContent(packageBytes);
        var result = await _factory.GetClient().UpdateDocumentMasters(content);

        // create a copy of the source file
        var backup = Path.ChangeExtension(filePath, ".bak");
        if (File.Exists(backup))
            backup = Path.Combine(Path.GetDirectoryName(backup) ?? string.Empty,
                Path.GetFileNameWithoutExtension(backup) + DateTime.Now.ToString("yyyyMMdd") + ".bak");
        File.Copy(filePath, backup);

        // overwrite the origin file after a successful update
        using var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write);
        {
            await result.CopyToAsync(fileStream);
        }

        // reopen the file
        fileStream.Close();
        Globals.ThisAddIn.Application.Documents.Open(filePath);
    }
}
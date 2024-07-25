using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Interfaces;
using DynamicData;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Services;

public class VisioService : IVisioService
{
    private readonly ISubject<bool> _isLoading = new ReplaySubject<bool>();
    private readonly Lazy<SourceCache<IVMaster, string>> _masters;

    public VisioService()
    {
        _masters = new Lazy<SourceCache<IVMaster, string>>(() =>
        {
            var source = new SourceCache<IVMaster, string>(x => x.BaseID);

            AppScheduler.VisioScheduler.Schedule(() =>
            {
                LoadImpl(() =>
                {
                    var initial = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>();
                    source.AddOrUpdate(initial);
                });

                var added = Observable.FromEvent<EDocument_MasterAddedEventHandler, IVMaster>(
                        handler => Globals.ThisAddIn.Application.ActiveDocument.MasterAdded += handler,
                        handler => Globals.ThisAddIn.Application.ActiveDocument.MasterAdded -= handler,
                        AppScheduler.VisioScheduler)
                    .Do(item => { LoadImpl(() => source.AddOrUpdate(item)); });

                var removed = Observable.FromEvent<EDocument_BeforeMasterDeleteEventHandler, IVMaster>(
                        handler => Globals.ThisAddIn.Application.ActiveDocument.BeforeMasterDelete += handler,
                        handler => Globals.ThisAddIn.Application.ActiveDocument.BeforeMasterDelete -= handler,
                        AppScheduler.VisioScheduler)
                    .Do(item => { LoadImpl(() => { source.Remove(item.BaseID); }); });

                added.Merge(removed).Subscribe();
            });

            return source;
        });
    }

    public IObservable<bool> IsLoading => _isLoading.AsObservable();

    public IObservableCache<IVMaster, string> Masters => _masters.Value.AsObservableCache();

    public void OpenDocument(string fullName)
    {
        Globals.ThisAddIn.Application.Documents.OpenEx(fullName,
            (short)VisOpenSaveArgs.visOpenDocked);
    }

    public bool CloseDocumentIfOpened(string fullName)
    {
        var currentDocument = Globals.ThisAddIn.Application.Documents.OfType<Document>()
            .SingleOrDefault(x => x.FullName == fullName);
        if (currentDocument == null) return false;

        currentDocument.Close();
        return true;
    }

    /// <summary>
    ///     Create selection in active page for shapes of specified masters.
    /// </summary>
    /// <param name="baseIds"></param>
    public bool SelectShapesByMasters(string[] baseIds)
    {
        var masters = Globals.ThisAddIn.Application.ActiveDocument.Masters.OfType<IVMaster>()
            .Where(x => baseIds.Contains(x.BaseID));

        var shapeIds = new List<int>();
        foreach (var master in masters)
        {
            Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByMaster,
                VisSelectMode.visSelModeSkipSuper, master).GetIDs(out var shapeIdsPerMaster);
            shapeIds.AddRange(shapeIdsPerMaster.OfType<int>());
        }

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        foreach (var id in shapeIds)
            selection.Select(Globals.ThisAddIn.Application.ActivePage.Shapes.ItemFromID[id],
                (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActiveWindow.Selection = selection;

        return selection.Count != 0;
    }

    /// <summary>
    ///     Create a selection in active page by specified shape id.
    /// </summary>
    /// <param name="id"></param>
    public bool SelectShapeById(int id)
    {
        var shape = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<Shape>().SingleOrDefault(x => x.ID == id);
        if (shape == null) return false;

        // select and center screen
        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.Select(shape, (short)VisSelectArgs.visSelect);
        Globals.ThisAddIn.Application.ActivePage.Application.ActiveWindow.CenterViewOnShape(shape,
            VisCenterViewFlags.visCenterViewSelectShape);
        return true;
    }

    private void LoadImpl(Action action)
    {
        _isLoading.OnNext(true);
        action.Invoke();
        _isLoading.OnNext(false);
    }
}
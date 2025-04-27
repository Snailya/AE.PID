using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using AE.PID.Core;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public class VisioProvider : DisposableBase, IVisioDataProvider
{
    private readonly VisioDocumentService _docService;
    private readonly FunctionLocationProcessor _functionLocationProcessor;
    private readonly MaterialLocationProcessor _materialLocationProcessor;
    private readonly ProjectLocationProcessor _projectLocationProcessor;
    private readonly VisioDocumentMonitor _visioDocumentMonitor;

    #region -- Constructors --

    public VisioProvider(Document document, IScheduler? scheduler = null)
    {
        scheduler ??= Scheduler.CurrentThread;

        _docService = new VisioDocumentService(document, scheduler);
        _visioDocumentMonitor = new VisioDocumentMonitor(document, scheduler);
        var overlayProcessor = new OverlayProcessor(document, scheduler);

        _projectLocationProcessor = new ProjectLocationProcessor(_docService);
        _functionLocationProcessor =
            new FunctionLocationProcessor(_docService, overlayProcessor, _visioDocumentMonitor.Shapes);
        _materialLocationProcessor =
            new MaterialLocationProcessor(_docService, overlayProcessor, _visioDocumentMonitor.Shapes,_functionLocationProcessor.Locations);

        // 2025.04.08：当更新虚拟单元的UnitMultiplier时，需要同步子物料的UnitMultiplier Overlay。这个逻辑暂时不知道该放在那里，但是直觉上放在ViewModel中是不合适的。
        _functionLocationProcessor.Locations.Connect()
            .Filter(x => x.Type == FunctionType.FunctionUnit && x.IsVirtual)
            .WhereReasonsAre(ChangeReason.Update)
            .SelectMany(change => change)
            .Where(x => x.Current.UnitMultiplier != x.Previous.Value.UnitMultiplier)
            .Select(x => x.Current)
            .Do(UpdateDescendantsUnitMultiplier)
            .Subscribe()
            .DisposeWith(CleanUp);

        CleanUp.Add(_materialLocationProcessor);
        CleanUp.Add(_functionLocationProcessor);
        CleanUp.Add(_visioDocumentMonitor);
    }

    #endregion

    #region -- IInteractive --

    public void Select(ICompoundKey[] ids)
    {
        _docService.Select(ids);
    }

    #endregion

    #region -- IOleSupport --

    public void InsertAsExcelSheet(string[,] dataArray)
    {
        var oleShape = Globals.ThisAddIn.Application.ActivePage.InsertObject("Excel.Sheet",
            (short)VisInsertObjArgs.visInsertAsEmbed);
        object oleObject = oleShape.Object;
        var workbook = (Workbook)oleObject;

        // 操作Excel对象
        var worksheet = (Worksheet)workbook.Worksheets[1];
        worksheet.Range["A1"].Resize[dataArray.GetLength(0), dataArray.GetLength(1)].Value = dataArray;
        worksheet.Columns.AutoFit();
        // 保存并关闭Excel工作簿
        workbook.Save();
        workbook.Close(false); // 关闭工作簿，但不保存改变
        Marshal.ReleaseComObject(worksheet);
        Marshal.ReleaseComObject(workbook);
    }

    #endregion

    private void UpdateDescendantsUnitMultiplier(FunctionLocation location)
    {
        // find out the descendants
        var descendants =
            GetDescendants<FunctionLocation, ICompoundKey>(location,
                    _functionLocationProcessor.Locations.Items.ToList())
                .Where(x => x.Type is FunctionType.Equipment or FunctionType.Instrument or FunctionType.FunctionElement)
                .Select(x => _materialLocationProcessor.Locations.Lookup(x.Id).Value)
                .Select(x => x with { UnitMultiplier = location.UnitMultiplier })
                .ToArray();

        _materialLocationProcessor.Update(descendants);
    }

    private static IEnumerable<T> GetDescendants<T, TKey>(T parent, List<T> allNodes) where T : ITreeNode<TKey>
    {
        // Find direct children
        var children = allNodes.Where(n => Equals(n.ParentId, parent.Id));

        // Recursively find descendants of each child
        foreach (var child in children)
        {
            yield return child;
            foreach (var descendant in GetDescendants<T, TKey>(child, allNodes)) yield return descendant;
        }
    }


    #region -- IDataProvider --

    public Lazy<IObservableCache<VisioMaster, string>> Masters => _visioDocumentMonitor.Masters;

    public IObservable<ProjectLocation> ProjectLocation => _projectLocationProcessor.ProjectLocation;

    public void UpdateProjectLocation(ProjectLocation projectLocation)
    {
        _projectLocationProcessor.Update(projectLocation);
    }

    public IObservableCache<FunctionLocation, ICompoundKey> FunctionLocations =>
        _functionLocationProcessor.Locations;

    public void UpdateFunctionLocations(FunctionLocation[] functionLocations)
    {
        _functionLocationProcessor.Update(functionLocations);
    }

    public IObservableCache<MaterialLocation, ICompoundKey> MaterialLocations => _materialLocationProcessor.Locations;

    public void UpdateMaterialLocations(MaterialLocation[] materialLocations)
    {
        _materialLocationProcessor.Update(materialLocations);
    }

    public ICompoundKey[] GetAdjacent(ICompoundKey id)
    {
        return _docService.GetAdjacent(id);
    }

    #endregion
}
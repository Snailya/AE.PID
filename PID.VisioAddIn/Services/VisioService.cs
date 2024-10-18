using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AE.PID.Visio.Core;
using DynamicData;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Visio;
using Shape = Microsoft.Office.Interop.Visio.Shape;

namespace AE.PID.Services;

/// <summary>
///     只操作Visio，绝对不要注入任何和业务逻辑有关的类
/// </summary>
public class VisioService : IVisioService
{
    public bool CloseDocument(string fullName)
    {
        bool closed;
        var currentDocument = Globals.ThisAddIn.Application.Documents.OfType<Document>()
            .SingleOrDefault(x => x.FullName == fullName);
        if (currentDocument == null)
        {
            closed = false;
        }
        else
        {
            currentDocument.Close();
            closed = true;
        }

        return closed;
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

    public int? Page => Globals.ThisAddIn.Application.ActivePage?.ID;

    public IObservableCache<IMaster, string> Masters => Globals.ThisAddIn.Application.ActiveDocument
        .ToMasterObservableChangeSet().AsObservableCache();

    public IObservableCache<FunctionLocation, CompositeId> FunctionLocations =>
        Globals.ThisAddIn.Application.ActiveDocument.ToFunctionLocationObservableChangeSet().AsObservableCache();

    public IObservableCache<MaterialLocation, CompositeId> MaterialLocations => Globals.ThisAddIn.Application
        .ActiveDocument
        .ToMaterialLocationObservableChangeSet().AsObservableCache();

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

    public void InsertAsExcelSheet(string[,] dataArray)
    {
        // todo
        var oleShape = Globals.ThisAddIn.Application.ActivePage.InsertObject("Excel.Sheet",
            (short)VisInsertObjArgs.visInsertAsEmbed);
        object oleObject = oleShape.Object;
        var workbook = (Workbook)oleObject;

        // 操作Excel对象
        Worksheet worksheet = workbook.Worksheets[1];
        worksheet.Range["A1"].Resize[dataArray.GetLength(0), dataArray.GetLength(1)].Value = dataArray;
        worksheet.Columns.AutoFit();

        // 保存并关闭Excel工作簿
        workbook.Save();
        workbook.Close(false); // 关闭工作簿，但不保存改变
        Marshal.ReleaseComObject(worksheet);
        Marshal.ReleaseComObject(workbook);
    }

    public void OpenDocument(string fullName)
    {
        Globals.ThisAddIn.Application.Documents.OpenEx(fullName,
            (short)VisOpenSaveArgs.visOpenDocked);
    }
}
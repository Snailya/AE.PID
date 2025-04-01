using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Client.Infrastructure;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public class VisioDocumentService(Document document, IScheduler scheduler) : IDisposable
{
    public void Dispose()
    {
        // TODO release managed resources here
    }

    public Document GetDocument()
    {
        return document;
    }

    public Shape GetShape(VisioShapeId id)
    {
        return document.Pages.ItemFromID[id.PageId].Shapes.ItemFromID[id.ShapeId];
    }

    public void Select(ICompoundKey[] ids)
    {
        var shapeIds = new List<int>();

        foreach (var id in ids)
            switch (id)
            {
                case VisioShapeId shapeId:
                    shapeIds.Add(shapeId.ShapeId);
                    break;
                case VisioMasterId masterId:
                {
                    var master = Globals.ThisAddIn.Application.ActiveDocument.Masters[$"B{masterId.BaseId}"];
                    Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeByMaster,
                        VisSelectMode.visSelModeSkipSuper, master).GetIDs(out var shapeIdsPerMaster);
                    shapeIds.AddRange(shapeIdsPerMaster.OfType<int>());
                    break;
                }
            }

        SelectAndCenterView(shapeIds.ToArray());
    }

    private void SelectAndCenterView(int[] shapeIds)
    {
        var selection = document.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        foreach (var id in shapeIds)
            selection.Select(document.Application.ActivePage.Shapes.ItemFromID[id],
                (short)VisSelectArgs.visSelect);
        document.Application.ActiveWindow.Selection = selection;
    }

    public void UpdateProperties(IEnumerable<PropertyPatch> properties)
    {
        scheduler.Schedule(() =>
        {
            foreach (var property in properties)
                switch (property.Target)
                {
                    case VisioMasterId visioMasterId:

                        break;
                    case VisioShapeId visioShapeId:
                        if (visioShapeId.PageId >= 0 && visioShapeId.ShapeId != 0) // skip the virtual one
                        {
                            var shape = GetShape(visioShapeId);
                            shape.TrySetValue(property.Name, property.Value, property.CreateIfNotExists,
                                property.LabelFormula);
                        }

                        // todo: handle the virtual item change
                        break;
                    case VisioPageId visioPageId:
                        var page = GetPage(visioPageId);
                        page.PageSheet.TrySetValue(property.Name, property.Value, property.CreateIfNotExists,
                            property.LabelFormula);
                        break;
                    case VisioDocumentId visioDocument:
                        if (document.ID == visioDocument.ComputedId)
                            document.DocumentSheet.TrySetValue(property.Name, property.Value,
                                property.CreateIfNotExists);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        });
    }

    public ICompoundKey[] GetAdjacent(ICompoundKey compositeId)
    {
        if (compositeId is VisioShapeId visioShapeId)
            return document.Pages.ItemFromID[visioShapeId.PageId].Shapes.ItemFromID[visioShapeId.ShapeId]
                .ConnectedShapes(VisConnectedShapesFlags.visConnectedShapesAllNodes, "").OfType<int>()
                .Select(x => new VisioShapeId(visioShapeId.PageId, x)).OfType<ICompoundKey>().ToArray();

        throw new ArgumentException();
    }


    private Page GetPage(VisioPageId visioPageId)
    {
        return document.Pages.ItemFromID[visioPageId.ComputedId];
    }
}
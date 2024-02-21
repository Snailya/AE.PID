using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AE.PID.Models;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

public abstract class LinkedControlManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static string _libraryPath = string.Empty;
    private static readonly BehaviorSubject<Position> PositionSubject = new(new Position(0, 0));

    public const string FunctionalElementBaseId = "{B28A5C75-E7CB-4700-A060-1A6D0A777A94}";

    public static List<int> PreviousCopy { get; set; }
    public const string LinkedShapePropertyName = "User.LinkedShapeID";

    /// <summary>
    /// Listen on right click event to cache the paste location.
    /// </summary>
    /// <returns></returns>
    public static IDisposable Listen()
    {
        return
            Observable.FromEvent<EApplication_MouseDownEventHandler, Tuple<int, int, double, double, bool>>(
                    rxHandler => (int button, int keyButtonState, double x, double y, ref bool cancelDefault) =>
                        rxHandler(Tuple.Create(button, keyButtonState, x, y, cancelDefault)),
                    handler => Globals.ThisAddIn.Application.MouseDown += handler,
                    handler => Globals.ThisAddIn.Application.MouseDown -= handler)
                .Where(args => args.Item1 == 2)
                .Select(args => new Position(args.Item3, args.Item4))
                .Subscribe(position => { PositionSubject.OnNext(position); },
                    ex =>
                    {
                        Logger.Error(ex,
                            $"Linked Control Manager ternimated accidently, you may only use limited feature from Linked Control Manager");
                    },
                    () => { Logger.Error("Document Update Service should never complete."); });
    }

    public static void InsertFunctionalElement(IVShape shape)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Insert Functional Element");

        var document =
            Globals.ThisAddIn.Application.Documents.OpenEx(GetLibraryPath(), (short)VisOpenSaveArgs.visOpenDocked);
        var master = document.Masters[$"B{FunctionalElementBaseId}"];

        // compute position
        var xPos = shape.CellsU["PinX"].ResultIU;
        var yPos = shape.CellsU["PinY"].ResultIU;

        // add shape
        var dropped = Globals.ThisAddIn.Application.ActivePage.Drop(master, xPos, yPos);

        // update shape property
        dropped.CellsU[LinkedShapePropertyName].FormulaU = shape.ID.ToString();

        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    public static bool CanInsert(Selection selection)
    {
        // verify is single selected
        if (selection.Count != 1) return false;

        // verify is shape on page selected not editing master
        var selected = selection[1];
        if (selected.ContainingPage == null) return false;

        if (selected.Master?.BaseID == FunctionalElementBaseId) return false;

        // verify if source exists
        if (Globals.ThisAddIn.Configuration.LibraryConfiguration.GetItems()
            .All(x => x.BaseId != FunctionalElementBaseId)) return false;

        // verify is this action valid
        return selected.Master != null && Globals.ThisAddIn.Configuration.LibraryConfiguration.GetItems()
            .Any(x => x.BaseId == selected.Master.BaseID);
    }

    public static void HighlightPrimary(IVShape shape)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Select Primary");

        var linkedShapeId = (int)shape.CellsU[LinkedShapePropertyName].ResultIU;
        ShapeSelector.SelectShapeById(linkedShapeId);

        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    public static bool CanHighlightPrimary(Selection selection)
    {
        // verify is single selected
        if (selection.Count != 1) return false;

        // verify is shape on page selected not editing master
        var selected = selection[1];
        if (selected.ContainingPage == null) return false;

        return selected.CellExists[LinkedShapePropertyName, (short)VisExistsFlags.visExistsLocally] ==
               (short)VBABool.True;
    }

    public static void PasteToLocation()
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Paste With Linked Items");

        var location = PositionSubject.Value;

        // after duplicate the items copied by the user with the functional elements related, we need to update the duplicated related items' linked shape id property.
        // so we need to know the one to one relationship between the source item and items after pasting.
        // as visio automatically creates a selection for targets after pasting with the same order of the source, we could cache which item in the source list is the primary item of other items.
        // that is if the first item is a primary item, and the second and the third are the related items, we could store the first item's primary item is the -1 order of the source list, and the second and third is the first item of the source list
        var primaryItemIndexes = new List<int>();

        // build up a new selection with linked shape
        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        for (var i = 0; i < PreviousCopy.Count; i++)
        {
            var copiedShapeId = PreviousCopy[i];
            var copiedShape = Globals.ThisAddIn.Application.ActivePage.Shapes.ItemFromID[copiedShapeId];

            // add the shape to selection
            selection.Select(copiedShape, (short)VisSelectArgs.visSelect);

            // if the shape is an equipment or a simple shape, it should has no primary item
            // if the shape is a functional element but it is orphan it should has no primary too
            if (copiedShape.CellExists[LinkedShapePropertyName, (short)VisExistsFlags.visExistsLocally] ==
                (short)VBABool.True)
            {
                var primaryItemId = copiedShape.Cells[LinkedShapePropertyName].ResultInt[VisUnitCodes.visNumber, 0];
                var indexOfPrimary = PreviousCopy.IndexOf(primaryItemId);
                primaryItemIndexes.Add(indexOfPrimary);
            }
            else
            {
                primaryItemIndexes.Add(Constants.NoPrimaryItemMagicIndex);
            }

            // if the item is already a functional element, no need to find the related item by looping the shapes of the page
            if (copiedShape.Master?.BaseID == FunctionalElementBaseId) continue;

            // try find out the related functional elements by checking the User.LinkedShapeID property
            var linkedElements = Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<IVShape>()
                .Where(IsLinkedPredicate(copiedShapeId))
                .ToList();
            if (linkedElements.Count == 0) continue;

            // append the related items to selection if it has not been selected yet.
            // cache their primary item index in the selection
            foreach (var item in from item in linkedElements
                     where !PreviousCopy.Contains(item.ID)
                     select item)
            {
                selection.Select((Shape)item, (short)VisSelectArgs.visSelect);
                primaryItemIndexes.Add(i);
            }
        }

        // perform copy and paste to specified location. the location is observed by the right click event, as no better approach i've known yet.
        Globals.ThisAddIn.Application.ActiveWindow.Selection =
            selection; // pay attention that we must assign the selection create to active window selection firstly, otherwise it would paste nothing and this is intuitive.
        var originLocation =
            new Position(Globals.ThisAddIn.Application.ActiveWindow.Selection.PrimaryItem.Cells["PinX"].ResultIU,
                Globals.ThisAddIn.Application.ActiveWindow.Selection.PrimaryItem.Cells["PinY"].ResultIU);
        selection.Duplicate();
        Globals.ThisAddIn.Application.ActiveWindow.Selection.Move(location.X - originLocation.X,
            location.Y - originLocation.Y);

        Logger.Debug(
            $"Pasted {Globals.ThisAddIn.Application.ActiveWindow.Selection.Count} elements: {string.Join(", ", Globals.ThisAddIn.Application.ActiveWindow.Selection.OfType<IVShape>().Select(x => x.Name))}");

        // update the linked shape id
        for (var i = 0; i < Globals.ThisAddIn.Application.ActiveWindow.Selection.Count; i++)
        {
            if (primaryItemIndexes[i] == -1) continue;

            var shape = Globals.ThisAddIn.Application.ActiveWindow
                .Selection[i + 1]; // pay attention to that the selection index starts with 1

            // get the target id
            var linkedId =
                Globals.ThisAddIn.Application.ActiveWindow.Selection[primaryItemIndexes[i] + 1]
                    .ID; // pay attention to that the selection index starts with 1
            shape.Cells[LinkedShapePropertyName].FormulaU = linkedId.ToString();

            Logger.Debug($"Update {shape.Name}'s linked shape id to {linkedId}");
        }

        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    public static bool CanPaste()
    {
        return PreviousCopy is { Count: > 0 };
    }

    public static void HighlightLinked(IVShape shape)
    {
        var undoScope = Globals.ThisAddIn.Application.BeginUndoScope("Select Linked");

        var selection = Globals.ThisAddIn.Application.ActivePage.CreateSelection(VisSelectionTypes.visSelTypeEmpty);
        selection.Select((Shape)shape, (short)VisSelectArgs.visSelect);

        foreach (var linked in Globals.ThisAddIn.Application.ActivePage.Shapes.OfType<IVShape>()
                     .Where(IsLinkedPredicate(shape.ID)))
            selection.Select((Shape)linked, (short)VisSelectArgs.visSelect);

        Globals.ThisAddIn.Application.ActiveWindow.Selection = selection;

        Globals.ThisAddIn.Application.EndUndoScope(undoScope, true);
    }

    public static bool CanHighlightLinked(Selection selection)
    {
        // verify is not opened master
        if (selection.ContainingPage == null) return false;

        // verify is single selected
        if (selection.Count != 1) return false;

        // verify if there is linked elements
        var selected = selection[1];
        return selected.ContainingPage.Shapes.OfType<IVShape>().Any(IsLinkedPredicate(selected.ID));
    }

    private static string GetLibraryPath()
    {
        if (!string.IsNullOrEmpty(_libraryPath)) return _libraryPath;

        var library = Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Single(x =>
            x.Items.Any(i => i.BaseId == FunctionalElementBaseId));
        _libraryPath = library.Path;

        return _libraryPath;
    }

    private static Func<IVShape, bool> IsLinkedPredicate(int primaryId)
    {
        return x =>
            x.CellExists[LinkedShapePropertyName, (short)VisExistsFlags.visExistsLocally] == (short)VBABool.True &&
            x.CellsU[LinkedShapePropertyName].ResultInt[VisUnitCodes.visNumber, 0] == primaryId;
    }
}
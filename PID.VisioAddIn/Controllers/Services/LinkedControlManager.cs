using System.Linq;
using AE.PID.Models;
using AE.PID.Models.Exceptions;
using Microsoft.Office.Interop.Visio;
using NLog;

namespace AE.PID.Controllers.Services;

public abstract class LinkedControlManager
{
    public const string MasterBaseId = "{B28A5C75-E7CB-4700-A060-1A6D0A777A94}";
    public const string LinkedShapePropertyName = "User.LinkedShapeID";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static string _libraryPath = string.Empty;

    public static void AddControl(IVShape shape)
    {
        var document =
            Globals.ThisAddIn.Application.Documents.OpenEx(GetLibraryPath(), (short)VisOpenSaveArgs.visOpenDocked);
        var master = document.Masters[$"B{MasterBaseId}"];

        // compute position
        var xPos = shape.CellsU["PinX"].ResultIU;
        var yPos = shape.CellsU["PinY"].ResultIU;

        // add shape
        var dropped = Globals.ThisAddIn.Application.ActivePage.Drop(master, xPos, yPos);

        // update shape property
        dropped.CellsU["User.LinkedShapeID"].FormulaU = shape.ID.ToString();
    }

    public static bool CanAddControl(Selection selection)
    {
        // verify is single selected
        if (selection.Count != 1) return false;

        // verify is shape on page selected not editing master
        var selected = selection[1];
        if (selected.ContainingPage == null) return false;

        if (selected.Master?.BaseID == MasterBaseId) return false;

        // verify if source exists
        if (Globals.ThisAddIn.Configuration.LibraryConfiguration.GetItems()
            .All(x => x.BaseId != MasterBaseId)) return false;

        // verify is this action valid
        return selected.Master != null && Globals.ThisAddIn.Configuration.LibraryConfiguration.GetItems()
            .Any(x => x.BaseId == selected.Master.BaseID);
    }

    public static void Highlight(IVShape shape)
    {
        var linkedShapeId = (int)shape.CellsU[LinkedShapePropertyName].ResultIU;
        ShapeSelector.SelectShapeById(linkedShapeId);
    }

    public static bool CanHighlight(Selection selection)
    {
        // verify is single selected
        if (selection.Count != 1) return false;

        // verify is shape on page selected not editing master
        var selected = selection[1];
        if (selected.ContainingPage == null) return false;

        return selected.CellExists[LinkedShapePropertyName, (short)VisExistsFlags.visExistsLocally] ==
               (short)VBABool.True;
    }

    private static string GetLibraryPath()
    {
        if (!string.IsNullOrEmpty(_libraryPath)) return _libraryPath;

        var library = Globals.ThisAddIn.Configuration.LibraryConfiguration.Libraries.Single(x =>
            x.Items.Any(i => i.BaseId == MasterBaseId));
        _libraryPath = library.Path;

        return _libraryPath;
    }
}
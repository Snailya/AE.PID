using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using AE.PID.Core;
using Microsoft.Office.Core;

namespace AE.PID.Client.VisioAddIn;

internal sealed class PasteShapeDataCommand : RibbonCommandBase
{
    public override string Id { get; } = nameof(PasteShapeDataCommand);

    public override void Execute(IRibbonControl control)
    {
        var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection[1]!;

        const string format = "Visio 15.0 Shapes";

        var memoryStream = (MemoryStream)Clipboard.GetDataObject()!.GetData(format);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var package = Package.Open(memoryStream, FileMode.Open, FileAccess.Read);
        var uri = new Uri("/visio/pages/page1.xml", UriKind.Relative);
        var page1Part = package.GetPart(uri);

        using var partStream = page1Part.GetStream();
        var partXml = XDocument.Load(partStream);
        XNamespace mainNs = @"http://schemas.microsoft.com/office/visio/2012/main";

        foreach (var cell in (partXml.Root.Element(XNames.ShapesElement).Element(XNames.ShapeElement)
                     .Elements(XNames.SectionElement)
                     .SingleOrDefault(i => i.Attribute(XNames.NAttribute)!.Value == "Property")?
                     .Elements(XNames.RowElement)
                     .Where(i => i.Attribute(XNames.NAttribute)?.Value != "FunctionalElement" &&
                                 i.Attribute(XNames.NAttribute)?.Value != "Class" &&
                                 i.Attribute(XNames.NAttribute)?.Value != "SubClass")
                     .Select(x =>
                         x.Elements(XNames.CellElement)
                             .SingleOrDefault(i => i.Attribute(XNames.NAttribute)?.Value == "Value"))
                     .Where(x => x != null))
                 .Where(x => !string.IsNullOrWhiteSpace(x.Attribute("V")?.Value)))
        {
            var name = $"Prop.{cell!.Parent!.Attribute(XNames.NAttribute)!.Value}";
            var value = cell.Attribute(XNames.VAttribute)!.Value;

            selection.TrySetValue(name, value);
        }
    }

    public override bool CanExecute(IRibbonControl control)
    {
        if (!IsPageWindow() || !IsSingleSelection()) return false;

        // if it is a visio shape in clipboard
        const string format = "Visio 15.0 Shapes";
        var clipboardData = Clipboard.GetDataObject();
        if (clipboardData == null) return false;
        if (!clipboardData.GetDataPresent(format)) return false;

        // check if there is only one shape copied
        var memoryStream = (MemoryStream)clipboardData.GetData(format);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var package = Package.Open(memoryStream, FileMode.Open, FileAccess.Read);
        var uri = new Uri("/visio/pages/page1.xml", UriKind.Relative);
        var page1Part = package.GetPart(uri);

        using var partStream = page1Part.GetStream();
        var partXml = XDocument.Load(partStream);
        XNamespace mainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
        return partXml.Root?.Element(XNames.ShapesElement)?.Elements(XNames.ShapeElement).Count() == 1;
    }

    public override bool GetVisible(IRibbonControl control)
    {
        return CanExecute(control);
    }

    public override string GetLabel(IRibbonControl control)
    {
        return "粘贴属性";
    }
}
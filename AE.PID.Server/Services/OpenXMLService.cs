using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Server.Data;

namespace AE.PID.Server.Services;

public abstract class OpenXmlService
{
    private static XElement ToXElement(PackagePart part)
    {
        using var partXmlReader = XmlReader.Create(part.GetStream());
        return XElement.Load(partXmlReader);
    }


    /// <summary>
    ///     Get the ids of item of the library from OpenXML file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static IEnumerable<LibraryItem> GetItems(string filePath)
    {
        using var package = Package.Open(filePath, FileMode.Open, FileAccess.Read);
        var mastersPart = VisioXmlWrapper.GetMastersPart(package);

        var styles = VisioXmlWrapper.GetStyles(package).ToList();

        // Loop through masters part to get
        var root = ToXElement(mastersPart);

        return (from masterElement in root.Elements()
            let masterDocument =
                XmlHelper.GetDocumentFromPart(
                    VisioXmlWrapper.GetMasterPartByMasterId(package, int.Parse(masterElement.Attribute("ID").Value)))
            let shapeElement = masterDocument.XPathSelectElement("/main:MasterContents/main:Shapes/main:Shape",
                VisioXmlWrapper.NamespaceManager)
            select new LibraryItem
            {
                BaseId = masterElement.Attribute("BaseID")!.Value,
                UniqueId = masterElement.Attribute("UniqueID")!.Value,
                Name = masterElement.Attribute("Name")!.Value,
                LibraryVersionItemXML = new LibraryVersionItemXML
                {
                    LineStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("LineStyle").Value))
                        .Name,
                    FillStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("FillStyle").Value))
                        .Name,
                    TextStyleName = styles.Single(x => x.Id == int.Parse(shapeElement.Attribute("TextStyle").Value))
                        .Name,
                    MasterElement = masterElement.ToString(SaveOptions.DisableFormatting),
                    MasterDocument = masterDocument.ToString(SaveOptions.DisableFormatting)
                }
            }).ToList();
    }
}
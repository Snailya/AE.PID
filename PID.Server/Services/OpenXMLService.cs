using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using AE.PID.Server.Data;

namespace AE.PID.Server.Services;

public class OpenXmlService
{
    private const string DocumentType = @"http://schemas.microsoft.com/visio/2010/relationships/document";
    private const string MastersType = @"http://schemas.microsoft.com/visio/2010/relationships/masters";
    private static readonly XNamespace M = @"http://schemas.microsoft.com/office/visio/2012/main";
    private static readonly XNamespace R = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";

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
    public static IEnumerable<LibraryItemEntity> GetItems(string filePath)
    {
        var items = new List<LibraryItemEntity>();

        using var package = Package.Open(filePath, FileMode.Open, FileAccess.Read);
        var mastersPart = GetMastersPart(package);

        // Loop through masters part to get
        var root = ToXElement(mastersPart);
        foreach (var masterElement in root.Elements())
        {
            var item = new LibraryItemEntity
            {
                Name = masterElement.Attribute("Name")?.Value ?? string.Empty,
                BaseId = masterElement.Attribute("BaseID")?.Value ?? string.Empty,
                UniqueId = masterElement.Attribute("UniqueID")?.Value ?? string.Empty
            };
            items.Add(item);
        }

        return items;
    }

    private static PackagePart GetMastersPart(Package package)
    {
        // get document relationship part
        var documentPartRelationship = package.GetRelationshipsByType(DocumentType).FirstOrDefault();
        if (documentPartRelationship == null) throw new Exception($"Document part not found by type: {DocumentType}");

        var documentPart = package.GetPart(PackUriHelper.ResolvePartUri(documentPartRelationship.SourceUri,
            documentPartRelationship.TargetUri));
        if (documentPart == null)
            throw new Exception($"Unable to get masters part through partUri: {documentPartRelationship}");

        // get masters part
        var mastersPartRelationship = documentPart
            .GetRelationshipsByType(MastersType)
            .FirstOrDefault();
        if (mastersPartRelationship == null) throw new Exception($"Masters part not found by type: {MastersType}");

        var mastersPart = package.GetPart(PackUriHelper.ResolvePartUri(mastersPartRelationship.SourceUri,
            mastersPartRelationship.TargetUri));
        if (mastersPart == null)
            throw new Exception($"Unable to get masters part through partUri: {mastersPartRelationship}");
        return mastersPart;
    }
}
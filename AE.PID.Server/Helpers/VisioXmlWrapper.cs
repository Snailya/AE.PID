using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Server.Models;

namespace AE.PID.Server.Services;

public abstract class VisioXmlWrapper
{
    public const string DocumentRel = @"http://schemas.microsoft.com/visio/2010/relationships/document";
    public const string MastersRel = @"http://schemas.microsoft.com/visio/2010/relationships/masters";
    public const string MasterRel = @"http://schemas.microsoft.com/visio/2010/relationships/master";
    public static readonly XmlNamespaceManager NamespaceManager = new(new NameTable());

    public static readonly Uri DocumentPartUri = new("/visio/document.xml", UriKind.Relative);
    public static readonly Uri MastersPartUri = new("/visio/masters/masters.xml", UriKind.Relative);
    public static readonly Uri PagesPartUri = new("/visio/pages/pages.xml", UriKind.Relative);

    public static readonly XNamespace MainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
    public static readonly XNamespace RelNs = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    static VisioXmlWrapper()
    {
        NamespaceManager.AddNamespace("main", MainNs.NamespaceName);
        NamespaceManager.AddNamespace("rel", RelNs.NamespaceName);
    }

    /// <summary>
    ///     Get /visio/document.xml.
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    public static PackagePart GetDocumentPart(Package package)
    {
        return package.GetPart(DocumentPartUri);
    }

    /// <summary>
    ///     Get /visio/masters/masters.xml.
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static PackagePart GetMastersPart(Package package)
    {
        // get document relationship part
        var documentPartRelationship = package.GetRelationshipsByType(DocumentRel).FirstOrDefault();
        if (documentPartRelationship == null) throw new Exception($"Document part not found by type: {DocumentRel}");

        var documentPart = package.GetPart(PackUriHelper.ResolvePartUri(documentPartRelationship.SourceUri,
            documentPartRelationship.TargetUri));
        if (documentPart == null)
            throw new Exception($"Unable to get masters part through partUri: {documentPartRelationship}");

        // get masters part
        var mastersPartRelationship = documentPart
            .GetRelationshipsByType(MastersRel)
            .FirstOrDefault();
        if (mastersPartRelationship == null) throw new Exception($"Masters part not found by type: {MastersRel}");

        var mastersPart = package.GetPart(PackUriHelper.ResolvePartUri(mastersPartRelationship.SourceUri,
            mastersPartRelationship.TargetUri));
        if (mastersPart == null)
            throw new Exception($"Unable to get masters part through partUri: {mastersPartRelationship}");
        return mastersPart;
    }

    public static IEnumerable<PackagePart> GetMasterPartCollection(Package package)
    {
        var mastersPart = GetMastersPart(package);
        var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);
        return mastersPart.GetRelationshipsByType(RelType.Master)
            .Select(x => x.Package.GetPart(PackUriHelper.ResolvePartUri(x.SourceUri, x.TargetUri)));
    }

    /// <summary>
    ///     Get /visio/masters/master{i}.xml by master id.
    /// </summary>
    /// <param name="package"></param>
    /// <param name="id">the id property of the </param>
    /// <returns></returns>
    public static PackagePart? GetMasterPartByMasterId(Package package, int id)
    {
        var mastersPart = GetMastersPart(package);
        var mastersDocument = XmlHelper.GetDocumentFromPart(mastersPart);

        var relElement = mastersDocument.XPathSelectElement($"//main:Master[@ID='{id}']//main:Rel", NamespaceManager);
        if (relElement == null) return null;

        var relId = GetRelId(relElement)!;
        return GetRelPart(mastersPart, relId);
    }

    /// <summary>
    ///     Get /visio/pages/pages.xml.
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    public static PackagePart GetPagesPart(Package package)
    {
        return package.GetPart(PagesPartUri);
    }

    /// <summary>
    ///     Get related part by relationship id.
    /// </summary>
    /// <param name="part"></param>
    /// <param name="relId"></param>
    /// <returns></returns>
    public static PackagePart GetRelPart(PackagePart part, string relId)
    {
        var rel = part.GetRelationship(relId);
        return part.Package.GetPart(PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri));
    }

    public static XDocument GetRelDocument(PackagePart part, string relId)
    {
        var relPart = GetRelPart(part, relId);
        return XmlHelper.GetDocumentFromPart(relPart);
    }

    public static string? GetRelId(XElement element)
    {
        return element.Name.LocalName == "Rel"
            ? element.Attribute(RelNs + "id")?.Value
            : element.Descendants(MainNs + "Rel").FirstOrDefault()?.Attribute(RelNs + "id")?.Value;
    }


    /// <summary>
    ///     Get style tables from package
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    public static IEnumerable<VisioStyle> GetStyles(Package package)
    {
        var documentPart = GetDocumentPart(package);
        var documentDocument = XmlHelper.GetDocumentFromPart(documentPart);

        return (from styleSheetElement in documentDocument.XPathSelectElements("//main:StyleSheet", NamespaceManager)
            select new VisioStyle
            {
                Id = int.Parse(styleSheetElement.Attribute("ID")!.Value),
                Name = styleSheetElement.Attribute("Name")!.Value
            }).ToList();
    }

    public class RelType
    {
        public const string Master = "http://schemas.microsoft.com/visio/2010/relationships/master";
    }
}
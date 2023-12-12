using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AE.PID.Controllers.Services;

public abstract class XmlHelper
{
    private const string DocumentRel = @"http://schemas.microsoft.com/visio/2010/relationships/document";
    private const string MastersRel = @"http://schemas.microsoft.com/visio/2010/relationships/masters";
    public const string MasterRelationship = @"http://schemas.microsoft.com/visio/2010/relationships/master";
    public static readonly Uri MastersPartUri = new("/visio/masters/masters.xml", UriKind.Relative);
    public static readonly XNamespace MainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
    public static readonly XNamespace RelNs = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static XDocument GetXmlFromPart(PackagePart packagePart)
    {
        XDocument partXml = null;
        // Open the packagePart as a stream and then 
        // open the stream in an XDocument object.
        using var partStream = packagePart.GetStream();
        partXml = XDocument.Load(partStream);
        return partXml;
    }

    public static IEnumerable<PackagePart> GetPackageParts(Package filePackage,
        PackagePart sourcePart, string relationship)
    {
        // This gets only the first PackagePart that shares the relationship
        // with the PackagePart passed in as an argument. You can modify the code
        // here to return a different PackageRelationship from the collection.
        var packageRel = sourcePart.GetRelationshipsByType(relationship);
        if (packageRel.Any())
            return packageRel.Select(x => filePackage.GetPart(PackUriHelper.ResolvePartUri(
                sourcePart.Uri, x.TargetUri)));

        return null;
    }

    public static IEnumerable<XElement> GetXElementsByName(
        XDocument packagePart, string elementType)
    {
        // Construct a LINQ query that selects elements by their element type.
        var elements =
            from element in packagePart.Descendants()
            where element.Name.LocalName == elementType
            select element;
        // Return the selected elements to the calling code.
        return elements.DefaultIfEmpty(null);
    }

    public static Package OpenRead(string filePath)
    {
        Package visioPackage = null;

        if (Directory.Exists(filePath))
            // Open the Visio file as a package with
            // read/write file access.
            visioPackage = Package.Open(
                filePath,
                FileMode.Open,
                FileAccess.Read);
        // Return the Visio file as a package.
        return visioPackage;
    }

    public static PackagePart GetMastersPart(Package package)
    {
        // get document relationship part
        var documentPartRelationship = package.GetRelationshipsByType(DocumentRel).FirstOrDefault();
        if (documentPartRelationship == null) throw new Exception($"Doucment part not found by type: {DocumentRel}");

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

    public static void SaveXDocumentToPart(PackagePart packagePart,
        XDocument partXml)
    {
        // Create a new XmlWriterSettings object to 
        // define the characteristics for the XmlWriter
        var partWriterSettings = new XmlWriterSettings();
        partWriterSettings.Encoding = Encoding.UTF8;
        // Create a new XmlWriter and then write the XML
        // back to the document part.
        var partWriter = XmlWriter.Create(packagePart.GetStream(),
            partWriterSettings);
        partXml.WriteTo(partWriter);
        // Flush and close the XmlWriter.
        partWriter.Flush();
        partWriter.Close();
    }
}
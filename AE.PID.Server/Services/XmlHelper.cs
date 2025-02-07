using System.IO.Packaging;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AE.PID.Server;

public abstract class XmlHelper
{
    public static XDocument GetDocumentFromPart(PackagePart packagePart)
    {
        // Open the packagePart as a stream and then 
        // open the stream in an XDocument object.
        using var partStream = packagePart.GetStream();
        var partXml = XDocument.Load(partStream);
        return partXml;
    }

    public static PackagePart? GetPackagePart(Package filePackage,
        string relationship)
    {
        // Use the namespace that describes the relationship 
        // to get the relationship.
        var packageRel =
            filePackage.GetRelationshipsByType(relationship).FirstOrDefault();
        PackagePart? part = null;
        // If the Visio file package contains this type of relationship with 
        // one of its parts, return that part.
        if (packageRel != null)
        {
            // Clean up the URI using a helper class and then get the part.
            var docUri = PackUriHelper.ResolvePartUri(
                new Uri("/", UriKind.Relative), packageRel.TargetUri);
            part = filePackage.GetPart(docUri);
        }

        return part;
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

        // Members that return a sequence should never return null. Return an empty sequence instead
        return [];
    }

    public static XElement? GetXElementByAttribute(IEnumerable<XElement> elements,
        string attributeName, string attributeValue)
    {
        // Construct a LINQ query that selects elements from a group
        // of elements by the value of a specific attribute.
        var selectedElements =
            from el in elements
            where el.Attribute(attributeName).Value == attributeValue
            select el;
        // If there aren't any elements of the specified type
        // with the specified attribute value in the document,
        // return null to the calling code.
        return selectedElements.DefaultIfEmpty(null).FirstOrDefault();
    }

    public static IEnumerable<XElement?> GetXElementsByName(
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

    public static Package? OpenRead(string filePath)
    {
        Package? visioPackage = null;

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


    public static void SaveXDocumentToPart(PackagePart packagePart,
        XDocument partXml)
    {
        // Create a new XmlWriterSettings object to 
        // define the characteristics for the XmlWriter
        var partWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
        };

        // reset the stream length to 0 to clear the stream content, otherwise if might be remained content after overwrite when the new xdocument is longer than the previous
        using var stream = packagePart.GetStream();
        stream.SetLength(0);

        // Create a new XmlWriter and then write the XML
        // back to the document part.
        var partWriter = XmlWriter.Create(stream,
            partWriterSettings);
        partXml.WriteTo(partWriter);
        // Flush and close the XmlWriter.
        partWriter.Flush();
        partWriter.Close();
    }

    public static int CheckForRecalculate(XDocument customPropsXDoc)
    {
        // Set the initial pidValue to -1, which is not an allowed value.
        // The calling code tests to see whether the pidValue is 
        // greater than -1.
        var pidValue = -1;
        // Get all of the property elements from the document. 
        var props = GetXElementsByName(
            customPropsXDoc, "property");
        // Get the RecalcDocument property from the document if it exists already.
        var recalculateProp = GetXElementByAttribute(props,
            "name", "RecalcDocument");
        // If there is already a RecalcDocument instruction in the 
        // Custom File Properties part, then we don't need to add another one. 
        // Otherwise, we need to create a unique pid value.
        if (recalculateProp != null) return pidValue;

        // Get all of the pid values of the property elements and then
        // convert the IEnumerable object into an array.
        var propIDs =
            from prop in props
            where prop.Name.LocalName == "property"
            select prop.Attribute("pid").Value;
        var propIdArray = propIDs.ToArray();
        // Increment this id value until a unique value is found.
        // This starts at 2, because 0 and 1 are not valid pid values.
        var id = 2;
        while (pidValue == -1)
            if (propIdArray.Contains(id.ToString()))
                id++;
            else
                pidValue = id;

        return pidValue;
    }

    public static void CreateNewPackagePart(Package filePackage,
        XDocument partXml, Uri packageLocation, string contentType, Uri relationshipLocation,
        string relationship)
    {
        PackagePart? part = null;
        // Need to check first to see whether the part exists already.
        if (!filePackage.PartExists(packageLocation))
        {
            // Create a new blank package part at the specified URI 
            // of the specified content type.
            part = filePackage.CreatePart(packageLocation,
                contentType);

            // Add a relationship from the file package to this
            // package part. You can also create relationships
            // between an existing package part and a new part.
            filePackage.CreateRelationship(relationshipLocation,
                TargetMode.Internal,
                relationship);
        }

        else
        {
            part = filePackage.GetPart(packageLocation);
        }

        // flush the stream
        using var partStream = part.GetStream(FileMode.OpenOrCreate,
            FileAccess.ReadWrite);
        partXml.Save(partStream);
    }

    public static void RecalculateDocument(Package filePackage)
    {
        // Get the Custom File Properties part from the package 
        // and then extract the XML from it.
        var customPart = GetPackagePart(filePackage,
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/" +
            "custom-properties");
        var customPartXML = GetDocumentFromPart(customPart);
        // Check to see whether document recalculation has already been 
        // set for this document. If it hasn't, use the integer
        // value returned by CheckForRecalc as the property ID.
        var pidValue = CheckForRecalculate(customPartXML);
        if (pidValue > -1)
        {
            var customPartRoot = customPartXML.Elements().ElementAt(0);
            // Two XML namespaces are needed to add XML data to this 
            // document. Here, we're using the GetNamespaceOfPrefix and 
            // GetDefaultNamespace methods to get the namespaces that 
            // we need. You can specify the exact strings for the 
            // namespaces, but that is not recommended.
            var customVTypesNS = customPartRoot.GetNamespaceOfPrefix("vt");
            var customPropsSchemaNS = customPartRoot.GetDefaultNamespace();
            // Construct the XML for the new property in the XDocument.Add method.
            // This ensures that the XNamespace objects will resolve properly, 
            // apply the correct prefix, and will not default to an empty namespace.
            customPartRoot.Add(
                new XElement(customPropsSchemaNS + "property",
                    new XAttribute("pid", pidValue.ToString()),
                    new XAttribute("name", "RecalcDocument"),
                    new XAttribute("fmtid",
                        "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}"),
                    new XElement(customVTypesNS + "bool", "true")
                ));
        }

        // Save the Custom Properties package part back to the package.
        SaveXDocumentToPart(customPart, customPartXML);
    }

}
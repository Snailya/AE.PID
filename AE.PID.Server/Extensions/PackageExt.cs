using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AE.PID.Server.Extensions;

public static class PackageExt
{
    public static XDocument GetDocumentFromPart(this PackagePart packagePart)
    {
        // Open the packagePart as a stream and then 
        // open the stream in an XDocument object.
        using var partStream = packagePart.GetStream();
        var partXml = XDocument.Load(partStream);
        return partXml;
    }

    /// <summary>
    ///     Overwrite the <see cref="XDocument" /> to the <see cref="PackagePart" />.
    /// </summary>
    /// <param name="packagePart"></param>
    /// <param name="xDocument"></param>
    public static void FlushXDocument(this PackagePart packagePart, XDocument xDocument)
    {
        using var stream = packagePart.GetStream();
        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true }))
        {
            xDocument.Save(xmlWriter);
        }

        // 将字符串写入 PackagePart
        using (var streamWriter = new StreamWriter(stream))
        {
            streamWriter.Write(stringWriter.ToString());
        }
    }

    public static XDocument ToXDocument<T>(this T data)
    {
        var serializer = new XmlSerializer(typeof(T));
        var xmlSettings = new XmlWriterSettings { OmitXmlDeclaration = true };

        // add namespace
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("s", "http://painting.aieplus.com/namespace");

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings);

        // serialize to XDocument
        serializer.Serialize(xmlWriter, data, namespaces);

        return XDocument.Parse(stringWriter.ToString());
    }

    public static string GetKeyXPath(this XElement source)
    {
        return string.Join("/", source.Ancestors().Select(GetXPath).Where(x => x != null));
    }

    public static string? GetXPath(this XElement x)
    {

        // 如果是Shapes节点, 因为不包含任何属性，直接返回Shapes
        if (x.Name == "Shapes") return x.Name.ToString();

        // 如果是Shape节点，通过MasterShape属性作为定位
        if (x.Name == "Shape") return $"{x.Name}[@MasterShape='{x.Attribute("MasterShape")!.Value}']";

        // 如果是Section、Row节点，则通过N属性作为定位
        if (x.Name == "Section" || x.Name == "Row")
            return x.Attribute("N") != null ? $"{x.Name}[@N='{x.Attribute("N")!.Value}']" : $"{x.Name}";

        if (x.Name == "PageContents")
            return null;

        throw new ArgumentOutOfRangeException("无法识别的节点");
    }
}
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
}
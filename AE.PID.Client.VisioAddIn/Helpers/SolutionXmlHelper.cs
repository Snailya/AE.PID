using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using AE.PID.Core.Models;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public abstract class SolutionXmlHelper
{
    public const string XmlNamespace = "http://painting.aieplus.com/namespace";

    /// <summary>
    ///     Save the <see cref="SolutionXmlElement{T}" /> element to the OpenXML document.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="element"></param>
    /// <typeparam name="T"></typeparam>
    public static void Store<T>(Document document, SolutionXmlElement<T> element)
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add("s", XmlNamespace);

        using var sw = new StringWriter();
        var xw = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true
        });

        var serializer = new XmlSerializer(typeof(SolutionXmlElement<T>));

        serializer.Serialize(xw, element, ns);

        var xml = sw.ToString();
        document.SolutionXMLElement[element.Name] = xml;
    }

    /// <summary>
    ///     Get the item from the document through xml deserialization.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static T Get<T>(Document document, string name)
    {
        if (!document.SolutionXMLElementExists[name])
            throw new FileNotFoundException();

        var xml = document.SolutionXMLElement[name];

        using var sr = new StringReader(xml);
        var serializer = new XmlSerializer(typeof(SolutionXmlElement<T>));

        var obj = serializer.Deserialize(sr);
        if (obj is not SolutionXmlElement<T> solutionXml)
            throw new ArgumentException(nameof(T));

        return solutionXml.Data;
    }
}
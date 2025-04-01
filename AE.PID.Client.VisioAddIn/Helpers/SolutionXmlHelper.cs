using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public abstract class SolutionXmlHelper
{
    public const string XmlNamespace = "http://painting.aieplus.com/namespace";

    private static readonly DataContractSerializerSettings DataContractSettings = new()
    {
        RootName = new XmlDictionaryString(XmlDictionary.Empty, "Data", 0),
        RootNamespace = new XmlDictionaryString(XmlDictionary.Empty, XmlNamespace, 0),
        KnownTypes = new List<Type> { typeof(List<LocationOverlay>) }
    };

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

    public static void StoreDataContract<T>(Document document, SolutionXmlElement<T> element)
    {
        var serializer = new DataContractSerializer(element.Data.GetType(), DataContractSettings);

        // 序列化
        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            OmitXmlDeclaration = true
        });

        xmlWriter.WriteStartElement("SolutionXML");
        xmlWriter.WriteAttributeString("Name", "location-overlay");
        xmlWriter.WriteAttributeString("xmlns", "s", null, XmlNamespace);

        // 序列化 DataContent 对象到 <Data> 元素内
        serializer.WriteObject(xmlWriter, element.Data);

        xmlWriter.WriteEndElement(); // 关闭 SolutionXML
        xmlWriter.Flush();

        var xml = writer.ToString();

        try
        {
            document.SolutionXMLElement[element.Name] = xml;
        }
        catch (Exception e)
        {
            Debugger.Break();
        }
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

    public static T GetDataContract<T>(Document document, string name)
    {
        if (!document.SolutionXMLElementExists[name])
            throw new FileNotFoundException();

        var xml = document.SolutionXMLElement[name];

        var serializer = new DataContractSerializer(typeof(SolutionXmlElement<T>), DataContractSettings);
        using var reader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(reader);

        var obj = serializer.ReadObject(xmlReader);
        if (obj is not SolutionXmlElement<T> solutionXml)
            throw new ArgumentException(nameof(T));

        return solutionXml.Data;
    }
}
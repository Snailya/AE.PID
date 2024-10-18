using System.Xml.Serialization;

namespace AE.PID.Core.Models;

[XmlRoot("SolutionXML")]
public class SolutionXmlElement<T>
{
    [XmlAttribute] public string Name { get; set; }

    [XmlElement(Namespace = "http://painting.aieplus.com/namespace")]
    public T Data { get; set; }
}
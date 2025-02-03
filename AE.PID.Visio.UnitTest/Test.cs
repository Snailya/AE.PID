using System.Xml.Serialization;
using AE.PID.Client.Core;
using AE.PID.Core.Models;
using FluentAssertions;

namespace AE.PID.Visio.UnitTest;

public class Test
{
    [Fact]
    public void XmlSerialization()
    {
        var txt =
            "<SolutionXML xmlns:s=\"http://ae.pid.com/\" Name=\"project\"><s:Data><s:Name>测试项目6</s:Name><s:Code>202308290002</s:Code><s:Director /><s:ProjectManager /></s:Data></SolutionXML>";
        var serializer = new XmlSerializer(typeof(SolutionXmlElement<Project>));
        var output = serializer.Deserialize(new StringReader(txt));
        output.Should().NotBeNull();
    }
}
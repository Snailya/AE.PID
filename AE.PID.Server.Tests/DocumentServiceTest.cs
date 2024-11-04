using System.Diagnostics;
using System.Xml.Linq;
using AE.PID.Server.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AE.PID.Server.Tests;

public class DocumentServiceTest
{
    private readonly DocumentService _service;

    public DocumentServiceTest()
    {
        var loggerMock = new Mock<ILogger<DocumentService>>();
        _service = new DocumentService(loggerMock.Object);
    }

    [Fact]
    public void UpdateMastersDocumentTest()
    {
        var sourceDocument = XDocument.Load("Fixture/MoqDocument/SourceMasters.xml");
        var snapshotElement = XElement.Load("Fixture/MoqSnapshot/MasterElement.xml");

        var expectedDocument = XDocument.Load("Fixture/MoqDocument/ExpectedMasters.xml");
        var actualDocument =
            _service.BuildMastersDocument(sourceDocument, snapshotElement, "{664C2F7C-9EA3-4342-935D-861C148E3D52}");

        CompareElements(expectedDocument.Root, actualDocument.Root).Should().Be(true);
    }


    [Fact]
    public void UpdateMasterDocumentTest()
    {
        var snapshotDocument = XDocument.Load("Fixture/MoqSnapshot/MasterDocument.xml");

        var expectedDocument = XDocument.Load("Fixture/MoqDocument/ExpectedMaster.xml");
        var actualDocument = _service.BuildMasterDocument(snapshotDocument, 3, 3, 3);

        CompareElements(expectedDocument.Root, actualDocument.Root).Should().Be(true);
    }

    [Fact]
    public void UpdatePageDocumentTest()
    {
        var sourceDocument = XDocument.Load("Fixture/MoqDocument/SourcePage.xml");
        var snapshotDocument = XDocument.Load("Fixture/MoqSnapshot/MasterDocument.xml");

        var expectedDocument = XDocument.Load("Fixture/MoqDocument/ExpectedPage.xml");
        var actualDocument = _service.BuildPageDocument(sourceDocument, snapshotDocument, "2");

        CompareElements(expectedDocument.Root, actualDocument.Root).Should().Be(true);
    }
    
    private static bool CompareElements(XElement? element1, XElement? element2)
    {
        if (element1 == null || element2 == null)
            return element1 == element2;

        // 比较元素名称
        if (element1.Name != element2.Name)
        {
            Debug.WriteLine(element1.Name + " != " + element2.Name);
            return false;
        }

        // 比较属性（忽略顺序）
        var attrs1 = element1.Attributes().OrderBy(a => a.Name.ToString()).ToList();
        var attrs2 = element2.Attributes().OrderBy(a => a.Name.ToString()).ToList();
        if (!attrs1.SequenceEqual(attrs2, new XAttributeComparer()))
        {
            Debug.WriteLine("Attributes are not equal.[Attribute1]: " + string.Join(", ", attrs1) + "[Attribute2]: " +
                            string.Join(", ", attrs2));
            return false;
        }

        // 比较子元素数量
        if (element1.Elements().Count() != element2.Elements().Count())
        {
            Debug.WriteLine("Elements are not equal.");
            return false;
        }

        // 递归比较子元素
        return element1.Elements().Zip(element2.Elements(), CompareElements).All(x => x);
    }

    private class XAttributeComparer : IEqualityComparer<XAttribute>
    {
        public bool Equals(XAttribute? x, XAttribute? y)
        {
            return x?.Name == y?.Name && x?.Value == y?.Value;
        }

        public int GetHashCode(XAttribute obj)
        {
            return obj.Name.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}
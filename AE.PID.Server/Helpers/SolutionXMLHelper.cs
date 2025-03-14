using System.IO.Packaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using AE.PID.Core;
using AE.PID.Server.Extensions;
using Version = AE.PID.Core.Models.Version;

namespace AE.PID.Server.Services;

public static class SolutionXmlHelper
{
    private const string SolutionsPath = "visio/solutions/solutions.xml";
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/office/visio/2012/main";
    private static readonly XNamespace RNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>
    ///     UpdateMaster the version solution xml element.
    /// </summary>
    /// <param name="visioPackage"></param>
    /// <param name="versionId"></param>
    public static void UpdateVersion(Package visioPackage, int versionId)
    {
        // 创建SolutionXmlElement
        var data = new SolutionXmlElement<Version>
        {
            Name = "version",
            Data = new Version
            {
                Id = versionId,
                ModifiedAt = DateTime.Now
            }
        };

        var doc = data.ToXDocument();

        // 这个步骤非常复杂，需要创建以下内容：
        // 1. 新建或更新 /visio/solutions/solution{i}.xml
        // 2. 新建或更新 /visio/solutions/solutions.xml
        // 3. 使用CreateRelationship创建或更新 /visio/solutions/.rels/solutions.xml.rel
        // 4. 使用CreateRelationship创建或更新 /visio/.rels/document.xml.rels

        // 首先保证solutionsPart存在
        visioPackage.EnsureSolutionsPartExist();

        // 获取VersionPart
        var versionPart = visioPackage.SolutionXMLElementExists("version")
            ? visioPackage.SolutionXMLELement("version")
            : visioPackage.CreateSolutionXMLElement("version");

        // 更新version.xml
        versionPart.FlushXDocument(doc);
    }

    private static PackagePart CreateSolutionXMLElement(this Package visioPackage, string elementName)
    {
        var solutionsPartUri = PackUriHelper.CreatePartUri(new Uri(SolutionsPath, UriKind.Relative));
        var solutionsPart = visioPackage.GetPart(solutionsPartUri);

        var part =
            // 新增part
            visioPackage.CreatePart(
                PackUriHelper.CreatePartUri(new Uri($"visio/solutions/{elementName}.xml", UriKind.Relative)),
                "application/vnd.ms-visio.solution+xml");

        // 创建关系
        var relationship = solutionsPart.CreateRelationship(new Uri($"{elementName}.xml", UriKind.Relative),
            TargetMode.Internal,
            "http://schemas.microsoft.com/visio/2010/relationships/solution");

        // 更新solutions.xml
        var solution = new XElement(Ns + "Solution",
            new XAttribute("Name", elementName),
            new XElement(Ns + "Rel", new XAttribute(RNs + "id", $"{relationship.Id}")));
        var solutionsDocument = solutionsPart.GetDocumentFromPart();
        solutionsDocument.Root!.Add(solution);
        solutionsPart.FlushXDocument(solutionsDocument);
        return part;
    }

    private static PackagePart SolutionXMLELement(this Package package, string elementName)
    {
        if (!package.SolutionXMLElementExists(elementName)) throw new FileNotFoundException();

        var solutionsPart =
            package.GetPart(PackUriHelper.CreatePartUri(new Uri("visio/solutions/solutions.xml", UriKind.Relative)));
        var nsManager = new XmlNamespaceManager(new NameTable());
        nsManager.AddNamespace("main", Ns.ToString());
        nsManager.AddNamespace("r", RNs.ToString());
        nsManager.AddNamespace("space", "preserve");

        var relElement = solutionsPart.GetDocumentFromPart().XPathSelectElement(
            $"//main:Solution[@Name='{elementName}']/main:Rel",
            nsManager);
        var relId = relElement
            ?.Attribute(XName.Get("id", RNs.ToString()))?.Value;

        if (relId == null) throw new InvalidOperationException();

        var relationship = solutionsPart.GetRelationship(relId);
        var packageUri = PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri);
        return package.GetPart(packageUri);
    }


    private static bool SolutionXMLElementExists(this Package package, string elementName)
    {
        var solutionsPart =
            package.GetPart(PackUriHelper.CreatePartUri(new Uri("visio/solutions/solutions.xml", UriKind.Relative)));
        var document = solutionsPart.GetDocumentFromPart();
        var exist = document.Descendants()
            .Descendants()
            .Any(e => e.Name.LocalName == "Solution" && e.Attribute("Name")?.Value == elementName);
        return exist;
    }


    private static void EnsureSolutionsPartExist(this Package visioPackage)
    {
        var solutionsPartUri = PackUriHelper.CreatePartUri(new Uri(SolutionsPath, UriKind.Relative));
        if (visioPackage.PartExists(solutionsPartUri)) return;

        // 如果solutionsPart根本不存在，说明我们需要完成1，2，3，4
        // 首先来创建个solutionsPart
        var solutionsPart = visioPackage.CreatePart(solutionsPartUri, "application/vnd.ms-visio.solutions+xml");

        // 使用空白模板更新solutionsPart的内容
        var solutionsDocTemplate = new XDocument(
            new XElement(Ns + "Solutions",
                new XAttribute("xmlns", Ns.ToString()),
                new XAttribute(XNamespace.Xmlns + "r",
                    RNs.ToString()),
                new XAttribute(XNamespace.Xml + "space", "preserve"))
        );
        solutionsPart.FlushXDocument(solutionsDocTemplate);

        // 光创建solutionsPart是不够的，还需要将这个solutionsPart关联到documentPart
        // 获取documentPart
        const string documentPath = "visio/document.xml";
        var documentPartUri = PackUriHelper.CreatePartUri(new Uri(documentPath, UriKind.Relative));
        var documentPart = visioPackage.GetPart(documentPartUri);

        // 关联的时候要使用相对Uri，而不是PartUri，注意没有“/”作为开头
        documentPart.CreateRelationship(new Uri("solutions/solutions.xml", UriKind.Relative),
            TargetMode.Internal,
            "http://schemas.microsoft.com/visio/2010/relationships/solutions");
    }
}
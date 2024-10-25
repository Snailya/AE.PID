using System.IO.Packaging;
using System.Xml.Linq;
using AE.PID.Server.Data;
using AE.PID.Server.Exceptions;
using AE.PID.Server.Extensions;
using AE.PID.Server.Interfaces;

namespace AE.PID.Server.Services;

public class DocumentService(ILogger<DocumentService> logger) : IDocumentService
{
    private readonly XNamespace _mainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
    private readonly XNamespace _relNs = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public XDocument BuildMastersDocument(XDocument source, XElement snapshot, string baseId)
    {
        // 定位需要被替换的element
        var sourceElement = source.Descendants(_mainNs + "Master")
            .Single(x => x.Attribute("BaseID")!.Value == baseId);

        // 将用来替换的element的ID更正为原element的值
        var sourceId = sourceElement.Attribute("ID")!.Value;
        snapshot.Attribute("ID")!.SetValue(sourceId);

        // 将用来替换的element的Rel更正为原element的node
        snapshot.Element(_mainNs + "Rel")?.Remove();
        snapshot.Add(sourceElement.Element(_mainNs + "Rel"));

        // 替换
        sourceElement.ReplaceWith(snapshot);

        // 修正替换后的namespace
        RemoveNamespace(source);

        return source;
    }

    public XDocument BuildMasterDocument(XDocument snapshot, int? lineStyle, int? fillStyle, int? textStyle)
    {
        foreach (var shapeElement in snapshot.Descendants(_mainNs + "Shape"))
        {
            if (lineStyle.HasValue) shapeElement.Attribute("LineStyle")!.SetValue(lineStyle);

            if (fillStyle.HasValue) shapeElement.Attribute("FillStyle")!.SetValue(fillStyle);

            if (textStyle.HasValue) shapeElement.Attribute("TextStyle")!.SetValue(textStyle);
        }

        return snapshot;
    }

    public XDocument BuildPageDocument(XDocument source, XDocument snapshot, string masterId)
    {
        int? maxId = null;

        foreach (var shapeElement in source.Descendants(_mainNs + "Shape")
                     .Where(x => x.Attribute("Master")?.Value == masterId))
        {
            var sourceElement = shapeElement.Element(_mainNs + "Shapes");
            if (sourceElement == null) continue;

            var targetElement = new XElement(_mainNs + "Shapes");
            foreach (var xElement in snapshot.Elements(_mainNs + "MasterContents")
                         .Elements(_mainNs + "Shapes")
                         .Elements(_mainNs + "Shape")
                         .Elements(_mainNs + "Shapes")
                         .Elements(_mainNs + "Shape"))
            {
                logger.LogDebug($"Processing element: {xElement.Name.LocalName} {xElement.Attribute("NameU")}");

                if (sourceElement.Elements(_mainNs + "Shape").SingleOrDefault(x =>
                        x.Attribute("MasterShape")?.Value == xElement.Attribute("ID")?.Value) is
                    { } subShapeElement)
                {
                    logger.LogDebug(
                        $"Processing element: {xElement.Name.LocalName} {xElement.Attribute("NameU")} - Copy");

                    targetElement.Add(subShapeElement);
                }
                else
                {
                    logger.LogDebug(
                        $"Processing element: {xElement.Name.LocalName} {xElement.Attribute("NameU")} - Append");

                    maxId ??= source.Descendants(_mainNs + "Shape")
                        .Where(x => x.Attribute("ID") != null)
                        .Max(x => int.Parse(x.Attribute("ID")!.Value));

                    // create new if not exist
                    var nameU = xElement.Attribute("NameU")!.Value;
                    var isCustomNameU = xElement.Attribute("IsCustomNameU")!.Value;
                    var name = xElement.Attribute("Name")!.Value;
                    var isCustomName = xElement.Attribute("IsCustomName")!.Value;
                    var type = xElement.Attribute("Type")!.Value;
                    var newSubShapeElement = new XElement("Shape",
                        new XAttribute("ID", ++maxId),
                        new XAttribute("NameU", nameU),
                        new XAttribute("IsCustomNameU", isCustomNameU),
                        new XAttribute("Name", name),
                        new XAttribute("IsCustomName", isCustomName),
                        new XAttribute("Type", type),
                        new XAttribute("MasterShape", xElement.Attribute("ID")!.Value));
                    targetElement.Add(newSubShapeElement);
                }
            }

            sourceElement.ReplaceWith(new XElement(_mainNs + "Shapes",
                targetElement.Elements().OrderBy(x => x.Attribute("ID")!.Value)));
        }

        // 修正替换后的namespace
        RemoveNamespace(source);

        return source;
    }

    public void Update(Package package, MasterContentSnapshot snapshot)
    {
        logger.LogInformation("Start checking {snapshot}...", snapshot.BaseId);

        var targetElement = XElement.Parse(snapshot.MasterElement);
        var targetDocument = XDocument.Parse(snapshot.MasterDocument);

        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        // 首先判断是否需要更新
        var masterElement = mastersDocument.Descendants(_mainNs + "Master")
            .SingleOrDefault(x => x.Attribute("BaseID")!.Value == snapshot.BaseId);
        if (masterElement == null ||
            masterElement.Attribute("UniqueID")!.Value == snapshot.UniqueId)
        {
            logger.LogInformation("{snapshot} is update to date.", snapshot.BaseId);
            return;
        }

        // 首先查看有多少页，以确定step的总步数
        var pagesUri = PackUriHelper.CreatePartUri(new Uri("visio/pages/pages.xml", UriKind.Relative));
        var pagesPart = package.GetPart(pagesUri);
        var currentStep = 0;
        var totalSteps = pagesPart.GetRelationships().Count();

        logger.LogDebug("Step({Current}/{Total}): Building /visio/masters.xml...", ++currentStep, totalSteps);

        mastersDocument = BuildMastersDocument(mastersDocument, targetElement, snapshot.BaseId);
        XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);

        /* ----------------------------------------
         * 准备masters{i}.xml
         * ----------------------------------------
         */

        // update /visio/master{i}.xml
        var relationshipId = masterElement.Element(_mainNs + "Rel")!.Attribute(_relNs + "id")!.Value;
        var relationship = mastersPart.GetRelationship(relationshipId);
        var masterUri = PackUriHelper.ResolvePartUri(relationship.SourceUri, relationship.TargetUri);
        var masterPart = package.GetPart(masterUri);

        logger.LogDebug("Step({Current}/{Total}): Building {Uri}", ++currentStep, totalSteps, masterPart.Uri);

        // 解析styleId
        var currentStyleTables = VisioXmlWrapper.GetStyles(package).ToList();
        var lineStyle = currentStyleTables
            .SingleOrDefault(x => x.Name == snapshot.LineStyleName)?.Id;
        var fillStyle = currentStyleTables
            .SingleOrDefault(x => x.Name == snapshot.FillStyleName)?.Id;
        var textStyle = currentStyleTables
            .SingleOrDefault(x => x.Name == snapshot.TextStyleName)?.Id;

        var masterDocument = BuildMasterDocument(targetDocument, lineStyle, fillStyle, textStyle);
        XmlHelper.SaveXDocumentToPart(masterPart, masterDocument);

        /* ----------------------------------------
         * 准备pages{i}.xml中缺少的子形状
         * ----------------------------------------
         */

        foreach (var pagePart in pagesPart.GetRelationships()
                     .Select(x => package.GetPart(PackUriHelper.ResolvePartUri(x.SourceUri, x.TargetUri))))
        {
            var pageDocument = pagePart.GetDocumentFromPart();

            logger.LogDebug("Step({Current}/{Total}): Building {Uri}", ++currentStep, totalSteps, pagePart.Uri);

            pageDocument =
                BuildPageDocument(pageDocument, targetDocument, targetElement.Attribute("ID")!.Value);
            XmlHelper.SaveXDocumentToPart(pagePart, pageDocument);
        }

        logger.LogInformation("{snapshot} is update to date.", snapshot.BaseId);
    }

    public void ValidateMasterBaseIdUnique(Package package, IEnumerable<string> baseIds)
    {
        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        var duplicates = mastersDocument.Root!.Elements()
            .GroupBy(x => x.Attribute("BaseID")!.Value) // Group the list by each value
            .Where(g => g.Count() > 1 && baseIds.Any(x=>x == g.Key)) // Filter groups where count > 1
            .Select(g => g.Key) // Select the value (key) from each group
            .ToList();

        if (duplicates.Count > 0) throw new MasterBaseIdNotUniqueException(string.Join(",", duplicates));
    }

    private static void RemoveNamespace(XDocument source)
    {
        foreach (var node in source.Root!.Descendants())
        {
            // Remove the xmlns='' attribute. Note the use of
            // Attributes rather than Attribute, in case the
            // attribute doesn't exist (which it might not if we'd
            // created the document "manually" instead of loading
            // it from a file.)
            node.Attributes("xmlns").Remove();
            // Inherit the parent namespace instead
            node.Name = node.Parent!.Name.Namespace + node.Name.LocalName;
        }
    }
}
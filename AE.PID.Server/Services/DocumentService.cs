using System.IO.Packaging;
using System.Xml.Linq;
using AE.PID.Server.Data;
using AE.PID.Server.Extensions;
using AE.PID.Server.Models;

namespace AE.PID.Server.Services;

public class DocumentService(ILogger<DocumentService> logger) : IDocumentService
{
    private static readonly XNamespace MainNs = @"http://schemas.microsoft.com/office/visio/2012/main";
    private static readonly XNamespace RelNs = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public void UpdateStyles(Package package)
    {
        // 20241104: 由于将默认字体从思源黑体修改为等线，需要在更新文档的时候帮助处理。
        var documentUri = PackUriHelper.CreatePartUri(new Uri("visio/document.xml", UriKind.Relative));
        var documentPart = package.GetPart(documentUri);
        var documentDocument = documentPart.GetDocumentFromPart();

        // find out the AE Normal style's character section
        var rowElement = documentDocument.Element(MainNs + "VisioDocument")?
            .Element(MainNs + "StyleSheets")?
            .Elements(MainNs + "StyleSheet")
            .SingleOrDefault(x => x.Attribute("NameU")?.Value == "AE Normal")?
            .Elements(MainNs + "Section")
            .SingleOrDefault(x => x.Attribute("N")?.Value == "Character")?
            .Elements(MainNs + "Row")
            .SingleOrDefault(x => x.Attribute("IX")?.Value == "0");

        if (rowElement != null)
        {
            // check if the font is 等线
            var fontElement = rowElement.Elements(MainNs + "Cell").Single(x => x.Attribute("N")?.Value == "Font");
            if (fontElement.Attribute("V")!.Value != "等线")
                fontElement.Attribute("V")!.SetValue("等线");

            // check if the font is 等线
            var asiaFontElement = rowElement.Elements(MainNs + "Cell")
                .Single(x => x.Attribute("N")?.Value == "AsianFont");
            if (asiaFontElement.Attribute("V")!.Value != "等线")
                asiaFontElement.Attribute("V")!.SetValue("等线");
        }

        XmlHelper.SaveXDocumentToPart(documentPart, documentDocument);
    }

    public VisioMaster[] GetDocumentMasters(Package package)
    {
        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        return mastersDocument.Root!.Elements(MainNs + "Master").Select(x => new VisioMaster
        {
            Name = x.Attribute("Name")!.Value,
            UniqueId = x.Attribute("UniqueID")!.Value,
            BaseId = x.Attribute("BaseID")!.Value
        }).ToArray();
    }

    public void UpdateMaster(Package package, string uniqueId, MasterContentSnapshot snapshot)
    {
        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        var originalMasterElementInMastersPart = mastersDocument.Descendants()
            .FirstOrDefault(x => x.Attribute("UniqueID")?.Value == uniqueId);
        if (originalMasterElementInMastersPart == null) return;

        var documentUri = PackUriHelper.CreatePartUri(new Uri("/visio/document.xml", UriKind.Relative));
        var documentPart = package.GetPart(documentUri);
        var documentDocument = documentPart.GetDocumentFromPart();

        // 所有存储在DocumentPart中的有用的属性
        var styles = new[]
            {
                snapshot.FillStyleName, snapshot.LineStyleName,
                snapshot.TextStyleName
            }
            .Select(
                name => documentDocument.Descendants(MainNs + "StyleSheet")
                    .SingleOrDefault(i => i.Attribute("Name")?.Value == name)?.Attribute("ID")?.Value).ToArray();
        var fileStyleId = styles[0];
        var lineStyleId = styles[1];
        var textStyleId = styles[2];

        var targetMasterElementInMastersPart = XElement.Parse(snapshot.MasterElement);
        var targetMasterDocument = XDocument.Parse(snapshot.MasterDocument);

        // 所有存储在 MastersPart 中的有用的属性
        var id = originalMasterElementInMastersPart.Attribute("ID")!.Value;
        var baseId = originalMasterElementInMastersPart.Attribute("BaseID")!.Value;
        var masterName = originalMasterElementInMastersPart.Attribute("Name")!.Value;
        var relId = originalMasterElementInMastersPart.Element(MainNs + "Rel")!.Attribute(RelNs + "id")!.Value;

        // 替换 MastersPart 中的 MasterElement:
        // 1. 修正目标 MasterElement 中 ID 为原来的 ID，这样在 PagePart 不需要再调整 Shape 的 Master 属性
        // 2. 修正目标 MasterElement 的 Rel 为原来的 Rel
        // 3. 使用修正后的 MasterElement 替换原有的 MasterElement
        targetMasterElementInMastersPart.Attribute("ID")!.SetValue(id);
        targetMasterElementInMastersPart.Element(MainNs + "Rel")?.Remove();
        targetMasterElementInMastersPart.Add(originalMasterElementInMastersPart.Element(MainNs + "Rel"));

        originalMasterElementInMastersPart.ReplaceWith(targetMasterElementInMastersPart);

        // 通过 Relationship 找到关联的 MasterPart
        var rel = mastersPart.GetRelationship(relId);
        var masterUri = PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri);
        var masterPart = package.GetPart(masterUri);

        // 替换 MasterPart:
        // 1. 修正目标 MasterDocument 中的 styleId，由于不同的文档中同一 Style 的ID不同，所以需要查询当前文档的 Style 名称与 Style ID 之间的匹配结果
        foreach (var shapeElementInMasterPart in targetMasterDocument.Descendants(MainNs + "Shape"))
        {
            if (!string.IsNullOrEmpty(fileStyleId))
                shapeElementInMasterPart.Attribute("LineStyle")!.SetValue(fileStyleId);
            if (!string.IsNullOrEmpty(lineStyleId))
                shapeElementInMasterPart.Attribute("FillStyle")!.SetValue(lineStyleId);
            if (!string.IsNullOrEmpty(textStyleId))
                shapeElementInMasterPart.Attribute("TextStyle")!.SetValue(textStyleId);
        }

        // 通过 Relationship 找到所有的 PagePart
        var pagesUri = PackUriHelper.CreatePartUri(new Uri("visio/pages/pages.xml", UriKind.Relative));
        var pagesPart = package.GetPart(pagesUri);
        foreach (var pageRel in pagesPart.GetRelationships())
        {
            var pagePart = package.GetPart(PackUriHelper.ResolvePartUri(pageRel.SourceUri, pageRel.TargetUri));
            var pageDocument = pagePart.GetDocumentFromPart();

            var maxShapeId = pageDocument.Descendants(MainNs + "Shape")
                .Where(x => x.Attribute("ID") != null && x.Attribute("Del") == null) // 被删除的Shape的ID会变成4294967295
                .Select(x => (int?)int.Parse(x.Attribute("ID")!.Value))
                .DefaultIfEmpty(null) // 如果集合为空，则返回 null
                .Max() ?? 0;

            foreach (var shapeElementInPagePart in pageDocument.Descendants(MainNs + "Shape")
                         .Where(x => x.Attribute("Master")?.Value == id))
            {
                // 修正 PagePart
                // 1. 去除SubClass中的公式，模具的新版本调整了Pro.SubClass.Format的值顺序，可能会导致更新后的形状显示为错误的子类，解决的方法是去掉公式，只使用值
                // 2. 重构Subshapes
                var subClassFormulaAttribute = shapeElementInPagePart.Elements(MainNs + "Section")
                    .SingleOrDefault(x => x.Attribute("N")?.Value == "Property")?
                    .Elements(MainNs + "Row")
                    .SingleOrDefault(x => x.Attribute("N")?.Value == "SubClass")?
                    .Element(MainNs + "Cell")?.Attribute("F");
                subClassFormulaAttribute?.Remove();

                var targetSubShapeElements = targetMasterDocument.Elements(MainNs + "MasterContents")
                    .Elements(MainNs + "Shapes")
                    .Elements(MainNs + "Shape")
                    .Descendants(MainNs + "Shape")
                    .Where(x => x.Attribute("ID") != null)
                    .ToList();
                if (targetSubShapeElements.Count == 0) continue;

                // todo: 有问题
                var targetSubShapesElement = new XElement(MainNs + "Shapes");
                foreach (var targetSubShapeElement in targetSubShapeElements)
                {
                    var masterShapeId = targetSubShapeElement.Attribute("ID")!.Value;
                    // content中需要的mastershape如果在原来的文件中已经存在了，则直接添加进去
                    if (shapeElementInPagePart.Descendants(MainNs + "Shape")
                            .SingleOrDefault(x => x.Attribute("MasterShape")?.Value == masterShapeId) is { } exist)
                    {
                        targetSubShapesElement.Add(exist);
                    }
                    // todo: 如果不存在，创建一个空白节点。这里需要进一步优化，因为空白节点中的数据，例如定位PinX，PinY之流必须在下次移动图纸的时候才会更新。
                    else
                    {
                        var empty = CopyEmpty(targetSubShapeElement, ref maxShapeId);
                        targetSubShapesElement.Add(empty);
                    }
                }

                shapeElementInPagePart.Element(MainNs + "Shapes")!.ReplaceWith(targetSubShapesElement);
            }

            RemoveNamespace(pageDocument);
            XmlHelper.SaveXDocumentToPart(pagePart, pageDocument);
        }

        // 保存
        RemoveNamespace(targetMasterDocument);
        XmlHelper.SaveXDocumentToPart(masterPart, targetMasterDocument);

        RemoveNamespace(mastersDocument);
        XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);
    }

    private static XElement CopyEmpty(XElement xElement, ref int maxShapeId)
    {
        if (xElement.Name.LocalName == "Shape")
        {
            var nameU = xElement.Attribute("NameU")!.Value;
            var isCustomNameU = xElement.Attribute("IsCustomNameU")!.Value;
            var name = xElement.Attribute("Name")!.Value;
            var isCustomName = xElement.Attribute("IsCustomName")!.Value;
            var type = xElement.Attribute("Type")!.Value;

            var empty = new XElement("Shape",
                new XAttribute("ID", ++maxShapeId),
                new XAttribute("NameU", nameU),
                new XAttribute("IsCustomNameU", isCustomNameU),
                new XAttribute("Name", name),
                new XAttribute("IsCustomName", isCustomName),
                new XAttribute("Type", type),
                new XAttribute("MasterShape", xElement.Attribute("ID")!.Value));

            return empty;
        }

        throw new ArgumentOutOfRangeException();
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
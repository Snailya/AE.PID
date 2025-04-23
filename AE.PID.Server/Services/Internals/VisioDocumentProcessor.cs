using System.IO.Packaging;
using System.Xml.Linq;
using AE.PID.Core;
using AE.PID.Server.Data;
using AE.PID.Server.Extensions;
using AE.PID.Server.Models;

namespace AE.PID.Server;

internal class VisioDocumentProcessor
{
    public void UpdateStyles(Package package)
    {
        // 20241104: 由于将默认字体从思源黑体修改为等线，需要在更新文档的时候帮助处理。
        var documentUri = PackUriHelper.CreatePartUri(new Uri("visio/document.xml", UriKind.Relative));
        var documentPart = package.GetPart(documentUri);
        var documentDocument = documentPart.GetDocumentFromPart();

        // find out the AE Normal style's character section
        var rowElement = documentDocument.Element(XNames.VisioDocumentElement)?
            .Element(XNames.StyleSheetsElement)?
            .Elements(XNames.StyleSheetElement)
            .SingleOrDefault(x => x.Attribute(XNames.NameUAttribute)?.Value == "AE Normal")?
            .Elements(XNames.SectionElement)
            .SingleOrDefault(x => x.Attribute(XNames.NAttribute)?.Value == "Character")?
            .Elements(XNames.RowElement)
            .SingleOrDefault(x => x.Attribute(XNames.IndexAttribute)?.Value == "0");

        if (rowElement != null)
        {
            // check if the font is 等线
            var fontElement = rowElement.Elements(XNames.CellElement)
                .Single(x => x.Attribute(XNames.NAttribute)?.Value == "Font");
            if (fontElement.Attribute(XNames.VAttribute)!.Value != "等线")
                fontElement.Attribute(XNames.VAttribute)!.SetValue("等线");

            // check if the font is 等线
            var asiaFontElement = rowElement.Elements(XNames.CellElement)
                .Single(x => x.Attribute(XNames.NAttribute)?.Value == "AsianFont");
            if (asiaFontElement.Attribute(XNames.VAttribute)!.Value != "等线")
                asiaFontElement.Attribute(XNames.VAttribute)!.SetValue("等线");
        }

        XmlHelper.SaveXDocumentToPart(documentPart, documentDocument);
    }

    public static VisioMaster[] GetDocumentMasters(Package package)
    {
        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        return mastersDocument.Root!.Elements(XNames.MasterElement).Select(x => new VisioMaster
            {
                Name = x.Attribute(XNames.NameAttribute)!.Value,
                UniqueId = x.Attribute(XNames.UniqueIdAttribute)!.Value,
                BaseId = x.Attribute(XNames.BaseIdAttribute)!.Value
            })
            .ToArray();
    }

    public static void UpdateMaster(Package package, string uniqueId, MasterContentSnapshot snapshot)
    {
        var mastersUri = PackUriHelper.CreatePartUri(new Uri("visio/masters/masters.xml", UriKind.Relative));
        var mastersPart = package.GetPart(mastersUri);
        var mastersDocument = mastersPart.GetDocumentFromPart();

        var originalMasterElementInMastersPart = mastersDocument.Descendants()
            .FirstOrDefault(x => x.Attribute(XNames.UniqueIdAttribute)?.Value == uniqueId);
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
                name => documentDocument.Descendants(XNames.StyleSheetElement)
                    .SingleOrDefault(i => i.Attribute(XNames.NameAttribute)?.Value == name)
                    ?.Attribute(XNames.IdAttribute)?.Value)
            .ToArray();
        var fillStyleId = styles[0];
        var lineStyleId = styles[1];
        var textStyleId = styles[2];

        var targetMasterElementInMastersPart = XElement.Parse(snapshot.MasterElement);
        var targetMasterDocument = XDocument.Parse(snapshot.MasterDocument);

        // 所有存储在 MastersPart 中的有用的属性
        var id = originalMasterElementInMastersPart.Attribute(XNames.IdAttribute)!.Value;
        var baseId = originalMasterElementInMastersPart.Attribute(XNames.BaseIdAttribute)!.Value;
        var masterName = originalMasterElementInMastersPart.Attribute(XNames.NameAttribute)!.Value;
        var relId = originalMasterElementInMastersPart.Element(XNames.RelElement)!.Attribute(XNames.RelIdAttribute)!
            .Value;

        // 替换 MastersPart 中的 MasterElement:
        // 1. 修正目标 MasterElement 中 ID 为原来的 ID，这样在 PagePart 不需要再调整 Shape 的 Master 属性
        // 2. 修正目标 MasterElement 的 Rel 为原来的 Rel
        // 3. 使用修正后的 MasterElement 替换原有的 MasterElement
        targetMasterElementInMastersPart.Attribute(XNames.IdAttribute)!.SetValue(id);
        targetMasterElementInMastersPart.Element(XNames.RelElement)?.Remove();
        targetMasterElementInMastersPart.Add(originalMasterElementInMastersPart.Element(XNames.RelElement));

        // 2025.04.17: 为mastercontent里的所有cell加上newValue标识
        foreach (var cellElement in targetMasterElementInMastersPart.Descendants(XNames.CellElement))
            cellElement.Add(new XProcessingInstruction("NewValue", "V"));

        originalMasterElementInMastersPart.ReplaceWith(targetMasterElementInMastersPart);

        // 通过 Relationship 找到关联的 MasterPart
        var rel = mastersPart.GetRelationship(relId);
        var masterUri = PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri);
        var masterPart = package.GetPart(masterUri);

        // 替换 MasterPart:
        // 1. 修正目标 MasterDocument 中的 styleId，由于不同的文档中同一 Style 的ID不同，所以需要查询当前文档的 Style 名称与 Style ID 之间的匹配结果
        foreach (var shapeElementInMasterPart in targetMasterDocument.Descendants(XNames.ShapeElement))
        {
            if (!string.IsNullOrEmpty(fillStyleId))
                shapeElementInMasterPart.Attribute(XNames.FillStyleAttribute)!.SetValue(fillStyleId);
            if (!string.IsNullOrEmpty(lineStyleId))
                shapeElementInMasterPart.Attribute(XNames.LineStyleAttribute)!.SetValue(lineStyleId);
            if (!string.IsNullOrEmpty(textStyleId))
                shapeElementInMasterPart.Attribute(XNames.TextStyleAttribute)!.SetValue(textStyleId);
        }

        // 通过 Relationship 找到所有的 PagePart
        var pagesUri = PackUriHelper.CreatePartUri(new Uri("visio/pages/pages.xml", UriKind.Relative));
        var pagesPart = package.GetPart(pagesUri);
        foreach (var pageRel in pagesPart.GetRelationships())
        {
            var pagePart = package.GetPart(PackUriHelper.ResolvePartUri(pageRel.SourceUri, pageRel.TargetUri));
            var pageDocument = pagePart.GetDocumentFromPart();

            var maxShapeId = pageDocument.Descendants(XNames.ShapeElement)
                .Where(x => x.Attribute(XNames.IdAttribute) != null &&
                            x.Attribute(XNames.DelAttribute) == null) // 被删除的Shape的ID会变成4294967295
                .Select(x => (int?)int.Parse(x.Attribute(XNames.IdAttribute)!.Value))
                .DefaultIfEmpty(null) // 如果集合为空，则返回 null
                .Max() ?? 0;

            var template = new XElement(
                targetMasterDocument.Element(XNames.MasterContentsElement)!.Element(XNames.ShapesElement)!.Element(
                    XNames.ShapeElement)!);
            RemoveFixValue(template);

            foreach (var shapeElementInPagePart in pageDocument.Descendants(XNames.ShapeElement)
                         .Where(x => x.Attribute(XNames.MasterAttribute)?.Value == id).ToList())
            {
                var overlay = new XElement(shapeElementInPagePart);

#if !STABLE
                // 待运行稳定后，取消此行代码

                // 修正 PagePart
                // 1. 去除SubClass中的公式，模具的新版本调整了Pro.SubClass.Format的值顺序，可能会导致更新后的形状显示为错误的子类，解决的方法是去掉公式，只使用值
                // 2. 重构Subshapes
                var subClassFormulaAttribute = overlay.Elements(XNames.SectionElement)
                    .SingleOrDefault(x => x.Attribute(XNames.NAttribute)?.Value == "Property")?
                    .Elements(XNames.RowElement)
                    .SingleOrDefault(x => x.Attribute(XNames.NAttribute)?.Value == "SubClass")?
                    .Element(XNames.CellElement)?.Attribute(XNames.FAttribute);
                subClassFormulaAttribute?.Remove();
                
                // 删除所有公式值为Themeval的Cell
                overlay.Elements(XNames.CellElement)
                    .Where(x=>x.Attribute(XNames.FAttribute)?.Value == "THEMEVAL()").Remove();
                
                // 2025.04.23：如果不是管线，删除主形状上的Geometry
                if (baseId is not ("{C53C83CB-E71A-43EC-9D65-72CFAA3E02E8}" or "{AA964FAF-E393-47F1-AFC9-AD74613F595E}"))
                {
                    var geometries = overlay.Elements(XNames.SectionElement)
                        .Where(x => x.Attribute(XNames.NAttribute)?.Value == "Geometry").ToList();
                    foreach (var geometry in geometries)
                        geometry.Remove();
                }
                
                // 删除Character
                overlay.Elements(XNames.SectionElement).Where(x => x.Attribute(XNames.NAttribute)?.Value == "Character")
                    .Remove();

                // 由于调整了子形状的ID，所以现阶段先忽略所有的overlay中的子形状，
                overlay.Elements(XNames.ShapesElement).Elements().Remove();
#endif
                var merged = StructuredXElementMerger.StructuredMerge(template, overlay);
                UpdateId(merged, ref maxShapeId);
                shapeElementInPagePart.ReplaceWith(merged);
            }

            RemoveNamespace(pageDocument);
            
            if (pageDocument.Descendants(XNames.ShapeElement).Any(x => x.Attribute(XNames.IdAttribute)?.Value == "-1"))
                throw new DocumentUpdateFailedException(
                    "Failed to update shape ID after recreate the sub-shapes. If this exception throws, means the UpdateId() not work as expected. Obviously this it a bug.");
            
            XmlHelper.SaveXDocumentToPart(pagePart, pageDocument);
        }

        // 保存
        RemoveNamespace(targetMasterDocument);
        XmlHelper.SaveXDocumentToPart(masterPart, targetMasterDocument);

        RemoveNamespace(mastersDocument);
        XmlHelper.SaveXDocumentToPart(mastersPart, mastersDocument);
    }

    private static void UpdateId(XElement merged, ref int maxShapeId)
    {
        foreach (var shapeElement in merged.Descendants(XNames.ShapeElement))
            if (shapeElement.Attribute(XNames.IdAttribute) is { Value: "-1" })
                shapeElement.SetAttributeValue(XNames.IdAttribute, (++maxShapeId).ToString());
    }

    private static void RemoveFixValue(XElement source)
    {
        foreach (var child in source.Elements().ToList())
            if (child.Name == XNames.CellElement)
            {
                if (child.Attribute(XNames.FAttribute) == null)
                    child.Remove();
                else
                    child.SetAttributeValue(XNames.FAttribute, "Inh");
            }
            else
            {
                RemoveFixValue(child);
            }

        if (source.IsEmpty) source.Remove();
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
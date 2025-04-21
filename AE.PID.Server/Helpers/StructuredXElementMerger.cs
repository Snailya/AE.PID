using System.Xml.Linq;

namespace AE.PID.Server;

internal abstract class StructuredXElementMerger
{
    // 节点白名单（定义合法子节点）
    private static readonly Dictionary<XName, XName[]> ValidChildren = new()
    {
        [XNames.ShapesElement] = [XNames.ShapeElement],
        [XNames.ShapeElement] = [XNames.SectionElement, XNames.CellElement, XNames.ShapesElement, XNames.TextElement],
        [XNames.SectionElement] = [XNames.RowElement, XNames.CellElement],
        [XNames.RowElement] = [XNames.CellElement],
        [XNames.CellElement] = []
    };

    // 主合并入口
    public static XElement StructuredMerge(XElement template, XElement overlay)
    {
        var rootPath = new Stack<XName>();
        return MergeElements(template, overlay, rootPath);
    }

    private static XElement MergeElements(XElement template, XElement overlay, Stack<XName> parentPath)
    {
        // 类型校验
        if (template.Name != overlay.Name)
            throw new ArgumentException($"Element type mismatch: {template.Name} vs {overlay.Name}");

        // 生成当前节点的结构路径
        var currentPath = new Stack<XName>(parentPath.Reverse());
        currentPath.Push(template.Name);

        // 校验子节点合法性
        ValidateChildren(template);
        ValidateChildren(overlay);

        // 创建合并后的元素（保留第二个元素的属性）
        var merged = new XElement(template.Name, overlay.Attributes());

        if (merged.Attribute(XNames.DelAttribute)?.Value == "1")
            return merged;

        // 分组处理子节点（按类型分组）
        var templateGroups = GroupChildren(template);
        var overlayGroups = GroupChildren(overlay);

        // 合并同类子节点组
        foreach (var groupName in templateGroups.Keys.Union(overlayGroups.Keys))
        {
            var templateChildren = templateGroups.TryGetValue(groupName, out var p) ? p : [];
            var overlayChildren =
                overlayGroups.TryGetValue(groupName, out var s) ? s : [];

            // 合并匹配节点
            var matchedPairs = FindMergePairs(templateChildren, overlayChildren, currentPath);
            foreach (var (templateChild, overlayChild) in matchedPairs)
            {
                var mergedElements = MergeElements(templateChild, overlayChild, currentPath);
                merged.Add(mergedElements);
            }

            // 添加未匹配的剩余节点
            foreach (var templateChild in templateChildren.Except(matchedPairs.Select(t => t.Item1)))
            {
                var toAdd = new XElement(templateChild);

                if (templateChild.Name == XNames.ShapeElement)
                {
                    toAdd.RemoveAttributes();

                    toAdd.Add(new XAttribute(XNames.IdAttribute, "-1"),
                        new XAttribute(XNames.NameUAttribute, templateChild.Attribute(XNames.NameUAttribute)!.Value),
                        new XAttribute(XNames.NameAttribute, templateChild.Attribute(XNames.NameAttribute)!.Value),
                        new XAttribute(XNames.TypeAttribute, templateChild.Attribute(XNames.TypeAttribute)!.Value),
                        new XAttribute(XNames.MasterShapeAttribute, templateChild.Attribute(XNames.IdAttribute)!.Value)
                    );
                }

                merged.Add(toAdd);
            }

            merged.Add(overlayChildren.Except(matchedPairs.Select(o => o.Item2)));
        }

        return merged;
    }

    // 查找需要合并的节点对
    private static List<Tuple<XElement, XElement>> FindMergePairs(
        IEnumerable<XElement> template,
        IEnumerable<XElement> overlay,
        Stack<XName> currentPath)
    {
        var pairs = new List<Tuple<XElement, XElement>>();
        var secondaryList = overlay.ToList();

        foreach (var pChild in template)
        {
            var match = secondaryList.FirstOrDefault(sChild =>
                IsMergeable(pChild, sChild, currentPath));

            if (match != null)
            {
                pairs.Add(Tuple.Create(pChild, match));
                secondaryList.Remove(match);
            }
        }

        return pairs;
    }

    // 判断两个节点是否可合并
    private static bool IsMergeable(XElement template, XElement overlay, Stack<XName> currentPath)
    {
        // 属性完全一致
        if (!KeyAttributesEquals(template, overlay)) return false;

        // 父结构路径一致（通过currentPath隐含）
        return true;
    }

    // 属性比较
    private static bool KeyAttributesEquals(XElement a, XElement b)
    {
        if (a.Name != b.Name) return false;

        if (a.Name == XNames.CellElement)
            return a.Attribute(XNames.NAttribute)!.Value == b.Attribute(XNames.NAttribute)!.Value;
        if (a.Name == XNames.RowElement)
            return a.Attribute(XNames.NAttribute) != null
                ? a.Attribute(XNames.NAttribute)!.Value == b.Attribute(XNames.NAttribute)?.Value
                : a.Attribute(XNames.IndexAttribute) != null && a.Attribute(XNames.IndexAttribute)?.Value ==
                b.Attribute(XNames.IndexAttribute)?.Value;
        if (a.Name == XNames.SectionElement)
            return a.Attribute(XNames.NAttribute)!.Value == b.Attribute(XNames.NAttribute)?.Value;
        if (a.Name == XNames.ShapeElement)
            return b.Attribute(XNames.MasterAttribute) != null || a.Attribute(XNames.IdAttribute)!.Value ==
                b.Attribute(XNames.MasterShapeAttribute)!.Value;

        var aAttrs = a.Attributes().OrderBy(attr => attr.Name.ToString());
        var bAttrs = b.Attributes().OrderBy(attr => attr.Name.ToString());
        return aAttrs.SequenceEqual(bAttrs, XAttributeComparer.Instance);
    }

    // 子节点合法性校验
    private static void ValidateChildren(XElement element)
    {
        if (!ValidChildren.TryGetValue(element.Name, out var allowedChildren))
            throw new InvalidOperationException($"Unexpected element type: {element.Name}");

        foreach (var child in element.Elements())
            if (!allowedChildren.Contains(child.Name))
                throw new InvalidOperationException($"Illegal child {child.Name} in {element.Name}");
    }

    // 子节点分组（按类型）
    private static Dictionary<XName, List<XElement>> GroupChildren(XElement parent)
    {
        return parent.Elements()
            .GroupBy(e => e.Name)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // 属性比较器
    private class XAttributeComparer : IEqualityComparer<XAttribute>
    {
        public static XAttributeComparer Instance { get; } = new();

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
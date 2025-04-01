using System.Collections.Generic;
using System.Linq;

namespace AE.PID.Client.UI.Avalonia;

public static class TreeExtensions
{
    public static IEnumerable<FunctionLocationTreeItemViewModel> Flatten(this FunctionLocationTreeItemViewModel node)
    {
        return node.Inferiors.Any()
            ? new[] { node }.Concat(node.Inferiors.SelectMany(child => child.Flatten())) // 使用自身，结合子节点递归展平
            : [node];
    }
}
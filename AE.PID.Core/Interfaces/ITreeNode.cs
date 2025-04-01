using System;

namespace AE.PID.Core;

public interface ITreeNode<T>
{
    /// <summary>
    ///     The id of the node.
    /// </summary>
    public T Id { get; }

    /// <summary>
    ///     The id of the parent node.
    /// </summary>
    public T? ParentId { get; }

    /// <summary>
    ///     The default label used in the tree structure.
    /// </summary>
    public string NodeName { get; }
}

public interface ITreeNode : ITreeNode<int>
{
}
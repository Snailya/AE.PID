namespace AE.PID.Core.Interfaces;

public interface ITreeNode<T>
{
    /// <summary>
    ///     The id of the node.
    /// </summary>
    public T Id { get; }

    /// <summary>
    ///     The id of the parent node.
    /// </summary>
    public T ParentId { get; set; }

    /// <summary>
    ///     The user-oriented name of the element.
    /// </summary>
    public string NodeName { get; }
}

public interface ITreeNode : ITreeNode<int>
{
}
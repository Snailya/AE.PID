namespace AE.PID.Core.Interfaces;

public interface ITreeNode
{
    public int Id { get; }
    public int ParentId { get; }
}
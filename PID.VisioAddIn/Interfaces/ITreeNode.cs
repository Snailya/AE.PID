namespace AE.PID.Interfaces;

public interface ITreeNode
{
    public int ParentId { get; }
    public int Id { get; }
}
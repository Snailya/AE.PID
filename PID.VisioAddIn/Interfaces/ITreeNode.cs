namespace AE.PID.Interfaces;

public interface ITreeNode
{
    public int Id { get; }
    public int ParentId { get; }
}
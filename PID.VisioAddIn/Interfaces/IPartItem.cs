namespace AE.PID.Interfaces;

public interface IPartItem
{
    public string FunctionalGroup { get; set; }
    public string MaterialNo { get; }
    public double Count { get; }
}
namespace AE.PID.Visio.Core.Models;

public class ComparePair<TKey, TLocal, TServer>
{
    public TKey Key { get; set; }
    public TLocal? Local { get; set; }
    public TServer? Server { get; set; }
}
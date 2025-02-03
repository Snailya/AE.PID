namespace AE.PID.Client.Core;

public class ComparePair<TKey, TLocal, TServer>
{
    public TKey Key { get; set; }
    public TLocal? Local { get; set; }
    public TServer? Server { get; set; }
}
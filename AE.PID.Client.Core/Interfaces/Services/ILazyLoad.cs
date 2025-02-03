namespace AE.PID.Client.Core;

/// <summary>
///     Some service are not regularly used and is CPU-bound work, so the data should only load on demand.
/// </summary>
public interface ILazyLoad
{
    void Load();
}
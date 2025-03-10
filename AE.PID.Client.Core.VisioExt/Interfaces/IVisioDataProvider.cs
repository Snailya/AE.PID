using DynamicData;

namespace AE.PID.Client.Core.VisioExt;

public interface IVisioDataProvider : IDataProvider, ILazyLoad, ISelectable, IOleSupport
{
    /// <summary>
    ///     Get the symbols from the document
    /// </summary>
    Lazy<IObservableCache<VisioMaster, string>> Masters { get; }
}
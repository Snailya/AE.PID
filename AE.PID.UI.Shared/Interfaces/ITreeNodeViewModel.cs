using System.Collections.ObjectModel;
using AE.PID.Core.Interfaces;

namespace AE.PID.UI.Shared.Interfaces;

public interface ITreeNodeViewModel<TObject, TKey> : ITreeNode<TKey> where TObject : ITreeNodeViewModel<TObject, TKey>
{
    ReadOnlyObservableCollection<ITreeNodeViewModel<TObject, TKey>> Inferiors { get; }
}
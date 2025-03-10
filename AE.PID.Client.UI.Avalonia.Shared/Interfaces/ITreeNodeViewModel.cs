using System.Collections.ObjectModel;
using AE.PID.Core;

namespace AE.PID.Client.UI.Avalonia.Shared.Interfaces;

public interface ITreeNodeViewModel<TObject, TKey> : ITreeNode<TKey> where TObject : ITreeNodeViewModel<TObject, TKey>
{
    ReadOnlyObservableCollection<ITreeNodeViewModel<TObject, TKey>> Inferiors { get; }
}
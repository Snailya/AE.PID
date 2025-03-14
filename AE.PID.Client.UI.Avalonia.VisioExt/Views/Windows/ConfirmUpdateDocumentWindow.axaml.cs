using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.VisioExt;

public partial class ConfirmUpdateDocumentWindow : ReactiveWindow<ConfirmUpdateDocumentWindowViewModel>
{
    public ConfirmUpdateDocumentWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(action =>
            {
                ViewModel!.Confirm.Subscribe(Close).DisposeWith(action);
                ViewModel.Cancel.Subscribe(_ => Close(null)).DisposeWith(action);
            }
        );
    }
}
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Client.UI.Avalonia.Shared;

public partial class  SimpleDialog : ReactiveWindow<SimpleDialogViewModel>
{
    public SimpleDialog()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(d =>
        {
            d(ViewModel!.Confirm.Subscribe(_ => Close(true)));
            d(ViewModel!.Cancel.Subscribe(_ => Close(false)));
        });
    }
}
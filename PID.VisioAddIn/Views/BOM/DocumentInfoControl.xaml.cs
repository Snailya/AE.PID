using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.Views.BOM;

/// <summary>
/// Interaction logic for DocumentInfo.xaml
/// </summary>
public partial class DocumentInfoControl
{
    public DocumentInfoControl()
    {
        InitializeComponent();

        this.WhenActivated(disposableRegistration =>
        {
            this.Bind(ViewModel, vm => vm.CustomerName,
                    v => v.CustomerNameInput.Text)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, vm => vm.DocumentNo,
                    v => v.DocumentNo.Text)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel, vm => vm.ProjectNo,
                    v => v.ProjectNoInput.Text)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel, vm => vm.VersionNo,
                    v => v.VersionNoInput.Text)
                .DisposeWith(disposableRegistration);
        });
    }
}
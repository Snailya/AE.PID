using ReactiveUI;
using System.Reactive.Disposables;


namespace AE.PID.Views;

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
            this.Bind(ViewModel, vm => vm.DocumentNo,
                    v => v.DocNoInput.Text)
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
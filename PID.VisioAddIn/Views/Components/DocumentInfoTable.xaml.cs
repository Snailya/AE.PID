using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.Views.Components;

/// <summary>
///     Interaction logic for DocumentInfo.xaml
/// </summary>
public partial class DocumentInfoTable
{
    public DocumentInfoTable()
    {
        InitializeComponent();

        this.WhenActivated(disposableRegistration =>
        {
            this.Bind(ViewModel, vm => vm.CustomerName,
                    v => v.CustomerNameInput.Text)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, vm => vm.DocumentNo,
                    v => v.DocumentNoInput.Text)
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
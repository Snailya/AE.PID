using System;
using System.Reactive.Disposables;
using AE.PID.Controllers.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views.BOM;

public partial class ExportView
{
    
    public ExportView()
    {
        InitializeComponent();
        var service = new DocumentExporter(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new ExportViewModel(service);

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.DocumentInfo,
                    v => v.DocumentInfo.ViewModel)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.BillsOfMaterials.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.Submit,
                    v => v.SubmitButton)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.Cancel,
                    v => v.CancelButton)
                .DisposeWith(disposableRegistration);

            this.WhenAnyObservable(
                    x => x.ViewModel.Cancel,
                    x => x.ViewModel.Submit
                )
                .Subscribe(_ => Close())
                .DisposeWith(disposableRegistration);
        });
    }
}
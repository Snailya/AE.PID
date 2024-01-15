using System;
using System.Reactive.Disposables;
using System.Windows;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

public partial class ExportView
{
    public ExportView()
    {
        InitializeComponent();
        ViewModel = new ExportViewModel();

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

            this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.BillsOfMaterials.ItemsSource)
                .DisposeWith(disposableRegistration);
            // this.OneWayBind(ViewModel,
            //     vm => vm.IsLoading,
            //     v => v.LoadingSkeleton.Visibility,
            //     IsLoadingToVisibilityTypeConverterFunc)
            //     .DisposeWith(disposableRegistration);

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

    private Visibility IsLoadingToVisibilityTypeConverterFunc(bool isLoading)
    {
        return isLoading ? Visibility.Visible : Visibility.Hidden;
    }
}
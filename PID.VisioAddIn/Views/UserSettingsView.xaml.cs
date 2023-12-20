using System;
using System.Reactive.Disposables;
using System.Windows;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

public partial class UserSettingsView
{
    public UserSettingsView()
    {
        InitializeComponent();
        ViewModel = new UserSettingsViewModel();

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.CheckFrequencyOptions,
                    v => v.AppCheckFrequencySelector.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel,
                    vm => vm.AppNextCheckFrequency,
                    v => v.AppCheckFrequencySelector.SelectedItem)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.CheckForAppUpdate,
                    v => v.AppCheckUpdateButton)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel,
                    vm => vm.TmpPath,
                    v => v.TmpPathInput.Text)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.OpenTmp,
                    v => v.OpenTmpButton)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.ClearCache,
                    v => v.ClearCacheButton)
                .DisposeWith(disposableRegistration);

            this.OneWayBind(ViewModel,
                    vm => vm.CheckFrequencyOptions,
                    v => v.LibraryCheckFrequencySelector.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel,
                    vm => vm.LibraryCheckFrequency,
                    v => v.LibraryCheckFrequencySelector.SelectedItem)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.CheckForLibrariesUpdate,
                    v => v.LibraryCheckUpdateButton)
                .DisposeWith(disposableRegistration);

            this.BindCommand(ViewModel,
                    vm => vm.Submit,
                    v => v.SubmitButton)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.Cancel,
                    v => v.CancelButton)
                .DisposeWith(disposableRegistration);
        });

        this.WhenAnyObservable(
                x => x.ViewModel.Cancel,
                x => x.ViewModel.Submit
            )
            .Subscribe(_ => Close());
    }

    private void Close()
    {
        var window = Window.GetWindow(this);
        if (window != null) window.Visibility = Visibility.Collapsed;
    }
}
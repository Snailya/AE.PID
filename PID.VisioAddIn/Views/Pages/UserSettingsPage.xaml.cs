using System.Reactive.Disposables;
using AE.PID.ViewModels.Pages;
using ReactiveUI;

namespace AE.PID.Views.Pages;

public partial class UserSettingsPage
{
    public UserSettingsPage()
    {
        InitializeComponent();
        ViewModel = new UserSettingsViewModel();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.CheckFrequencyOptions,
                    v => v.AppCheckFrequencySelector.ItemsSource)
                .DisposeWith(d);
            this.Bind(ViewModel,
                    vm => vm.AppNextCheckFrequency,
                    v => v.AppCheckFrequencySelector.SelectedItem)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.CheckForAppUpdate,
                    v => v.AppCheckUpdateButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel,
                    vm => vm.TmpPath,
                    v => v.TmpPathInput.Text)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.OpenTmp,
                    v => v.OpenTmpButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.ClearCache,
                    v => v.ClearCacheButton)
                .DisposeWith(d);

            this.OneWayBind(ViewModel,
                    vm => vm.CheckFrequencyOptions,
                    v => v.LibraryCheckFrequencySelector.ItemsSource)
                .DisposeWith(d);
            this.Bind(ViewModel,
                    vm => vm.LibraryCheckFrequency,
                    v => v.LibraryCheckFrequencySelector.SelectedItem)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.CheckForLibrariesUpdate,
                    v => v.LibraryCheckUpdateButton)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.Libraries,
                    v => v.LibraryList.ItemsSource)
                .DisposeWith(d);

            this.Bind(ViewModel,
                    vm => vm.OkCancelFeedbackViewModel,
                    v => v.Feedback.ViewModel)
                .DisposeWith(d);
        });
    }
}
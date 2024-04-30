using System.Reactive.Disposables;
using AE.PID.Controllers.Services;
using AE.PID.ViewModels.Pages;
using ReactiveUI;
using Splat;

namespace AE.PID.Views.Pages;

public partial class UserSettingsPage
{
    public UserSettingsPage()
    {
        InitializeComponent();

        var configuration = Locator.Current.GetService<ConfigurationService>();
        var appUpdater = Locator.Current.GetService<AppUpdater>();
        var libraryUpdater = Locator.Current.GetService<LibraryUpdater>();
        ViewModel = new UserSettingsViewModel(configuration!, appUpdater!, libraryUpdater!);

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel,
                    vm => vm.AppNextCheckFrequency,
                    v => v.AppCheckFrequencySelector.SelectedItem)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.CheckForAppUpdate,
                    v => v.AppCheckUpdateButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel,
                    vm => vm.OpenTmp,
                    v => v.OpenTmpButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.ClearCache,
                    v => v.ClearCacheButton)
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
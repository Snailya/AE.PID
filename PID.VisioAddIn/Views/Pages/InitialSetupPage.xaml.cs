using System.Reactive.Disposables;
using AE.PID.Services;
using AE.PID.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using Splat;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class InitialSetupPage
{
    public InitialSetupPage() : base("Initial Setup")
    {
        InitializeComponent();
        var configuration = Locator.Current.GetService<ConfigurationService>();
        ViewModel = new InitialSetupPageViewModel(configuration!);

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.Server, v => v.ServerInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.User, v => v.UserInput.Text)
                .DisposeWith(d);

            this.BindValidation(ViewModel, vm=>vm.Server ,v => v.ServerInput.Error).DisposeWith(d);
            this.BindValidation(ViewModel, vm=>vm.User,v => v.UserInput.Error).DisposeWith(d);

            this.Bind(ViewModel,
                    vm => vm.OkCancelFeedbackViewModel,
                    v => v.Feedback.ViewModel)
                .DisposeWith(d);
        });
    }
}
﻿using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.ViewModels;
using ReactiveUI;
using Splat;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class SettingsPage : IEnableLogger
{
    public SettingsPage() : base("Settings")
    {
        InitializeComponent();

        ViewModel = new SettingsPageViewModel();

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.Server, v => v.ServerInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.User, v => v.UserInput.Text)
                .DisposeWith(d);

            this.Bind(ViewModel,
                    vm => vm.AppCheckFrequency,
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

            this.Bind(ViewModel, vm=>vm.UseServerSideUpdate, v=>v.ServerUpdateButton.IsChecked)
                .DisposeWith(d);
        });
    }
}
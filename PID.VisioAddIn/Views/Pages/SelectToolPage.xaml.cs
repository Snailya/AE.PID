using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class SelectToolPage
{
    public SelectToolPage()
    {
        InitializeComponent();

        using var service = new ShapeSelector(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new SelectToolPageViewModel(service);

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel,
                    viewModel => viewModel.ShapeId,
                    view => view.IdTextBox.Text)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    viewModel => viewModel.Masters,
                    view => view.MastersCheckBox.ItemsSource)
                .DisposeWith(d);

            this.Bind(ViewModel,
                    viewModel => viewModel.OkCancelFeedbackViewModel,
                    view => view.Feedback.ViewModel)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ByIdButton.IsChecked)
                .Where(isChecked => isChecked is true)
                .Select(_ => SelectionMode.ById)
                .BindTo(ViewModel, x => x.Mode)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.ByMasterButton.IsChecked)
                .Where(isChecked => isChecked is true)
                .Select(_ => SelectionMode.ByMasters)
                .BindTo(ViewModel, vm => vm.Mode)
                .DisposeWith(d);

            ViewModel.WhenAnyValue(x => x.Mode)
                .Subscribe(v =>
                {
                    if (v == SelectionMode.ById)
                    {
                        IdTextBox.IsEnabled = true;
                        MastersCheckBox.IsEnabled = false;
                    }
                    else
                    {
                        IdTextBox.IsEnabled = false;
                        MastersCheckBox.IsEnabled = true;
                    }
                })
                .DisposeWith(d);
        });
    }
}
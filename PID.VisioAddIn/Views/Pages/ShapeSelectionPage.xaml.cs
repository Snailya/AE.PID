using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.ViewModels.Pages;
using ReactiveUI;

namespace AE.PID.Views.Pages;

/// <summary>
///     ModelSelectPromptView.xaml 的交互逻辑
/// </summary>
public partial class ShapeSelectionPage
{
    public ShapeSelectionPage()
    {
        InitializeComponent();

        using var service = new ShapeSelector(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new ShapeSelectionViewModel(service);

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
                .Select(_ => SelectionType.ById)
                .BindTo(ViewModel, x => x.SelectionType)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.ByMasterButton.IsChecked)
                .Where(isChecked => isChecked is true)
                .Select(_ => SelectionType.ByMasters)
                .BindTo(ViewModel, vm => vm.SelectionType)
                .DisposeWith(d);

            ViewModel.WhenAnyValue(x => x.SelectionType)
                .Subscribe(v =>
                {
                    if (v == SelectionType.ById)
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
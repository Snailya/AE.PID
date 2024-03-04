using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using AE.PID.Controllers.Services;
using AE.PID.Models;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     ModelSelectPromptView.xaml 的交互逻辑
/// </summary>
public partial class ShapeSelectionView
{
    public ShapeSelectionView()
    {
        InitializeComponent();
        ViewModel = new ShapeSelectionViewModel(new ShapeSelector(Globals.ThisAddIn.Application.ActiveDocument));

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
            
            this.BindCommand(ViewModel,
                    viewModel => viewModel.Select,
                    view => view.OkButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    viewModel => viewModel.Cancel,
                    view => view.CancelButton)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ByIdButton.IsChecked)
                .Where(isChecked => isChecked is true)
                .Select(x=>SelectionType.ById)
                .BindTo(ViewModel, x => x.SelectionType)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.ByMasterButton.IsChecked)
                .Where(isChecked => isChecked is true)
                .Select(x=>SelectionType.ByMasters)
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

            ViewModel.WhenAnyObservable(
                    x => x.Cancel,
                    x => x.Select
                )
                .Subscribe(_ => Close());
        });
    }
}
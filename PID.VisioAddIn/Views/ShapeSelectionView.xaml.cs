using System;
using System.Reactive.Disposables;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     ModelSelectPromptView.xaml 的交互逻辑
/// </summary>
public partial class ShapeSelectionView : ReactiveUserControl<ShapeSelectionViewModel>
{
    public ShapeSelectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposableRegistration =>
        {
            ViewModel = new ShapeSelectionViewModel();
            this.Bind(ViewModel, viewModel => viewModel.IsByIdChecked, view => view.ByIdButton.IsChecked)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel, viewModel => viewModel.IsByMastersChecked, view => view.ByMasterButton.IsChecked)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, viewModel => viewModel.IsByIdChecked, view => view.IdTextBox.IsEnabled)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel, viewModel => viewModel.ShapeId, view => view.IdTextBox.Text)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel, viewModel => viewModel.Select, view => view.SelectButton)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel, viewModel => viewModel.Cancel, view => view.CancelButton)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, viewModel => viewModel.Masters, view => view.MastersListView.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, viewModel => viewModel.IsByMastersChecked,
                    view => view.MastersListView.IsEnabled)
                .DisposeWith(disposableRegistration);
        });

        this.WhenAnyObservable(x => x.ViewModel.Cancel).Subscribe(x => Close());
    }

    private void Close()
    {
    }
}
﻿using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AE.PID.Models;
using AE.PID.Tools;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class MaterialsSelectionPage
{
    public MaterialsSelectionPage() : base("Material Selection")
    {
        InitializeComponent();

        ViewModel = new DesignMaterialsViewModel();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.Categories,
                    v => v.CategoryTree.ItemsSource)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.CategoryTree.SelectedItem)
                .BindTo(ViewModel, vm => vm.SelectedCategory)
                .DisposeWith(d);

            this.Bind(ViewModel,
                    vm => vm.UserFiltersViewModel,
                    v => v.Conditions.ViewModel)
                .DisposeWith(d);

            this.OneWayBind(ViewModel,
                    vm => vm.LastUsed,
                    v => v.LastUsedGrid.ItemsSource)
                .DisposeWith(d);

            this.OneWayBind(ViewModel,
                    vm => vm.ValidMaterials,
                    v => v.DesignMaterialsGrid.ItemsSource)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                vm => vm.Load,
                v => v.DesignMaterialsGrid,
                "LoadMore");

            // close the host window on close button clicked
            this.BindCommand(ViewModel,
                    vm => vm.Close,
                    v => v.CloseButton)
                .DisposeWith(d);
            ViewModel.WhenAnyObservable(x => x.Close)
                .Subscribe(_ => Close())
                .DisposeWith(d);

            // bind to all double-clicked rows to selected property
            Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => DesignMaterialsGrid.MouseDoubleClick += handler,
                    handler => DesignMaterialsGrid.MouseDoubleClick -= handler)
                .Merge(Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => LastUsedGrid.MouseDoubleClick += handler,
                    handler => LastUsedGrid.MouseDoubleClick -= handler))
                .Select(GetHitDataGridRow)
                .WhereNotNull()
                .Select(row => row.Item)
                .Cast<DesignMaterial>()
                .InvokeCommand(ViewModel?.Select)
                .DisposeWith(d);
        });
    }

    private static DataGridRow? GetHitDataGridRow(EventPattern<MouseButtonEventArgs> e)
    {
        if (e.Sender is not DataGrid dataGrid) return null;

        var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.EventArgs.GetPosition(dataGrid));
        return hitTestResult.VisualHit.FindParent<DataGridRow>();
    }
}
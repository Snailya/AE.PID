using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AE.PID.Tools;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class ProjectExplorerPage
{
    public ProjectExplorerPage() : base("Project Explorer")
    {
        InitializeComponent();

        ViewModel = new ProjectExplorerPageViewModel();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    x => x.IsLoading,
                    x => x.BusyIndicator.IsBusy)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.StructureMaterials,
                    v => v.StructureMaterials.ItemsSource)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.FlattenMaterials,
                    v => v.FlattenMaterials.ItemsSource)
                .DisposeWith(d);
            this.Bind(ViewModel,
                    vm => vm.OkCancelFeedbackViewModel,
                    v => v.Feedback.ViewModel)
                .DisposeWith(d);

            this.BindCommand(ViewModel,
                    vm => vm.CopyMaterial,
                    v => v.CopyMaterial)
                .DisposeWith(d);
            this.BindCommand(ViewModel,
                    vm => vm.PasteMaterial,
                    v => v.PasteMaterial)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.ExportToPage, v => v.ExportToPageButton).DisposeWith(d);

            //set selected item for right mouse click event, otherwise the context menu not work as expected
            Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => StructureMaterials.PreviewMouseRightButtonDown += handler,
                    handler => StructureMaterials.PreviewMouseRightButtonDown -= handler)
                .Subscribe(SetSelected)
                .DisposeWith(d);

            // after selecting d_bom for a selected element, a new instance of an element will be created as the d_bom property updated.
            // so the previous selection will be lost because it points to an address not exist
            // therefore, instead directly two-way bind the SelectedItem to the ViewModel.Selected property, only update the viewmodel as the SelectedItem is not null
            this.WhenAnyValue(x => x.StructureMaterials.SelectedItem)
                .Cast<StructureMaterialLocationViewModel>()
                .Select(x => (int)x.Type >= 3 ? x as MaterialLocationViewModel : null)
                .Merge(Observable.FromEventPattern<SelectionChangedEventHandler, SelectionChangedEventArgs>(
                        handler => FlattenMaterials.SelectionChanged += handler,
                        handler => FlattenMaterials.SelectionChanged -= handler
                    )
                    .Select(x => FlattenMaterials.SelectedItem)
                    .Cast<MaterialLocationViewModel>()
                )
                .BindTo(ViewModel, vm => vm.Selected)
                .DisposeWith(d);
        });
    }

    private static void SetSelected(EventPattern<MouseButtonEventArgs> e)
    {
        if (e.Sender is not TreeListView container) return;

        var hitTestResult = VisualTreeHelper.HitTest(container, e.EventArgs.GetPosition(container));
        var item = hitTestResult.VisualHit.FindParent<TreeListViewItem>();
        if (item != null)
            item.IsSelected = true;
    }
}
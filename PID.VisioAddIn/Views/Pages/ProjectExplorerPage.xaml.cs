using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AE.PID.Models;
using AE.PID.Services;
using AE.PID.Tools;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class ProjectExplorerPage
{
    public ProjectExplorerPage()
    {
        InitializeComponent();

        var service = new ProjectService(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new ProjectExplorerPageViewModel(service);

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.DocumentInfo,
                    v => v.DocumentInfo.ViewModel)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, 
                    x => x.IsBusy, 
                    x => x.BusyIndicator.IsBusy)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.ElementTree,
                    v => v.Elements.ItemsSource)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.PartListItems,
                    v => v.PartItems.ItemsSource)
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
            
            //set selected item for right mouse click event, otherwise the context menu not work as expected
            Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => Elements.PreviewMouseRightButtonDown += handler,
                    handler => Elements.PreviewMouseRightButtonDown -= handler)
                .Subscribe(SetSelected)
                .DisposeWith(d);

            // after selecting d_bom for selected element, a new instance of element will be created as the d_bom property updated.
            // so the previous selection will be lost because it points to an address not exist
            // therefore, instead directly two-way bind the SelectedItem to the ViewModel.Selected property, only update the viewmodel as the SelectedItem is not null
            this.WhenAnyValue(x => x.Elements.SelectedItem)
                .Where(x => x is TreeNodeViewModel<ElementBase>)
                .Select(x => ((TreeNodeViewModel<ElementBase>)x).Source)
                .Merge(this.WhenAnyValue(x => x.PartItems.SelectedItem)) // todo: this not work for filterdatagrid
                .Where(x => x is PartItem)
                .WhereNotNull()
                .Cast<PartItem>()
                .WhereNotNull()
                .BindTo(ViewModel, vm => vm.Selected)
                .DisposeWith(d);

            // highlight the item on page
            this.WhenAnyValue(x => x.ViewModel!.Selected)
                .WhereNotNull()
                // .Delay(TimeSpan.FromMilliseconds(300))
                .ObserveOn(ThisAddIn.Dispatcher!)
                .Subscribe(x => x.Select())
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
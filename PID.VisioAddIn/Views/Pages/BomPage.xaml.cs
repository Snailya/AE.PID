using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AE.PID.Controllers.Services;
using AE.PID.Models.BOM;
using AE.PID.Tools;
using AE.PID.ViewModels;
using AE.PID.ViewModels.Pages;
using AE.PID.Views.Controls;
using ReactiveUI;

namespace AE.PID.Views.Pages;

public partial class BomPage
{
    private readonly MaterialsSelectionPage _sidePage = new();

    public BomPage()
    {
        InitializeComponent();

        var service = new DocumentExporter(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new BomViewModel(service);

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.DocumentInfo,
                    v => v.DocumentInfo.ViewModel)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.BOMTree,
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


            // set selected item for right mouse click event, otherwise the context menu not work as expected
            Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
                    handler => Elements.PreviewMouseRightButtonDown += handler,
                    handler => Elements.PreviewMouseRightButtonDown -= handler)
                .Subscribe(SetSelected)
                .DisposeWith(d);

            // after selecting d_bom for selected element, a new instance of element will be create as the d_bom property updated.
            // so the previous selection will be lost because it point to a address not exist
            // therefore, instead directly two way bind the SelectedItem to the ViewModel.Selected property, only update the viewmodel as the SelectedItem is not null
            this.WhenAnyValue(x => x.Elements.SelectedItem)
                .Where(x => x is TreeNodeViewModel<Element>)
                .Select(x => ((TreeNodeViewModel<Element>)x).Source)
                .BindTo(ViewModel, vm => vm.Selected)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.PartItems.SelectedItem)
                .Where(x => x is PartItem)
                .Select(x => (PartItem)x)
                .BindTo(ViewModel, vm => vm.Selected)
                .DisposeWith(d);

            // highlight the item on page
            ViewModel.WhenAnyValue(x => x.Selected)
                .WhereNotNull()
                .Subscribe(x => x.Select())
                .DisposeWith(d);
            
            // when user click any of the item in bom, open the material selection page in side window
            ViewModel.WhenAnyValue(x => x.Selected)
                .Subscribe(selected => { Globals.ThisAddIn.WindowManager.SideShow(_sidePage); })
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
using System;
using System.Reactive.Disposables;
using AE.PID.Controllers.Services;
using AE.PID.ViewModels;
using AE.PID.ViewModels.Pages;
using ReactiveUI;

namespace AE.PID.Views.Pages;

public partial class ExportPage
{
    private readonly MaterialsSelectionPage _sidePage = new();

    public ExportPage()
    {
        InitializeComponent();

        var service = new DocumentExporter(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new ExportViewModel(service);

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.DocumentInfo,
                    v => v.DocumentInfo.ViewModel)
                .DisposeWith(d);
            this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.Elements.ItemsSource)
                .DisposeWith(d);
            this.Bind(ViewModel,
                    vm => vm.OkCancelFeedbackViewModel,
                    v => v.Feedback.ViewModel)
                .DisposeWith(d);

            // after selecting d_bom for selected element, a new instance of element will be create as the d_bom property updated.
            // so the previous selection will be lost because it point to a address not exist
            // therefore, instead directly two way bind the SelectedItem to the ViewModel.Selected property, only update the viewmodel as the SelectedItem is not null
            this.WhenAnyValue(x => x.Elements.SelectedItem)
                .WhereNotNull()
                .Subscribe(x => ViewModel.Selected = (ElementViewModel)x)
                .DisposeWith(d);

            // when user click any of the item in bom, open the material selection page in side window
            ViewModel.WhenAnyValue(x => x.Selected)
                .WhereNotNull()
                .Subscribe(_ => { Globals.ThisAddIn.WindowManager.SideShow(_sidePage); })
                .DisposeWith(d);
        });
    }
}
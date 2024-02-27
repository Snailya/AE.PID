using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using AE.PID.Controllers.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

public partial class ExportView
{
    public ExportView()
    {
        InitializeComponent();

        var service = new DocumentExporter(Globals.ThisAddIn.Application.ActivePage);
        ViewModel = new ExportViewModel(service);

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.DocumentInfo,
                    v => v.DocumentInfo.ViewModel)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.Elements.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel,
                    vm => vm.Selected,
                    v => v.DesignMaterialsHost.IsOpen,
                    x => x != null)
                .DisposeWith(disposableRegistration);

            this.Bind(ViewModel,
                    vm => vm.DesignMaterialsViewModel,
                    v => v.DesignMaterialsSelectionControl.ViewModel)
                .DisposeWith(disposableRegistration);

            this.BindCommand(ViewModel,
                    vm => vm.Submit,
                    v => v.SubmitButton)
                .DisposeWith(disposableRegistration);
            this.BindCommand(ViewModel,
                    vm => vm.Cancel,
                    v => v.CancelButton)
                .DisposeWith(disposableRegistration);

            // after selecting d_bom for selected element, a new instance of element will be create as the d_bom property updated.
            // so the previous selection will be lost because it point to a address not exist
            // therefore, instead directly two way bind the SelectedItem to the ViewModel.Selected property, only update the viewmodel as the SelectedItem is not null
            this.WhenAnyValue(x => x.Elements.SelectedItem)
                .WhereNotNull()
                .Subscribe(x => ViewModel.Selected = (ElementViewModel)x)
                .DisposeWith(disposableRegistration);

            // whenever the submit or cancel button is clicked, close the current window
            this.WhenAnyObservable(
                    x => x.ViewModel!.Cancel,
                    x => x.ViewModel!.Submit
                )
                .Subscribe(_ =>
                {
                    DesignMaterialsHost.IsOpen = false; // close the popup first
                    Close();
                })
                .DisposeWith(disposableRegistration);

            // as the window move or resize, the design materials panel will not change along with the window, so we need to observe on the window LocationChange and SizeChange event
            // then reset the size and location of the host.
            var window = Window.GetWindow(this);
            Observable.FromEventPattern(
                    handler => window!.LocationChanged += handler,
                    handler => window!.LocationChanged -= handler)
                .Select(x => x.Sender)
                .Merge(Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
                    handler => window!.SizeChanged += handler,
                    handler => window!.SizeChanged -= handler
                ).Select(x => x.Sender))
                .ObserveOnDispatcher()
                .Subscribe(x =>
                    {
                        if (x is not Window w) return;

                        DesignMaterialsHost.HorizontalOffset--; // change the value other wise it will not invoke position change so that it will not re render
                        DesignMaterialsHost.HorizontalOffset = w.ActualWidth - 8;

                        DesignMaterialsHost.Height = ((UserControl)w.Content).Height;
                    }
                )
                .DisposeWith(disposableRegistration);
        });
    }
}
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AE.PID.Client.Infrastructure.VisioExt;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Client.UI.Avalonia.VisioExt;
using AE.PID.Client.VisioAddIn.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Office.Interop.Visio;
using ReactiveUI;
using Window = Microsoft.Office.Interop.Visio.Window;

namespace AE.PID.Client.VisioAddIn;

public static class WindowHelper
{
    private static readonly Dictionary<string, object> Opened = new();

    public static void ShowTaskPane<TView, TViewModel>(string caption,
        Action<Shape, TViewModel> onSelectionChanged)
        where TView : UserControl where TViewModel : ViewModelBase
    {
        var viewName = typeof(TView).Name;

        if (Opened.TryGetValue(viewName, out var control) && control is TView)
        {
            // if there is already an opened window with the same id, no need to re-create it.
        }
        else
        {
            var scope = ThisAddIn.Services.CreateScope();

            Observable.Start(() =>
                {
                    var viewModel = scope.ServiceProvider.GetRequiredService<TViewModel>();
                    // update the viewmodel when selection change
                    var subscription = Observable
                        .FromEvent<EWindow_SelectionChangedEventHandler, Window>(
                            handler => Globals.ThisAddIn.Application.ActiveWindow.SelectionChanged += handler,
                            handler => Globals.ThisAddIn.Application.ActiveWindow.SelectionChanged -= handler,
                            SchedulerManager.VisioScheduler
                        )
                        .Where(x => x.Selection.Count == 1 && x.Selection[1] is { } shape)
                        .Select(x => x.Selection[1])
                        .Subscribe(shape => onSelectionChanged(shape, viewModel)); // todo:不知道为什么第一次打开窗口的时候，总是无法触发这个订阅

                    var view = Activator.CreateInstance<TView>();
                    view.DataContext = viewModel;

                    var pane = new VisioTaskPane(view);

                    pane.HandleDestroyed += (_, _) =>
                    {
                        // remove the window from the opened window list
                        Opened.Remove(viewName);

                        // dispose the selection change event handler as the pane is no longer visible
                        subscription.Dispose();

                        // Dispose the scope and any services tied to it when the window closes
                        scope.Dispose();
                    };

                    return new { pane, viewModel };
                }, RxApp.MainThreadScheduler)
                .ObserveOn(SchedulerManager.VisioScheduler)
                .Select(v =>
                {
                    // set up the initial value
                    onSelectionChanged(Globals.ThisAddIn.Application.ActiveWindow.Selection[1], v.viewModel);

                    var hostWindow = Globals.ThisAddIn.Application.ActiveWindow.Windows.Add(caption,
                        VisWindowStates.visWSVisible | VisWindowStates.visWSAnchorRight |
                        VisWindowStates.visWSAnchorTop,
                        VisWinTypes.visAnchorBarAddon,
                        0, 0,
                        v.pane.Width, v.pane.Height);

                    return new { v.pane, hostWindow };
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(v =>
                {
                    SetOwner(v.pane.Handle, new IntPtr(v.hostWindow.WindowHandle32));

                    // record as an opened window
                    Opened.Add(viewName, v.pane);
                });
        }
    }

    private static void SetOwner(IntPtr window, IntPtr parent)
    {
        User32.SetParent(window, parent);
    }
}
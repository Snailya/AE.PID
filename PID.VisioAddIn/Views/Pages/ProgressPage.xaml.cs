using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using AE.PID.Core.Models;
using AE.PID.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class ProgressPage
{
    public ProgressPage(ProgressPageViewModel progressViewModel) : base("Progress")
    {
        InitializeComponent();

        ViewModel = progressViewModel;

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.ToggleExpand, v => v.ExpandButton).DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.ExpandButton.Content, b => b ? "隐藏" : "展开")
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.Log.Visibility,
                b => b ? Visibility.Visible : Visibility.Collapsed).DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.ProgressValue.Message, v => v.Message.Text).DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.ProgressValue.Value, v => v.ProgressBar.Value).DisposeWith(d);

            this.WhenAnyValue(x => x.Log.Visibility)
                .Subscribe(_ =>
                {
                    if (Window.GetWindow(this) is { } window)
                        Dispatcher.BeginInvoke(() =>
                        {
                            window.SizeToContent = SizeToContent.Manual;
                            window.SizeToContent = SizeToContent.WidthAndHeight;
                        }, DispatcherPriority.Background);
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.ProgressValue.Message)
                .Where(x => !string.IsNullOrEmpty(x))
                .Subscribe(message =>
                {
                    Log.AppendText($"{message}\n");
                    Log.ScrollToEnd();
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.ProgressValue.Status)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(x => Debug.WriteLine($"Observe status on {Thread.CurrentThread.Name}"))
                .Subscribe(status =>
                {
                    switch (status)
                    {
                        case TaskStatus.Created:
                        case TaskStatus.Running:
                            ProgressBar.IsIndeterminate = true;
                            break;
                        case TaskStatus.OnError:
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                            break;
                        case TaskStatus.RanToCompletion:
                            Close();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(status), status, null);
                    }
                })
                .DisposeWith(d);
        });
    }
}
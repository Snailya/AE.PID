using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using AE.PID.Services;
using AE.PID.ViewModels;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for MockPage.xaml
/// </summary>
public partial class ProgressPage
{
    public ProgressPage(ProgressPageViewModel progressViewModel)
    {
        InitializeComponent();

        ViewModel = progressViewModel;

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.ProgressValue.Value, v => v.ProgressBar.Value).DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.ProgressValue.Message)
                .Subscribe(msg =>
                {
                    Message.Inlines.Add(new Run(msg));
                    Message.Inlines.Add(new LineBreak());
                })
                .DisposeWith(d);
            
            this.WhenAnyValue(x => x.ViewModel!.ProgressValue.Status)
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
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
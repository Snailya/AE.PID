using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for OkCancelControl.xaml
/// </summary>
public partial class OkCancelFeedback
{
    public static readonly DependencyProperty IsCancelButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsCancelButtonVisible), typeof(bool), typeof(OkCancelFeedback), new PropertyMetadata(true));

    public static readonly DependencyProperty OkTextProperty = DependencyProperty.Register(
        nameof(OkText), typeof(string), typeof(OkCancelFeedback), new PropertyMetadata("确认"));

    public static readonly DependencyProperty CancelTextProperty = DependencyProperty.Register(
        nameof(CancelText), typeof(string), typeof(OkCancelFeedback), new PropertyMetadata("取消"));

    public static readonly DependencyProperty CloseOnOkProperty = DependencyProperty.Register(
        nameof(CloseOnOk), typeof(bool), typeof(OkCancelFeedback), new PropertyMetadata(true));

    public OkCancelFeedback()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Message, v => v.Message.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Ok, v => v.OkButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Cancel, v => v.CancelButton)
                .DisposeWith(d);

            ViewModel.WhenAnyObservable(x => x.Cancel)
                .Merge(
                    ViewModel.WhenAnyObservable(x => x.Ok)
                        .Where(_ => CloseOnOk)
                )
                .Subscribe(_ => Close())
                .DisposeWith(d);
        });
    }

    public bool IsCancelButtonVisible
    {
        get => (bool)GetValue(IsCancelButtonVisibleProperty);
        set => SetValue(IsCancelButtonVisibleProperty, value);
    }

    public bool CloseOnOk
    {
        get => (bool)GetValue(CloseOnOkProperty);
        set => SetValue(CloseOnOkProperty, value);
    }

    public string OkText
    {
        get => (string)GetValue(OkTextProperty);
        set => SetValue(OkTextProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }
}
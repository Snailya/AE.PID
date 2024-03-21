using System;
using System.Reactive.Disposables;
using System.Windows;
using ReactiveUI;

namespace AE.PID.Views.Components;

/// <summary>
/// Interaction logic for OkCancelControl.xaml
/// </summary>
public partial class OkCancelFeedback
{
    public static readonly DependencyProperty OkTextProperty = DependencyProperty.Register(
        nameof(OkText), typeof(string), typeof(OkCancelFeedback), new PropertyMetadata("确认"));

    public string OkText
    {
        get => (string)GetValue(OkTextProperty);
        set => SetValue(OkTextProperty, value);
    }

    public static readonly DependencyProperty CancelTextProperty = DependencyProperty.Register(
        nameof(CancelText), typeof(string), typeof(OkCancelFeedback), new PropertyMetadata("取消"));

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

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

            ViewModel.WhenAnyObservable(x => x.Ok, x => x.Cancel)
                .Subscribe(_ => Close())
                .DisposeWith(d);
        });
    }
}
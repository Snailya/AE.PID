using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using AE.PID.Converters;

namespace AE.PID.Views.Windows;

public class SecondaryWindow : WindowBase
{
    public SecondaryWindow(Window owner) : base(owner)
    {
        ShowInTaskbar = false;
        WindowButtonStyle = WindowButton.CloseOnly;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // binding top
        SetBinding(TopProperty,
            new Binding
            {
                Path = new PropertyPath("Top"),
                Source = owner,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay,
            });
        
        // binding left
        // todo: still not work as expected, I want to when the user change the main window right or secondary window left, the two window 's total width not change
        SetBinding(LeftProperty,
            new Binding
            {
                Source = owner,
                Path = new PropertyPath("Left"),
                Converter = new SecondaryWindowLeftConvertor(),
                ConverterParameter = owner,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });

        // binding height
        var heightBinding = new Binding
        {
            Path = new PropertyPath("ActualHeight"),
            Source = owner,
            Mode = BindingMode.OneWay
        };
        SetBinding(MaxHeightProperty, heightBinding);
        SetBinding(MinHeightProperty, heightBinding);

        // binding max width
        SetBinding(MaxWidthProperty, new MultiBinding
        {
            Converter = new SecondaryWindowMaxWidthConvertor(),
            Mode = BindingMode.OneWay,
            Bindings = { new Binding("ActualWidth") { Source = owner }, new Binding("Left") { Source = owner } }
        });
    }
}
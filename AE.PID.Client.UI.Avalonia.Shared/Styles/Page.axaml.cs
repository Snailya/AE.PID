﻿using Avalonia;
using Avalonia.Controls;

namespace AE.PID.Client.UI.Avalonia.Shared;

public class Page : ContentControl
{
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<Page, bool>(
        nameof(IsLoading));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}
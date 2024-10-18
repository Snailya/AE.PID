using System;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace AE.PID.Visio.UI.Avalonia.Views;

public partial class MaterialsView : ReactiveUserControl<MaterialsViewModel>
{
    public MaterialsView()
    {
        InitializeComponent();
    }
}
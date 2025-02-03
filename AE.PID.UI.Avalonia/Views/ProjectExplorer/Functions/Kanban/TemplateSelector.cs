using System.Collections.Generic;
using AE.PID.Client.UI.Avalonia;
using AE.PID.Core.Models;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace AE.PID.UI.Avalonia.Views;

public class FunctionLocationPropertiesSelector : IDataTemplate
{
    // This Dictionary should store our shapes. We mark this as [Content], so we can directly add elements to it later.
    [Content] public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new();

    // Build the DataTemplate here
    public Control Build(object? param)
    {
        var key = (param as FunctionLocationPropertiesViewModel)!.FunctionType switch
        {
            FunctionType.ProcessZone => nameof(FunctionType.ProcessZone),
            FunctionType.FunctionGroup => nameof(FunctionType.FunctionGroup),
            FunctionType.FunctionUnit => nameof(FunctionType.FunctionUnit),
            FunctionType.Equipment => nameof(FunctionType.Equipment),
            FunctionType.Instrument => nameof(FunctionType.Instrument),
            FunctionType.FunctionElement => nameof(FunctionType.FunctionElement),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(key)) return null;

        if (AvailableTemplates.TryGetValue(key, out var template))
            return
                template.Build(
                    param); // finally we look up the provided key and let the System build the DataTemplate for us

        return null;
    }

    // Check if we can accept the provided data
    public bool Match(object? data)
    {
        return data is FunctionLocationPropertiesViewModel;
    }
}
using System;
using System.Windows.Controls;
using AE.PID.Tools;
using DynamicData.Binding;

namespace AE.PID.Views.Controls;

public class AutoColumnsDataGrid : DataGrid
{
    private IDisposable? _cleanup;

    public AutoColumnsDataGrid()
    {
        Loaded += (_, _) =>
        {
            _cleanup = Items.ObserveCollectionChanges()
                .Subscribe(_ => this.PopulateColumns());
        };

        Unloaded += (_, _) => { _cleanup?.Dispose(); };
    }
}
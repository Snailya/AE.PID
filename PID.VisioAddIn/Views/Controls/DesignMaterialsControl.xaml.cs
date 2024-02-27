using AE.PID.Controllers.Services;
using AE.PID.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AE.PID.Views.Controls;

/// <summary>
/// Interaction logic for DesignMaterialsSelectionControl.xaml
/// </summary>
public partial class DesignMaterialsControl
{
    public DesignMaterialsControl()
    {
        InitializeComponent();

        this.WhenActivated(disposableRegistration =>
        {
            this.OneWayBind(ViewModel,
                    vm => vm.Seed.Name,
                    v => v.ElementTextBlock.Text)
                .DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel,
                    vm => vm.Items,
                    v => v.DesignMaterialsGrid.ItemsSource)
                .DisposeWith(disposableRegistration);
            this.Bind(ViewModel,
                    vm => vm.Selected,
                    v => v.DesignMaterialsGrid.SelectedItem)
                .DisposeWith(disposableRegistration);

            this.BindCommand(ViewModel,
                    vm => vm.Select,
                    v => v.SubmitButton)
                .DisposeWith(disposableRegistration);

            ViewModel.WhenAnyValue(x => x.Columns)
                .Subscribe(columns =>
                {
                    DesignMaterialsGrid.Columns.Clear();

                    DesignMaterialsGrid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });
                    DesignMaterialsGrid.Columns.Add(new DataGridTextColumn { Header = "Id", Binding = new Binding("Id") });

                    var array = columns.ToArray();

                    for (var i = 0; i < array.Length; i++)
                    {
                        var binding = new Binding
                        {
                            Path = new PropertyPath($"Properties[{i}].Value")
                        };

                        DesignMaterialsGrid.Columns.Add(new DataGridTextColumn { Header = array[i], Binding = binding });
                    }
                });
        });
    }
}
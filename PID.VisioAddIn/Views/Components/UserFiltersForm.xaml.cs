using System.Reactive.Disposables;
using ReactiveUI;

namespace AE.PID.Views;

/// <summary>
///     Interaction logic for DesignMaterialsSelectionControl.xaml
/// </summary>
public partial class UserFiltersForm
{
    public UserFiltersForm()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.Bind(ViewModel, vm => vm.Name, v => v.NameInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.Brand, v => v.BrandInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.Specifications, v => v.SpecificationsInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.Model, v => v.ModelInput.Text)
                .DisposeWith(d);
            this.Bind(ViewModel, vm => vm.Manufacturer, v => v.ManufacturerInput.Text)
                .DisposeWith(d);
        });
    }
}
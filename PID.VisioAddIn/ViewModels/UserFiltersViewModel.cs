using ReactiveUI;

namespace AE.PID.ViewModels;

public class UserFiltersViewModel : ViewModelBase
{
    private string _brand = string.Empty;
    private string _manufacturer = string.Empty;
    private string _model = string.Empty;
    private string _name = string.Empty;
    private string _specifications = string.Empty;

    #region Read-Write Properties

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Brand
    {
        get => _brand;
        set => this.RaiseAndSetIfChanged(ref _brand, value);
    }

    public string Specifications
    {
        get => _specifications;
        set => this.RaiseAndSetIfChanged(ref _specifications, value);
    }

    public string Model
    {
        get => _model;
        set => this.RaiseAndSetIfChanged(ref _model, value);
    }

    public string Manufacturer
    {
        get => _manufacturer;
        set => this.RaiseAndSetIfChanged(ref _manufacturer, value);
    }

    #endregion
}
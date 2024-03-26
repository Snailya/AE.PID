using System;

namespace AE.PID.Models.BOM;

public record DesignMaterialsQueryTerms
{
    private int _categoryId;
    private int _pageNumber;

    public DesignMaterialsQueryTerms(int categoryId, int pageNumber = 1)
    {
        _categoryId = categoryId;
        _pageNumber = pageNumber;
    }

    /// <summary>
    ///     The code of the category, category code could not be empty
    /// </summary>
    public int CategoryId
    {
        get => _categoryId;
        set
        {
            if (value == default) throw new ArgumentException("CategoryId must be specified");
            _categoryId = value;
        }
    }

    /// <summary>
    ///     The page number, must be large than 1
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set
        {
            if (value <= 0) throw new ArgumentException("PageNumber is not allowed to be less than 1");
            _pageNumber = value;
        }
    }

    public override string ToString()
    {
        return $"CategoryId:{CategoryId}, PageNumber:{PageNumber}";
    }
}

public record OptionalTerms(
    string MaterialName,
    string Brand,
    string Specifications,
    string Model,
    string Manufacturer)
{
    public string MaterialName { get; set; } = MaterialName;
    public string Brand { get; set; } = Brand;
    public string Specifications { get; set; } = Specifications;
    public string Model { get; set; } = Model;
    public string Manufacturer { get; set; } = Manufacturer;
}
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using DynamicData;

namespace AE.PID.Client.Infrastructure;

/// <summary>
///     The store service provides the ability to read and write the material location data from and to the source. The
///     source might be either a visio document as in this case or others with different implementation.
///     The word store here is equivalent to the word repository but to differ it from a real database.
/// </summary>
public class MaterialLocationStore : DisposableBase, IMaterialLocationStore
{
    private readonly PartListItemConvertor _convertor;
    private readonly IDataProvider _dataProvider;
    private readonly IExportService _exportService;
    private readonly ILocalCacheService _localCacheService;

    /// <summary>
    /// </summary>
    /// <param name="dataProvider">
    ///     implementation for getting the material location source, and highlight the shape in the
    ///     source.
    /// </param>
    /// <param name="functionLocationStore">implementation for getting the function info by providing only the function id.</param>
    /// <param name="resolver"></param>
    /// <param name="localCacheService"></param>
    /// <param name="exportService"></param>
    public MaterialLocationStore(
        IDataProvider dataProvider,
        IFunctionLocationStore functionLocationStore,
        IMaterialResolver resolver,
        ILocalCacheService localCacheService,
        IExportService exportService
    )
    {
        _dataProvider = dataProvider;
        _localCacheService = localCacheService;
        _exportService = exportService;

        _convertor = new PartListItemConvertor(functionLocationStore, resolver);

        MaterialLocations = _dataProvider.MaterialLocations.Connect()
            .Transform(x =>
                new ValueTuple<MaterialLocation, Lazy<Task<ResolveResult<Material?>>>>(x,
                    new Lazy<Task<ResolveResult<Material?>>>(
                        () => resolver.ResolvedAsync(x.Code))))
            .AsObservableCache();
    }

    public IObservableCache<(MaterialLocation Location, Lazy<Task<ResolveResult<Material?>>> Material), ICompoundKey>
        MaterialLocations { get; }


    public void Update(MaterialLocation[] locations)
    {
        _dataProvider.UpdateMaterialLocations(locations);
    }

    public Task Locate(ICompoundKey id)
    {
        if (_dataProvider is ISelectable selectable) selectable.Select([id]);
        return Task.CompletedTask;
    }

    public async Task ExportPartList(string? fileName = null)
    {
        var parts = await _convertor.ConvertAsync(_dataProvider.MaterialLocations.Items);

        if (fileName != null)
             _exportService.ExportAsPartLists(parts, fileName);
        else if (_dataProvider is IOleSupport excel) excel.InsertAsExcelSheet(ToDataArray(parts.ToArray()));
    }

    public Task ExportProcurementList(string fileName)
    {
        throw new NotImplementedException();
    }


    public async void Save()
    {
        try
        {
            var tasks = MaterialLocations.Items.Select(x => x.Material.Value).ToList();

            var materials = (await Task.WhenAll(tasks)).Select(x => x?.Value).Where(x => x != null)
                .Cast<Material>().ToArray();

            _localCacheService.AddRange(materials);
        }
        catch (AggregateException ex)
        {
            // todo: not implement
            Debugger.Break();
        }
    }


    private static string[,] ToDataArray(PartListItem[] list)
    {
        var array = new string[list.Length + 2, 7];

        // append column name
        array[0, 0] = "序号";
        array[0, 1] = "功能元件";
        array[0, 2] = "描述";
        array[0, 3] = "供应商";
        array[0, 4] = "型号";
        array[0, 5] = "规格";
        array[0, 6] = "物料号";

        array[1, 0] = "Index";
        array[1, 1] = "Function Element";
        array[1, 2] = "Description";
        array[1, 3] = "Manufacturer";
        array[1, 4] = "Type";
        array[1, 5] = "Specification";
        array[1, 6] = "Material No.";

        // append data
        for (var i = 0; i < list.Length; i++)
        {
            var line = list[i];
            array[i + 2, 0] = (i + 1).ToString();
            array[i + 2, 1] = line.FunctionalElement;
            array[i + 2, 2] = line.Description;
            array[i + 2, 3] = line.Supplier;
            array[i + 2, 4] = line.OrderType;
            array[i + 2, 5] = line.Specification;
            array[i + 2, 6] = line.MaterialNo;
        }

        return array;
    }
}
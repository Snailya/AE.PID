using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using DynamicData;

namespace AE.PID.Client.Infrastructure;

/// <summary>
///     The store service provides the ability to read and write the material location data from and to the source. The
///     source might be either a visio document as in this case, or others with different implementation.
///     The word store here is equivalent to the word repository but to differ it from a real database.
/// </summary>
public class MaterialLocationStore : DisposableBase, IMaterialLocationStore
{
    private readonly IDataProvider _dataProvider;
    private readonly IFunctionLocationStore _functionLocationStore;
    private readonly ILocalCacheService _localCacheService;
    private readonly IMaterialResolver _resolver;
    private readonly IStorageService _storageService;

    /// <summary>
    /// </summary>
    /// <param name="dataProvider">
    ///     implementation for getting the material location source, and highlight the shape in the
    ///     source.
    /// </param>
    /// <param name="functionLocationStore">implementation for getting the function info by providing only the function id.</param>
    /// <param name="resolver"></param>
    /// <param name="localCacheService"></param>
    /// <param name="storageService"></param>
    public MaterialLocationStore(
        IDataProvider dataProvider,
        IFunctionLocationStore functionLocationStore,
        IMaterialResolver resolver,
        ILocalCacheService localCacheService,
        IStorageService storageService
    )
    {
        _dataProvider = dataProvider;
        _localCacheService = localCacheService;
        _functionLocationStore = functionLocationStore;
        _resolver = resolver;
        _storageService = storageService;

        MaterialLocations = _dataProvider.MaterialLocations.Connect()
            .Transform(x =>
                new ValueTuple<MaterialLocation, Lazy<Task<ResolveResult<Material?>>>>(x,
                    new Lazy<Task<ResolveResult<Material?>>>(
                        () => resolver.ResolvedAsync(x.Code))))
            .AsObservableCache();
    }

    public void Load()
    {
        if (_dataProvider is ILazyLoad lazyLoad)
            lazyLoad.Load();
    }

    public IObservableCache<(MaterialLocation Location, Lazy<Task<ResolveResult<Material?>>> Material), ICompoundKey>
        MaterialLocations { get; }


    public void Update(MaterialLocation[] locations)
    {
        _dataProvider.MaterialLocationsUpdater.OnNext(locations);
    }

    public Task Locate(ICompoundKey id)
    {
        if (_dataProvider is ISelectable selectable) selectable.Select([id]);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExportAsWorkbook(string fileName)
    {
        var parts = await BuildPartListItems();
        await _storageService.SaveAsWorkbookAsync(fileName, new
        {
            Parts = parts,
            CustomerName = "customerName",
            DocumentNo = "documentNo",
            ProjectNo = "projectNo",
            VersionNo = "versionNo"
        });
    }


    /// <inheritdoc />
    public async Task ExportAsEmbeddedObject()
    {
        var parts = await BuildPartListItems();
        if (_dataProvider is IOleSupport excel)
            excel.InsertAsExcelSheet(ToDataArray(parts.ToArray()));
        return;

        string[,] ToDataArray(PartListItem[] list)
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
                array[i + 2, 0] = line.Index.ToString();
                array[i + 2, 1] = line.FunctionalElement;
                array[i + 2, 2] = line.Description;
                array[i + 2, 3] = line.Supplier;
                array[i + 2, 4] = line.Type;
                array[i + 2, 5] = line.Specification;
                array[i + 2, 6] = line.MaterialNo;
            }

            return array;
        }
    }

    /// <inheritdoc />
    public async void Save()
    {
        try
        {
            var tasks = MaterialLocations.Items.Select(x => x.Material.Value).ToList();

            var materials = (await Task.WhenAll(tasks)).Select(x => x?.Value).Where(x => x != null)
                .ToArray();

            _localCacheService.AddRange(materials);
        }
        catch (AggregateException ex)
        {
            // todo: not implement
            Debugger.Break();
        }
    }


    private async Task<PartListItem[]> BuildPartListItems()
    {
        // 2025.02.21: 首先将所有重要的属性展开
        var materialLocationsExt = MaterialLocations.Items.Select(x =>
            {
                var function = _functionLocationStore.Find(x.Location.Id);
                return (
                    FunctionLocation: function,
                    MaterialLocation: x.Location,
                    MaterialKey: string.IsNullOrEmpty(x.Location.Code)
                        ? x.Location.Category
                        : x.Location.Code,
                    x.Material
                );
            })
            .OrderBy(x => x.FunctionLocation)
            .ToList();

        // sort

        // 计算出数量字典
        var inGroupDic = materialLocationsExt
            .GroupBy(x =>
            (
                x.FunctionLocation?.Zone,
                x.FunctionLocation?.Group,
                x.MaterialKey
            ))
            .ToDictionary(g => g.Key,
                g =>
                    g.Sum(m => m.MaterialLocation.ComputedQuantity)
            );

        var inZoneDic = materialLocationsExt
            .GroupBy(x =>
            (
                x.FunctionLocation?.Zone,
                x.MaterialKey
            ))
            .ToDictionary(g => g.Key,
                g => g.Sum(m => m.MaterialLocation.ComputedQuantity)
            );

        // 转换为part list item
        var tasks = materialLocationsExt
            .Select(async (x, i) =>
            {
                var material = (await x.Material.Value).Value;
                var materialKey = string.IsNullOrEmpty(x.MaterialLocation.Code)
                    ? x.MaterialLocation.Category
                    : x.MaterialLocation.Code;

                var inGroupQuantity =
                    inGroupDic[(x.FunctionLocation?.Zone, x.FunctionLocation?.Group, materialKey)];
                var inZoneQuantity =
                    inZoneDic[(x.FunctionLocation?.Zone, materialKey)];
                return ToPartListItem(x.FunctionLocation, x.MaterialLocation, material, inGroupQuantity,
                    inZoneQuantity);
            }).ToList();

        var parts = await Task.WhenAll(tasks);

        // append index
        var i = 1;
        foreach (var part in parts)
        {
            part.Index = i;
            i++;
        }

        return parts;
    }


    private static PartListItem ToPartListItem(FunctionLocation? functionLocation, MaterialLocation materialLocation,
        Material? material, double inGroupQuantity, double inZoneQuantity)
    {
        return new PartListItem
        {
            // 2025.02.21: add the subclass property as the default material no if the material no. is missing.
            Category = materialLocation.Category,

            ProcessArea = functionLocation?.Zone ?? string.Empty,
            FunctionalGroup = functionLocation?.Group ?? string.Empty,
            FunctionalElement = functionLocation?.Element ?? string.Empty,
            Description = functionLocation?.Description ?? string.Empty,

            MaterialNo = materialLocation.Code,
            Count = materialLocation.ComputedQuantity,

            Specification = material?.Specifications ?? string.Empty,
            Type = material?.Type ?? string.Empty,
            TechnicalDataChinese = material?.TechnicalData ?? string.Empty,
            TechnicalDataEnglish = material?.TechnicalDataEnglish ?? string.Empty,
            Unit = material?.Unit ?? string.Empty,
            Supplier = material?.Supplier ?? string.Empty,
            ManufacturerMaterialNo = material?.ManufacturerMaterialNumber ?? string.Empty,
            Classification = material?.Classification ?? string.Empty,
            Attachment = material?.Attachment ?? string.Empty,

            InGroup = inGroupQuantity,
            Total = inZoneQuantity
        };
    }

    private class PartListItem
    {
        public string Category { get; set; }

        /// <summary>
        ///     序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     区域号
        /// </summary>
        public string ProcessArea { get; set; } = string.Empty;

        /// <summary>
        ///     功能组
        /// </summary>
        public string FunctionalGroup { get; set; } = string.Empty;

        /// <summary>
        ///     功能元件
        /// </summary>
        public string FunctionalElement { get; set; } = string.Empty;

        /// <summary>
        ///     物料号
        /// </summary>
        public string MaterialNo { get; set; } = string.Empty;

        /// <summary>
        ///     描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        ///     规格
        /// </summary>
        public string Specification { get; set; } = string.Empty;

        /// <summary>
        ///     技术参数-中文
        /// </summary>
        public string TechnicalDataChinese { get; set; } = string.Empty;

        /// <summary>
        ///     技术参数-英文
        /// </summary>
        public string TechnicalDataEnglish { get; set; } = string.Empty;

        /// <summary>
        ///     数量
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        ///     总数量
        /// </summary>
        public double Total { get; set; }

        /// <summary>
        ///     组内数量
        /// </summary>
        public double InGroup { get; set; }

        /// <summary>
        ///     单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        ///     供应商
        /// </summary>
        public string Supplier { get; set; } = string.Empty;

        /// <summary>
        ///     制造商物品编号
        /// </summary>
        public string ManufacturerMaterialNo { get; set; } = string.Empty;

        /// <summary>
        ///     型号
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        ///     分类
        /// </summary>
        public string Classification { get; set; } = string.Empty;

        /// <summary>
        ///     附件
        /// </summary>
        public string Attachment { get; set; } = string.Empty;
    }
}
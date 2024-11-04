using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Shared.Services;

/// <summary>
///     The store service provides the ability to read and write the material location data from and to the source. The
///     source might be either a visio document as in this case, or others with different implementation.
///     The word store here is equivalent to the word repository but to differ it from a real database.
/// </summary>
public class MaterialLocationStore : DisposableBase, IMaterialLocationStore
{
    private readonly IFunctionLocationStore _functionLocationStore;
    private readonly ILocalCacheService _localCacheService;
    private readonly IMaterialResolver _materialResolver;
    private readonly IStorageService _storageService;
    private readonly IVisioService _visioService;

    /// <summary>
    /// </summary>
    /// <param name="visioService">
    ///     implementation for getting the material location source, and highlight the shape in the
    ///     source.
    /// </param>
    /// <param name="functionLocationStore">implementation for getting the function info by providing only the function id.</param>
    /// <param name="materialResolver"></param>
    /// <param name="localCacheService"></param>
    /// <param name="storageService"></param>
    public MaterialLocationStore(
        IVisioService visioService,
        IFunctionLocationStore functionLocationStore,
        IMaterialResolver materialResolver,
        ILocalCacheService localCacheService,
        IStorageService storageService
        )
    {
        _visioService = visioService;
        _localCacheService = localCacheService;
        _functionLocationStore = functionLocationStore;
        _materialResolver = materialResolver;
        _storageService = storageService;

        // initialize the data
        MaterialLocations = visioService.MaterialLocations.Value;

        // observable property changes and propagate to Visio shape
        var propagateChangeToVisio = MaterialLocations.Connect()
            .AutoRefresh(propertyChangeThrottle: TimeSpan.FromMilliseconds(400))
            .WhereReasonsAre(ChangeReason.Refresh) // 当通过addorupdate更新sourcecache中的某个对象时，如果对象不存在，则reason为add，如果存在，则为update。当直接修改item时，则reason为refresh。
            .ObserveOn(SchedulerManager.VisioScheduler)
            .Subscribe(changes =>
            {
                foreach (var change in changes)
                {
                    visioService.UpdateShapeProperties(change.Key,
                        [
                            new ValuePatch(CellNameDict.MaterialCode, change.Current.Code, true),
                            new ValuePatch(CellNameDict.UnitQuantity, change.Current.UnitQuantity)
                        ]
                    );
                }
            });


        CleanUp.Add(propagateChangeToVisio);
    }

    /// <inheritdoc />
    public IObservableCache<MaterialLocation, CompositeId> MaterialLocations { get; }

    /// <inheritdoc />
    public Task Locate(CompositeId id)
    {
        _visioService.SelectAndCenterView(id);
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
        _visioService.InsertAsExcelSheet(ToDataArray(parts.ToArray()));
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
                array[i + 2, 0] = (i + 1).ToString();
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
        // append the item that is not in the cache
        var codes = MaterialLocations.Items.Select(x => x.Code)
            .Union(_localCacheService.GetMaterials().Select(x => x.Code))
            .Where(x => !string.IsNullOrEmpty(x));

        try
        {
            // fill codes

            var tasks = codes.Select(async x =>
            {
                // first try to get from the server
                try
                {
                    return await _materialResolver.GetMaterialByCodeAsync(x);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }).ToList();

            var materials = (await Task.WhenAll(tasks)).Select(x => x?.Value).Where(x => x != null)
                .ToArray();

            if (materials.Any())
                _localCacheService.PersistAsSolutionXml<Material, string>("materials", materials!, x => x.Code);
        }
        catch (AggregateException ex)
        {
            // todo: not implement
            Debugger.Break();
        }
    }


    private async Task<PartListItem[]> BuildPartListItems()
    {
        // convert the location into part list item
        var tasks = MaterialLocations.Items.Select(async x => await ToPartListItem(x)).ToList();
        var parts = await Task.WhenAll(tasks);

        // fill in-group and total quantity
        var grouped = parts
            .GroupBy(m => new
            {
                MaterialNo = string.IsNullOrEmpty(m.MaterialNo) ? Guid.NewGuid().ToString() : m.MaterialNo,
                m.FunctionalGroup
            })
            .Select(group => new
            {
                group.Key.MaterialNo,
                group.Key.FunctionalGroup,
                CountInGroup = group.Sum(m => m.Count),
                Total = parts.Where(m => m.MaterialNo == group.Key.MaterialNo).Sum(m => m.Count)
            });

        foreach (var group in grouped)
        foreach (var item in parts.Where(m =>
                     m.MaterialNo == group.MaterialNo && m.FunctionalGroup == group.FunctionalGroup))
        {
            item.InGroup = group.CountInGroup;
            item.Total = group.Total;
        }

        // fill index
        parts.Aggregate(0, (index, person) =>
        {
            person.Index = index + 1;
            return index + 1;
        });

        return parts;
    }

    private async Task<PartListItem> ToPartListItem(MaterialLocation location)
    {
        var functionLocation = _functionLocationStore.Find(location.LocationId);
        var material = await _materialResolver.GetMaterialByCodeAsync(location.Code);

        return new PartListItem
        {
            ProcessArea = functionLocation?.Zone ?? string.Empty,
            FunctionalGroup = functionLocation?.Group ?? string.Empty,
            FunctionalElement = functionLocation?.Name ?? string.Empty,
            Description = functionLocation?.Description ?? string.Empty,

            MaterialNo = location.Code,
            Count = location.Quantity,

            Specification = material?.Value.Specifications ?? string.Empty,
            Type = material?.Value.Type ?? string.Empty,
            TechnicalDataChinese = material?.Value.TechnicalData ?? string.Empty,
            TechnicalDataEnglish = material?.Value.TechnicalDataEnglish ?? string.Empty,
            Unit = material?.Value.Unit ?? string.Empty,
            Supplier = material?.Value.Supplier ?? string.Empty,
            ManufacturerMaterialNo = material?.Value.ManufacturerMaterialNumber ?? string.Empty,
            Classification = material?.Value.Classification ?? string.Empty,
            Attachment = material?.Value.Attachment ?? string.Empty
        };
    }

    private class PartListItem
    {
        #region Properties

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

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AE.PID.Client.Core;

namespace AE.PID.Client.Infrastructure;

internal class PartListItemConvertor(
    IFunctionLocationStore functionLocationStore,
    IMaterialResolver materialResolver)
{
    private readonly IFunctionLocationStore _functionLocationStore =
        functionLocationStore ?? throw new ArgumentNullException(nameof(functionLocationStore));

    private readonly IMaterialResolver _materialResolver =
        materialResolver ?? throw new ArgumentNullException(nameof(materialResolver));

    public async Task<PartListItem[]> ConvertAsync(IReadOnlyList<MaterialLocation> materialLocations)
    {
        var materialLocationsExt = materialLocations.Select(x =>
            {
                var function = _functionLocationStore.FunctionLocations.Lookup(x.Id);
                return new MaterialLocationExt(function.Value.Location!, x,
                    string.IsNullOrEmpty(x.Code) ? x.Category : x.Code, _materialResolver.ResolvedAsync(x.Code)
                );
            })
            .OrderBy(x => x.FunctionLocation)
            .ToList();

        var inGroupDict = CreateQuantityDictionary(materialLocationsExt, x => (
            Zone: x.FunctionLocation?.Zone ?? string.Empty,
            Group: x.FunctionLocation?.Group ?? string.Empty,
            x.GroupKey
        ));

        var inZoneDict = CreateQuantityDictionary(materialLocationsExt, x => (
            Zone: x.FunctionLocation?.Zone ?? string.Empty,
            x.GroupKey
        ));

        var tasks = materialLocationsExt
            .Select(async (x, i) =>
            {
                var material = (await x.MaterialTask).Value;
                return ToPartListItem(x.FunctionLocation, x.MaterialLocation, material, inGroupDict[
                        (x.FunctionLocation?.Zone ?? string.Empty, x.FunctionLocation?.Group ?? string.Empty,
                            x.GroupKey)],
                    inZoneDict[(x.FunctionLocation?.Zone ?? string.Empty, x.GroupKey)]);
            });

        return await Task.WhenAll(tasks);
    }

    private static Dictionary<TKey, double> CreateQuantityDictionary<TKey>(
        List<MaterialLocationExt> materialLocationsExt, Func<MaterialLocationExt, TKey> keySelector)
        where TKey : notnull
    {
        return materialLocationsExt
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.MaterialLocation.ComputedQuantity));
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
            OrderType = material?.Model ?? string.Empty,
            TechnicalDataChinese = material?.TechnicalData ?? materialLocation.KeyParameters,
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

    private record MaterialLocationExt(
        FunctionLocation FunctionLocation,
        MaterialLocation MaterialLocation,
        string GroupKey,
        Task<ResolveResult<Material?>> MaterialTask)
    {
        public Task<ResolveResult<Material?>> MaterialTask { get; } = MaterialTask;
        public string GroupKey { get; } = GroupKey;
        public MaterialLocation MaterialLocation { get; } = MaterialLocation;
        public FunctionLocation FunctionLocation { get; } = FunctionLocation;
    }
}
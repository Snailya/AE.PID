using System.Collections.Generic;
using System.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Models;
using Bogus;

namespace AE.PID.Visio.UI.Design.Design;

public class DesignData
{
    static DesignData()
    {
        FunctionLocations =
        [
            new FunctionLocation(new CompositeId(1, 1), FunctionType.ProcessZone)
            {
                ParentId = new CompositeId(1),
                Name = "工艺区域1",
                Description = "工艺区域1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = null,
                GroupName = null,
                Element = null,

                Remarks = "工艺区域1备注"
            },
            new FunctionLocation(new CompositeId(1, 2), FunctionType.FunctionGroup)
            {
                ParentId = new CompositeId(1,
                    1),
                Name = "功能组1",
                Description = "功能组1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = null,

                Remarks = "功能组1备注"
            },
            new FunctionLocation(new CompositeId(1, 3), FunctionType.Equipment)
            {
                ParentId = new CompositeId(1,
                    2),
                Name = "设备1",
                Description = "设备1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = "E19348",

                Remarks = "设备1备注"
            },
            new FunctionLocation(new CompositeId(1, 4), FunctionType.FunctionUnit)
            {
                ParentId = new CompositeId(1,
                    2),
                Name = "单元1",
                Description = "单元1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = "",

                Remarks = "单元1备注"
            },
            new FunctionLocation(new CompositeId(1, 5), FunctionType.Instrument)
            {
                ParentId = new CompositeId(1,
                    4),
                Name = "仪表1",
                Description = "仪表1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = "I19361",

                Remarks = "仪表1备注"
            },
            new FunctionLocation(new CompositeId(1, 6), FunctionType.Equipment)
            {
                ParentId = new CompositeId(1,
                    4),
                Name = "设备2",
                Description = "设备2描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = "E19362",

                Remarks = "设备2备注"
            },
            new FunctionLocation(new CompositeId(1, 7), FunctionType.FunctionElement)
            {
                ParentId = new CompositeId(1,
                    6),
                Name = "功能元件1",
                Description = "功能元件1描述",

                Zone = "P1V1",
                ZoneName = "工艺区域1",
                Group = "G101",
                GroupName = "功能组1",
                Element = "E19362",

                Remarks = "功能元件1备注"
            }
        ];

        var validFunctionsForMaterial = FunctionLocations.Where(x => (int)x.Type > 2).ToList();

        var materialLocationGenerator = new Faker<MaterialLocation>()
            .CustomInstantiator(f =>
                new MaterialLocation(validFunctionsForMaterial[f.Random.Int(0, validFunctionsForMaterial.Count - 1)]
                    .Id))
            .RuleFor(x => x.Type, f => f.PickRandom("离心泵","齿轮泵"))
            .RuleFor(x => x.KeyParameters, f => f.Lorem.Sentence())
            .RuleFor(x => x.Code, f => f.PickRandom("0001","0002",""))
            .RuleFor(x => x.Quantity, f => f.Random.Double(0, 5));

        MaterialLocations = materialLocationGenerator.Generate(15);
    }

    public static List<MaterialLocation> MaterialLocations { get; set; }

    public static List<FunctionLocation> FunctionLocations { get; set; }
}
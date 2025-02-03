using System.Collections.Generic;

namespace AE.PID.Visio.UI.Design.Design;

public class DesignData
{
    public static Dictionary<string, string> DocumentSheet = new();
    public static Dictionary<(int, string), string> PageSheet = new();

    static DesignData()
    {
        Shapes =
        [
            new ShapeProxy
            {
                Id = 1,
                ParentId = 0,
                PDMSFunctionId = 0,
                ShapeCategory = "Frame",
                Zone = "P1V1A1",
                ZoneName = "功能",
                ZoneNameEnglish = "null",
                Group = "null",
                GroupName = "null",
                GroupNameEnglish = "null",
                Element = "null",
                MaterialType = "null",
                MaterialCode = "null",
                Description = "",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 2,
                ParentId = 1,
                PDMSFunctionId = 0,
                ShapeCategory = "FunctionalGroup",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "WG501",
                GroupName = "废气焚烧炉燃气阀组",
                GroupNameEnglish = "",
                Element = "",
                MaterialType = "",
                MaterialCode = "",
                Description = "本地新增",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 3,
                ParentId = 2,
                PDMSFunctionId = 0,
                ShapeCategory = "Equipment",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "WG501",
                GroupName = "废气焚烧炉燃气阀组",
                GroupNameEnglish = "",
                Element = "QM181",
                MaterialType = "球阀",
                MaterialCode = "",
                Description = "",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 4,
                ParentId = 1,
                PDMSFunctionId = 26,
                ShapeCategory = "FunctionalGroup",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "TWV001",
                GroupName = "前处理主工艺设备RGV",
                GroupNameEnglish = "Pretreatment Main process system Rail Guided Vehcile",
                Element = "",
                MaterialType = "",
                MaterialCode = "",
                Description = "通过ID匹配",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 5,
                ParentId = 1,
                PDMSFunctionId = 0,
                ShapeCategory = "FunctionalGroup",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "TWT002",
                GroupName = "前处理主工艺设备母车（子母车）",
                GroupNameEnglish = "Pretreatment Main process system Transfer Cart",
                Element = "",
                MaterialType = "",
                MaterialCode = "",
                Description = "通过Group Code匹配",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 6,
                ParentId = 1,
                PDMSFunctionId = 28,
                ShapeCategory = "FunctionalGroup",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "TWS003",
                GroupName = "前处理主工艺设备巷道堆垛机",
                GroupNameEnglish = "Pretreatment Main process system Stacker\t\n",
                Element = "",
                MaterialType = "",
                MaterialCode = "",
                Description = "",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 7,
                ParentId = 2,
                PDMSFunctionId = 0,
                ShapeCategory = "Equipment",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "WG501",
                GroupName = "废气焚烧炉燃气阀组",
                GroupNameEnglish = "",
                Element = "QM182",
                MaterialType = "球阀",
                MaterialCode = "",
                Description = "",
                Remarks = ""
            },
            new ShapeProxy
            {
                Id = 8,
                ParentId = 2,
                PDMSFunctionId = 0,
                ShapeCategory = "Equipment",
                Zone = "",
                ZoneName = "",
                ZoneNameEnglish = "",
                Group = "WG501",
                GroupName = "废气焚烧炉燃气阀组",
                GroupNameEnglish = "",
                Element = "QN183",
                MaterialType = "蝶阀",
                MaterialCode = "",
                Description = "",
                Remarks = ""
            }
        ];
    }

    public static ShapeProxy[] Shapes { get; set; }
}
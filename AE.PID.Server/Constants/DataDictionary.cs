﻿namespace AE.PID.Server;

public abstract class DataDictionary
{
    public static readonly Dictionary<string, string[]> MaterialCategories = new()
    {
        { "齿轮泵", ["11010205"] },
        { "隔膜泵", ["11010201"] },
        { "离心泵", ["110101"] },
        { "双封离心泵", ["110101"] },
        { "软管泵", ["11010203"] },
        { "计量泵", ["11010202"] },
        { "通用泵", ["1101"] },
        { "柱塞泵", ["11010206"] },
        { "离心风机", ["11090105", "11090102"] },
        { "轴流风机", ["11090103", "11090104"] },
        { "气动搅拌器", ["11060102"] },
        { "电动搅拌器", ["11060101"] },
        { "板框压滤机", ["11070501"] },
        { "液体过滤器", ["11070901"] },
        { "气体过滤器", ["11080202", "11080203", "11080204", "11080205", "11080206", "11080207"] },
        { "活性炭过滤器", ["11080201"] },
        { "气体过滤器1", ["110301"] },
        { "气体过滤器2", ["110301"] },
        { "通用换热器3", ["110301"] },
        { "列管式换热器", ["110301"] },
        { "浮头式换热器", ["110301"] },
        { "电机", ["1114"] },
        { "UV杀菌", ["11050101"] },
        { "篮式过滤器", ["12011502"] },
        { "Y型过滤器", ["12011501"] },
        { "消音器", ["11090201"] },
        { "异径管", ["12020304", "12030304", "12030408"] },
        { "漏斗", ["12011602"] },
        { "柔性软连接", ["1206"] },
        { "快速接头", ["120402"] },
        { "软管", ["120401"] },
        { "阀", ["1201"] },
        { "电动阀", ["1201"] },
        { "电动隔膜阀", ["1201"] },
        { "电磁阀", ["130201"] },
        { "电磁隔膜阀", ["1201"] },
        { "电磁气动阀", ["1201"] },
        { "气动阀", ["1201"] },
        { "气动隔膜阀", ["1201"] },
        { "角式阀", ["1201"] },
        { "电动角式阀", ["1201"] },
        { "电磁气动角式阀", ["1201"] },
        { "三通阀", ["1201"] },
        { "电动三通阀", ["1201"] },
        { "电动隔膜三通阀", ["1201"] },
        { "电磁隔膜三通阀", ["1201"] },
        { "电磁气动三通阀", ["1201"] },
        { "四通阀", ["1201"] },
        { "电动四通阀", ["1201"] },
        { "截止阀", ["1201"] },
        { "电动截止阀", ["1201"] },
        { "球阀", ["1201"] },
        { "电动球阀", ["1201"] },
        { "电磁气动球阀", ["1201"] },
        { "角式球阀", ["1201"] },
        { "电动角式球阀", ["1201"] },
        { "电磁气动角式球阀", ["1201"] },
        { "三通球阀", ["1201"] },
        { "电动三通球阀", ["1201"] },
        { "电磁气动三通球阀", ["1201"] },
        { "四通球阀", ["1201"] },
        { "闸阀", ["1201"] },
        { "蝶阀", ["1201"] },
        { "电动蝶阀", ["1201"] },
        { "电磁气动蝶阀", ["1201"] },
        { "针阀", ["1201"] },
        { "止回阀", ["1201"] },
        { "升降式止回阀", ["1201"] },
        { "旋起式止回阀", ["1201"] },
        { "安全阀", ["1201"] },
        { "弹簧直列式安全阀", ["1201"] },
        { "弹簧角式安全阀", ["1201"] },
        { "呼吸阀", ["1201"] },
        { "三通分流调节阀", ["1201"] },
        { "三通合流调节阀", ["1201"] },
        { "背压调节阀", ["1201"] },
        { "减压调节阀", ["1201"] },
        { "平衡调节阀", ["1201"] },
        { "排气阀", ["12010701"] },
        { "疏水阀", ["12010501"] },
        { "调压阀", ["130401"] },
        { "过滤调压阀", ["130401"] },
        { "风阀", ["120901", "120902"] },
        { "百叶风阀", ["120901"] },
        { "散流器", ["120901"] },
        { "电动风阀", ["120901"] },
        { "电动百叶风阀", ["120901"] },
        { "气动风阀", ["120901"] },
        { "气动百叶风阀", ["120901"] },
        { "灯", ["160201", "160202"] },
        { "烤灯", ["16020304"] },
        { "液体喷嘴", ["120801"] },
        { "气体喷嘴", ["120802"] },
        { "汽液喷嘴", ["120803"] },
        { "燃烧机", ["11040101"] },
        { "烟囱", ["11040403"] },
        { "离心分离器", ["110701"] },
        { "磁性分离器", ["110702"] },
        { "管状隔膜阳极", ["11020101"] },
        { "弧形隔膜阳极", ["11020101"] },
        { "板式隔膜阳极", ["11020101"] },
        { "管式裸阳极", ["11020101"] },
        { "卷帘门", ["16030101"] },
        { "容器", ["13060302"] },
        { "洗眼器", ["16080201"] }
    };
}
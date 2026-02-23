namespace NoRuneDrops;

using SoulsFormats;
using SoulsFormats.Cryptography;

public static class Generator
{
    public static void Generate(string gameDirectory, bool noGetSoul, bool noDropGoldRune, bool noPickGoldRune, bool celebrantWeaponsOnStartup)
    {
        var regulationPath = Path.Combine(gameDirectory, "regulation.bin");
        if (!File.Exists(regulationPath))
        {
            throw new FileNotFoundException("regulation.bin not found in the game directory");
        }
        LoadDefs();
        var regulation = RegulationDecryptor.DecryptERRegulation(regulationPath);
        PARAM? param = null;
        foreach (var file in regulation.Files)
        {
            var filename = Path.GetFileName(file.Name);
            if ((noGetSoul || noPickGoldRune) && filename == EquipParamGoodsName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                var firstRow = param.Rows[0];
                var sellValueIndex = -1;
                for (var i = 0; i < firstRow.Cells.Count; i++)
                {
                    switch (firstRow.Cells[i].Def.InternalName)
                    {
                        case "sellValue": sellValueIndex = i; break;
                    }
                }
                foreach (var row in param.Rows)
                {
                    if (noGetSoul && RemembranceIds.Contains(row.ID))
                    {
                        row.Cells[sellValueIndex].Value = (int)0;
                    }
                    if (noPickGoldRune && GoldenRuneIds.Contains(row.ID))
                    {
                        row.Cells[sellValueIndex].Value = (int)0;
                    }
                }
                SetBndFile(regulation, EquipParamGoodsName, param.Write());
                continue;
            }
            if ((noGetSoul || noPickGoldRune) && filename == SpEffectParamName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                var firstRow = param.Rows[0];
                var soulIndex = -1;
                for (var i = 0; i < firstRow.Cells.Count; i++)
                {
                    switch (firstRow.Cells[i].Def.InternalName)
                    {
                        case "soul": soulIndex = i; break;
                    }
                }
                foreach (var row in param.Rows)
                {
                    if (noGetSoul && RemembranceSpEffectIds.Contains(row.ID))
                    {
                        row.Cells[soulIndex].Value = (int)0;
                    }
                    if (noPickGoldRune && GoldenRuneSpEffectIds.Contains(row.ID))
                    {
                        row.Cells[soulIndex].Value = (int)0;
                    }
                }
                SetBndFile(regulation, SpEffectParamName, param.Write());
                continue;
            }
            if (noGetSoul && filename == GameAreaParamName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                var firstRow = param.Rows[0];
                var bonusSoulSingleIndex = -1;
                var bonusSoulMultiIndex = -1;
                for (var i = 0; i < firstRow.Cells.Count; i++)
                {
                    switch (firstRow.Cells[i].Def.InternalName)
                    {
                        case "bonusSoul_single": bonusSoulSingleIndex = i; break;
                        case "bonusSoul_multi": bonusSoulMultiIndex = i; break;
                    }
                }
                foreach (var row in param.Rows)
                {
                    row.Cells[bonusSoulSingleIndex].Value = (uint)0;
                    row.Cells[bonusSoulMultiIndex].Value = (uint)0;
                }
                SetBndFile(regulation, GameAreaParamName, param.Write());
                continue;
            }
            if (noGetSoul && filename == NpcParamName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                var firstRow = param.Rows[0];
                var getSoulIndex = -1;
                for (var i = 0; i < firstRow.Cells.Count; i++)
                {
                    switch (firstRow.Cells[i].Def.InternalName)
                    {
                        case "getSoul": getSoulIndex = i; break;
                    }
                }
                foreach (var row in param.Rows)
                {
                    row.Cells[getSoulIndex].Value = (uint)0;
                }
                SetBndFile(regulation, NpcParamName, param.Write());
                continue;
            }
            if (noDropGoldRune && filename == itemLotParamEnemyName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                var firstRow = param.Rows[0];
                var itemIdIndex = -1;
                var enableLuckIndex = -1;
                var itemCatIndex = -1;
                var itemRateIndex = -1;
                var cumuRateIndex = -1;
                var cumuResetIndex = -1;
                var cumulateNumMaxIndex = -1;
                for (var i = 0; i < firstRow.Cells.Count; i++)
                {
                    switch (firstRow.Cells[i].Def.InternalName)
                    {
                        case "lotItemId01": itemIdIndex = i; break;
                        case "lotItemCategory01": itemCatIndex = i; break;
                        case "lotItemBasePoint01": itemRateIndex = i; break;
                        case "enableLuck01": enableLuckIndex = i; break;
                        case "cumulateLotPoint01": cumuRateIndex = i; break;
                        case "cumulateReset01": cumuResetIndex = i; break;
                        case "cumulateNumMax": cumulateNumMaxIndex = i; break;
                    }
                }
                foreach (var row in param.Rows)
                {
                    for (var i = 0; i < 8; i++)
                    {
                        if ((int)row.Cells[itemCatIndex + i].Value == 1 && GoldenRuneIds.Contains((int)row.Cells[itemIdIndex + i].Value))
                        {
                            row.Cells[itemRateIndex + i].Value = (ushort)0;
                            row.Cells[enableLuckIndex + i].Value = (ushort)0;
                            row.Cells[cumuRateIndex + i].Value = (ushort)0;
                            row.Cells[cumuResetIndex + i].Value = (ushort)0;
                            row.Cells[cumulateNumMaxIndex].Value = (byte)0;
                        }
                    }
                }

                SetBndFile(regulation, itemLotParamEnemyName, param.Write());
                continue;
            }

            if ((noPickGoldRune || celebrantWeaponsOnStartup) && filename == itemLotParamMapName)
            {
                param = PARAM.Read(file.Bytes);
                param.ApplyParamdefCarefully(_paramDefs);
                if (noPickGoldRune)
                {
                    var firstRow = param.Rows[0];
                    var itemIdIndex = -1;
                    var itemCatIndex = -1;
                    for (var i = 0; i < firstRow.Cells.Count; i++)
                    {
                        switch (firstRow.Cells[i].Def.InternalName)
                        {
                            case "lotItemId01": itemIdIndex = i; break;
                            case "lotItemCategory01": itemCatIndex = i; break;
                        }
                    }
                    foreach (var row in param.Rows)
                    {
                        for (var i = 0; i < 8; i++)
                        {
                            int itemId;
                            if ((int)row.Cells[itemCatIndex + i].Value == 1 && GoldenRuneIds.Contains(itemId = (int)row.Cells[itemIdIndex + i].Value))
                            {
                                if (itemId < 100000)
                                    row.Cells[itemIdIndex + i].Value = MaterialIds[Random.Shared.Next(MaterialIds.Count)];
                                else
                                    row.Cells[itemIdIndex + i].Value = DLCMaterialIds[Random.Shared.Next(DLCMaterialIds.Count)];
                            }
                        }
                    }
                }

                if (celebrantWeaponsOnStartup)
                {
                    AddItemWithLotId(param, 10010001, 1060000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010002, 1060000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010003, 12130000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010004, 12130000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010005, 14060000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010006, 14060000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010007, 16060000, 2, 1, 60210);
                    AddItemWithLotId(param, 10010008, 16060000, 2, 1, 60210);
                }

                param.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));

                SetBndFile(regulation, itemLotParamMapName, param.Write());
                continue;
            }

        }
        RegulationDecryptor.EncryptERRegulation($"./output/regulation.bin", regulation);
    }

    private static void LoadDefs()
    {
        var resources = Directory.GetFiles("Params/Defs");
        _paramDefs = [.. resources.Select(filename => PARAMDEF.XmlDeserialize(filename))];
    }

    private static void SetBndFile(IBinder binder, string fileName, byte[] bytes)
    {
        var file = binder.Files.First(file => Path.GetFileName(file.Name) == fileName) ?? throw new Exception($"{fileName} not found");
        file.Bytes = bytes;
    }

    private static void AddItemWithLotId(PARAM param, int id, int itemId, int itemCat, int itemCount, int flagId)
    {
        PARAM.Row row = new(id, "", param.AppliedParamdef);
        row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemId01")!.Value = itemId;
        row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemCategory01")!.Value = itemCat;
        row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemBasePoint01")!.Value = (ushort)1000;
        row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemNum01")!.Value = (byte)itemCount;
        row.Cells.FirstOrDefault(c => c.Def.InternalName == "getItemFlagId")!.Value = flagId;
        param.Rows.Add(row);
    }

    private static List<PARAMDEF>? _paramDefs;

    private const string EquipParamGoodsName = "EquipParamGoods.param";
    private const string SpEffectParamName = "SpEffectParam.param";
    private const string GameAreaParamName = "GameAreaParam.param";
    private const string NpcParamName = "NpcParam.param";
    private const string itemLotParamEnemyName = "ItemLotParam_enemy.param";
    private const string itemLotParamMapName = "ItemLotParam_map.param";

    private static readonly HashSet<int> GoldenRuneIds = [
        // Base game golden runes
        2900, 2901, 2902, 2903, 2904, 2905, 2906, 2907, 2908, 2909, 2910, 2911, 2912, 2913, 2914, 2915, 2916, 2917, 2918, 2919,
        // DLC golden runes
        2002950, 2002951, 2002952, 2002953, 2002954, 2002955, 2002956, 2002957, 2002958, 2002959, 2002960,
    ];
    private static readonly HashSet<int> RemembranceIds = [
        // Base game remembrances
        2950, 2951, 2952, 2953, 2954, 2955, 2956, 2957, 2958, 2959, 2960, 2961, 2962, 2963, 2964,
        // DLC remembrances
        2002900, 2002901, 2002902, 2002903, 2002904, 2002905, 2002906, 2002907, 2002908, 2002909, 2002910,
    ];
    private static readonly HashSet<int> GoldenRuneSpEffectIds = [
        // Base game runes sp effects
        3269, 3270, 3271, 3272, 3273, 3274, 3275, 3276, 3277, 3278, 3279, 3280, 3281, 3282, 3283, 3284, 3285, 3286, 3287, 3288,
        // DLC runes sp effects
        20502950, 20502951, 20502952, 20502953, 20502954, 20502955, 20502956, 20502957, 20502958, 20502959, 20502960,
    ];
    private static readonly HashSet<int> RemembranceSpEffectIds = [
        // Base game remembrance sp effects
        3720, 3721, 3722, 3723, 3724, 3725, 3726, 3727, 3728, 3729, 3730, 3731, 3732, 3733, 3734,
        // DLC remembrance sp effects
        20502900, 20502901, 20502902, 20502903, 20502904, 20502905, 20502906, 20502907, 20502908, 20502909, 20502910,
    ];

    private static readonly List<int> MaterialIds = [15000, 15010, 15020, 15030, 15040, 15050, 15060, 15070, 15080, 15090, 15100, 15110, 15120, 15130, 15140, 15150, 15160, 15310, 15340, 15341, 15390, 15400, 15410, 15420, 15430, 20650, 20651, 20652, 20653, 20654, 20660, 20680, 20681, 20682, 20683, 20685, 20690, 20691, 20710, 20720, 20721, 20722, 20723, 20740, 20750, 20751, 20753, 20760, 20761, 20770, 20775, 20780, 20795, 20800, 20801, 20802, 20810, 20811, 20812, 20820, 20825, 20830, 20831, 20840, 20841, 20842, 20845, 20850, 20852, 20855];
    private static readonly List<int> DLCMaterialIds = [2015000, 2015010, 2015020, 2015030, 2015040, 2020001, 2020002, 2020003, 2020004, 2020005, 2020006, 2020007, 2020008, 2020009, 2020010, 2020011, 2020012, 2020013, 2020014, 2020015, 2020016, 2020017, 2020018, 2020019, 2020020, 2020021, 2020022, 2020023, 2020024, 2020025, 2020026, 2020027, 2020028, 2020029, 2020030, 2020031, 2020032, 2020033, 2020034, 2020035];
}

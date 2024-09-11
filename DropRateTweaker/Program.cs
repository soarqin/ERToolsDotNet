using System.Reflection;
using SoulsFormats;

const string regulationName = "regulation.bin";
const string regulationPath = $@"D:\Steam\steamapps\common\ELDEN RING\Game\{regulationName}";
const string itemLotParamName = "ItemLotParam_enemy.param";

List<PARAMDEF> paramDefs;
PARAM? param = null;

LoadDefs();
var regulation = SFUtil.DecryptERRegulation(regulationPath);
foreach (var file in regulation.Files)
{
    var filename = Path.GetFileName(file.Name);
    if (filename == itemLotParamName)
    {
        param = PARAM.Read(file.Bytes);
        break;
    }
}

if (param == null)
{
    Console.WriteLine($"Failed to load {itemLotParamName}");
    return;
}

param.ApplyParamdefCarefully(paramDefs);
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
        if (row.Cells[itemRateIndex + i].Value is <= 0)
        {
            continue;
        }

        if ((int)row.Cells[itemCatIndex + i].Value == 2 && (int)row.Cells[itemIdIndex + i].Value is < 43100000 or >= 60000000)
        {
            if ((ushort)row.Cells[itemRateIndex + i].Value < 1000)
            {
                row.Cells[itemRateIndex + i].Value = (ushort)1000;
                row.Cells[enableLuckIndex + i].Value = (ushort)0;
                row.Cells[cumuRateIndex + i].Value = (ushort)0;
                row.Cells[cumuResetIndex + i].Value = (ushort)0;
                row.Cells[cumulateNumMaxIndex].Value = (byte)0;
            }
        }
        else
        {
            if ((ushort)row.Cells[itemRateIndex + i].Value < 1000)
            {
                row.Cells[itemRateIndex + i].Value = (ushort)0;
                row.Cells[enableLuckIndex + i].Value = (ushort)0;
                row.Cells[cumuRateIndex + i].Value = (ushort)0;
                row.Cells[cumuResetIndex + i].Value = (ushort)0;
                row.Cells[cumulateNumMaxIndex].Value = (byte)0;
            }
        }
    }
}
SetBndFile(regulation, itemLotParamName, param.Write());
SFUtil.EncryptERRegulation($"./output/{regulationName}", regulation);

return;

void LoadDefs() {
    var resources = Directory.GetFiles("Params/Defs");
    paramDefs = resources.Select(filename => PARAMDEF.XmlDeserialize(filename)).ToList();
}

void SetBndFile(IBinder binder, string fileName, byte[] bytes) {
    var file = binder.Files.First(file => Path.GetFileName(file.Name) == fileName) ?? throw new Exception($"{fileName} not found");
    file.Bytes = bytes;
}

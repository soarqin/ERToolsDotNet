using System.Diagnostics;
using SoulsFormats;
using SoulsFormats.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;

EMEVD commonEmevd;
var cancellationToken = CancellationToken.None;
const string gamePath = "D:/Steam/steamapps/common/ELDEN RING/Game";
const string regulationName = "regulation.bin";
const string regulationPath = $@"{gamePath}/{regulationName}";
const string itemLotParamName = "ItemLotParam_map.param";
List<PARAMDEF> paramDefs;

(int, int, int)[] startupItems =
[
    (2002960, 1, 99), // 玛莉卡的卢恩x99
    (2002960, 1, 99), // 玛莉卡的卢恩x99
    (2140000, 2, 1), // 夜与火之剑
    (3170000, 2, 1), // 黄金律法大剑
    (4080000, 2, 1), // 遗迹大剑
    (7100000, 2, 1), // 日蚀钩剑
    (7510000, 2, 1), // 镰型刀
    (10010000, 2, 1), // 神皮剥制剑
    (12200000, 2, 1), // 吞世权杖
    (16090000, 2, 1), // 古兰桑克斯的雷电
    (5230100, 3, 1), // 守墓鸟黑羽铠甲
    (1042, 4, 1), // 黄金树的恩惠＋２
    (1051, 4, 1), // 拉达冈的糜烂烙印
    (1140, 4, 1), // 诺克史黛拉之月
    (1221, 4, 1), // 玛莉卡的糜烂烙印
    (1231, 4, 1), // 亚历山大的碎片
    (1250, 4, 1), // 米莉森的义手
    (2081, 4, 1), // 腐败翼剑徽章
    (2160, 4, 1), // 鲜血君王的欢愉
    (2180, 4, 1), // 钩爪护符
    (3060, 4, 1), // 古王护符
    (3070, 4, 1), // 拉达冈的肖像
    (3090, 4, 1), // 葛孚雷的肖像
    (4003, 4, 1), // 龙徽大盾护符
    (60300, 5, 1), // 战灰：黄金树立誓
];

LoadDefs();
var regulation = RegulationDecryptor.DecryptERRegulation(regulationPath);
var itemLotParam = (from file in regulation.Files let filename = Path.GetFileName(file.Name) where filename == itemLotParamName select PARAM.Read(file.Bytes)).FirstOrDefault();
itemLotParam!.ApplyParamdefCarefully(paramDefs);

var bhd5Reader = new BHD5Reader(gamePath, Config.CacheBhDs, cancellationToken);
GetEmevds();
var common = commonEmevd;
if (common == null)
{
    throw new InvalidOperationException($"Missing emevd {Const.CommonEventPath}");
}

const int newEventId = 279551112; // Arbitrary number
List<EMEVD.Instruction> newInstrs =
[
    new EMEVD.Instruction(1003, 2, new List<object> { (byte)0, (byte)1, (byte)0, (uint)76100 }),
    // IfEventFlag(MAIN, ON, TargetEventFlagType.EventFlag, 76100)
    new EMEVD.Instruction(3, 0, new List<object> { (sbyte)0, (byte)1, (byte)0, (uint)76100 })
];
for (var i = 0; i < startupItems.Length; i++)
{
    var (id, cat, count) = startupItems[i];
    AddItemWithLotId(70010 + i, id, cat, count);
    if (i % 10 == 0)
    {
        // AwardItemLot(70010 + i)
        newInstrs.Add(new EMEVD.Instruction(2003, 4, new List<object> { 70010 + i }));
    }
}

// EndEvent()
newInstrs.Add(new EMEVD.Instruction(1000, 4, new List<object> { (byte)0 }));
var newEvent = new EMEVD.Event(newEventId, EMEVD.Event.RestBehaviorType.Default)
{
    Instructions = newInstrs
};
common.Events.Add(newEvent);
var constrEvent = common.Events.Find(e => e.ID == 0);
if (constrEvent == null)
{
    throw new InvalidOperationException($"{Const.CommonEventPath} missing of required event: 0");
}

// Initialize new event
constrEvent.Instructions.Add(new EMEVD.Instruction(2000, 0, new List<object> { 0, newEventId, 0 }));

Directory.CreateDirectory(Path.GetDirectoryName($"./output/{Const.CommonEventPath}") ?? throw new InvalidOperationException());

SetBndFile(regulation, itemLotParamName, itemLotParam.Write());
RegulationDecryptor.EncryptERRegulation($"./output/{regulationName}", regulation);
File.WriteAllBytes($"./output/{Const.CommonEventPath}", commonEmevd.Write());

return;

void LoadDefs()
{
    var resources = Directory.GetFiles("Params/Defs");
    paramDefs = resources.Select(filename => PARAMDEF.XmlDeserialize(filename)).ToList();
}

void SetBndFile(IBinder binder, string fileName, byte[] bytes)
{
    var file = binder.Files.First(file => Path.GetFileName(file.Name) == fileName) ?? throw new Exception($"{fileName} not found");
    file.Bytes = bytes;
}

void AddItemWithLotId(int id, int itemId, int itemCat, int itemCount)
{
    var row = new PARAM.Row(id, "", itemLotParam.AppliedParamdef);
    row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemId01")!.Value = itemId;
    row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemCategory01")!.Value = itemCat;
    row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemBasePoint01")!.Value = (ushort)1000;
    row.Cells.FirstOrDefault(c => c.Def.InternalName == "lotItemNum01")!.Value = (byte)itemCount;
    itemLotParam.Rows.Add(row);
}

void GetEmevds()
{
    // This is only used in S3, but always fetch and write them for now.
    var emevdBytes = GetOrOpenFile(Const.CommonEventPath);
    commonEmevd = EMEVD.Read(emevdBytes);
}

byte[] GetOrOpenFile(string path)
{
    if (File.Exists($"{Config.CachePath}/{path}"))
    {
        return File.ReadAllBytes($"{Config.CachePath}/{path}");
    }

    var file = bhd5Reader.GetFile(path) ?? throw new InvalidOperationException($"Could not find file {Config.CachePath}/{path}");
    Directory.CreateDirectory(System.IO.Path.GetDirectoryName($"{Config.CachePath}/{path}") ?? throw new InvalidOperationException($"Could not get directory name for file {Config.CachePath}/{path}"));
    File.WriteAllBytes($"{Config.CachePath}/{path}", file);
    return file;
}

public static class Config
{
    public static readonly string CachePath = $"{Const.ExeDir}\\Cache";
    public const bool CacheBhDs = false;
}

public static class Const
{
    public static readonly string ExeDir = Environment.CurrentDirectory;
    public const string CommonEventPath = "/event/common.emevd.dcx";

    public static class ArchiveKeys
    {
        public const string Data0 = """
                                    -----BEGIN RSA PUBLIC KEY-----
                                    MIIBCwKCAQEA9Rju2whruXDVQZpfylVEPeNxm7XgMHcDyaaRUIpXQE0qEo+6Y36L
                                    P0xpFvL0H0kKxHwpuISsdgrnMHJ/yj4S61MWzhO8y4BQbw/zJehhDSRCecFJmFBz
                                    3I2JC5FCjoK+82xd9xM5XXdfsdBzRiSghuIHL4qk2WZ/0f/nK5VygeWXn/oLeYBL
                                    jX1S8wSSASza64JXjt0bP/i6mpV2SLZqKRxo7x2bIQrR1yHNekSF2jBhZIgcbtMB
                                    xjCywn+7p954wjcfjxB5VWaZ4hGbKhi1bhYPccht4XnGhcUTWO3NmJWslwccjQ4k
                                    sutLq3uRjLMM0IeTkQO6Pv8/R7UNFtdCWwIERzH8IQ==
                                    -----END RSA PUBLIC KEY-----
                                    """;

        public const string Data1 = """
                                    -----BEGIN RSA PUBLIC KEY-----
                                    MIIBCwKCAQEAxaBCHQJrtLJiJNdG9nq3deA9sY4YCZ4dbTOHO+v+YgWRMcE6iK6o
                                    ZIJq+nBMUNBbGPmbRrEjkkH9M7LAypAFOPKC6wMHzqIMBsUMuYffulBuOqtEBD11
                                    CAwfx37rjwJ+/1tnEqtJjYkrK9yyrIN6Y+jy4ftymQtjk83+L89pvMMmkNeZaPON
                                    4O9q5M9PnFoKvK8eY45ZV/Jyk+Pe+xc6+e4h4cx8ML5U2kMM3VDAJush4z/05hS3
                                    /bC4B6K9+7dPwgqZgKx1J7DBtLdHSAgwRPpijPeOjKcAa2BDaNp9Cfon70oC+ZCB
                                    +HkQ7FjJcF7KaHsH5oHvuI7EZAl2XTsLEQIENa/2JQ==
                                    -----END RSA PUBLIC KEY-----
                                    """;

        public const string Data2 = """
                                    -----BEGIN RSA PUBLIC KEY-----
                                    MIIBDAKCAQEA0iDVVQ230RgrkIHJNDgxE7I/2AaH6Li1Eu9mtpfrrfhfoK2e7y4O
                                    WU+lj7AGI4GIgkWpPw8JHaV970Cr6+sTG4Tr5eMQPxrCIH7BJAPCloypxcs2BNfT
                                    GXzm6veUfrGzLIDp7wy24lIA8r9ZwUvpKlN28kxBDGeCbGCkYeSVNuF+R9rN4OAM
                                    RYh0r1Q950xc2qSNloNsjpDoSKoYN0T7u5rnMn/4mtclnWPVRWU940zr1rymv4Jc
                                    3umNf6cT1XqrS1gSaK1JWZfsSeD6Dwk3uvquvfY6YlGRygIlVEMAvKrDRMHylsLt
                                    qqhYkZNXMdy0NXopf1rEHKy9poaHEmJldwIFAP////8=
                                    -----END RSA PUBLIC KEY-----
                                    """;

        public const string Data3 = """
                                    -----BEGIN RSA PUBLIC KEY-----
                                    MIIBCwKCAQEAvRRNBnVq3WknCNHrJRelcEA2v/OzKlQkxZw1yKll0Y2Kn6G9ts94
                                    SfgZYbdFCnIXy5NEuyHRKrxXz5vurjhrcuoYAI2ZUhXPXZJdgHywac/i3S/IY0V/
                                    eDbqepyJWHpP6I565ySqlol1p/BScVjbEsVyvZGtWIXLPDbx4EYFKA5B52uK6Gdz
                                    4qcyVFtVEhNoMvg+EoWnyLD7EUzuB2Khl46CuNictyWrLlIHgpKJr1QD8a0ld0PD
                                    PHDZn03q6QDvZd23UW2d9J+/HeBt52j08+qoBXPwhndZsmPMWngQDaik6FM7EVRQ
                                    etKPi6h5uprVmMAS5wR/jQIVTMpTj/zJdwIEXszeQw==
                                    -----END RSA PUBLIC KEY-----
                                    """;

        public const string Sd = """
                                 -----BEGIN RSA PUBLIC KEY-----
                                 MIIBCwKCAQEAmYJ/5GJU4boJSvZ81BFOHYTGdBWPHnWYly3yWo01BYjGRnz8NTkz
                                 DHUxsbjIgtG5XqsQfZstZILQ97hgSI5AaAoCGrT8sn0PeXg2i0mKwL21gRjRUdvP
                                 Dp1Y+7hgrGwuTkjycqqsQ/qILm4NvJHvGRd7xLOJ9rs2zwYhceRVrq9XU2AXbdY4
                                 pdCQ3+HuoaFiJ0dW0ly5qdEXjbSv2QEYe36nWCtsd6hEY9LjbBX8D1fK3D2c6C0g
                                 NdHJGH2iEONUN6DMK9t0v2JBnwCOZQ7W+Gt7SpNNrkx8xKEM8gH9na10g9ne11Mi
                                 O1FnLm8i4zOxVdPHQBKICkKcGS1o3C2dfwIEXw/f3w==
                                 -----END RSA PUBLIC KEY-----
                                 """;
    }
}

internal class BHDInfo(BHD5 bhd, string bdt)
{
    private readonly string _bdtPath = $"{bdt}.bdt";
    private string _bhdPath = $"{bdt}.bhd";

    public byte[]? GetFile(ulong hash)
    {
        var index = hash % (ulong)bhd.Buckets.Count;
        var bucket = bhd.Buckets[(int)index];

        foreach (var header in bucket)
        {
            if (header.FileNameHash != hash)
            {
                continue;
            }

            using FileStream fs = new(_bdtPath, FileMode.Open);
            return header.ReadFile(fs);
        }

        return null;
    }

    public string GetSalt()
    {
        return bhd.Salt;
    }
}

internal class BHD5Reader
{
    private const string Data0 = "Data0";
    private const string Data1 = "Data1";
    private const string Data2 = "Data2";
    private const string Data3 = "Data3";
    private static readonly string Data0CachePath = $"{Config.CachePath}/{Data0}";
    private static readonly string Data1CachePath = $"{Config.CachePath}/{Data1}";
    private static readonly string Data2CachePath = $"{Config.CachePath}/{Data2}";
    private static readonly string Data3CachePath = $"{Config.CachePath}/{Data3}";

    private readonly BHDInfo _data0;

    public BHD5Reader(string path, bool cache, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(Config.CachePath))
        {
            Directory.CreateDirectory(Config.CachePath);
        }

        var cacheExists = File.Exists(Data0CachePath);
        var bhdBytes = new byte[4][];
        List<Task> tasks = new();
        switch (cacheExists)
        {
            case false:
                tasks.Add(Task.Run(() => { bhdBytes[0] = CryptoUtil.DecryptRsa($"{path}/{Data0}.bhd", Const.ArchiveKeys.Data0, cancellationToken).ToArray(); }));
                break;
            default:
                bhdBytes[0] = File.ReadAllBytes(Data0CachePath);
                break;
        }


        try
        {
            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }
        catch (AggregateException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        var data0 = ReadBhd5(bhdBytes[0]);
        _data0 = new BHDInfo(data0, $"{path}/{Data0}");
        cancellationToken.ThrowIfCancellationRequested();

        if (cache && !cacheExists)
        {
            File.WriteAllBytes($"{Data0CachePath}.bhd", bhdBytes[0]);
        }
    }

    private static BHD5 ReadBhd5(byte[] bytes)
    {
        return BHD5.Read(bytes, BHD5.Game.EldenRing);
    }

    // Right now just works for data0, as that is where all of the files we need, are, and none of the other header files are being loaded, as it takes a while to decrypt them.    
    public byte[]? GetFile(string filePath)
    {
        var hash = Util.ComputeHash(filePath, BHD5.Game.EldenRing);
        var file = _data0.GetFile(hash);
        if (file != null)
        {
            Debug.WriteLine($"{filePath} Data0: {_data0.GetSalt()}");
            return file;
        }

        return file;
    }
}

internal static class Util
{
    private const uint Prime = 37;
    private const ulong Prime64 = 0x85ul;

    public static ulong ComputeHash(string path, BHD5.Game game)
    {
        var hashable = path.Trim().Replace('\\', '/').ToLowerInvariant();
        if (!hashable.StartsWith("/"))
        {
            hashable = '/' + hashable;
        }

        return game >= BHD5.Game.EldenRing ? hashable.Aggregate(0ul, (i, c) => i * Prime64 + c) : hashable.Aggregate(0u, (i, c) => i * Prime + c);
    }
}

internal static class CryptoUtil
{
    /// <summary>
    ///     Decrypts a file with a provided decryption key.
    /// </summary>
    /// <param name="filePath">An encrypted file</param>
    /// <param name="key">The RSA key in PEM format</param>
    /// <exception cref="ArgumentNullException">When the argument filePath is null</exception>
    /// <exception cref="ArgumentNullException">When the argument keyPath is null</exception>
    /// <returns>A memory stream with the decrypted file</returns>
    public static MemoryStream DecryptRsa(string filePath, string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        ArgumentNullException.ThrowIfNull(key);

        var keyParameter = GetKeyOrDefault(key) ?? throw new InvalidOperationException();
        RsaEngine engine = new();
        engine.Init(false, keyParameter);

        MemoryStream outputStream = new();
        using (var inputStream = File.OpenRead(filePath))
        {
            var inputBlockSize = engine.GetInputBlockSize();
            var outputBlockSize = engine.GetOutputBlockSize();
            var inputBlock = new byte[inputBlockSize];
            while (inputStream.Read(inputBlock, 0, inputBlock.Length) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ;
                var outputBlock = engine.ProcessBlock(inputBlock, 0, inputBlockSize);

                var requiredPadding = outputBlockSize - outputBlock.Length;
                if (requiredPadding > 0)
                {
                    var paddedOutputBlock = new byte[outputBlockSize];
                    outputBlock.CopyTo(paddedOutputBlock, requiredPadding);
                    outputBlock = paddedOutputBlock;
                }

                outputStream.Write(outputBlock, 0, outputBlock.Length);
            }
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    private static AsymmetricKeyParameter? GetKeyOrDefault(string key)
    {
        try
        {
            PemReader pemReader = new(new StringReader(key));
            return (AsymmetricKeyParameter)pemReader.ReadObject();
        }
        catch
        {
            return null;
        }
    }
}
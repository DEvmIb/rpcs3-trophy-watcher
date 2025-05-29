// https://github.com/Lacro59/playnite-plugincommon/blob/6587fd0720762e9aa93543d89da76eeb2fe35b8a/CommonPluginsShared/Tools.cs#L17
// https://github.com/Lacro59/playnite-successstory-plugin/blob/master/source/Clients/Rpcs3Achievements.cs
// https://learn.microsoft.com/de-de/dotnet/api/system.io.filesystemwatcher?view=net-9.0&redirectedfrom=MSDN
// todo: there wrote some xml's ha groups. game: ?skate?
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

Console.OutputEncoding = Encoding.UTF8;


if (args.Length == 0)
{
    string json = JsonSerializer.Serialize(new
    {
        a=1,
        e="missing path argument"
    }, new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    Console.WriteLine(json);
    System.Environment.Exit(1);
}

if (!Directory.Exists(args[0]))
{
    string json = JsonSerializer.Serialize(new
    {
        a = 1,
        e = "path does not exists"
    }, new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    Console.WriteLine(json);
    System.Environment.Exit(1);
}
else
{
    string json = JsonSerializer.Serialize(new
    {
        a = 2,
        i = $"watching {args[0]}"
    }, new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    Console.WriteLine(json);
}

    static string ToHex(byte[] bytes)
    {
        char[] c = new char[bytes.Length * 2];

        byte b;

        for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
        {
            b = (byte)(bytes[bx] >> 4);
            c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

            b = (byte)(bytes[bx] & 0x0F);
            c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
        }

        return new string(c);
    }

FileSystemWatcher watcher = new FileSystemWatcher();
watcher.Path = args[0];
//watcher.Path = @"c:\tmp";

watcher.NotifyFilter = NotifyFilters.Attributes
                        | NotifyFilters.CreationTime
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.FileName
                        | NotifyFilters.LastAccess
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Security
                        | NotifyFilters.Size;

watcher.Filter = "TROPUSR.DAT";

watcher.IncludeSubdirectories = true;
watcher.EnableRaisingEvents = true;

watcher.Changed += OnChanged;
watcher.Created += OnCreated;
//watcher.Deleted += OnDeleted;
watcher.Renamed += OnRenamed;
//watcher.Error += OnError;

Console.ReadLine();

static void OnChanged(object sender, FileSystemEventArgs e)
{
    if (e.ChangeType != WatcherChangeTypes.Changed)
    {
        return;
    }
    Action(new FileInfo(e.FullPath).Directory.FullName);
}

static void OnCreated(object sender, FileSystemEventArgs e)
{
    Action(new FileInfo(e.FullPath).Directory.FullName);
}

static void OnDeleted(object sender, FileSystemEventArgs e) =>
    Action(new FileInfo(e.FullPath).Directory.FullName);

static void OnRenamed(object sender, RenamedEventArgs e)
{
    Action(new FileInfo(e.FullPath).Directory.FullName);
}

static void OnError(object sender, ErrorEventArgs e) =>
    PrintException(e.GetException());

static void PrintException(Exception? ex)
{
    if (ex != null)
    {
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine("Stacktrace:");
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine();
        PrintException(ex.InnerException);
    }
}

static void Action(string Path)
{
    try
    {
        List<string> Known = new List<string>();
        Thread.Sleep(1000);

        string trophyFilePath = @$"{Path}\TROPUSR.DAT";
        string trophyFileDetailsPath = @$"{Path}\TROPCONF.SFM";

        if (!File.Exists(trophyFilePath))
        {
            return;
        }
        if (!File.Exists(trophyFileDetailsPath))
        {
            return;
        }

        string Name;

        int TrophyCount = 0;

        List<string> TrophyHexData = new List<string>();
        List<string> TrophysName = new List<string>();
        List<string> TrophysDesc = new List<string>();
        List<string> TrophysType = new List<string>();

        XDocument TrophyDetailsXml = XDocument.Load(trophyFileDetailsPath);
        string GameName = TrophyDetailsXml.Descendants("title-name").FirstOrDefault().Value.Trim();
        string Np = TrophyDetailsXml.Descendants("npcommid").FirstOrDefault().Value.Trim();

        foreach (XElement TrophyXml in TrophyDetailsXml.Descendants("trophy"))
        {
            _ = int.TryParse(TrophyXml.Attribute("id").Value, out int TrophyDetailsId);
            string TrophyType = TrophyXml.Attribute("ttype").Value;
            Name = TrophyXml.Element("name").Value;
            string Description = TrophyXml.Element("detail").Value;

            TrophyCount += 1;
            TrophysName.Add(Name);
            TrophysDesc.Add(Description);
            TrophysType.Add(TrophyType);

        }

        byte[] TrophyByte = File.ReadAllBytes(trophyFilePath);
        string hex = ToHex(TrophyByte);
        List<string> splitHex = hex.Split(new[] { "0000000400000050000000", "0000000600000060000000" }, StringSplitOptions.None).ToList();

        TrophyHexData = splitHex.Count >= TrophyCount
            ? splitHex.GetRange(splitHex.Count - TrophyCount, TrophyCount)
            : new List<string>();


        foreach (string HexData in TrophyHexData)
        {
            if (HexData.Length < 58) continue;
            string stringHexId = HexData.Substring(0, 2);
            int Id = (int)long.Parse(stringHexId, NumberStyles.HexNumber);

            if (Id >= TrophyCount) continue;

            string Unlocked = HexData.Substring(18, 8);
            bool IsUnlocked = Unlocked == "00000001";


            if (IsUnlocked)
            {
                try
                {
                    string dtHex = HexData.Substring(44, 14);
                    DateTime dt = new DateTime(long.Parse(dtHex, NumberStyles.AllowHexSpecifier) * 10L);
                    string json = JsonSerializer.Serialize(new
                    {
                        a = "0",
                        id = Id,
                        np = Np,
                        game = GameName,
                        name = TrophysName[Id],
                        desc = TrophysDesc[Id],
                        type = TrophysType[Id],
                        img = @$"{Path}\TROP{Id.ToString("000")}.PNG",
                        time = dt
                    }, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    Console.WriteLine(json);
                }
                catch (Exception ex)
                { }
            }
        }
    }
    catch (Exception ex)
    { }
}

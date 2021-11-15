using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.XPath;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("    >EpubRenamer TargetDirName");
    return 1;
}

var targetDir = args[0];
var filepaths = Directory.EnumerateFiles(targetDir, "*.epub", SearchOption.AllDirectories);

foreach (var filepath in filepaths)
    Rename(filepath);

return 0;

static bool Rename(string filepath)
{
    filepath = Path.GetFullPath(filepath);

    var removeNamespace = static (XDocument doc) =>
    {
        foreach (var e in doc.Descendants())
            e.Name = e.Name.LocalName;
    };

    var title = "";
    {
        using var zip = new FileStream(filepath, FileMode.Open);
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read);

        var contentOpfPath = "";

        var containerXmlEntry = archive.GetEntry("META-INF/container.xml");
        if (containerXmlEntry == null)
            return false;

        using (var reader = new StreamReader(containerXmlEntry.Open()))
        {
            var doc = XDocument.Load(reader);
            removeNamespace(doc);

            var rootfile = doc.XPathSelectElement("container/rootfiles/rootfile");
            contentOpfPath = rootfile?.Attribute("full-path")?.Value;
        }

        if (contentOpfPath == null)
            return false;

        var contentOpfXmlEntry = archive.GetEntry(contentOpfPath);
        if (contentOpfXmlEntry == null)
            return false;

        using (var reader = new StreamReader(contentOpfXmlEntry.Open()))
        {
            var doc = XDocument.Load(reader);
            removeNamespace(doc);

            title = doc.XPathSelectElement("package/metadata/title")?.Value;
        }
    }

    if (string.IsNullOrEmpty(title))
        return false;

    title = string.Concat(title.Select(c => Local.InvalidChars.Contains(c) ? '_' : c));

    var dir = Path.GetDirectoryName(filepath);
    if (string.IsNullOrEmpty(dir))
        return false;

    var newFilepath = Path.Combine(dir, title + ".epub");

    if (filepath != newFilepath)
        File.Move(filepath, newFilepath);

    return true;
}

internal static class Local
{
    static public readonly HashSet<char> InvalidChars = new(Path.GetInvalidFileNameChars());
}

namespace FolderVerse;
using System;
using System.IO;

// Read once at startup so textures and character metadata always use the same edition.
public static class PortraitAssets
{
    public static readonly string Version = ReadVersion();
    public static string ContentPath => "Images/Portraits/" + Version;
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory,"Content","Images","Portraits",Version);

    private static string ReadVersion()
    {
        string path=Path.Combine(AppContext.BaseDirectory,"Content","Images","Portraits","active-version.txt");
        string version=File.ReadAllText(path).Trim();
        if(version is not ("v1-original" or "v2-toy-style"))
            throw new InvalidDataException("Portrait active-version.txt must contain v1-original or v2-toy-style.");
        return version;
    }
}

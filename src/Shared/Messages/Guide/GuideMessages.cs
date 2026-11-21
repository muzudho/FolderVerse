namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

public static class GuideMessages
{
    private static readonly Dictionary<string,string[]> Messages=Load();
    private static Dictionary<string,string[]> Load()
    {
        using var stream=typeof(GuideMessages).Assembly.GetManifestResourceStream("FolderVerse.GuideMessages.json")
            ??throw new InvalidDataException("Guide message resource is missing.");
        return JsonSerializer.Deserialize<Dictionary<string,string[]>>(stream)
            ??throw new InvalidDataException("Guide messages are empty.");
    }
    public static string[] Page(string key,IReadOnlyDictionary<string,string> values=null)=>Messages[key].Select(line=>
        Regex.Replace(line,@"\{([A-Za-z]+)\}",match=>values!=null && values.TryGetValue(match.Groups[1].Value,out string value)
            ?value:throw new InvalidDataException("Missing guide message value: "+match.Value))).ToArray();
    public static string Text(string key)=>Page(key)[0];
}

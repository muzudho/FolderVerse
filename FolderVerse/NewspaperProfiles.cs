namespace FolderVerse;

// A publisher's identity is fixed across world/cast seeds. Allegiances are assigned by the scene.
public sealed record NewspaperProfile(string Id,string Title,string Label,string PhotoKind,bool FavorsArmy=false,bool AllowFallback=false);
public static class NewspaperProfiles
{
    public static readonly NewspaperProfile[] All={
        new("campaign","THE CAMPAIGN HERALD","従軍紙","formal",true,true),
        new("local","THE LOCAL GAZETTE","地元紙","paparazzi"),
        new("faro","El Faro de Papel","海外紙 / うわさ重視","paparazzi"),
        new("jagat","खिलौना जगत","海外紙 / 記録重視","paparazzi"),
        new("gwiazd","Głos Gwiazd","海外紙 / 批評","paparazzi"),
        new("school","ほうそう部だより","学級新聞 / 放送部","friends",false,true)
    };
}

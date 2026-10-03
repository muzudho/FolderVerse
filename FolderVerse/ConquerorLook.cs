namespace FolderVerse;

public enum EyeStyle { Normal, Upturned, Downturned, HalfLidded, Narrow, Closed }
public enum EarStyle { Normal, Large, Demon, Elf }
public enum EyewearStyle { None, Normal, Round, Red, Bottle, Monocle, Patch, Bandage }

public readonly record struct ConquerorLook(int BaseId, int VariantId)
{
    public PortraitKeywords Keywords => ConquerorCatalog.Keywords[BaseId*6+VariantId];
    public EyeStyle Eyes => (EyeStyle)Keywords.Eye;
    public EarStyle Ears => (EarStyle)Keywords.Ear;
    public EyewearStyle Eyewear => (EyewearStyle)Keywords.Eyewear;
    public string PersonalityName => ConquerorCatalog.Personalities[Keywords.Personality];
    public string SituationName => ConquerorCatalog.Situations[Keywords.Situation];
    public string EyeName => Eyes switch { EyeStyle.Upturned=>"吊り上がり目",EyeStyle.Downturned=>"垂れ目",EyeStyle.HalfLidded=>"ジト目",EyeStyle.Narrow=>"細目",EyeStyle.Closed=>"線目",_=>"ふつう" };
    public string EarName => Ears switch { EarStyle.Large=>"大きい耳",EarStyle.Demon=>"悪魔耳",EarStyle.Elf=>"エルフ耳",_=>"ふつう" };
    public string EyewearName => Eyewear switch { EyewearStyle.Normal=>"眼鏡",EyewearStyle.Round=>"丸眼鏡",EyewearStyle.Red=>"赤眼鏡",EyewearStyle.Bottle=>"瓶底眼鏡",EyewearStyle.Monocle=>"片眼鏡",EyewearStyle.Patch=>"眼帯",EyewearStyle.Bandage=>"片目包帯",_=>"眼鏡無し" };
}

public static class ConquerorCatalog
{
    public const int BaseCount=60, VariantsPerBase=6;
    public static readonly PortraitKeywords[] Keywords = System.Text.Json.JsonSerializer.Deserialize<PortraitKeywords[]>(System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory,"Content","Images","Portraits","manifest.json")));
    public static readonly string[] Personalities = { "勝ち気","インドアが好き","シャイ","高飛車","悪だくみ","おしとやか","大慌て","落ち着きがある","おっちょこちょい","人の話を聞かない","ネガティブ","怒っている","大笑い","ふつう" };
    public static readonly string[] Situations = { "ふつう","スパイ","交渉","呪文詠唱","戦闘","お茶","眠気","絶好調","風邪気味","風呂上り","宿題","徒競走中","食事中","落下中" };
    public static readonly string[] Themes={
        "玩具の女王","アヒル将軍","鉄道提督","星の女王","衛星将軍","通信隊長","ウサギ探検家","恐竜将軍","飛行船隊長","車の女王",
        "フクロウ女王","潜水艦提督","探検家","音楽箱の女王","宇宙隊長","熊の女王","光の女王","撮影隊長","蝶の女王","鼓笛隊長",
        "雪国女王","ジャングル探検家","レーサー","西洋甲冑","海賊船長","中華将軍","料理長","エジプト女王","熊着ぐるみ","宇宙提督",
        "砂漠隊長","北極技師","花の女王","蒸気飛行士","時計魔術師","消防隊長","海中隊長","劇場女王","バイキング","お菓子将軍",
        "雪国の姉妹","ジャングル女王","レーサーの姉妹","銀の甲冑","海賊の姉妹","中華女王","菓子料理長","エジプト天文学者","パンダ着ぐるみ","金の甲冑",
        "砂漠の女王","極地船長","竹の侍","気球飛行士","玩具の指揮者","登山家","図書魔術師","祭りの隊長","玩具宇宙飛行士","玩具発明家"
    };
}
public sealed record PortraitKeywords(int BaseId,int VariantId,int Eye,int Ear,int Eyewear,int Personality,int Situation);

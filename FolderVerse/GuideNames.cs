namespace FolderVerse;
using System;

// Fictional name-like adaptations of everyday tools and handcrafts, not translations.
// Keep this pool separate from conqueror virtues/nature and city geography.
public static class GuideNames
{
    private static readonly string[] Given={
        "リボネ","ボティナ","スピネラ","ベリナ","テリナ","インカ",
        "ニディア","ピニア","シンベラ","ステリア","タセリア","ボビナ",
        "シャトリア","ウィーヴァ","フェルティナ","レシア","パチリア","クイルナ",
        "ペネリア","チョーリア","クリピア","ブクレナ","スプリア","モザリア"
    };
    private static readonly string[] Family={
        "ステッチ","クロス","コット","チャイム","グレイズ","ペンネル",
        "ボビン","スプール","シャトル","シンブル","タッセル","パッチ",
        "フェルト","レース","クイル","ニブレット","クリップ","ブックレット",
        "チョーク","タイル","ラッカー","リベット","スタンプ","スケイル"
    };

    // Country index is the stable array index. The caller owns/persists the seed.
    // A shuffled finite pool avoids retries or accidental infinite loops.
    public static string[] Generate(int count,int seed)
    {
        if(count<0 || count>GameLimits.MaxConquerors)throw new ArgumentOutOfRangeException(nameof(count));
        var random=new SeedRandom(seed^0x47554944);
        var pool=new int[Given.Length*Family.Length];
        for(int i=0;i<pool.Length;i++)pool[i]=i;
        var result=new string[count];
        for(int i=0;i<count;i++){
            int selected=i+random.Next(pool.Length-i);
            (pool[i],pool[selected])=(pool[selected],pool[i]);
            int pair=pool[i];
            result[i]=Given[pair/Family.Length]+"・"+Family[pair%Family.Length];
        }
        return result;
    }
}

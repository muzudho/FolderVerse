namespace FolderVerse;
using Microsoft.Xna.Framework;

// Readings are game-friendly katakana approximations. Sources are listed in README.md.
public static class CoastalNames
{
    public static readonly string[] Terms={"プエルト","プラヤ","ハーフェン","シュトラント","ポール","プラージュ","スピアッジャ","プライア","ハーバー","ビーチ","ショア"};
    public static string For(int seed,int cell,Point point)
    {
        int serial=cell*100+point.Y*10+point.X+1;
        var random=new SeedRandom(seed^serial^0x504F5254);
        return Terms[random.Next(Terms.Length)]+"・"+serial.ToString("D3");
    }
}

namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static class WorldNames
{
    // Katakana readings are game-friendly approximations, combined as fictional place names.
    private static readonly string[] Prefix={"ノヴァ","ヴィクトリア","ミラコロ","アルト","バホ","ローザ","メーラ","ヴェッキオ","フォンス","リオ"};
    private static readonly string[] Settlement={"ポリス","シウダード","プラサ","ヴィル","タウン"};
    private static readonly string[][] Land={
        new[]{"マーレ","インスラ","ペニンスラ"},
        new[]{"ペニンスラ","インスラ","ファレーズ","マーレ"},
        new[]{"プラデラ","ヴァレー","タール","バッサン","フルス","フォンス"},
        new[]{"モンターニャ","ベルク","コリナ","クエバ"},
        new[]{"モンターニャ","ベルク","コリナ","クエバ"}
    };
    public static string[] Cities(SurfaceCell[] cells,int seed)
    {
        var random=new SeedRandom(seed^0x43A17);var offset=WorldTerrain.Offset(seed);
        var names=new string[cells.Length];var used=new HashSet<string>();
        var kinds=cells.Select(c=>WorldTerrain.CellKind(c,offset)).ToArray();
        foreach(var cell in cells)
        {
            var terms=Land[(int)kinds[cell.Id]];string name;
            do{name=Prefix[random.Next(Prefix.Length)]+terms[random.Next(terms.Length)]+Settlement[random.Next(Settlement.Length)];}while(!used.Add(name));
            names[cell.Id]=name;
        }
        foreach(var cell in cells.Where(c=>kinds[c.Id]==TerrainKind.Sea))
        {
            var queue=new Queue<int>();var visited=new HashSet<int>{cell.Id};queue.Enqueue(cell.Id);int anchor=-1;
            while(queue.Count>0 && anchor<0)
            {
                int id=queue.Dequeue();
                foreach(int next in cells[id].Neighbors.OrderBy(n=>n))
                {
                    if(!visited.Add(next))continue;
                    if(kinds[next]!=TerrainKind.Sea){anchor=next;break;}
                    queue.Enqueue(next);
                }
            }
            if(anchor<0)continue;
            var land=cells[anchor];var normal=land.Normal;
            var up=normal.Y>0.5f?Vector3.Backward:normal.Y<-.5f?Vector3.Forward:Vector3.Up;
            var right=Vector3.Cross(up,normal);var delta=cell.Center-land.Center;
            float north=Vector3.Dot(delta,up),east=Vector3.Dot(delta,right);
            string direction=MathF.Abs(north)>=MathF.Abs(east)?(north>=0?"ノルテ":"スール"):(east>=0?"エステ":"オエステ");
            used.Remove(names[cell.Id]);string name=names[anchor]+"・"+direction;
            while(!used.Add(name))name+="・"+Prefix[random.Next(Prefix.Length)];
            names[cell.Id]=name;
        }
        return names;
    }
    public static string[] Conquerors(ConquerorLook[] looks,int seed)
    {
        string[] first={"リリア","エミリア","アリア","ルチア","セレナ","エレナ","ミリア","クララ","ヴィオラ","ソフィア","フローラ","イリス","レイナ","ノエル","ステラ","ローザ","フィオナ","ミレイ","シルヴィア","ルナ"};
        string[] last={"ベル","アルメ","リーヴ","ミエル","ロゼ","ヴェール","ルーチェ","シエル","ノア","リュミ"};
        var random=new SeedRandom(seed^0x15A96);var used=new HashSet<string>();var names=new string[looks.Length];
        for(int i=0;i<names.Length;i++)
        {
            string name;
            do{name=first[(looks[i].BaseId+random.Next(first.Length))%first.Length]+"・"+last[random.Next(last.Length)];}while(!used.Add(name));
            names[i]=name;
        }
        return names;
    }
}

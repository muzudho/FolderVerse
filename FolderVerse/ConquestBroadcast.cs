namespace FolderVerse;
using System;
using System.Linq;

// Read-only broadcast snapshot. Its private RNG never advances the simulation's RNG.
public sealed class ConquestBroadcast
{
    public string Announcer {get;}
    public int Location {get;}
    public bool Outdoor {get;}
    private readonly string[][] _lines;
    private readonly bool[] _changed;
    public static bool Detailed(WorldSetup w,int ruler,int rank)=>rank<=2 ||
        (w.HumanPlayers.Count>0?w.HumanPlayers.Contains(ruler):ruler==w.Relations.Leader);
    public bool Changed(int ruler)=>_changed[ruler];
    public string[] Lines(int ruler)=>_lines[ruler];
    public ConquestBroadcast(WorldSetup w)
    {
        Announcer=GuideNames.Generate(1,w.PlacementSeed^w.FortuneSeed^0x42524F41)[0];
        var random=new SeedRandom(w.PlacementSeed^w.FortuneSeed^unchecked(w.Population.Turn*104729)^0x54564E45);
        var nodes=w.Nodes.All.Where(n=>n.Functional).ToArray();
        Location=nodes.Length==0?-1:nodes[random.Next(nodes.Length)].Id;Outdoor=random.Next(2)==0;
        _lines=new string[w.ActiveCount][];_changed=new bool[w.ActiveCount];
        foreach(var r in w.Relations.ProvisionalRanking(w)){
            int delta=w.Campaign.SectorChange(w,r.Ruler,r.Cells),rank=w.Campaign.RankChange(w,r.Ruler,r.Rank);
            _changed[r.Ruler]=delta!=0;
            string status=!w.Relations.Powered[r.Ruler]?"今回は脱落となりました。":w.Relations.Superiors[r.Ruler]>=0?"配下として活動しています。":$"支配地域は {r.Cells} 地域です！";
            string change=delta==0?status:$"支配地域は {Math.Abs(delta)} 地域{(delta>0?"増えました！":"減りました。")}";
            string places="";var previous=w.Campaign.PreviousNodeOwners;
            if(previous.Length==w.Nodes.All.Length){
                var lost=w.Nodes.All.Where(n=>n.Functional && previous[n.Id]==r.Ruler && n.Owner>=0 && n.Owner!=r.Ruler && !w.Relations.Allied(n.Owner,r.Ruler)).GroupBy(n=>n.Owner).OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key).ToArray();
                if(lost.Length>0){var first=lost[0];int other=lost.Skip(1).Sum(g=>g.Count());places=$"{w.ConquerorNames[first.Key]}さんへ {first.Count()} 地点"+(other>0?$"、ほか {other} 地点":"")+"の支配が移りました。";}
            }
            string rankLine=rank==0?$"現在 {r.Rank} 位です。":$"順位は {Math.Abs(rank)} つ{(rank>0?"下がりました。":"上がりました！")}";
            _lines[r.Ruler]=new[]{$"{w.ConquerorNames[r.Ruler]}さん、",change,places,rankLine,delta<0?"巻き返しに期待しましょう～！":"今日も見ていきましょう！"}.Where(s=>s.Length>0).ToArray();
        }
    }
}
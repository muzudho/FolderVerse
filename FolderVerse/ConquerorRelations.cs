namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum DefeatedChoice { Recruit,Release,RemoveBattery }
public sealed record Captive(int Ruler,int Victor,int Node);
public sealed class ConquerorRelations
{
    public int Leader=-1;
    private const int Capacity=GameLimits.MaxConquerors;
    public int[] Superiors {get;}=Enumerable.Repeat(-1,Capacity).ToArray();
    public int[] Teams=>Enumerable.Range(0,Capacity).Select(Root).ToArray();
    public bool[] Powered {get;}=Enumerable.Repeat(true,Capacity).ToArray();
    public bool[] Released {get;}=new bool[Capacity];
    public List<Captive> Pending {get;}=new();
    public string Message {get;private set;}="";
    public void Reset()
    {
        Leader=-1;Pending.Clear();Message="";
        for(int i=0;i<Capacity;i++){Superiors[i]=-1;Powered[i]=true;Released[i]=false;}
    }
    public int Root(int ruler)
    {
        for(int steps=0;steps<Capacity;steps++){if(Superiors[ruler]<0)return ruler;ruler=Superiors[ruler];}
        throw new InvalidOperationException("Conqueror hierarchy contains a cycle.");
    }
    public bool SetSuperior(int ruler,int superior)
    {
        if(ruler<0 || ruler>=Capacity || superior< -1 || superior>=Capacity)throw new ArgumentOutOfRangeException();
        for(int parent=superior;parent>=0;parent=Superiors[parent])if(parent==ruler)return false;
        Superiors[ruler]=superior;return true;
    }
    public (int Ruler,int Depth)[] Forest(WorldSetup world)
    {
        var result=new List<(int,int)>();
        void Visit(int ruler,int depth){result.Add((ruler,depth));foreach(int child in Enumerable.Range(0,world.ActiveCount).Where(c=>Superiors[c]==ruler))Visit(child,depth+1);}
        foreach(int root in Enumerable.Range(0,world.ActiveCount).Where(r=>Superiors[r]<0).OrderBy(r=>r==Leader?0:1).ThenBy(r=>r))Visit(root,0);
        return result.ToArray();
    }
    public static int RankingScale(int participants){int scale=100;while(participants>=scale)scale=checked(scale*10);return scale;}
    public static string EntryNumber(int entry,int participants)=>entry.ToString(new string('0',(RankingScale(participants)-1).ToString().Length));
    public static int RankingScore(int cells,int entry,int participants)=>checked(cells*RankingScale(participants)+entry);
    // Ruler slots are the permanent clockwise IDs assigned by SetCast / PortraitCell.
    public (int Ruler,int Rank,int Cells,int Nodes)[] ProvisionalRanking(WorldSetup world)
    {
        bool InSubtree(int owner,int ruler)
        {
            if(owner<0 || owner>=world.ActiveCount)return false;
            for(int current=owner;current>=0;current=Superiors[current])if(current==ruler)return true;
            return false;
        }
        var scores=Enumerable.Range(0,world.ActiveCount).Select(ruler=>new
        {
            Ruler=ruler,
            Cells=world.Cells.Count(cell=>world.Nodes.InCell(cell.Id).Any() && world.Nodes.InCell(cell.Id).All(node=>InSubtree(node.Owner,ruler))),
            Nodes=world.Nodes.All.Count(node=>InSubtree(node.Owner,ruler))
        }).OrderByDescending(r=>RankingScore(r.Cells,r.Ruler+1,world.ActiveCount)).ToArray();
        return scores.Select((r,index)=>(r.Ruler,index+1,r.Cells,r.Nodes)).ToArray();
    }
    public (int Ruler,int Depth,int Rank,int Cells,int Nodes)[] RankedForest(WorldSetup world)
    {
        var ranking=ProvisionalRanking(world).ToDictionary(r=>r.Ruler);
        var result=new List<(int,int,int,int,int)>();
        var level=Enumerable.Range(0,world.ActiveCount).Where(r=>Superiors[r]<0).ToArray();
        int depth=0;
        while(level.Length>0)
        {
            foreach(int ruler in level.OrderBy(r=>ranking[r].Rank))
            {var score=ranking[ruler];result.Add((ruler,depth,score.Rank,score.Cells,score.Nodes));}
            var parents=level.ToHashSet();
            level=Enumerable.Range(0,world.ActiveCount).Where(r=>parents.Contains(Superiors[r])).ToArray();depth++;
        }
        return result.ToArray();
    }
    public bool Allied(int a,int b)=>a>=0 && b>=0 && Root(a)==Root(b);
    public bool Controls(int ruler){if(ruler<0 || Leader<0)return false;for(int steps=0;steps<Capacity && ruler>=0;steps++,ruler=Superiors[ruler])if(ruler==Leader)return true;return false;}
    public int[] Party(WorldSetup world)=>Leader<0?Array.Empty<int>():Forest(world).Where(r=>CanAct(r.Ruler) && Controls(r.Ruler) && (!world.HumanPlayers.Contains(r.Ruler) || r.Ruler==Leader)).Select(r=>r.Ruler).ToArray();
    public bool CanAct(int ruler)=>Powered[ruler] && !Released[ruler] && !Pending.Any(p=>p.Ruler==ruler);
    public bool Accepts(WorldSetup world,int ruler)=>world.Looks[ruler].Keywords.Personality is not (3 or 9 or 11);
    public void Capture(WorldSetup world,int ruler,int victor,int node)
    {
        if(ruler==victor || Allied(ruler,victor) || !Powered[ruler] || Released[ruler])return;
        if(Allied(victor,Leader) || world.HumanPlayers.Any(h=>Allied(victor,h)))
        {if(!Pending.Any(p=>p.Ruler==ruler)){Pending.Add(new(ruler,victor,node));Message="処遇を選んでください";}}
        else if(!Allied(ruler,Leader) && !world.HumanPlayers.Any(h=>Allied(ruler,h)))Released[ruler]=true;
    }
    public bool Choose(WorldSetup world,Captive captive,DefeatedChoice choice)
    {
        if(!Pending.Contains(captive))throw new InvalidOperationException("Conqueror disposition is not pending.");
        int ruler=captive.Ruler;
        if(choice==DefeatedChoice.Recruit && !Accepts(world,ruler))
        {Message="手下になることを拒んでいる / 残りの選択肢を選んでください";return false;}
        if(choice==DefeatedChoice.Recruit)
        {
            if(!SetSuperior(ruler,captive.Victor)){Message="上下関係が循環するため手下にできない";return false;}
            Released[ruler]=false;Message="手下になった";
        }
        else if(choice==DefeatedChoice.Release){SetSuperior(ruler,-1);Released[ruler]=true;Message="その節点に留まっている";}
        else
        {
            int former=Superiors[ruler];
            for(int child=0;child<Capacity;child++)if(Superiors[child]==ruler)SetSuperior(child,former);
            SetSuperior(ruler,-1);Powered[ruler]=false;Released[ruler]=true;
            foreach(var node in world.Nodes.All.Where(n=>n.Owner==ruler))
            {node.Owner=-1;Array.Clear(node.Population.Migration);}
            world.Nodes.Recount();Message="電池を引き抜いた / 領地は未征服";
        }
        Pending.Remove(captive);return true;
    }
    public Captive InsertBattery(WorldSetup world,int ruler)
    {
        if(Powered[ruler])throw new InvalidOperationException("Battery is already inserted.");
        Powered[ruler]=true;Released[ruler]=true;
        var captive=new Captive(ruler,world.PlayerSlot,world.Nodes.Current(ruler).Id);
        Pending.Add(captive);Message="電池を入れた / 処遇を選んでください";return captive;
    }
}


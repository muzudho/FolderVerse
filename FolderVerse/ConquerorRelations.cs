namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum DefeatedChoice { Recruit,Release,RemoveBattery }
public sealed record Captive(int Ruler,int Victor,int Node);
public sealed class ConquerorRelations
{
    public int Leader=-1;
    public int[] Superiors {get;}=Enumerable.Repeat(-1,20).ToArray();
    public int[] Teams=>Enumerable.Range(0,20).Select(Root).ToArray();
    public bool[] Powered {get;}=Enumerable.Repeat(true,20).ToArray();
    public bool[] Released {get;}=new bool[20];
    public List<Captive> Pending {get;}=new();
    public string Message {get;private set;}="";
    public void Reset()
    {
        Leader=-1;Pending.Clear();Message="";
        for(int i=0;i<20;i++){Superiors[i]=-1;Powered[i]=true;Released[i]=false;}
    }
    public int Root(int ruler)
    {
        for(int steps=0;steps<20;steps++){if(Superiors[ruler]<0)return ruler;ruler=Superiors[ruler];}
        throw new InvalidOperationException("Conqueror hierarchy contains a cycle.");
    }
    public bool SetSuperior(int ruler,int superior)
    {
        if(ruler<0 || ruler>=20 || superior< -1 || superior>=20)throw new ArgumentOutOfRangeException();
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
    public bool Allied(int a,int b)=>a>=0 && b>=0 && Root(a)==Root(b);
    public int[] Party(WorldSetup world)=>Leader<0?Array.Empty<int>():Forest(world).Where(r=>Powered[r.Ruler] && !Released[r.Ruler] && Allied(r.Ruler,Leader)).Select(r=>r.Ruler).ToArray();
    public bool CanAct(int ruler)=>Powered[ruler] && !Released[ruler] && !Pending.Any(p=>p.Ruler==ruler);
    public bool Accepts(WorldSetup world,int ruler)=>world.Looks[ruler].Keywords.Personality is not (3 or 9 or 11);
    public void Capture(WorldSetup world,int ruler,int victor,int node)
    {
        if(ruler==victor || Allied(ruler,victor) || !Powered[ruler] || Released[ruler])return;
        if(Allied(victor,Leader))
        {if(!Pending.Any(p=>p.Ruler==ruler)){Pending.Add(new(ruler,victor,node));Message="処遇を選んでください";}}
        else if(!Allied(ruler,Leader))Released[ruler]=true;
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
            for(int child=0;child<20;child++)if(Superiors[child]==ruler)SetSuperior(child,former);
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

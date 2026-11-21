namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GuideRoster
{
    public int WorldSeed,CastSeed;
    public string[] Names;
    public int[] Bodies,LastTurns;
    public bool[] Introduced;
    public static GuideRoster Create(WorldSetup world)
    {
        int seed=world.WorldSeed^world.CastSeed;
        var random=new SeedRandom(seed^0x47424F44);
        return new(){WorldSeed=world.WorldSeed,CastSeed=world.CastSeed,Names=GuideNames.Generate(world.ActiveCount,seed),
            Bodies=Enumerable.Range(0,world.ActiveCount).Select(_=>random.Next(4)).ToArray(),
            LastTurns=Enumerable.Repeat(-1,world.ActiveCount).ToArray(),Introduced=new bool[world.ActiveCount]};
    }
}
public sealed partial class WorldSetup
{
    public GuideRoster Guides;
    public GuideRoster EnsureGuides()
    {
        if(Guides==null || Guides.WorldSeed!=WorldSeed || Guides.CastSeed!=CastSeed || Guides.Names?.Length!=ActiveCount)
            Guides=GuideRoster.Create(this);
        return Guides;
    }
}
public sealed record GuideBriefing(string[][] Pages,int OfficeNode);
public static class GuideNarration
{
    public static GuideBriefing Build(WorldSetup world,int ruler,bool introduction)
    {
        int root=world.Relations.Root(ruler);
        var score=world.Relations.ProvisionalRanking(world).Single(r=>r.Ruler==root);
        var node=world.Nodes.All.Where(n=>n.Functional && n.Owner==ruler && n.Purpose==NodePurpose.General && !n.PermanentLandmark)
            .OrderBy(n=>n.Id).FirstOrDefault(n=>world.Campaign.RobotBattles.MarchTargets(world,n.Id)
                .Any(t=>world.Nodes.All[t].Owner>=0 && !world.Relations.Allied(ruler,world.Nodes.All[t].Owner)));
        int rivals=Enumerable.Range(0,world.ActiveCount).Count(r=>world.Relations.CanWinConquest(r) && !world.Relations.Allied(ruler,r));
        var values=new Dictionary<string,string>{["conquerorName"]=world.ConquerorNames[ruler],
            ["totalRegions"]=world.Cells.Length.ToString(),["ownedRegions"]=score.Cells.ToString(),
            ["ownedNodes"]=score.Nodes.ToString(),["rank"]=score.Rank.ToString(),["rivals"]=rivals.ToString(),
            ["nodeName"]=node==null?"":world.Nodes.DisplayName(node)};
        var pages=new List<string[]>();
        if(introduction){pages.Add(GuideMessages.Page("introduction",values));pages.Add(GuideMessages.Page("overview",values));pages.Add(GuideMessages.Page("safety",values));}
        else pages.Add(GuideMessages.Page("daily",values));
        pages.Add(GuideMessages.Page(node==null?"cards":"defense",values));
        return new(pages.ToArray(),node?.Id??-1);
    }
}

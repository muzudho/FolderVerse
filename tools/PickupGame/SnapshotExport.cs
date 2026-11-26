using FolderVerse;
using Microsoft.Xna.Framework;
using System.Text.Json;

// Read-only observation. Call after the industry's work and before the next domestic phase.
public static class SnapshotExport
{
    public static void Capture(WorldSetup w,string directory,bool force=false)
    {
        Directory.CreateDirectory(directory);
        var ranking=w.Relations.ProvisionalRanking(w).Select(r=>new{r.Ruler,r.Cells,r.Rank}).ToArray();
        var daily=new{turn=w.Population.Turn,military=w.Nodes.All.Count(n=>n.Functional),civilian=w.Nodes.All.Count(n=>!n.Functional),ranking};
        File.AppendAllText(Path.Combine(directory,"daily.jsonl"),JsonSerializer.Serialize(daily)+"\n");
        if(!force && w.Population.Turn%20!=0)return;
        string stem=$"turn-{w.Population.Turn:D4}";
        var data=new{
            turn=w.Population.Turn,seed=w.WorldSeed,cast=w.CastSeed,rights=w.PlacementSeed,fortune=w.FortuneSeed,
            mode=w.EquipmentMode.ToString(),raw=w.Robots.MapleSupplyEnabled,relay=w.RelayLogisticsEnabled,scrap=w.Robots.ScrapRulesEnabled,
            military=daily.military,civilian=daily.civilian,ranking,
            countries=Enumerable.Range(0,w.ActiveCount).Select(r=>new{id=r,entry=r+1,name=w.ConquerorNames[r],root=w.Relations.Root(r),superior=w.Relations.Superiors[r],powered=w.Relations.Powered[r],color=Hex(w.OwnerColor(r)),sites=w.Nodes.All.Count(n=>n.Functional&&n.Owner==r)}),
            cells=w.Cells.Select(c=>new{c.Id,c.Face,c.X,c.Y,owner=w.Owners[c.Id],terrain=Enumerable.Range(0,100).Select(i=>w.Routes.Terrain(c.Id,new Point(i%10,i/10)).ToString())}),
            nodes=w.Nodes.All.Select(n=>new{n.Id,n.Cell,x=n.Center.X,y=n.Center.Y,n.Owner,root=n.Owner<0?-1:w.Relations.Root(n.Owner),n.Functional,purpose=n.Purpose.ToString(),n.IsHarbor,n.HasFinalDisposal,n.FunctionHost,capacity=w.Robots.Nodes[n.Id].TotalCapacity,stock=w.Robots.Nodes[n.Id].Count,limit=w.Robots.Nodes[n.Id].WarehouseLimit}),
            edges=w.Nodes.All.SelectMany(n=>w.Routes.Neighbors(n).Select(e=>new{source=n.Id,target=w.Nodes.At(e.Target,e.Entry).Id})).Where(e=>e.source<e.target).Distinct()
        };
        File.WriteAllText(Path.Combine(directory,stem+".json"),JsonSerializer.Serialize(data));
        AutoSaveStore.Write(Path.Combine(directory,stem+".fvs"),w);
    }
    private static string Hex(Color c)=>$"#{c.R:X2}{c.G:X2}{c.B:X2}";
}

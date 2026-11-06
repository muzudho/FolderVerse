using FolderVerse;
using System.Reflection;
using Microsoft.Xna.Framework;
void Check(bool ok,string text){if(!ok)throw new Exception(text);}
for(int seed=1;seed<=3;seed++){
var w=new WorldSetup();w.SetWorld(seed);w.SetCast(seed);w.SetTerritoryRights(seed);
Check(w.TerritoryRights.Sum()==w.Cells.Length,"rights sum");Check(w.Owners.All(i=>i==-1),"unclaimed");Check(w.TerritoryCounts.SequenceEqual(w.TerritoryRights),"rights display");
Check(w.TerritoryRights.Take(w.ActiveCount).Min()==1,"one-cell challenge");
try{w.ClaimLand(0,Array.Empty<int>());throw new Exception("invalid claim accepted");}catch(ArgumentException){} 
try{w.CompleteLand(seed);throw new Exception("incomplete claims accepted");}catch(InvalidOperationException){} 
w.SelectPlayer(0);foreach(int ruler in w.LandOrder())w.ClaimLand(ruler,w.Cells.Where(c=>w.Owners[c.Id]<0).Take(w.TerritoryRights[ruler]).Select(c=>c.Id).ToArray());
w.CompleteLand(seed);Check(!w.LandPending && w.Owners.All(i=>i>=0),"completion");foreach(int ruler in Enumerable.Range(0,w.ActiveCount))Check(w.Nodes.Current(ruler)!=null,"current node");
Check(w.TerritoryCounts.SequenceEqual(w.TerritoryRights),"land conservation");
Console.WriteLine($"PASS seed {seed}: {w.Cells.Length} cells, {w.Nodes.All.Length} nodes");}

WorldSetup Scatter(int percent)
{
    var w=new WorldSetup();w.SetWorld(1);w.SetCast(2);w.SetTerritoryRights(3);
    foreach(int ruler in w.LandOrder())
        w.ClaimLand(ruler,w.Cells.Where(c=>w.Owners[c.Id]<0).Take(w.TerritoryRights[ruler]).Select(c=>c.Id).ToArray());
    w.CompleteLand(4,percent);
    return w;
}
int baseline=Scatter(100).Robots.Nodes.Sum(s=>s.Count);
int[]? initialCapacities=null;
foreach(int percent in new[]{0,25,100}){
    var w=Scatter(percent);
    var capacities=w.Robots.Nodes.Select(s=>s.TotalCapacity).ToArray();
    initialCapacities??=capacities;
    Check(capacities.SequenceEqual(initialCapacities) && capacities.Distinct().Count()>1,"World seed fixes varied capacities independently of scatter");
    foreach(var node in w.Nodes.All){
        int capacity=capacities[node.Id];
        int area=Enumerable.Range(0,100).Count(i=>w.Routes.Terrain(node.Cell,new Point(i%10,i/10))==node.Terrain);
        Check(capacity is >=1 and <=12 && w.Robots.Workshops[node.Id].ReservedSlotLimit==capacity,"Hangar and factory use the same capacity");
        if(node.Terrain==TravelTerrain.Land && area==100)Check(capacity is >=8 and <=12,"Entirely flat region draws 8–12");
        if(node.Terrain!=TravelTerrain.Land && area<=50)Check(capacity is >=1 and <=3,"Small sea or mountain area draws 1–3");
        if(node.Terrain!=TravelTerrain.Land && area==100)Check(capacity is >=3 and <=5,"Entirely sea or mountain draws 3–5");
    }
    Check(w.Robots.Nodes.Sum(s=>s.Count)==(baseline*percent+50)/100,"Scatter scales the total from the same seed");
    for(int ruler=0;ruler<w.ActiveCount;ruler++){
        var pocket=w.Robots.Retinues[ruler];
        Check(pocket.Count==2 && pocket.Robots.All(r=>r.Owner==ruler && r.CanFight && r.NativeParts(ruler)==3 && r.CheapParts==0),"Each pocket starts with two regular native complete robots");
    }
    w.Robots.Check();
}
var fresh=Scatter(0);
var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(fresh));
Check(saved.Robots.Nodes.Select(s=>s.TotalCapacity).SequenceEqual(initialCapacities!),"Save restores each exact initial capacity");
Console.WriteLine("PASS: 0/25/100 scatter and two regular complete pocket robots for every conqueror.");

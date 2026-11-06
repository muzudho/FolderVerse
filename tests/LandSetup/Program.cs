using FolderVerse;
using System.Reflection;
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
foreach(int percent in new[]{0,25,100}){
    var w=Scatter(percent);
    Check(w.Robots.Nodes.Sum(s=>s.Count)==(baseline*percent+50)/100,"Scatter scales the total from the same seed");
    for(int ruler=0;ruler<w.ActiveCount;ruler++){
        var pocket=w.Robots.Retinues[ruler];
        Check(pocket.Count==2 && pocket.Robots.All(r=>r.Owner==ruler && r.CanFight && r.NativeParts(ruler)==3 && r.CheapParts==0),"Each pocket starts with two regular native complete robots");
    }
    w.Robots.Check();
}
Console.WriteLine("PASS: 0/25/100 scatter and two regular complete pocket robots for every conqueror.");

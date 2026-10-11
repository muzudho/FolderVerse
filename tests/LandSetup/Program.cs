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

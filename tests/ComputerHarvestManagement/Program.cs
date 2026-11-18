using FolderVerse;
using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
var assembly=typeof(WorldSetup).Assembly;
var manager=assembly.GetType("FolderVerse.ComputerHarvestManagement")!;
var policyType=assembly.GetType("FolderVerse.ComputerPolicy")!;
string[] fields={"ReleaseExhaustedHarvester","ClearExhaustedHarvestPurpose","ReleaseUnplannedHarvester","ClearUnplannedHarvestPurpose"};
void Check(bool yes,string text){if(!yes)throw new Exception(text);}
var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(0,2);w.SetWorld(104729);w.SetCast(1);w.SetTerritoryRights(2);
foreach(int ruler in w.LandOrder())w.ClaimLand(ruler,new ComputerLandPlanner(w).Select(ruler,null).Cells);
w.CompleteLand(3,25,25);
foreach(var n in w.Nodes.All)n.Owner=0;
w.Relations.Leader=0;
int source=w.Nodes.All.First(n=>n.Functional && w.Robots.RawDeposit(n.Id,RawMaterial.PP)>0).Id;
var stock=w.Robots;var store=stock.Nodes[source];

typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{Array.Empty<MaplePackage>()});
typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{Array.Empty<Robot>()});
typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(store,new object[]{60});
typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{12});
var keeper=stock.Create(0,RobotParts.Complete) with {WarehouseKeeper=true};var worker=stock.Create(0,RobotParts.Complete);
typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{new[]{keeper,worker}});
typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(stock.Workshops[source],null);stock.Workshops[source].LimitLines(3);
Check(stock.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Harvest,0),"Appointment");
typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(stock.PartPlanning,new object[]{w});
w.Nodes.All[source].Purpose=NodePurpose.Harvest;stock.Workshops[source].Automation=NodeAutomation.Auto;
var baseline=AutoSaveStore.Capture(w);
void Policy(int a,int b,int c,int d){
 var p=typeof(WorldSetup).GetMethod("RulerComputerPolicy",F)!.Invoke(w,new object[]{0})!;
 foreach(var pair in fields.Zip(new[]{a,b,c,d}))policyType.GetProperty(pair.First)!.SetValue(p,pair.Second);
 typeof(WorldSetup).GetMethod("SetCharacterComputerPolicy",F)!.Invoke(w,new[]{(object)w.Looks[0].BaseId,p});
}
void Deposits(bool exhausted){
 var r=w.Robots;
 var maple=(int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(r)!;maple[source]=0;
 var rest=(int[][])typeof(RobotWorld).GetField("_rawDeposits",F)!.GetValue(r)!;
 foreach(var row in rest)row[source]=0;
 if(!exhausted)rest[(int)RawMaterial.PP-1][source]=8;
}
bool Review(HashSet<RawMaterial> planned)=>(bool)manager.GetMethod("Review",S)!.Invoke(null,new object[]{w,w.Nodes.All[source],planned})!;
foreach(bool exhausted in new[]{true,false})
foreach(int workerChance in new[]{0,100})
foreach(int purposeChance in new[]{0,100}){
 w=AutoSaveStore.Restore<WorldSetup>(baseline);Deposits(exhausted);
 Policy(exhausted?workerChance:100-workerChance,exhausted?purposeChance:100-purposeChance,
        exhausted?100-workerChance:workerChance,exhausted?100-purposeChance:purposeChance);
 bool keep=Review(new());
 Check(w.Robots.PartPlanning.Orders.Count==(workerChance==100?1:0),"Wrong worker channel");
 Check(keep==(purposeChance==0) && (w.Nodes.All[source].Purpose==NodePurpose.Harvest)==keep,"Wrong purpose channel");
 Review(new());Check(w.Robots.PartPlanning.Orders.Count==(workerChance==100?1:0),"Duplicate cancellation");
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
 typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(restored.Robots.PartPlanning,new object[]{restored});
 Check(restored.Robots.Nodes[source].Robots.Single(r=>r.Id==worker.Id).HarvestWorker==(workerChance==0),"Actual role after save");
 Check(restored.Robots.Workshops[source].ProductionLines.Length==(workerChance==0?1:0),"Actual lane after save");
}
w=AutoSaveStore.Restore<WorldSetup>(baseline);Deposits(false);Policy(100,100,100,100);
Check(!Review(new(){RawMaterial.PP}) && w.Robots.PartPlanning.Orders.Count==0 && w.Nodes.All[source].Purpose==NodePurpose.Harvest,"Retain planned harvest");
var roll=manager.GetMethod("Roll",S)!;
for(int n=0;n<1000;n++){
 bool Draw(int chance)=>(bool)roll.Invoke(null,new object[]{chance,123,n,source,worker.Id,1})!;
 Check(!Draw(0)&&Draw(100),"Roll boundary");Check(Draw(50)==Draw(50),"Repeated roll changed");
}
var snapshot=AutoSaveStore.Capture(w);
foreach(var entry in snapshot.Objects.Where(e=>e.Type.StartsWith("FolderVerse.ComputerPolicy,")))
 foreach(string name in fields)entry.Fields.Remove("<"+name+">k__BackingField");
var old=AutoSaveStore.Restore<WorldSetup>(snapshot);
var oldPolicy=typeof(WorldSetup).GetMethod("RulerComputerPolicy",F)!.Invoke(old,new object[]{0})!;
Check((int)policyType.GetProperty(fields[0])!.GetValue(oldPolicy)! ==75,"Old policy fallback");
// Booking alone is not demand: remove every manufacturing recipe across the network.
w=AutoSaveStore.Restore<WorldSetup>(baseline);Deposits(false);
foreach(var shop in w.Robots.Workshops)foreach(var line in shop.ProductionLines)line.SelectPlan(3);
w.Robots.Workshops[source].ProductionLines.Single().SelectMapleHarvest(1,RawMaterial.PP);
HashSet<RawMaterial> Planned()=>(HashSet<RawMaterial>)manager.GetMethod("PlannedMaterials",S)!.Invoke(null,new object[]{w,source})!;
Check(Planned().Count==0,"Old harvest booking is not intention");
// Select a real catalogue recipe at a reachable own site that uses PP.
int target=RobotLongDelivery.Neighbors(w,source,0).First(n=>w.Robots.Workshops[n].ProductionLines.Length>0);
var recipe=w.Robots.Workshops[target].ProductionLines.First();
int pp=Enumerable.Range(0,recipe.PlanCount).Where(i=>i!=3).First(i=>{
 var product=recipe.PlanAt(i).Preview(0);
 return (int)typeof(RobotWorld).GetMethod("RawCost",S)!.Invoke(null,new object[]{product,RawMaterial.PP})!>0;
});
recipe.SelectPlan(pp);
typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(w.Robots.Nodes[target],new object[]{Array.Empty<MaplePackage>()});
Check(Planned().Contains(RawMaterial.PP),"Reachable manufacturing demand lost");
Console.WriteLine("PASS: 8 independent probability fixtures, actual cancellation/save/lane removal, demand retention, 0/100 boundaries, repeat stability, old-policy save, booking-only rejection, reachable recipe demand.");
// A neighboring manufacturer's demand can create a trade harvest intention.
w.Nodes.All[target].Owner=1;
var targetStore=w.Robots.Nodes[target];
var keepers=targetStore.Robots.Where(r=>r.WarehouseKeeper).ToArray();
typeof(RobotStore).GetMethod("Replace",F)!.Invoke(targetStore,new object[]{keepers});
var offered=w.Robots.Create(1,RobotParts.Body|RobotParts.Legs);targetStore.Add(offered);
w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Head));
Check(w.Campaign.Trade.Partners(w,new(0,source,false)).Any(p=>p.Node==target),"Trade partner fixture");
Check(Planned().Contains(RawMaterial.PP),"Useful trade harvest demand lost");
recipe.SelectPlan(3);
Check(!Planned().Contains(RawMaterial.PP),"Trade without use must not create demand");
// Exercise the public planning pipeline's purpose update: cleared Harvest must not be reintroduced by stale bookings.
w=AutoSaveStore.Restore<WorldSetup>(baseline);Deposits(true);Policy(100,100,0,0);
w.Robots.Workshops[source].ProductionLines.Single().SelectMapleHarvest(1,RawMaterial.PP);
var purposePlanner=assembly.GetType("FolderVerse.ComputerNodePurposes")!.GetMethod("Plan",S)!;
w.Robots.Workshops[source].Automation=NodeAutomation.Manual;
purposePlanner.Invoke(null,new object[]{w,true});
Check(!w.Robots.PartPlanning.Orders.Any(o=>o.Source==source) && w.Nodes.All[source].Purpose==NodePurpose.Harvest,"Manual site changed");
w.Robots.Workshops[source].Automation=NodeAutomation.Auto;
purposePlanner.Invoke(null,new object[]{w,true});
Check(w.Robots.PartPlanning.Orders.Any(o=>o.RobotId==worker.Id) && w.Nodes.All[source].Purpose!=NodePurpose.Harvest,"Integrated plan did not release/clear");
Console.WriteLine("PASS: useful trade demand, no-use trade rejection, manual-site protection, integrated purpose cancellation without stale-booking revival.");

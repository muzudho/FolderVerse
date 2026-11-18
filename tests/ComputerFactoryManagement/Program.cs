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
void Deposits(bool exhausted){
 var r=w.Robots;
 var maple=(int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(r)!;maple[source]=0;
 var rest=(int[][])typeof(RobotWorld).GetField("_rawDeposits",F)!.GetValue(r)!;
 foreach(var row in rest)row[source]=0;
 if(!exhausted)rest[(int)RawMaterial.PP-1][source]=8;
}

var factory=assembly.GetType("FolderVerse.ComputerFactoryManagement")!;
void FactoryPolicy(int worker,int purpose,int unstaffed,int near=50,int central=50){
 var p=typeof(WorldSetup).GetMethod("RulerComputerPolicy",F)!.Invoke(w,new object[]{0})!;
 string[] names={"ReleaseUnsuppliedManufacturer","ClearUnsuppliedFactoryPurpose","ClearUnstaffedFactoryPurpose","NearResourceFactoryPreference","CentralResourceFactoryPreference"};
 foreach(var pair in names.Zip(new[]{worker,purpose,unstaffed,near,central}))policyType.GetProperty(pair.First)!.SetValue(p,pair.Second);
 typeof(WorldSetup).GetMethod("SetCharacterComputerPolicy",F)!.Invoke(w,new[]{(object)w.Looks[0].BaseId,p});
}
void Resolve()=>typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(w.Robots.PartPlanning,new object[]{w});
bool FactoryReview()=>(bool)factory.GetMethod("Review",S)!.Invoke(null,new object[]{w,w.Nodes.All[source]})!;
bool Ensure(HashSet<RawMaterial> planned)=>(bool)factory.GetMethod("EnsureHarvester",S)!.Invoke(null,new object[]{w,w.Nodes.All[source],planned})!;
void PPPlan(RobotWorkshop line){line.Configure(RobotParts.Head,3);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,new ProductionPlan(RobotParts.Head,3){Material=RobotMaterial.Polypropylene});}
// Change existing harvest worker to manufacturer using the same real reservation path.
Check(w.Robots.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Worker,0),"Manufacturing appointment");Resolve();
PPPlan(w.Robots.Workshops[source].ProductionLines.Single());w.Nodes.All[source].Purpose=NodePurpose.Manufacturing;
var manufacturingBaseline=AutoSaveStore.Capture(w);
foreach(int workerChance in new[]{0,100})foreach(int purposeChance in new[]{0,100}){
 w=AutoSaveStore.Restore<WorldSetup>(manufacturingBaseline);FactoryPolicy(workerChance,purposeChance,100-purposeChance);
 bool kept=FactoryReview();
 Check(w.Robots.PartPlanning.Orders.Count==(workerChance==100?1:0),"Manufacturing release probability");
 Check(kept==(purposeChance==0) && (w.Nodes.All[source].Purpose==NodePurpose.Manufacturing)==kept,"Independent manufacturing purpose probability");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Resolve();
 Check(w.Robots.Nodes[source].Robots.Single(r=>r.Id==worker.Id).FactoryWorker==(workerChance==0),"Saved manufacturer release");
}
// With no manufacturer, the third probability applies independently.
w=AutoSaveStore.Restore<WorldSetup>(manufacturingBaseline);FactoryPolicy(100,0,0);FactoryReview();Resolve();
var idleBaseline=AutoSaveStore.Capture(w);
Check(FactoryReview() && w.Nodes.All[source].Purpose==NodePurpose.Manufacturing,"Unstaffed0 did not retain");
FactoryPolicy(0,0,100);Check(!FactoryReview() && w.Nodes.All[source].Purpose==NodePurpose.General,"Unstaffed100 did not clear");
// An incoming manufacturer appointment protects an otherwise unstaffed factory.
w=AutoSaveStore.Restore<WorldSetup>(idleBaseline);FactoryPolicy(0,0,100);
Check(w.Robots.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Worker,0),"Pending manufacturer fixture");
Check(FactoryReview() && w.Nodes.All[source].Purpose==NodePurpose.Manufacturing,"Pending manufacturer ignored");
w.Robots.Workshops[source].Automation=NodeAutomation.Manual;
Check(!(bool)factory.GetMethod("Managed",S)!.Invoke(null,new object[]{w,w.Nodes.All[source],true})!,"Manual factory is managed");
// Concrete incoming raw trade keeps the manufacturer even with release100.
w=AutoSaveStore.Restore<WorldSetup>(manufacturingBaseline);FactoryPolicy(100,100,100);
int neighbor=RobotLongDelivery.Neighbors(w,source,0).First();w.Nodes.All[neighbor].Owner=1;
typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(w.Robots.Nodes[neighbor],new object[]{new[]{new MaplePackage(900099,16){Material=RawMaterial.PP}}});
var incoming=new TradeOffer(w.Population.Turn,new(1,neighbor,false),new(0,source,false),new(900099,true),new(worker.Id,false));
w.Campaign.Trade.Offers.Add(incoming);
Check(FactoryReview() && !w.Robots.PartPlanning.Orders.Any(),"Incoming trade did not protect manufacturing");
// A real scheduled raw shipment is also a reason to wait.
w.Campaign.Trade.Offers.Clear();w.Nodes.All[neighbor].Owner=0;
Check(w.Robots.ReserveMapleShipment(w,neighbor,source,900099),"Incoming shipment fixture");
Check(FactoryReview() && !w.Robots.PartPlanning.Orders.Any(),"Incoming shipment did not protect manufacturing");
// A released worker can be reappointed to harvest and really yields one load.
w=AutoSaveStore.Restore<WorldSetup>(idleBaseline);Deposits(false);
Check(Ensure(new(){RawMaterial.PP}),"Harvester reappointment failed");
Check(!Ensure(new(){RawMaterial.PP}) && w.Robots.PartPlanning.Orders.Count==1,"Duplicate harvest appointment");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Resolve();
var harvest=w.Robots.Workshops[source].ProductionLines.Single();
Check(harvest.HarvestOnly && harvest.MapleRemaining==1 && harvest.MapleProgress==0,"Saved harvest settings");
int deposit=w.Robots.RawDeposit(source,RawMaterial.PP);
for(int tick=0;tick<IndustryTime.UnitsPerTurn;tick++)w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(w.Robots.Nodes[source].MaplePackages.Single().Units==16 && w.Robots.RawDeposit(source,RawMaterial.PP)==deposit-1,"Reappointed harvest did not yield");
// Clear harvest role, retain the harvested PP, and let factory planner appoint normally.
Check(w.Robots.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Harvest,0),"Clear harvester");Resolve();
foreach(var n in w.Nodes.All)w.Robots.Workshops[n.Id].Automation=NodeAutomation.Manual;
w.Robots.Workshops[source].Automation=NodeAutomation.Auto;w.Nodes.All[source].Purpose=NodePurpose.General;
factory.GetMethod("AppointManufacturers",S)!.Invoke(null,new object[]{w,true});
Check(w.Robots.PartPlanning.Orders.Single().Role==HangarReservation.Worker,"Manufacturer reappointment failed");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Resolve();
var manufacture=w.Robots.Workshops[source].ProductionLines.Single();
Check(manufacture.Manufacturing && !manufacture.HarvestOnly && manufacture.ProductionAge==0,"Saved new manufacturing recipe");
// Reconnect the purpose callback as the game does after reading a save.
typeof(RobotWorld).GetField("_nodePurpose",F)!.SetValue(w.Robots,(Func<int,NodePurpose>)(n=>w.Nodes.All[n].Purpose));
// Use the appointed catalogue recipe, including its real duration and raw cost.
var product=manufacture.CurrentProductionPlan.Preview(0);
int cost=(int)typeof(RobotWorld).GetMethod("RawCost",S)!.Invoke(null,new object[]{product,RawMaterial.PP})!;
manufacture.SetDoNotRepeat(true);manufacture.SetRest(1,1);manufacture.SetReservedSlots(0);
int period=(int)typeof(RobotWorkshop).GetMethod("EffectiveProductionPeriod",F)!.Invoke(manufacture,new object[]{w.Nodes.All[source].Purpose})!;
for(int tick=0;tick<period;tick++)w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(w.Robots.ProductionIntakes.Count==1 && w.Robots.Nodes[source].MaplePackages.Sum(p=>p.Units)==16-cost,$"Reappointed manufacturing: period={period}, age={manufacture.ProductionAge}, cost={cost}, raw={w.Robots.Nodes[source].MaplePackages.Sum(p=>p.Units)}, intakes={w.Robots.ProductionIntakes.Count}, failure={manufacture.IntakeFailure}, rest={manufacture.RestRemaining}");
// Location scores respond independently to the two published weights and actual raw quantities.
FactoryPolicy(0,0,0,100,0);double near=(double)factory.GetMethod("LocationScore",S,null,new[]{typeof(WorldSetup),typeof(int),typeof(Robot)},null)!.Invoke(null,new object[]{w,source,product})!;
FactoryPolicy(0,0,0,0,100);double central=(double)factory.GetMethod("LocationScore",S,null,new[]{typeof(WorldSetup),typeof(int),typeof(Robot)},null)!.Invoke(null,new object[]{w,source,product})!;
Check(!double.IsNaN(near) && !double.IsNaN(central),"Invalid location scores");
var distance=(Dictionary<int,int>)factory.GetMethod("Distances",S)!.Invoke(null,new object[]{w,source})!;
int far=distance.Where(x=>x.Value>0 && w.Nodes.All[x.Key].Owner==0).OrderByDescending(x=>x.Value).First().Key;
var mapleDeposits=(int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(w.Robots)!;
var otherDeposits=(int[][])typeof(RobotWorld).GetField("_rawDeposits",F)!.GetValue(w.Robots)!;
Array.Clear(mapleDeposits);foreach(var values in otherDeposits)Array.Clear(values);
otherDeposits[(int)RawMaterial.PP-1][source]=8;otherDeposits[(int)RawMaterial.Styrene-1][far]=8;
var mixed=new Robot(0,0,RobotParts.Head|RobotParts.Body){HeadMaterial=RobotMaterial.Polypropylene,BodyMaterial=RobotMaterial.Polystyrene};
var scoreMethod=factory.GetMethod("LocationScore",S,null,new[]{typeof(WorldSetup),typeof(int),typeof(Robot)},null)!;
FactoryPolicy(0,0,0,100,0);double nearest=(double)scoreMethod.Invoke(null,new object[]{w,source,mixed})!;
FactoryPolicy(0,0,0,0,100);double total=(double)scoreMethod.Invoke(null,new object[]{w,source,mixed})!;
Check(nearest==0 && Math.Abs(total+50*distance[far])<.001,"Independent nearest and quantity-weighted multiple-supplier distances");
var saved=AutoSaveStore.Capture(w);var restored=AutoSaveStore.Restore<WorldSetup>(saved);
var restoredPolicy=typeof(WorldSetup).GetMethod("RulerComputerPolicy",F)!.Invoke(restored,new object[]{0})!;
Check((int)policyType.GetProperty("CentralResourceFactoryPreference")!.GetValue(restoredPolicy)! ==100,"Factory policy save");
foreach(var entry in saved.Objects.Where(e=>e.Type.StartsWith("FolderVerse.ComputerPolicy,")))
 foreach(string field in new[]{"ReleaseUnsuppliedManufacturer","ClearUnsuppliedFactoryPurpose","ClearUnstaffedFactoryPurpose","NearResourceFactoryPreference","CentralResourceFactoryPreference"})entry.Fields.Remove("<"+field+">k__BackingField");
AutoSaveStore.Restore<WorldSetup>(saved);
Console.WriteLine("PASS factory release channels, unstaffed policy, incoming trade protection, harvest and manufacture reappointment/save/actual production, location score and old-policy fields.");

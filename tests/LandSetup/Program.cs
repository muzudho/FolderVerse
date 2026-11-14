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

WorldSetup Scatter(int percent,RobotEquipmentMode mode=RobotEquipmentMode.Legacy)
{
    var w=new WorldSetup{EquipmentMode=mode};w.SetWorld(1);w.SetCast(2);w.SetTerritoryRights(3);
    foreach(int ruler in w.LandOrder())
        w.ClaimLand(ruler,w.Cells.Where(c=>w.Owners[c.Id]<0).Take(w.TerritoryRights[ruler]).Select(c=>c.Id).ToArray());
    w.CompleteLand(4,percent);
    return w;
}
int ScatterCount(WorldSetup world)=>world.Robots.Nodes.Sum(s=>s.Robots.Count(r=>!r.FactoryWorker));
int baseline=ScatterCount(Scatter(100));
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
        if(w.Routes.NearWhirlpool(node)){
            Check(capacity==1,"Whirlpool sites have one slot");
            continue;
        }
        if(node.Terrain==TravelTerrain.Land && area==100)Check(capacity is >=8 and <=12,"Entirely flat region draws 8–12");
        if(node.Terrain!=TravelTerrain.Land && area<=50)Check(capacity is >=1 and <=3,"Small sea or mountain area draws 1–3");
        if(node.Terrain!=TravelTerrain.Land && area==100)Check(capacity is >=3 and <=5,"Entirely sea or mountain draws 3–5");
    }
    Check(ScatterCount(w)==(baseline*percent+50)/100,"Scatter scales unassigned robots from the same seed");
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

var materials=Scatter(100,RobotEquipmentMode.Materials);
materials=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(materials));
var assigned=materials.Nodes.All.SelectMany(node=>materials.Robots.Nodes[node.Id].Robots
    .Where(r=>r.FactoryWorker).Select(r=>(Node:node.Id,Robot:r))).ToArray();
Check(materials.Robots.Workshops.All(w=>w.BaseLineCount==0),"No permanent lanes in a new world");
Check(assigned.Any(a=>a.Robot.HarvestWorker) && assigned.Any(a=>!a.Robot.HarvestWorker),"Both worker roles are seeded");
var harvestSites=assigned.Where(a=>a.Robot.HarvestWorker).Select(a=>a.Node).ToArray();
var initialDeposits=harvestSites.Select(materials.Robots.MapleDeposit).ToArray();
var generatedDeposits=materials.Nodes.All.Select(n=>materials.Robots.MapleDeposit(n.Id)).ToArray();
Check(generatedDeposits.Any(n=>n>=13) && generatedDeposits.Any(n=>n is >=1 and <=3),
    "Material world contains both rich valley deposits and scattered small deposits");
foreach(var (node,worker) in assigned)
{
    var workshop=materials.Robots.Workshops[node];
    var lane=Enumerable.Range(0,workshop.ProductionLines.Length).Single(i=>workshop.WorkerAt(i)==worker.Id);
    Check(workshop.ProductionLines[lane].HarvestOnly==worker.HarvestWorker,"Worker owns the lane of the assigned kind");
    if(worker.HarvestWorker)
        Check(materials.Robots.MapleDeposit(node)>0 && workshop.ProductionLines[lane].MapleRemaining==1,"Harvester starts with a valid order");
    var role=worker.HarvestWorker?HangarReservation.Harvest:HangarReservation.Worker;
    int lanesBeforeRelease=workshop.ProductionLines.Length;
    Check(materials.Robots.ToggleReservation(node,worker.Id,role),"Initial assignment can be released");
    var released=materials.Robots.Nodes[node].Robots.Single(r=>r.Id==worker.Id);
    Check(released.CanFight && !released.FactoryWorker &&
          workshop.ProductionLines.Length==lanesBeforeRelease-1,
          "Released worker can fight and its lane disappears");
}
materials.Robots.Check();
var harvestWorld=Scatter(100,RobotEquipmentMode.Materials);
// Isolate harvesting from simultaneous manufacturing intake reservations.
foreach(var workshop in harvestWorld.Robots.Workshops)
    foreach(var lane in workshop.ProductionLines.Where(l=>!l.HarvestOnly))
        lane.SelectPlan(3);
for(int unit=0;unit<IndustryTime.UnitsPerTurn;unit++)
    harvestWorld.Robots.AdvanceIndustry(node=>harvestWorld.Nodes.All[node].Owner);
for(int i=0;i<harvestSites.Length;i++){
    int node=harvestSites[i];
    Check(harvestWorld.Robots.Nodes[node].MapleUnits==RobotWorld.MaplePackageSize &&
        harvestWorld.Robots.MapleDeposit(node)==initialDeposits[i]-1,
        $"Initial harvest worker at {node}: stock {harvestWorld.Robots.Nodes[node].MapleUnits}, "+
        $"deposit {initialDeposits[i]} → {harvestWorld.Robots.MapleDeposit(node)}, "+
        $"intakes {harvestWorld.Robots.ProductionIntakes.Count(x=>(x.Target??x.Source)==node)}");
}
Console.WriteLine("PASS: initial worker lanes, save round trip and release.");
Console.WriteLine("PASS: initial harvest deposits and stock at maximum robot scatter.");

var competing=Scatter(100,RobotEquipmentMode.Materials);
const int blockedSite=26;
var stock=competing.Robots;
var harvestLane=stock.Workshops[blockedSite].ProductionLines.Single(l=>l.HarvestOnly);
void AdvanceHarvestSite()
{
    for(int unit=0;unit<IndustryTime.UnitsPerTurn;unit++)
        stock.AdvanceIndustry(node=>competing.Nodes.All[node].Owner,node=>node!=blockedSite);
}
int remainingDeposit=stock.MapleDeposit(blockedSite);
AdvanceHarvestSite();
Check(stock.ProductionIntakes.Any(i=>(i.Target??i.Source)==blockedSite) &&
    stock.Nodes[blockedSite].MapleUnits==0 && stock.MapleDeposit(blockedSite)==remainingDeposit &&
    harvestLane.MapleRemaining==1,"Manufacturing intake keeps the initial harvest order waiting");
foreach(var lane in stock.Workshops[blockedSite].ProductionLines.Where(l=>!l.HarvestOnly))
    lane.SelectPlan(3);
var intake=stock.ProductionIntakes.Single(i=>(i.Target??i.Source)==blockedSite);
typeof(RobotWorld).GetMethod("ResolveProductionIntakes",BindingFlags.Instance|BindingFlags.NonPublic)!
    .Invoke(stock,new object[]{competing});
Check(!stock.ProductionIntakes.Contains(intake) && stock.Nodes[blockedSite].Robots.Any(r=>r.Id==intake.Product.Id),
    "Normal intake resolution places the competing product in the hangar");
int owner=competing.Nodes.All[blockedSite].Owner;
competing.SelectPlayer(owner);
competing.SelectTurnRuler(owner);
int target=FunctionalDeliveryRoutes.Targets(competing,blockedSite).First(n=>
    competing.Nodes.All[n].Owner==owner && stock.Nodes[n].HasSpace);
var order=stock.DirectDelivery.Reserve(competing,blockedSite,target,intake.Product.Id,intake.Product.Parts,null,stock.Nodes[target].Count);
stock.Advance((from,to)=>FunctionalDeliveryRoutes.Connected(competing,from,to),
    node=>competing.Nodes.All[node].Owner,4,industry:false);
Check(stock.DirectDelivery.CompletedArrivals.TryGetValue(order.Number,out long arrival) &&
    stock.Nodes[target].Robots.Any(r=>r.Id==arrival) && stock.Nodes[blockedSite].Robots.All(r=>r.Id!=intake.Product.Id),
    "Direct delivery ships the competing product and frees its original slot");
AdvanceHarvestSite();
Check(harvestLane.MapleRemaining==0 && stock.Nodes[blockedSite].MapleUnits==RobotWorld.MaplePackageSize &&
    stock.MapleDeposit(blockedSite)==remainingDeposit-1,"The same order resumes once hangar space is freed");
Console.WriteLine("PASS: first harvest resumes after the competing product ships by direct delivery.");

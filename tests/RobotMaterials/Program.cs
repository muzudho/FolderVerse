using FolderVerse;

static void Check(bool condition,string message)
{
    if(!condition)throw new Exception(message);
}

var materials=Enum.GetValues<RobotMaterial>();
var expected=new RobotEquipment[]{new(3,3,4),new(0,0,1),new(5,1,1),new(2,2,2),new(0,6,2),new(7,1,0),new(8,0,3),new(4,5,3),new(19,22,13)};
var basic=new Robot(1,0,RobotParts.Complete);
foreach(var material in materials)
{
    var unit=basic with {HeadMaterial=material,BodyMaterial=material,LegsMaterial=material};
    Check(unit.Equipment(0,RobotEquipmentMode.Materials)==expected[(int)material],$"Wrong equipment for {material}");
}
var mixed=basic with {HeadMaterial=RobotMaterial.Tin,BodyMaterial=RobotMaterial.Paper,LegsMaterial=RobotMaterial.Paper};
Check(mixed.Equipment(0,RobotEquipmentMode.Materials)==new RobotEquipment(6,7,5),"Mixed thirds must round once");
var changed=mixed.Merge(basic with {Id=2,Parts=RobotParts.Body,BodyMaterial=RobotMaterial.Maple},0);
Check(changed.HeadMaterial==RobotMaterial.Tin && changed.Slice(RobotParts.Body).BodyMaterial==RobotMaterial.Maple,"Material lost during merge or slice");
Check((mixed with {DamagedParts=RobotParts.Head}).Equipment(0,RobotEquipmentMode.Materials)==default,"Damaged part supplied equipment");
Check((mixed with {CheapParts=RobotParts.Complete}).Equipment(0,RobotEquipmentMode.Legacy)==new RobotEquipment(3,3,4),"Legacy cheap equipment changed");
Check((mixed with {PremiumParts=RobotParts.Head}).Equipment(0,RobotEquipmentMode.Legacy)==new RobotEquipment(4,4,5),"Legacy premium equipment changed");
var battle=new BattleState(1){EquipmentMode=RobotEquipmentMode.Materials};
battle.AddArmy(0,0,new[]{mixed});
var entry=battle.Armies.Single().Waiting.Single();
Check(new RobotEquipment(entry.Shields,entry.Daggers,entry.Rifles)==new RobotEquipment(6,7,5),"Battle entry ignored materials");

var factory=new RobotWorkshop();
for(int seed=0;seed<32;seed++)
{
    factory.GenerateLines(seed,RobotEquipmentMode.Materials);
    if(factory.ProductionLines.Length>0)break;
}
foreach(var line in factory.ProductionLines)
{
    var slots=Enumerable.Range(0,line.PlanCount).Where(i=>i!=3).ToArray();
    Check(slots.Select(i=>line.PlanAt(i).Material).Distinct().Count()==8,"Factory did not offer eight materials");
    Check(slots.All(i=>line.PlanAt(i).Material!=RobotMaterial.Tin),"Factory offered tin");
    int slot=slots.First(i=>line.PlanAt(i).Material==RobotMaterial.Maple);
    line.SelectPlan(3);
    line.SelectPlan(slot);
    var preview=line.CurrentProductionPlan.Preview(0);
    Check(new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(preview.Parts&p)!=0)
        .All(p=>preview.Material(p)==RobotMaterial.Maple),"Factory output lost material");
}
Check(factory.ProductionLines.Length>0,"Factory test generated no lines");
var stock=new RobotWorld();
stock.Initialize(1,1);
var production=typeof(RobotWorkshop).GetMethod("CompleteProduction",
    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
int productionTurns=factory.ProductionLines.Max(line=>line.ProductionPeriod);
for(int turn=0;turn<productionTurns;turn++)
    production.Invoke(factory,new object[]{stock,0,0});
var produced=stock.Nodes[0].Robots.ToArray();
Check(produced.Length>=factory.ProductionLines.Length && produced.All(unit=>
    new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(part=>(unit.Parts&part)!=0)
        .All(part=>unit.Material(part)==RobotMaterial.Maple)),"Factory storage lost material");
var world=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};
world.Robots.Initialize(1,1);
world.Robots.Nodes[0].Add(mixed);
AutoSaveStore.Write(args[0],world);
var restored=AutoSaveStore.Read<WorldSetup>(args[0]);
Check(restored.EquipmentMode==RobotEquipmentMode.Materials &&
    restored.Robots.Nodes[0].Robots.Single().Equipment(0,restored.EquipmentMode)==new RobotEquipment(6,7,5),
    "Equipment mode or part material did not survive save");
if(args.Length>1)
{
    var old=AutoSaveStore.Read<object>(args[1]);
    var oldWorld=old as WorldSetup ?? (WorldSetup)old.GetType().GetProperty("World")!.GetValue(old)!;
    Check(oldWorld.EquipmentMode==RobotEquipmentMode.Legacy,"Older save did not default to legacy");
}
if(args.Length>3)
{
    var legacy=AutoSaveStore.Read<WorldSetup>(args[2]);
    var modern=AutoSaveStore.Read<WorldSetup>(args[3]);
    Check(legacy.EquipmentMode==RobotEquipmentMode.Legacy && modern.EquipmentMode==RobotEquipmentMode.Materials,"Seeded worlds use wrong modes");
    Check(legacy.Owners.SequenceEqual(modern.Owners),"Material mode changed initial territory assignment");
    var tin=new List<(int Node,RobotParts Part)>();
    for(int node=0;node<modern.Robots.Nodes.Length;node++)
    {
        var oldRobots=legacy.Robots.Nodes[node].Robots;
        var newRobots=modern.Robots.Nodes[node].Robots;
        Check(oldRobots.Select(r=>(r.Id,r.Parts)).SequenceEqual(newRobots.Select(r=>(r.Id,r.Parts))),"Material mode changed initial stock layout");
        foreach(var robot in newRobots.Where(r=>r.Parts is RobotParts.Head or RobotParts.Body or RobotParts.Legs))
            if(robot.Material(robot.Parts)==RobotMaterial.Tin)tin.Add((node,robot.Parts));
    }
    Check(tin.Count>0 && tin.Select(t=>t.Node).Distinct().Count()==tin.Count &&
        tin.Select(t=>t.Part).Distinct().Count()==tin.Count,"Initial tin did not use separate single-part sites");
    Console.WriteLine($"PASS same-seed initial stock and territories match; {tin.Count} isolated tin parts.");
}
var groups=new Dictionary<RobotEquipment,int>();
for(int a=0;a<9;a++)for(int b=a;b<9;b++)for(int c=b;c<9;c++)
{
    var equipment=(basic with {HeadMaterial=materials[a],BodyMaterial=materials[b],LegsMaterial=materials[c]}).Equipment(0,RobotEquipmentMode.Materials);
    groups[equipment]=groups.GetValueOrDefault(equipment)+1;
}
Check(groups.Values.Sum()==165 && groups.Count==110 && groups.Values.Count(n=>n>1)==35 && groups.Values.Max()==5,"Equipment catalogue totals changed");
Console.WriteLine("PASS material arithmetic, merge/slice, legacy, battle entry, factory plans, save compatibility, 165/110 catalogue.");

var supply=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};
supply.SetWorld(0);supply.SetCast(123);supply.SetPlacement(456);supply.SelectPlayer(0);
foreach(var node in supply.Nodes.All)node.Owner=0;
var cargo=supply.Robots;
Check(cargo.MapleSupplyEnabled,"New materials worlds enable maple");
var forest=supply.Nodes.All.First(n=>cargo.MapleDeposit(n.Id)>0 && FunctionalDeliveryRoutes.Targets(supply,n.Id).Length>0);
int source=forest.Id,target=FunctionalDeliveryRoutes.Targets(supply,source)[0];
foreach(var workshop in cargo.Workshops)
    foreach(var line in workshop.ProductionLines)line.SelectPlan(3);
int deposit=cargo.MapleDeposit(source);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)cargo.AdvanceIndustry(_=>0,n=>n!=source);
var package=cargo.Nodes[source].MaplePackages.Single();
Check(package.Units==16 && cargo.MapleDeposit(source)==deposit-1,"Harvesting must exchange one deposit for one load");
Check(cargo.ReserveMapleShipment(supply,source,target,package.Id),"Adjacent maple shipment unavailable");
var restoredSupply=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
Check(restoredSupply.Robots.MapleShipments.Single().PackageId==package.Id && restoredSupply.Robots.Nodes[source].MapleUnits==16,"Maple stock or shipment lost on save");
var resolveMaple=typeof(RobotWorld).GetMethod("ResolveMapleShipments",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
resolveMaple.Invoke(cargo,new object[]{supply});
Check(cargo.Nodes[source].MapleUnits==0 && cargo.Nodes[target].MapleUnits==16 && cargo.MapleShipments.Count==0,"Shipment duplicated or lost cargo");
var mixedProduct=new Robot(0,0,RobotParts.Complete){HeadMaterial=RobotMaterial.Paper,BodyMaterial=RobotMaterial.Cardboard,LegsMaterial=RobotMaterial.Maple};
Check(RobotWorld.MapleCost(mixedProduct)==21 && RobotWorld.MapleCost(basic)==0,"Maple requirements do not match occupied parts");
var mapleFactory=cargo.Workshops[target];
for(int seed=0;mapleFactory.ProductionLines.Length==0;seed++)mapleFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
var mapleLine=mapleFactory.ProductionLines[0];
int paper=Enumerable.Range(0,mapleLine.PlanCount).First(i=>i!=3 && mapleLine.PlanAt(i).Material==RobotMaterial.Paper);
foreach(var line in mapleFactory.ProductionLines)line.SelectPlan(3);
mapleLine.SelectPlan(paper);
int cost=RobotWorld.MapleCost(mapleLine.CurrentProductionPlan.Preview(0));
Check(mapleLine.WaitingForMaple(cargo,Array.FindIndex(cargo.Nodes,s=>s.MapleUnits==0)),"Empty stock does not show material wait");
mapleFactory.SetReservedSlots(cargo.Nodes[target].TotalCapacity);
for(int i=0;i<mapleLine.ProductionPeriod;i++)production.Invoke(mapleFactory,new object[]{cargo,target,0});
Check(cargo.Nodes[target].MapleUnits==16 && mapleLine.Waiting,"A full intake consumed maple");
mapleFactory.SetReservedSlots(0);
production.Invoke(mapleFactory,new object[]{cargo,target,0});
Check(cargo.Nodes[target].MapleUnits==16-cost && cargo.ProductionIntakes.Any(i=>i.Source==target),"Successful intake did not consume exact maple units");
Check(supply.Campaign.FunctionTransfers.CanReserve(supply,target,source),"Fixture cannot transfer functions");
supply.Campaign.FunctionTransfers.Reserve(supply,target,source);
int remaining=cargo.Nodes[target].MapleUnits;
Check(cargo.PartPlanning.Preview(supply).Nodes[source].MapleUnits==remaining,"Transfer forecast lost maple stock");
supply.Campaign.FinishDomestic(supply);
Check(cargo.Nodes[source].MapleUnits==remaining && cargo.Nodes[target].MapleUnits==0 && cargo.MapleDeposit(target)==0,"Transfer lost stock or left a source open");
var previousSave=AutoSaveStore.Capture(world);
foreach(var savedObject in previousSave.Objects){savedObject.Fields?.Remove("_mapleDeposits");savedObject.Fields?.Remove("_mapleShipments");savedObject.Fields?.Remove("_maplePackages");}
Check(!AutoSaveStore.Restore<WorldSetup>(previousSave).Robots.MapleSupplyEnabled,"An older materials save was opted into maple");
Console.WriteLine("PASS maple harvesting, cargo shipping, save round-trip, exact recipe, blocked intake and function transfer.");

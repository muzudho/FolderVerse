using FolderVerse;

static void Check(bool condition,string message)
{
    if(!condition)throw new Exception(message);
}

var materials=Enum.GetValues<RobotMaterial>();
var packedStore=new RobotStore();
const System.Reflection.BindingFlags internalInstance=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(RobotStore).GetMethod("SetCapacity",internalInstance)!.Invoke(packedStore,new object[]{1});
var receiveMaple=typeof(RobotStore).GetMethod("TryReceiveMaple",internalInstance)!;
bool Receive(long id)=>(bool)receiveMaple.Invoke(packedStore,new object[]{new MaplePackage(id,16),0,(Func<long,bool>)(_=>false)})!;
packedStore.Add(new Robot(100,0,RobotParts.Head));
Check(Receive(101) && Receive(102) && !Receive(103) && packedStore.Count==1,"Two loads fit beside a head in one bay");
packedStore.Remove(100);
Check(Receive(103) && !Receive(104) && packedStore.Count==1,"Three maple loads fit in one empty bay");
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
foreach(var input in new[]{
    (RawMaterial.Styrene,RobotMaterial.Foam,2),
    (RawMaterial.Vinyl,RobotMaterial.SoftVinyl,4),
    (RawMaterial.PP,RobotMaterial.Polypropylene,8),
    (RawMaterial.Silicone,RobotMaterial.Silicone,8)})
{
    var copy=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
    var rawSite=copy.Nodes.All.FirstOrDefault(n=>copy.Robots.RawDeposit(n.Id,input.Item1)>0);
    for(int seed=1;rawSite==null && seed<=20;seed++)
    {
        copy.SetWorld(seed);copy.SetCast(123);copy.SetPlacement(456);
        rawSite=copy.Nodes.All.FirstOrDefault(n=>copy.Robots.RawDeposit(n.Id,input.Item1)>0);
    }
    var raw=copy.Robots;
    Check(rawSite!=null,$"Missing generated deposit for {input.Item1}");
    int node=rawSite!.Id, before=raw.RawDeposit(node,input.Item1);
    foreach(var robot in raw.Nodes[node].Robots.ToArray())raw.Nodes[node].Remove(robot.Id);
    var lane=new RobotWorkshop();
    lane.Configure(RobotParts.Head,1);
    typeof(RobotWorkshop).GetField("_selectedProductPlan",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
        .SetValue(lane,new ProductionPlan(RobotParts.Head,1){Material=input.Item2});
    production.Invoke(lane,new object[]{raw,node,0});
    Check(lane.WaitingForMaple(raw,node) && lane.ProductionAge==0,"Missing raw input blocks manufacturing progress");
    var harvestMethod=typeof(RobotWorld).GetMethod("HarvestMaple",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
    Check((bool)harvestMethod.Invoke(raw,new object[]{node,1,input.Item1})! &&
        raw.RawDeposit(node,input.Item1)==before-1 &&
        raw.Nodes[node].MaplePackages.Single().Material==input.Item1,"Harvest debits the matching deposit and stores the matching cargo");
    var rawCost=typeof(RobotWorld).GetMethod("RawCost",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
    var part=new Robot(0,0,RobotParts.Head){HeadMaterial=input.Item2};
    Check((int)rawCost.Invoke(null,new object[]{part,input.Item1})! ==input.Item3,"Raw input cost per part");
    var reload=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(copy));
    Check(reload.Robots.RawDeposit(node,input.Item1)==before-1 &&
        reload.Robots.Nodes[node].MaplePackages.Single().Material==input.Item1,"Raw supply survives save and load");
    if(input.Item1==RawMaterial.PP){
        foreach(var location in copy.Nodes.All)location.Owner=0;
        copy.SelectPlayer(0);
        copy.SelectTurnRuler(0);
        int destination=FunctionalDeliveryRoutes.Targets(copy,node).First(n=>raw.Nodes[n].HasSpace);
        var destinationStore=raw.Nodes[destination];
        foreach(var resident in destinationStore.Robots.ToArray())destinationStore.Remove(resident.Id);
        // Model a narrow site where the limit allows only three of the keeper's extra slots.
        typeof(RobotStore).GetMethod("SetWarehouseLimit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
            .Invoke(destinationStore,new object[]{4});
        var ppLoad=raw.Nodes[node].MaplePackages.Single();
        Check(lane.WaitingForMaple(raw,destination) &&
            raw.ReserveMapleShipment(copy,node,destination,ppLoad.Id),"Reserve one PP load while destination waits");
        var resolvePp=typeof(RobotWorld).GetMethod("ResolveMapleShipments",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var blockers=new List<long>();
        while(destinationStore.HasSpace){
            var blocker=raw.Create(0,RobotParts.Complete);
            destinationStore.Add(blocker);
            blockers.Add(blocker.Id);
        }
        resolvePp.Invoke(raw,new object[]{copy});
        Check(raw.Nodes[node].MaplePackages.Single()==ppLoad &&
            raw.MapleShipments.Single().PackageId==ppLoad.Id && lane.WaitingForMaple(raw,destination),
            "A full destination keeps the PP shipment queued and the material at its source");
        copy=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(copy));
        raw=copy.Robots;
        destinationStore=raw.Nodes[destination];
        var previewPp=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var blockedPp=((int Blocked,MaplePackage[] Packages))previewPp.Invoke(raw,new object[]{copy,destination,destinationStore})!;
        Check(blockedPp.Blocked==1 && blockedPp.Packages.Length==0,
            "The restored PP shipment is forecast to wait while the destination is full");
        resolvePp.Invoke(raw,new object[]{copy});
        Check(!destinationStore.HasSpace && destinationStore.MaplePackages.Count==0 &&
            raw.Nodes[node].MaplePackages.Single()==ppLoad && raw.MapleShipments.Single().PackageId==ppLoad.Id,
            "Restoring a full destination preserves the PP cargo and its pending shipment");
        int capacityBeforeAppointment=destinationStore.TotalCapacity;
        Check(raw.PartPlanning.ReserveRole(copy,destination,blockers[^1],HangarReservation.Warehouse,0),
            "Reserve a warehouse keeper at the full destination");
        typeof(RobotPartPlanning).GetMethod("Resolve",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
            .Invoke(raw.PartPlanning,new object[]{copy});
        Check(capacityBeforeAppointment==1 && destinationStore.TotalCapacity==4 &&
            destinationStore.Robots.Any(r=>r.Id==blockers[^1] && r.WarehouseKeeper),
            "Resolving the appointment expands capacity without removing a robot");
        var arrivingPp=((int Blocked,MaplePackage[] Packages))previewPp.Invoke(raw,new object[]{copy,destination,destinationStore})!;
        Check(arrivingPp.Blocked==0 && arrivingPp.Packages.Single()==ppLoad &&
            destinationStore.MaplePackages.Count==0 && raw.MapleShipments.Single().PackageId==ppLoad.Id,
            "After capacity expands, the forecast shows the same PP cargo without committing its delivery");
        resolvePp.Invoke(raw,new object[]{copy});
        Check(raw.Nodes[node].MaplePackages.Count==0 && raw.MapleShipments.Count==0 &&
            raw.Nodes[destination].MaplePackages.Single()==ppLoad &&
            !lane.WaitingForMaple(raw,destination),"Delivered PP enables the destination lane");
        var releaseWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(copy));
        var releaseStock=releaseWorld.Robots;
        var releaseStore=releaseStock.Nodes[destination];
        var keeper=releaseStore.Robots.Single(r=>r.WarehouseKeeper);
        Check(!releaseStock.CanSetReservation(destination,keeper,HangarReservation.Warehouse,0),
            "An arrived PP package prevents shrinking the hangar to one slot");
        typeof(RobotWorld).GetMethod("SpendMaple",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
            .Invoke(releaseStock,new object[]{destination,ppLoad.Units,RawMaterial.PP});
        Check(releaseStock.ToggleReservation(destination,keeper.Id,HangarReservation.Warehouse,0) &&
            releaseStore.TotalCapacity==1 && releaseStore.Robots.Single().Id==keeper.Id,
            "After consuming the last PP, the keeper can release the added slots");
        foreach(long id in blockers.Take(blockers.Count-1))destinationStore.Remove(id);
        node=destination;
    }
    for(int tick=0;tick<3 && raw.Nodes[node].MaplePackages.Single().Units==16;tick++)
        production.Invoke(lane,new object[]{raw,node,copy.Nodes.All[node].Owner});
    Check(raw.Nodes[node].MaplePackages.Single().Units==16-input.Item3 &&
        (raw.Nodes[node].Robots.Any(r=>r.HeadMaterial==input.Item2) ||
         raw.ProductionIntakes.Any(i=>i.Product.HeadMaterial==input.Item2)),
        "Manufacturing consumes the matching harvested input and produces the selected part");
}
Check(cargo.MapleSupplyEnabled,"New materials worlds enable maple");
var forest=supply.Nodes.All.First(n=>cargo.MapleDeposit(n.Id)>0 && FunctionalDeliveryRoutes.Targets(supply,n.Id).Length>0);
int source=forest.Id,target=FunctionalDeliveryRoutes.Targets(supply,source)[0];
var workerWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
var workerStock=workerWorld.Robots;
var harvestWorkshop=workerStock.Workshops[source];
harvestWorkshop.LimitLines(harvestWorkshop.BaseLineCount+1);
foreach(var resident in workerStock.Nodes[source].Robots.ToArray())workerStock.Nodes[source].Remove(resident.Id);
var harvester=workerStock.Create(0,RobotParts.Complete);
workerStock.Nodes[source].Add(harvester);
Check(workerStock.PartPlanning.ReserveRole(workerWorld,source,harvester.Id,HangarReservation.Harvest,0),"Reserve a harvest worker");
Check(workerStock.PartPlanning.Preview(workerWorld).Nodes[source].Robots.Single().HarvestWorker,"Reservation previews the harvest role");
workerWorld.Campaign.FinishDomestic(workerWorld);
var harvestWorkerLine=harvestWorkshop.ProductionLines.Last();
Check(harvestWorkerLine.HarvestOnly && harvestWorkshop.WorkerAt(harvestWorkshop.ProductionLines.Length-1)==harvester.Id,"Delivery activates a dedicated harvest lane");
harvestWorkerLine.SelectPlan(0);
Check(!harvestWorkerLine.Manufacturing,"The harvest worker lane cannot manufacture");
harvestWorkerLine.SelectMapleHarvest(1);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)workerStock.AdvanceIndustry(_=>0,n=>n!=source);
Check(workerStock.Nodes[source].MapleUnits==16,"The added lane harvests one load through industry");
var workerReload=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(workerWorld));
Check(workerReload.Robots.Workshops[source].ProductionLines.Last().HarvestOnly &&
    workerReload.Robots.Nodes[source].Robots.Single(r=>r.Id==harvester.Id).HarvestWorker,"Harvest assignment survives save and load");
Check(workerStock.ToggleReservation(source,harvester.Id,HangarReservation.Harvest) &&
    harvestWorkshop.ProductionLines.Length==harvestWorkshop.BaseLineCount,"Removing the harvest worker removes only their lane");
foreach(var workshop in cargo.Workshops)
    foreach(var line in workshop.ProductionLines)line.SelectPlan(3);
int deposit=cargo.MapleDeposit(source);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)cargo.AdvanceIndustry(_=>0,n=>n!=source);
Check(cargo.Nodes[source].MapleUnits==0 && cargo.MapleDeposit(source)==deposit,"Idle lanes must not harvest");
var harvest=cargo.Workshops[source];
for(int seed=0;harvest.ProductionLines.Length==0;seed++)harvest.GenerateLines(seed,RobotEquipmentMode.Materials);
foreach(var line in harvest.ProductionLines)line.SelectPlan(3);
var harvestLine=harvest.ProductionLines[0];
harvestLine.SelectMapleHarvest(1);
harvest.SetReservedSlots(harvest.ReservedSlotLimit);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)cargo.AdvanceIndustry(_=>0,n=>n!=source);
Check(cargo.MapleDeposit(source)==deposit && harvestLine.MapleProgress==0,"A blocked harvest must wait without spending a deposit");
harvest.SetReservedSlots(0);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)cargo.AdvanceIndustry(_=>0,n=>n!=source);
var package=cargo.Nodes[source].MaplePackages.Single();
Check(package.Units==16 && cargo.MapleDeposit(source)==deposit-1 && harvestLine.MapleRemaining==0,"Harvesting must exchange one deposit for one load");
Check(cargo.MaplePlantingCount(source)==1,"A successful harvest leaves one planting marker");
harvestLine.SelectMapleHarvest(1);
Check(cargo.MapleAvailableToPlan(source,null)==deposit-2,"Harvest orders must share the remaining deposit");
Check(cargo.ReserveMapleShipment(supply,source,target,package.Id),"Adjacent maple shipment unavailable");
var restoredSupply=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
Check(restoredSupply.Robots.MapleShipments.Single().PackageId==package.Id && restoredSupply.Robots.Nodes[source].MapleUnits==16,"Maple stock or shipment lost on save");
Check(restoredSupply.Robots.Workshops[source].ProductionLines[0].MapleRemaining==1,"Harvest order lost on save");
foreach(var (largeUnits,secondUnits,firstMaterial,secondMaterial) in new[]{
    (32,32,RawMaterial.Maple,RawMaterial.Maple),(48,48,RawMaterial.Maple,RawMaterial.Maple),
    (32,48,RawMaterial.Maple,RawMaterial.Maple),(48,32,RawMaterial.Maple,RawMaterial.Maple),
    (32,32,RawMaterial.Maple,RawMaterial.PP),(32,32,RawMaterial.PP,RawMaterial.Maple)})
{
    var largeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
    var largeStock=largeWorld.Robots;
    largeStock.CancelMapleShipment(source,package.Id);
    var timber=package with {Units=largeUnits,Material=firstMaterial};
    var secondTimber=timber with {Id=timber.Id+1000000,Units=secondUnits,Material=secondMaterial};
    typeof(RobotStore).GetMethod("SetMaplePackages",internalInstance)!
        .Invoke(largeStock.Nodes[source],new object[]{new[]{timber,secondTimber}});
    var store=largeStock.Nodes[target];
    var added=new List<long>();
    while(store.HasSpace){
        var blocker=largeStock.Create(0,RobotParts.Complete);
        store.Add(blocker);
        added.Add(blocker.Id);
    }
    store.Remove(added[^1]);
    Check(largeStock.ReserveMapleShipment(largeWorld,source,target,timber.Id) &&
        !largeStock.ReserveMapleShipment(largeWorld,source,target,secondTimber.Id),
        "Only one large shipment can book the last whole slot");
    store.Remove(added[^2]);
    Check(largeStock.ReserveMapleShipment(largeWorld,source,target,secondTimber.Id),
        "A second whole slot allows the later reservation");
    store.Add(new Robot(added[^2],0,RobotParts.Complete));
    var preview=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!;
    var forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(largeStock,new object[]{largeWorld,target,store})!;
    Check(forecast.Blocked==1 && forecast.Packages.Single()==timber && store.MaplePackages.Count==0,
        "Preview shows the first shipment without modifying the destination");
    store.Add(new Robot(added[^1],0,RobotParts.Complete));
    var resolve=typeof(RobotWorld).GetMethod("ResolveMapleShipments",internalInstance)!;
    resolve.Invoke(largeStock,new object[]{largeWorld});
    Check(largeStock.MapleShipments.Count==2 &&
        largeStock.Nodes[source].MaplePackages.Contains(timber) && store.MaplePackages.Count==0,
        "A large shipment waits intact when the destination fills after booking");
    largeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(largeWorld));
    largeStock=largeWorld.Robots;
    store=largeStock.Nodes[target];
    store.Remove(added[^1]);
    store.Add(new Robot(added[^1],0,RobotParts.Head|RobotParts.Body));
    forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(largeStock,new object[]{largeWorld,target,store})!;
    Check(forecast.Blocked==2 && forecast.Packages.Length==0,
        "After restore one empty section cannot receive a two- or three-section parcel");
    resolve.Invoke(largeStock,new object[]{largeWorld});
    Check(largeStock.MapleShipments.Count==2 &&
        largeStock.Nodes[source].MaplePackages.Contains(timber) && store.MaplePackages.Count==0,
        "The same large parcel remains at the source after retrying with insufficient room");
    store.Remove(added[^1]);
    if(largeUnits!=secondUnits || firstMaterial!=secondMaterial)
    {
        store.Add(new Robot(added[^1],0,RobotParts.Head));
        var twoSection=largeUnits==32 ? timber : secondTimber;
        var waiting=largeUnits==48 ? timber : secondTimber;
        forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(largeStock,new object[]{largeWorld,target,store})!;
        Check(forecast.Blocked==1 && forecast.Packages.Single()==twoSection && store.MaplePackages.Count==0,
            "With two free sections, only the first restored shipment that fits is forecast");
        resolve.Invoke(largeStock,new object[]{largeWorld});
        Check(store.MaplePackages.Single()==twoSection && largeStock.Nodes[source].MaplePackages.Single()==waiting &&
            largeStock.MapleShipments.Single().PackageId==waiting.Id,"Delivery preserves the waiting cargo and agrees with the forecast");
        store.Remove(added[^2]);
        forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(largeStock,new object[]{largeWorld,target,store})!;
        Check(forecast.Blocked==0 && forecast.Packages.SequenceEqual(new[]{twoSection,waiting}),
            "A whole slot forecasts the remaining parcel");
        resolve.Invoke(largeStock,new object[]{largeWorld});
        Check(largeStock.MapleShipments.Count==0 && largeStock.Nodes[source].MaplePackages.Count==0 &&
            store.MaplePackages.SequenceEqual(forecast.Packages),"Both mixed-size shipments arrive intact exactly once");
        if(firstMaterial!=secondMaterial)
        {
            var material=firstMaterial==RawMaterial.Maple ? RobotMaterial.Paper : RobotMaterial.Polypropylene;
            int recipeUnits=firstMaterial==RawMaterial.Maple ? 1 : 8;
            var receiver=new RobotWorkshop();
            receiver.Configure(RobotParts.Head,1);
            typeof(RobotWorkshop).GetField("_selectedProductPlan",internalInstance)!
                .SetValue(receiver,new ProductionPlan(RobotParts.Head,1){Material=material});
            var delivered=store.MaplePackages.ToArray();
            Check(!store.HasSpace,"The newly delivered cargo occupies the remaining capacity");
            for(int tick=0;tick<3;tick++)
                production.Invoke(receiver,new object[]{largeStock,target,largeWorld.Nodes.All[target].Owner});
            Check(receiver.WaitingProductionTurns>0 && store.MaplePackages.SequenceEqual(delivered) &&
                !largeStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,receiver)),
                "Blocked production restores both parcels and does not queue a product");
            // Removing the head leaves its raw cargo occupying that slot.
            store.Remove(added[^1]);
            var productionBlocker=store.Robots.FirstOrDefault(r=>r.Parts==RobotParts.Complete && !r.Stationed);
            if(productionBlocker!=null)store.Remove(productionBlocker.Id);
            Check(store.HasSpace,"The receiving hangar must have room for the manufactured head");
            for(int tick=0;tick<3 && store.MaplePackages.Contains(twoSection);tick++)
                production.Invoke(receiver,new object[]{largeStock,target,largeWorld.Nodes.All[target].Owner});
            Check(store.MaplePackages.Single(p=>p.Id==twoSection.Id)==(twoSection with {Units=twoSection.Units-recipeUnits}) &&
                store.MaplePackages.Single(p=>p.Id==waiting.Id)==waiting &&
                (store.Robots.Any(r=>r.HeadMaterial==material) ||
                 largeStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,receiver) && i.Product.HeadMaterial==material)),
                "Production uses only the matching delivered raw material and preserves the other parcel");
        }
        continue;
    }
    forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(largeStock,new object[]{largeWorld,target,store})!;
    Check(forecast.Blocked==1 && forecast.Packages.Single()==timber && store.MaplePackages.Count==0,
        "After restore the first reservation alone is forecast for the reopened slot");
    resolve.Invoke(largeStock,new object[]{largeWorld});
    Check(largeStock.MapleShipments.Single().PackageId==secondTimber.Id && largeStock.Nodes[source].MaplePackages.Single()==secondTimber &&
        store.MaplePackages.Single()==timber,"The same large shipment arrives intact when a whole slot opens");
    store.Remove(added[^2]);
    resolve.Invoke(largeStock,new object[]{largeWorld});
    Check(largeStock.MapleShipments.Count==0 && largeStock.Nodes[source].MaplePackages.Count==0 &&
        store.MaplePackages.SequenceEqual(new[]{timber,secondTimber}),
        "The later reservation arrives intact when the next slot opens");
}
foreach(int secondUnits in new[]{8,12})
{
    var mergeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
    var mergeStock=mergeWorld.Robots;
    var first=package with {Units=8};
    var second=first with {Id=first.Id+1000000,Units=secondUnits};
    typeof(RobotStore).GetMethod("SetMaplePackages",internalInstance)!
        .Invoke(mergeStock.Nodes[source],new object[]{new[]{first,second}});
    var store=mergeStock.Nodes[target];
    long lastBlocker=0;
    while(store.Count<store.TotalCapacity-1){
        var blocker=mergeStock.Create(0,RobotParts.Complete);
        store.Add(blocker);
        lastBlocker=blocker.Id;
    }
    Check(mergeStock.ReserveMapleShipment(mergeWorld,source,target,second.Id),
        "Reserve a second partial load for the same destination");
    var mergedPackage=first with {Units=16};
    bool overflow=secondUnits>8;
    var otherCargo=overflow?new[]{
        new MaplePackage(second.Id+1,16){Material=RawMaterial.PP},
        new MaplePackage(second.Id+2,16){Material=RawMaterial.PP}
    }:Array.Empty<MaplePackage>();
    typeof(RobotStore).GetMethod("SetMaplePackages",internalInstance)!.Invoke(store,new object[]{otherCargo});
    var preview=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!;
    var forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(mergeStock,new object[]{mergeWorld,target,store})!;
    var forecastPackage=overflow?first:mergedPackage;
    Check(forecast.Blocked==(overflow?1:0) && forecast.Packages.SequenceEqual(otherCargo.Append(forecastPackage)) &&
        store.MaplePackages.SequenceEqual(otherCargo),
        "The arrival preview accounts for whether the two loads fit together");
    var resolve=typeof(RobotWorld).GetMethod("ResolveMapleShipments",internalInstance)!;
    resolve.Invoke(mergeStock,new object[]{mergeWorld});
    if(overflow){
        Check(store.MaplePackages.SequenceEqual(otherCargo.Append(first)) && mergeStock.Nodes[source].MaplePackages.Single()==second &&
            mergeStock.MapleShipments.Single().PackageId==second.Id,
            "The second load waits intact when its remainder has no room");
        store.Remove(lastBlocker);
        resolve.Invoke(mergeStock,new object[]{mergeWorld});
        Check(store.MaplePackages.SequenceEqual(otherCargo.Append(mergedPackage).Append(second with {Units=secondUnits-8})) &&
            mergeStock.MapleShipments.Count==0 && mergeStock.Nodes[source].MaplePackages.Count==0,
            "A free slot allows the second load to arrive, conserving its remainder");
        continue;
    }
    Check(mergeStock.MapleShipments.Count==0 && mergeStock.Nodes[source].MaplePackages.Count==0 &&
        store.MaplePackages.Single()==mergedPackage && !store.HasSpace,
        "Delivery merges the loads into one full package in the last slot");
}
{
    var intakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
    var intakeStock=intakeWorld.Robots;
    var store=intakeStock.Nodes[target];
    var deployed=intakeStock.Create(0,RobotParts.Complete);
    store.Add(deployed);
    typeof(RobotStore).GetMethod("Depart",internalInstance)!.Invoke(store,new object[]{deployed.Id});
    long blockerId=0;
    while(store.Count<store.StorageLimit-1){
        var blocker=intakeStock.Create(0,RobotParts.Complete);
        store.Add(blocker);
        blockerId=blocker.Id;
    }
    var product=intakeStock.Create(0,RobotParts.Complete);
    intakeStock.ProductionIntakes.Add(new(source,target,product,intakeStock.Workshops[source].ProductionLines[0]));
    var preview=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!;
    var plannedStore=intakeStock.PartPlanning.Preview(intakeWorld).Nodes[target];
    var forecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(intakeStock,new object[]{intakeWorld,target,plannedStore})!;
    Check(plannedStore.ReservedReturns==1 && plannedStore.Robots.Any(r=>r.Id==product.Id) &&
        forecast.Blocked==1 && forecast.Packages.Length==0,
        "The hangar forecast keeps separate slots for the returning robot and the manufactured arrival");
    var resolveCargo=typeof(RobotWorld).GetMethod("ResolveMapleShipments",internalInstance)!;
    resolveCargo.Invoke(intakeStock,new object[]{intakeWorld});
    Check(intakeStock.MapleShipments.Single().PackageId==package.Id && store.MaplePackages.Count==0,
        "A pending manufactured arrival protects its slot during raw delivery");
    typeof(RobotWorld).GetMethod("ResolveProductionIntakes",internalInstance)!.Invoke(intakeStock,new object[]{intakeWorld});
    Check(intakeStock.ProductionIntakes.Count==0 && store.Robots.Any(r=>r.Id==product.Id),
        "The manufactured robot arrives in its reserved slot");
    typeof(RobotStore).GetMethod("ReleaseReturn",internalInstance)!.Invoke(store,new object[]{deployed.Id});
    store.Add(deployed);
    Check(store.ReservedReturns==0 && !store.HasSpace,"The returning robot fills its reserved slot");
    resolveCargo.Invoke(intakeStock,new object[]{intakeWorld});
    Check(intakeStock.MapleShipments.Single().PackageId==package.Id && store.MaplePackages.Count==0,
        "Returning does not let the waiting raw shipment displace either robot");
    store.Remove(blockerId);
    resolveCargo.Invoke(intakeStock,new object[]{intakeWorld});
    Check(intakeStock.MapleShipments.Count==0 && store.MaplePackages.Single()==package,
        "The same raw shipment arrives when another slot opens");
}
foreach(bool fillsLastSection in new[]{false,true})
{
    var mergeIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
    var mergeIntakeStock=mergeIntakeWorld.Robots;
    var store=mergeIntakeStock.Nodes[target];
    var receiver=mergeIntakeStock.Create(0,fillsLastSection?RobotParts.Head|RobotParts.Legs:RobotParts.Head);
    store.Add(receiver);
    while(store.HasSpace)store.Add(mergeIntakeStock.Create(0,RobotParts.Complete));
    var body=mergeIntakeStock.Create(0,RobotParts.Body);
    mergeIntakeStock.ProductionIntakes.Add(new(source,target,body,mergeIntakeStock.Workshops[source].ProductionLines[0]));
    var planned=mergeIntakeStock.PartPlanning.Preview(mergeIntakeWorld).Nodes[target];
    var forecast=((int Blocked,MaplePackage[] Packages))typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!
        .Invoke(mergeIntakeStock,new object[]{mergeIntakeWorld,target,planned})!;
    var combined=receiver.Parts|RobotParts.Body;
    Check(planned.Robots.Single(r=>r.Id==receiver.Id).Parts==combined &&
        forecast.Blocked==(fillsLastSection?1:0) && store.MaplePackages.Count==0,
        "Forecast accounts for the manufactured body before assigning a section to the raw load");
    typeof(RobotWorld).GetMethod("ResolveProductionIntakes",internalInstance)!.Invoke(mergeIntakeStock,new object[]{mergeIntakeWorld});
    typeof(RobotWorld).GetMethod("ResolveMapleShipments",internalInstance)!.Invoke(mergeIntakeStock,new object[]{mergeIntakeWorld});
    Check(mergeIntakeStock.ProductionIntakes.Count==0 &&
        store.Robots.Single(r=>r.Id==receiver.Id).Parts==combined &&
        store.MaplePackages.SequenceEqual(forecast.Packages) && store.Count==store.TotalCapacity,
        "Production and raw deliveries agree with the forecast");
    if(fillsLastSection)
    {
        Check(forecast.Packages.Length==0 && mergeIntakeStock.MapleShipments.Single().PackageId==package.Id &&
            mergeIntakeStock.Nodes[source].MaplePackages.Contains(package),
            "The last section goes to the manufactured body and the same raw load waits at the source");
        mergeIntakeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(mergeIntakeWorld));
        mergeIntakeStock=mergeIntakeWorld.Robots;
        store=mergeIntakeStock.Nodes[target];
        var preview=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!;
        var restoredForecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(mergeIntakeStock,
            new object[]{mergeIntakeWorld,target,mergeIntakeStock.PartPlanning.Preview(mergeIntakeWorld).Nodes[target]})!;
        Check(restoredForecast.Blocked==1 && restoredForecast.Packages.Length==0 &&
            store.Robots.Single(r=>r.Id==receiver.Id).Parts==RobotParts.Complete,
            "Restore preserves the completed robot and forecasts that the raw load still waits");
        var resolve=typeof(RobotWorld).GetMethod("ResolveMapleShipments",internalInstance)!;
        resolve.Invoke(mergeIntakeStock,new object[]{mergeIntakeWorld});
        Check(mergeIntakeStock.MapleShipments.Single().PackageId==package.Id &&
            mergeIntakeStock.Nodes[source].MaplePackages.Contains(package) && store.MaplePackages.Count==0,
            "Retry while full keeps the same reservation and does not move the restored raw load");
        store.Remove(store.Robots.First(r=>r.Id!=receiver.Id).Id);
        restoredForecast=((int Blocked,MaplePackage[] Packages))preview.Invoke(mergeIntakeStock,
            new object[]{mergeIntakeWorld,target,mergeIntakeStock.PartPlanning.Preview(mergeIntakeWorld).Nodes[target]})!;
        Check(restoredForecast.Blocked==0 && restoredForecast.Packages.Single()==package && store.MaplePackages.Count==0,
            "The opened slot forecasts the restored parcel exactly once");
        resolve.Invoke(mergeIntakeStock,new object[]{mergeIntakeWorld});
        Check(!mergeIntakeStock.Nodes[source].MaplePackages.Contains(package),
            "Completing the restored reservation removes the original raw load from its source");
    }
    Check(mergeIntakeStock.MapleShipments.Count==0 && store.MaplePackages.Single()==package,
        "The original raw load arrives once space is available");
}
// A reserved load must become available to the destination lane only after delivery.
var restoredCargo=restoredSupply.Robots;
Check(restoredCargo.MaplePlantingCount(source)==1,"Planting markers survive a save round trip");
var receivingFactory=restoredCargo.Workshops[target];
for(int seed=0;receivingFactory.ProductionLines.Length==0;seed++)
    receivingFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
var receivingLine=receivingFactory.ProductionLines[0];
int receivingPlan=Enumerable.Range(0,receivingLine.PlanCount)
    .First(i=>i!=3 && receivingLine.PlanAt(i).Material==RobotMaterial.Paper);
receivingLine.SelectPlan(receivingPlan);
Check(receivingLine.WaitingForMaple(restoredCargo,target),"Destination cannot use an undelivered load");
restoredSupply.Campaign.FinishDomestic(restoredSupply);
Check(restoredCargo.Nodes[target].MapleUnits==16 && restoredCargo.MapleShipments.Count==0 &&
    !receivingLine.WaitingForMaple(restoredCargo,target) && receivingLine.ProductionAge==0,
    "Delivery must release the material wait without spending an industry step");
var receivingRecipe=receivingLine.CurrentProductionPlan.Preview(0);
int receivingCost=RobotWorld.MapleCost(receivingRecipe);
for(int unit=0;unit<receivingLine.ProductionPeriod;unit++)
    production.Invoke(receivingFactory,new object[]{restoredCargo,target,0});
var receivingIntake=restoredCargo.ProductionIntakes.Single(i=>ReferenceEquals(i.Line,receivingLine));
Check(receivingCost>0 && restoredCargo.Nodes[target].MapleUnits==16-receivingCost &&
    receivingIntake.Product.Parts==receivingRecipe.Parts &&
    new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(receivingRecipe.Parts&p)!=0)
        .All(p=>receivingIntake.Product.Material(p)==receivingRecipe.Material(p)),
    "The next industry cycle consumes the delivered maple and queues the selected product");
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
// Exercise mixed-material production from an empty material store through delivery.
var waitingNode=source;
var waitingFactory=cargo.Workshops[waitingNode];
for(int seed=0;waitingFactory.ProductionLines.Length==0;seed++)waitingFactory.GenerateLines(seed,RobotEquipmentMode.Materials);
var waitingLine=waitingFactory.ProductionLines[0];
int mixedChoice=Array.FindIndex(waitingLine.Plans,p=>p.Product==RobotParts.Complete);
for(int seed=0;mixedChoice<0;seed++){
    waitingLine.GeneratePlans(seed);
    mixedChoice=Array.FindIndex(waitingLine.Plans,p=>p.Product==RobotParts.Complete);
}
waitingLine.Plans[mixedChoice]=new(RobotParts.Complete,3) {
    HeadMaterial=RobotMaterial.Paper,BodyMaterial=RobotMaterial.Cardboard,LegsMaterial=RobotMaterial.Maple};
waitingLine.SelectPlan(mixedChoice);
production.Invoke(waitingFactory,new object[]{cargo,waitingNode,0});
Check(waitingLine.WaitingForMaple(cargo,waitingNode) && waitingLine.ProductionAge==0,
    "Mixed recipe waits without progress when maple is missing");
Check((bool)receiveMaple.Invoke(cargo.Nodes[waitingNode],new object[]{new MaplePackage(999001,21),0,(Func<long,bool>)(_=>false)})!,
    "Mixed recipe fixture can receive exactly 21 units");
Check(cargo.ReserveMapleShipment(supply,waitingNode,target,999001),"Mixed recipe fixture can reserve its only stock for delivery");
production.Invoke(waitingFactory,new object[]{cargo,waitingNode,0});
Check(waitingLine.WaitingForMaple(cargo,waitingNode) && waitingLine.ProductionAge==0 &&
    cargo.Nodes[waitingNode].MapleUnits==21,"Stock reserved for shipping cannot start manufacturing");
Check(cargo.CancelMapleShipment(waitingNode,999001),"Cancelling delivery makes the stock available again");
for(int i=0;i<waitingLine.ProductionPeriod;i++)production.Invoke(waitingFactory,new object[]{cargo,waitingNode,0});
var mixedIntake=cargo.ProductionIntakes.Single(i=>ReferenceEquals(i.Line,waitingLine));
Check(cargo.Nodes[waitingNode].MapleUnits==0,"Mixed recipe consumes all 21 units");
typeof(RobotWorld).GetMethod("ResolveProductionIntakes",internalInstance)!.Invoke(cargo,new object[]{supply});
Check(!cargo.ProductionIntakes.Contains(mixedIntake) &&
    cargo.Nodes[waitingNode].Robots.Any(r=>r.Id==mixedIntake.Product.Id &&
        r.Material(RobotParts.Head)==RobotMaterial.Paper && r.Material(RobotParts.Body)==RobotMaterial.Cardboard &&
        r.Material(RobotParts.Legs)==RobotMaterial.Maple),"Intake places the exact mixed materials in the hangar");

// Both ingredients of one product must roll back together if its intake is full.
var recipeWorld=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(supply));
var recipeStock=recipeWorld.Robots;
var recipeFactory=recipeStock.Workshops[waitingNode];
var recipeLine=recipeFactory.ProductionLines[0];
recipeLine.Plans[mixedChoice]=recipeLine.Plans[mixedChoice] with {HeadMaterial=RobotMaterial.Polypropylene};
recipeLine.SelectPlan(3);
recipeLine.SelectPlan(mixedChoice);
var waitingStore=recipeStock.Nodes[waitingNode];
Check((bool)receiveMaple.Invoke(waitingStore,new object[]{new MaplePackage(999002,21),0,(Func<long,bool>)(_=>false)})! &&
    (bool)receiveMaple.Invoke(waitingStore,new object[]{new MaplePackage(999003,16){Material=RawMaterial.PP},0,(Func<long,bool>)(_=>false)})!,
    "The two-ingredient fixture stores Maple and PP separately");
var beforeMixedIntake=waitingStore.MaplePackages.ToArray();
Check(recipeStock.ReserveMapleShipment(recipeWorld,waitingNode,target,999003),
    "The mixed recipe fixture reserves only its PP cargo for delivery");
production.Invoke(recipeFactory,new object[]{recipeStock,waitingNode,0});
Check(recipeLine.WaitingForMaple(recipeStock,waitingNode) && recipeLine.ProductionAge==0 &&
    waitingStore.MaplePackages.SequenceEqual(beforeMixedIntake) &&
    !recipeStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,recipeLine)),
    "A reserved PP ingredient blocks the complete recipe without consuming the unreserved maple");
var reservedRecipe=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(recipeWorld));
var reservedStock=reservedRecipe.Robots;
var reservedFactory=reservedStock.Workshops[waitingNode];
var reservedLine=reservedFactory.ProductionLines[0];
Check(reservedLine.WaitingForMaple(reservedStock,waitingNode) &&
    reservedStock.Nodes[waitingNode].MaplePackages.SequenceEqual(beforeMixedIntake) &&
    reservedStock.CancelMapleShipment(waitingNode,999003),
    "The restored PP reservation still blocks the recipe and can be cancelled");
for(int i=0;i<reservedLine.ProductionPeriod;i++)production.Invoke(reservedFactory,new object[]{reservedStock,waitingNode,0});
Check(reservedStock.ProductionIntakes.Count(i=>ReferenceEquals(i.Line,reservedLine))==1 &&
    reservedStock.Nodes[waitingNode].MapleUnits==1 &&
    reservedStock.Nodes[waitingNode].MaplePackages.Single(p=>p.Material==RawMaterial.PP).Units==8,
    "Cancelling the restored shipment restarts the mixed recipe and consumes both ingredients once");
var suppliedRecipe=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(recipeWorld));
var suppliedStock=suppliedRecipe.Robots;
var suppliedFactory=suppliedStock.Workshops[waitingNode];
var suppliedLine=suppliedFactory.ProductionLines[0];
var suppliedStore=suppliedStock.Nodes[waitingNode];
var heldShipment=suppliedStock.MapleShipments.Single(o=>o.PackageId==999003);
var heldPackage=suppliedStore.MaplePackages.Single(p=>p.Id==heldShipment.PackageId);
var freePP=new MaplePackage(999004,16){Material=RawMaterial.PP};
Check((bool)receiveMaple.Invoke(suppliedStore,new object[]{freePP,0,(Func<long,bool>)(id=>id==heldShipment.PackageId)})!,
    "The factory receives separate PP stock while its earlier PP cargo is reserved");
for(int i=0;i<suppliedLine.ProductionPeriod;i++)production.Invoke(suppliedFactory,new object[]{suppliedStock,waitingNode,0});
Check(suppliedStock.ProductionIntakes.Count(i=>ReferenceEquals(i.Line,suppliedLine))==1 &&
    suppliedStore.MapleUnits==1 &&
    suppliedStock.MapleShipments.Contains(heldShipment) && suppliedStore.MaplePackages.Contains(heldPackage) &&
    suppliedStore.MaplePackages.Single(p=>p.Id==freePP.Id)==(freePP with {Units=8}),
    "The mixed recipe uses unreserved PP while keeping the reserved shipment and cargo intact");
var simultaneous=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(suppliedRecipe));
var simultaneousStock=simultaneous.Robots;
var productIntake=simultaneousStock.ProductionIntakes.Single(i=>i.Source==waitingNode);
var productDestination=productIntake.Target??waitingNode;
int rawDestination=heldShipment.Target;
Check(productDestination!=rawDestination && simultaneous.Campaign.FinishDomestic(simultaneous),
    "Domestic confirmation delivers the queued product and the reserved PP to different sites");
Check(simultaneousStock.ProductionIntakes.All(i=>i.Product.Id!=productIntake.Product.Id) &&
    simultaneousStock.Nodes[productDestination].Robots.Count(r=>r.Id==productIntake.Product.Id)==1 &&
    simultaneousStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    simultaneousStock.Nodes[rawDestination].MaplePackages.Count(p=>p==heldPackage)==1 &&
    !simultaneousStock.Nodes[waitingNode].MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Confirmation delivers both saved reservations exactly once");
var competing=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(suppliedRecipe));
var competingStock=competing.Robots;
var competingIntake=competingStock.ProductionIntakes.Single(i=>i.Source==waitingNode);
// Product intake needs one adjacent route, while direct material delivery can span relay links.
int sharedDestination=competing.Routes.Neighbors(competing.Nodes.All[waitingNode])
    .Select(route=>competing.Nodes.At(route.Target,route.Entry).Id)
    .First(id=>id!=waitingNode && competing.Nodes.All[id].Functional &&
        FunctionalDeliveryRoutes.Connected(competing,waitingNode,id));
Check(competingStock.CancelMapleShipment(waitingNode,heldPackage.Id) &&
    competingStock.ReserveMapleShipment(competing,waitingNode,sharedDestination,heldPackage.Id),
    "Retarget the PP shipment to a destination that also accepts production intake");
competingStock.SetIntakeTarget(competingIntake.Line,sharedDestination);
var competingStore=competingStock.Nodes[sharedDestination];
long addedBlocker=0;
while(competingStore.Count<competingStore.StorageLimit-1)
{
    var blocker=competingStock.Create(0,RobotParts.Complete);
    competingStore.Add(blocker);
    addedBlocker=blocker.Id;
}
Check(addedBlocker!=0 && competingStore.Count==competingStore.StorageLimit-1,
    "The shared destination has exactly one slot for two pending arrivals");
Check(competing.Campaign.FinishDomestic(competing) && !competingStore.HasSpace &&
    competingStore.Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    competingStock.ProductionIntakes.All(i=>i.Product.Id!=competingIntake.Product.Id) &&
    competingStock.MapleShipments.Count(o=>o.PackageId==heldPackage.Id)==1 &&
    competingStock.Nodes[waitingNode].MaplePackages.Contains(heldPackage) &&
    !competingStore.MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "The mixed product takes the last slot and the original PP stays reserved at its source");
competing=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(competing));
competingStock=competing.Robots;
competingStore=competingStock.Nodes[sharedDestination];
resolveMaple.Invoke(competingStock,new object[]{competing});
Check(!competingStore.HasSpace &&
    competingStore.Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    competingStock.ProductionIntakes.All(i=>i.Product.Id!=competingIntake.Product.Id) &&
    competingStock.MapleShipments.Count(o=>o.PackageId==heldPackage.Id && o.Target==sharedDestination)==1 &&
    competingStock.Nodes[waitingNode].MaplePackages.Contains(heldPackage) &&
    !competingStore.MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Restoring the full destination keeps the delivered product and the waiting PP separate");
var relocated=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(competing));
relocated.Campaign.BeginMilitary();
relocated.Campaign.Advance(relocated,-1,0,enemies:false);
var relocatedStock=relocated.Robots;
var transfers=relocated.Campaign.FunctionTransfers;
int relocationTarget=relocated.Routes.Neighbors(relocated.Nodes.All[sharedDestination])
    .Select(route=>relocated.Nodes.At(route.Target,route.Entry).Id)
    .First(id=>id!=waitingNode && transfers.CanReserve(relocated,sharedDestination,id));
Check(relocatedStock.MapleShipments.Count(o=>o.PackageId==heldPackage.Id && o.Target==sharedDestination)==1,
    "The restored PP shipment is still pending before the destination moves");
transfers.Reserve(relocated,sharedDestination,relocationTarget);
Check(relocated.Campaign.FinishDomestic(relocated) && !relocated.Nodes.All[sharedDestination].Functional &&
    relocatedStock.Nodes[relocationTarget].Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    relocatedStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    relocatedStock.Nodes[waitingNode].MaplePackages.Count(p=>p==heldPackage)==1 &&
    !relocatedStock.Nodes[relocationTarget].MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Moving the full destination cancels the saved shipment and leaves the PP at its source");
Check(relocated.Nodes.All[sharedDestination].FunctionHost==relocationTarget &&
    relocated.Nodes.All[relocationTarget].FunctionUnits>1,
    "Full transfer leaves a civilian settlement and adds a facility group to its host");
var relay = AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(competing));
relay.Campaign.BeginMilitary();
relay.Campaign.Advance(relay,-1,0,enemies:false);
var relayStock = relay.Robots;
var relayStore = relayStock.Nodes[sharedDestination];
var relayTransfers = relay.Campaign.FunctionTransfers;
int factoryTarget = relay.Routes.Neighbors(relay.Nodes.All[sharedDestination])
    .Select(route=>relay.Nodes.At(route.Target,route.Entry).Id)
    .First(id=>id!=waitingNode && relayTransfers.CanReserve(relay,sharedDestination,id,factoryOnly:true));
int incomingFactoriesBefore=relay.Nodes.All[factoryTarget].IncomingFactories;
int sourceFactoriesBefore=relay.Nodes.All[sharedDestination].IncomingFactories;
Check(relayStock.MapleShipments.Count(o=>o.PackageId==heldPackage.Id && o.Target==sharedDestination)==1,
    "The restored PP shipment is still pending before the destination factory moves");
relayTransfers.Reserve(relay,sharedDestination,factoryTarget,factoryOnly:true);
Check(relay.Campaign.FinishDomestic(relay) && relay.Nodes.All[sharedDestination].Purpose==NodePurpose.Relay &&
    relayStore.Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    !relayStock.Nodes[factoryTarget].MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Factory-only transfer leaves the delivered product and the PP destination at the relay");
Check(relay.Nodes.All[sharedDestination].Functional &&
    relay.Nodes.All[sharedDestination].IncomingFactories==0 &&
    relay.Nodes.All[factoryTarget].IncomingFactories==incomingFactoriesBefore+sourceFactoriesBefore+1,
    "Factory-only transfer moves facility markers and leaves the original hangar operating");
Check(!relayStore.HasSpace && relayStock.MapleShipments.Count(o=>o.PackageId==heldPackage.Id && o.Target==sharedDestination)==1 &&
    relayStock.Nodes[waitingNode].MaplePackages.Contains(heldPackage) && !relayStore.MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "A still-full relay preserves the PP reservation and cargo");
var waitingAtRelay = relayStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id);
relay = AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(relay));
Check(relay.Nodes.All[factoryTarget].IncomingFactories==incomingFactoriesBefore+sourceFactoriesBefore+1,
    "Restoring a saved factory transfer retains destination facility markers");
relayStock = relay.Robots;
relayStore = relayStock.Nodes[sharedDestination];
resolveMaple.Invoke(relayStock,new object[]{relay});
Check(relay.Nodes.All[sharedDestination].Purpose==NodePurpose.Relay && !relayStore.HasSpace &&
    relayStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id)==waitingAtRelay &&
    relayStock.Nodes[waitingNode].MaplePackages.Contains(heldPackage) && !relayStore.MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Restoring an already full relay preserves its purpose, its pending shipment and the source package");
var retiringRelay=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(relay));
retiringRelay.Campaign.BeginMilitary();
retiringRelay.Campaign.Advance(retiringRelay,-1,0,enemies:false);
var retiringStock=retiringRelay.Robots;
var retiringTransfers=retiringRelay.Campaign.FunctionTransfers;
int finalHost=retiringRelay.Routes.Neighbors(retiringRelay.Nodes.All[sharedDestination])
    .Select(route=>retiringRelay.Nodes.At(route.Target,route.Entry).Id)
    .First(id=>id!=waitingNode && retiringTransfers.CanReserve(retiringRelay,sharedDestination,id));
Check(retiringStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id)==waitingAtRelay,
    "The restored relay still has the same waiting order before its full transfer");
retiringTransfers.Reserve(retiringRelay,sharedDestination,finalHost);
Check(retiringRelay.Campaign.FinishDomestic(retiringRelay) &&
    !retiringRelay.Nodes.All[sharedDestination].Functional &&
    retiringStock.Nodes[finalHost].Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    retiringStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    retiringStock.Nodes[waitingNode].MaplePackages.Count(p=>p==heldPackage)==1 &&
    !retiringStock.Nodes[finalHost].MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "Full transfer of the restored relay cancels the incoming PP and moves only the resident product");
retiringRelay.Campaign.BeginMilitary();
retiringRelay.Campaign.Advance(retiringRelay,-1,0,enemies:false);
Check(!retiringStock.ReserveMapleShipment(retiringRelay,waitingNode,sharedDestination,heldPackage.Id),
    "An inactive former relay cannot accept a new material shipment");
int reroutedTarget=retiringRelay.Nodes.All.Where(node=>node.Id!=waitingNode && node.Id!=sharedDestination &&
    retiringStock.Nodes[node.Id].HasSpace && retiringStock.Nodes[node.Id].MaplePackages.Count==0)
    .Select(node=>node.Id)
    .First(id=>retiringStock.ReserveMapleShipment(retiringRelay,waitingNode,id,heldPackage.Id));
var rebookedShipment=retiringStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id);
Check(rebookedShipment.Target==reroutedTarget,"The PP order names the replacement destination");
retiringRelay=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(retiringRelay));
retiringStock=retiringRelay.Robots;
Check(retiringStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id)==rebookedShipment &&
    retiringStock.Nodes[waitingNode].MaplePackages.Count(p=>p==heldPackage)==1 &&
    !retiringRelay.Nodes.All[sharedDestination].Functional,
    "Restoring a rebooked shipment preserves only the new destination and its source cargo");
var previewReroute=typeof(RobotWorld).GetMethod("PreviewMapleArrivals",internalInstance)!;
(int Blocked,MaplePackage[] Packages) PreviewRebookedDestination(int node)=>
    ((int,MaplePackage[]))previewReroute.Invoke(retiringStock,new object[]{retiringRelay,node,retiringStock.Nodes[node]})!;
var reroutedForecast=PreviewRebookedDestination(reroutedTarget);
var retiredForecast=PreviewRebookedDestination(sharedDestination);
Check(reroutedForecast.Blocked==0 && reroutedForecast.Packages.Single()==heldPackage &&
    retiredForecast.Blocked==0 && retiredForecast.Packages.Length==0 &&
    retiringStock.Nodes[reroutedTarget].MaplePackages.Count==0 &&
    retiringStock.Nodes[waitingNode].MaplePackages.Count(p=>p==heldPackage)==1 &&
    retiringStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id)==rebookedShipment,
    "The restored forecast shows PP only at the new destination without consuming the reservation");
var fullReroute=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(retiringRelay));
var fullRerouteStock=fullReroute.Robots;
var fullRerouteStore=fullRerouteStock.Nodes[reroutedTarget];
var rerouteBlockers=new List<long>();
while(fullRerouteStore.HasSpace)
{
    var blocker=fullRerouteStock.Create(0,RobotParts.Complete);
    fullRerouteStore.Add(blocker);
    rerouteBlockers.Add(blocker.Id);
}
Check(rerouteBlockers.Count>0,"The rebooked destination fills after booking");
fullReroute=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(fullReroute));
fullRerouteStock=fullReroute.Robots;
fullRerouteStore=fullRerouteStock.Nodes[reroutedTarget];
var fullForecast=((int Blocked,MaplePackage[] Packages))previewReroute.Invoke(fullRerouteStock,
    new object[]{fullReroute,reroutedTarget,fullRerouteStore})!;
Check(fullForecast.Blocked==1 && fullForecast.Packages.Length==0 &&
    fullReroute.Campaign.FinishDomestic(fullReroute) &&
    fullRerouteStock.MapleShipments.Single(o=>o.PackageId==heldPackage.Id)==rebookedShipment &&
    fullRerouteStock.Nodes[waitingNode].MaplePackages.Count(p=>p==heldPackage)==1 &&
    fullRerouteStore.MaplePackages.Count==0,
    "The restored full destination forecasts and preserves the rebooked PP waiting at its source");
var reroutedLane=new RobotWorkshop();
reroutedLane.Configure(RobotParts.Head,1);
typeof(RobotWorkshop).GetField("_selectedProductPlan",internalInstance)!
    .SetValue(reroutedLane,new ProductionPlan(RobotParts.Head,1){Material=RobotMaterial.Polypropylene});
production.Invoke(reroutedLane,new object[]{fullRerouteStock,reroutedTarget,0});
Check(reroutedLane.WaitingForMaple(fullRerouteStock,reroutedTarget) && reroutedLane.ProductionAge==0,
    "The PP recipe cannot progress while the rebooked shipment is waiting");
fullRerouteStore.Remove(rerouteBlockers[^1]);
resolveMaple.Invoke(fullRerouteStock,new object[]{fullReroute});
Check(fullRerouteStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    fullRerouteStore.MaplePackages.Single()==heldPackage &&
    fullRerouteStock.Nodes.Sum(store=>store.MaplePackages.Count(p=>p.Id==heldPackage.Id))==1,
    "Freeing the rebooked destination delivers the original PP exactly once");
Check(!reroutedLane.WaitingForMaple(fullRerouteStock,reroutedTarget),"The rebooked PP clears the material wait");
reroutedLane.SetReservedSlots(fullRerouteStore.TotalCapacity);
for(int tick=0;tick<reroutedLane.ProductionPeriod;tick++)
    production.Invoke(reroutedLane,new object[]{fullRerouteStock,reroutedTarget,0});
Check(reroutedLane.Waiting && reroutedLane.WaitingProductionTurns>0 &&
    fullRerouteStore.MaplePackages.Single()==heldPackage &&
    !fullRerouteStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,reroutedLane)),
    "When the destination cannot accept the product, the delivered PP remains intact and production waits");
reroutedLane.SetReservedSlots(0);
foreach(long id in rerouteBlockers.SkipLast(1))fullRerouteStore.Remove(id);
for(int tick=0;tick<3 && fullRerouteStore.MaplePackages.Sum(p=>p.Units)==heldPackage.Units;tick++)
    production.Invoke(reroutedLane,new object[]{fullRerouteStock,reroutedTarget,0});
Check(fullRerouteStore.MaplePackages.Single()==(heldPackage with {Units=heldPackage.Units-8}) &&
    fullRerouteStock.ProductionIntakes.Count(i=>ReferenceEquals(i.Line,reroutedLane) &&
        i.Product.Parts==RobotParts.Head && i.Product.HeadMaterial==RobotMaterial.Polypropylene)==1,
    "The restarted route supplies exactly eight units for one PP head");
var reroutedProduct=fullRerouteStock.ProductionIntakes.Single(i=>ReferenceEquals(i.Line,reroutedLane)).Product;
fullReroute=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(fullReroute));
fullRerouteStock=fullReroute.Robots;
fullRerouteStore=fullRerouteStock.Nodes[reroutedTarget];
var savedReroutedIntake=fullRerouteStock.ProductionIntakes.Single(i=>i.Product.Id==reroutedProduct.Id);
Check((savedReroutedIntake.Target??savedReroutedIntake.Source)==reroutedTarget &&
    savedReroutedIntake.Product.Parts==RobotParts.Head && savedReroutedIntake.Product.HeadMaterial==RobotMaterial.Polypropylene &&
    !fullRerouteStore.Robots.Any(r=>r.Id==reroutedProduct.Id) &&
    fullRerouteStore.MaplePackages.Single()==(heldPackage with {Units=heldPackage.Units-8}),
    "Restoring the reserved PP head keeps the intake and the remaining raw stock separate");
var reroutedIntakeForecast=fullRerouteStock.PartPlanning.Preview(fullReroute).Nodes[reroutedTarget];
Check(reroutedIntakeForecast.Robots.Single(r=>r.Id==reroutedProduct.Id)==savedReroutedIntake.Product &&
    reroutedIntakeForecast.Robots.Count==fullRerouteStore.Robots.Count+1 &&
    reroutedIntakeForecast.MaplePackages.SequenceEqual(fullRerouteStore.MaplePackages) &&
    fullRerouteStock.ProductionIntakes.Contains(savedReroutedIntake) &&
    !fullRerouteStore.Robots.Any(r=>r.Id==reroutedProduct.Id),
    "The restored forecast places the PP head in the destination store without changing the live intake");
var resolveReroutedIntakes=typeof(RobotWorld).GetMethod("ResolveProductionIntakes",internalInstance)!;
resolveReroutedIntakes.Invoke(fullRerouteStock,new object[]{fullReroute});
resolveReroutedIntakes.Invoke(fullRerouteStock,new object[]{fullReroute});
Check(fullRerouteStock.ProductionIntakes.All(i=>i.Product.Id!=reroutedProduct.Id) &&
    fullRerouteStore.Robots.Count(r=>r.Id==reroutedProduct.Id && r.Parts==RobotParts.Head &&
        r.HeadMaterial==RobotMaterial.Polypropylene)==1 &&
    fullRerouteStock.Nodes.Sum(store=>store.Robots.Count(r=>r.Id==reroutedProduct.Id))==1 &&
    fullRerouteStore.Count==reroutedIntakeForecast.Count &&
    fullRerouteStore.Robots.SequenceEqual(reroutedIntakeForecast.Robots) &&
    fullRerouteStore.MaplePackages.Single()==(heldPackage with {Units=heldPackage.Units-8}),
    "The resumed PP head enters the receiving hangar once without a second material debit");
Check(retiringRelay.Campaign.FinishDomestic(retiringRelay) &&
    retiringStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    retiringStock.Nodes[reroutedTarget].MaplePackages.SequenceEqual(reroutedForecast.Packages) &&
    retiringStock.Nodes.Sum(store=>store.MaplePackages.Count(p=>p.Id==heldPackage.Id))==1,
    "The cancelled PP cargo can be booked again and lands intact at the new destination exactly once");
relayStore.Remove(addedBlocker);
resolveMaple.Invoke(relayStock,new object[]{relay});
resolveMaple.Invoke(relayStock,new object[]{relay});
Check(relayStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    relayStore.MaplePackages.Count(p=>p==heldPackage)==1 &&
    relayStore.Robots.Count(r=>r.Id==competingIntake.Product.Id)==1 &&
    !relayStock.Nodes[factoryTarget].MaplePackages.Any(p=>p.Id==heldPackage.Id) &&
    !relayStock.Nodes[waitingNode].MaplePackages.Any(p=>p.Id==heldPackage.Id),
    "The restored PP shipment reaches the original hangar exactly once after a factory-only transfer");
competingStore.Remove(addedBlocker);
resolveMaple.Invoke(competingStock,new object[]{competing});
resolveMaple.Invoke(competingStock,new object[]{competing});
Check(competingStock.MapleShipments.All(o=>o.PackageId!=heldPackage.Id) &&
    competingStore.MaplePackages.Count(p=>p==heldPackage)==1 &&
    competingStore.Robots.Count(r=>r.Id==competingIntake.Product.Id)==1,
    "Freeing one slot admits the waiting PP once without duplicating the product");
Check(recipeStock.CancelMapleShipment(waitingNode,999003),"Clear the original fixture before testing full intake");
recipeFactory.SetReservedSlots(waitingStore.TotalCapacity);
for(int i=0;i<recipeLine.ProductionPeriod;i++)production.Invoke(recipeFactory,new object[]{recipeStock,waitingNode,0});
Check(recipeLine.Waiting && recipeLine.WaitingProductionTurns>0 &&
    !recipeStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,recipeLine)) &&
    waitingStore.MaplePackages.SequenceEqual(beforeMixedIntake),
    "A blocked mixed intake restores the exact packages for both ingredients");
var restoredRecipe=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(recipeWorld));
foreach(var pendingRecipe in new[]{recipeWorld,restoredRecipe})
{
    var pendingStock=pendingRecipe.Robots;
    var pendingFactory=pendingStock.Workshops[waitingNode];
    var pendingLine=pendingFactory.ProductionLines[0];
    var pendingStore=pendingStock.Nodes[waitingNode];
    int waitingTurns=pendingLine.WaitingProductionTurns;
    Check(pendingLine.Waiting && waitingTurns>0 && pendingStore.MaplePackages.SequenceEqual(beforeMixedIntake),
        "The waiting lane and both exact ingredient packages survive save and restore");
    production.Invoke(pendingFactory,new object[]{pendingStock,waitingNode,0});
    Check(pendingLine.WaitingProductionTurns==waitingTurns+1 &&
        !pendingStock.ProductionIntakes.Any(i=>ReferenceEquals(i.Line,pendingLine)) &&
        pendingStore.MaplePackages.SequenceEqual(beforeMixedIntake),
        "Retrying while full still queues no product and consumes neither ingredient");
    pendingFactory.SetReservedSlots(0);
    production.Invoke(pendingFactory,new object[]{pendingStock,waitingNode,0});
    var resumedMixed=pendingStock.ProductionIntakes.Single(i=>ReferenceEquals(i.Line,pendingLine));
    Check(pendingStore.MapleUnits==1 && pendingStore.MaplePackages.Single(p=>p.Material==RawMaterial.PP).Units==8 &&
        resumedMixed.Product.HeadMaterial==RobotMaterial.Polypropylene &&
        resumedMixed.Product.BodyMaterial==RobotMaterial.Cardboard && resumedMixed.Product.LegsMaterial==RobotMaterial.Maple,
        "The same lane retries once and consumes 20 Maple and 8 PP for the mixed product");
    typeof(RobotWorld).GetMethod("ResolveProductionIntakes",internalInstance)!.Invoke(pendingStock,new object[]{pendingRecipe});
    Check(!pendingStock.ProductionIntakes.Contains(resumedMixed) &&
        pendingStore.Robots.Count(r=>r.Id==resumedMixed.Product.Id && r.HeadMaterial==RobotMaterial.Polypropylene)==1,
        "The retried mixed product arrives exactly once in the destination hangar");
}
Console.WriteLine("PASS a single Maple/PP recipe rolls back both ingredients and resumes exactly once before and after save.");
Check(supply.Campaign.FunctionTransfers.CanReserve(supply,target,source),"Fixture cannot transfer functions");
supply.Campaign.FunctionTransfers.Reserve(supply,target,source);
int remaining=cargo.Nodes[target].MapleUnits;
Check(cargo.PartPlanning.Preview(supply).Nodes[source].MapleUnits==remaining,"Transfer forecast lost maple stock");
mapleLine.SelectMapleHarvest(2);
int receivingDeposit=cargo.MapleDeposit(source);
supply.Campaign.FinishDomestic(supply);
Check(cargo.Nodes[source].MapleUnits==remaining && cargo.Nodes[target].MapleUnits==0 && cargo.MapleDeposit(target)==0,"Transfer lost stock or left a source open");
Check(cargo.MapleDeposit(source)==receivingDeposit && cargo.Workshops[source].ProductionLines.Contains(mapleLine) && mapleLine.MapleRemaining==2,"Transfer must move the lane and its order without moving the deposit");
var previousSave=AutoSaveStore.Capture(world);
foreach(var savedObject in previousSave.Objects){savedObject.Fields?.Remove("_mapleDeposits");savedObject.Fields?.Remove("_mapleShipments");savedObject.Fields?.Remove("_maplePackages");}
Check(!AutoSaveStore.Restore<WorldSetup>(previousSave).Robots.MapleSupplyEnabled,"An older materials save was opted into maple");
Console.WriteLine("PASS maple harvesting, cargo shipping, save round-trip, exact recipe, blocked intake and function transfer.");

var timberStore=new RobotStore();
typeof(RobotStore).GetMethod("SetCapacity",internalInstance)!.Invoke(timberStore,new object[]{1});
bool ReceiveTimber(long id,int units)=>(bool)receiveMaple.Invoke(timberStore,new object[]{new MaplePackage(id,units),0,(Func<long,bool>)(_=>false)})!;
timberStore.Add(new Robot(200,0,RobotParts.Body));
Check(!ReceiveTimber(201,32) && !ReceiveTimber(202,48),"A large timber needs contiguous sections");
timberStore.Remove(200);
Check(ReceiveTimber(202,48) && !ReceiveTimber(203,1),"A full timber occupies all three sections");
typeof(RobotStore).GetMethod("SetMaplePackages",internalInstance)!.Invoke(timberStore,new object[]{new[]{new MaplePackage(202,31)}});
Check(ReceiveTimber(203,12) && !ReceiveTimber(204,8) && timberStore.MaplePackages.First().Units==31,"Small loads cannot refill a large timber");
Check(ReceiveTimber(204,4) && timberStore.MaplePackages.Last().Units==16,"Small loads combine up to sixteen");
{
    var rearrange=typeof(RobotWorld).GetMethod("RearrangeMaple",internalInstance)!;
    supply.Campaign.BeginMilitary();
    supply.Campaign.Advance(supply,-1,0,enemies:false);
    var setPackages=typeof(RobotStore).GetMethod("SetMaplePackages",internalInstance)!;
    var local=cargo.Nodes[source];
    setPackages.Invoke(local,new object[]{new[]{new MaplePackage(901,15),new MaplePackage(902,9),new MaplePackage(903,32)}});
    bool Arrange(long id,long? into,bool split)=>(bool)rearrange.Invoke(cargo,new object[]{supply,source,id,into,split})!;
    Check(Arrange(902,901,false) && local.MaplePackages.Single(p=>p.Id==901).Units==16 &&
        local.MaplePackages.Single(p=>p.Id==902).Units==8,"Manual merge keeps overflow in the source");
    Check(!Arrange(903,null,true) && !Arrange(902,903,false),"Large timbers stay intact");
    Check(Arrange(902,null,true) && local.MaplePackages.Last().Units==4 && local.MapleUnits==56,
        "Splitting a small load preserves the total stock");
}

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

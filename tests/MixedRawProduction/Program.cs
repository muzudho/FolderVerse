using FolderVerse;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
var seed=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};seed.SetParticipantCounts(0,2);seed.SetWorld(104729);seed.SetCast(1);seed.SetTerritoryRights(2);foreach(int ruler in seed.LandOrder())seed.ClaimLand(ruler,new ComputerLandPlanner(seed).Select(ruler,null).Cells);seed.CompleteLand(3,25,25);
var baseline=AutoSaveStore.Capture(seed);
if(args.Contains("--cpu-logistics"))
{
 CpuRawDelivery();
 return;
}
try{foreach(var materials in new[]{new[]{RobotMaterial.Maple,RobotMaterial.Polystyrene,RobotMaterial.SoftVinyl},new[]{RobotMaterial.Polypropylene,RobotMaterial.Silicone,RobotMaterial.Polypropylene}}){
 var w=AutoSaveStore.Restore<WorldSetup>(baseline);var r=w.Robots;int node=w.Nodes.All.First(n=>n.Functional).Id;w.Nodes.All[node].Owner=0;r.ConfigureFactories(w);
 var store=r.Nodes[node];typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(store,new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{12});typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{Array.Empty<MaplePackage>()});store.Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});store.Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});var worker=r.Create(0,RobotParts.Complete);store.Add(worker);typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(r.Workshops[node],null);r.Workshops[node].LimitLines(3);if(!r.PartPlanning.ReserveRole(w,node,worker.Id,HangarReservation.Worker,0))throw new Exception("Actual worker appointment rejected");typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(r.PartPlanning,new object[]{w});if(r.Workshops[node].BaseLineCount!=0||r.Workshops[node].WorkerAt(0)!=worker.Id)throw new Exception("Expected worker-created lane");w.Nodes.All[node].Purpose=NodePurpose.General;
 var plan=new ProductionPlan(RobotParts.Complete,3){HeadMaterial=materials[0],BodyMaterial=materials[1],LegsMaterial=materials[2]};var preview=plan.Preview(0);
 int Cost(RawMaterial m)=>(int)typeof(RobotWorld).GetMethod("RawCost",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{preview,m})!;
 var needs=RobotWorld.RawMaterials.Where(m=>Cost(m)>0).ToArray();var packages=needs.Select((m,i)=>new MaplePackage(990000+i,48){Material=m}).ToArray();packages[^1]=packages[^1] with{Units=Cost(needs[^1])-1};
 typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{packages});
 var line=r.Workshops[node].ProductionLines.Single();line.Configure(RobotParts.Complete,3);line.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,plan);
 var before=store.MaplePackages.ToArray();for(int i=0;i<9;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=node);
 if(!store.MaplePackages.SequenceEqual(before)||line.ProductionAge!=0||r.ProductionIntakes.Count!=0)throw new Exception("Missing ingredient consumed others or advanced");
 packages[^1]=packages[^1] with{Units=48};typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{packages});
 while(store.HasSpace)store.Add(r.Create(1,RobotParts.Complete));before=store.MaplePackages.ToArray();
 for(int i=0;i<6;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=node);
 if(!store.MaplePackages.SequenceEqual(before)||r.ProductionIntakes.Count!=0||!line.IntakeFailure.Contains("格納枠"))throw new Exception($"Capacity fixture: count={store.Count}/{store.StorageLimit}, intakes={r.ProductionIntakes.Count}, age={line.ProductionAge}, failure={line.IntakeFailure}, before={string.Join(",",before.Select(p=>p.Units))}, after={string.Join(",",store.MaplePackages.Select(p=>p.Units))}");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;r.ConfigureFactories(w);store=r.Nodes[node];line=r.Workshops[node].ProductionLines.Single();store.Remove(store.Robots.First(x=>x.Owner==1).Id);
 r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=node);
 if(r.ProductionIntakes.Count!=1||needs.Any(m=>store.MaplePackages.Where(p=>p.Material==m).Sum(p=>p.Units)!=48-Cost(m)))throw new Exception("Atomic mixed consumption failed");
 for(int i=0;i<9;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=node);
 if(r.ProductionIntakes.Count!=1||needs.Any(m=>store.MaplePackages.Where(p=>p.Material==m).Sum(p=>p.Units)!=48-Cost(m)))throw new Exception("Repeated consumption");
 Console.WriteLine($"PASS actual worker lane {string.Join('/',materials)}: missing one retains all; full rollback/save; space freed consumes each once; no repeat.");
}}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}

void CpuRawDelivery()
{
 static void Check(bool result,string message){if(!result)throw new Exception(message);}
 var planner=typeof(WorldSetup).Assembly.GetType("FolderVerse.ComputerRawLogistics")!
    .GetMethod("Plan",BindingFlags.NonPublic|BindingFlags.Static)!;
 void Plan(WorldSetup w)=>planner.Invoke(null,new object[]{w,true});
 void Deliver(WorldSetup w)=>typeof(RobotWorld).GetMethod("ResolveMapleShipments",F)!.Invoke(w.Robots,new object[]{w});
 void SetStock(WorldSetup w,int node,params int[] quantities)=>typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(
    w.Robots.Nodes[node],new object[]{quantities.Select((units,index)=>new MaplePackage(980000+node*10+index,units){Material=RawMaterial.PP}).ToArray()});
 int Stock(WorldSetup w,int node)=>w.Robots.Nodes[node].MaplePackages.Sum(p=>p.Units);
 void Recipe(WorldSetup w,int node,RobotParts parts)
 {
    var line=new RobotWorkshop();
    line.Configure(parts,3);
    line.SetDoNotRepeat(true);
    typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,new ProductionPlan(parts,3){
        HeadMaterial=RobotMaterial.Polypropylene,BodyMaterial=RobotMaterial.Polypropylene,LegsMaterial=RobotMaterial.Polypropylene});
    typeof(RobotWorkshop).GetProperty("ProductionLines")!.SetValue(w.Robots.Workshops[node],new[]{line});
 }
 WorldSetup Fixture()
 {
    var w=AutoSaveStore.Restore<WorldSetup>(baseline);
    foreach(var node in w.Nodes.All)
    {
        node.Owner=0;
        typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(w.Robots.Workshops[node.Id],null);
        var store=w.Robots.Nodes[node.Id];
        SetStock(w,node.Id);
        typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(store,new object[]{12});
        typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{new[]{w.Robots.Create(0,RobotParts.Complete) with{WarehouseKeeper=true}}});
    }
    return w;
 }
 var w=Fixture();
 var source=w.Nodes.All.Where(n=>n.Functional).First(n=>FunctionalDeliveryRoutes.Targets(w,n.Id).Length>=2).Id;
 var receivers=FunctionalDeliveryRoutes.Targets(w,source).Take(2).ToArray();
 Recipe(w,source,RobotParts.Head|RobotParts.Body);
 foreach(int node in receivers)Recipe(w,node,RobotParts.Complete);
 SetStock(w,source,48);
 Plan(w);
 Plan(w);
 Check(w.Robots.MapleShipments.Count==2 && receivers.All(n=>w.Robots.MapleShipments.Count(o=>o.Target==n)==1),
    "Split two loads from a 48-unit source and prioritize both factories before topping either up");
 Check(w.Robots.MapleShipments.All(o=>o.Computer && w.Robots.Nodes[source].MaplePackages.Single(p=>p.Id==o.PackageId).Units==16),
    "Each dispatch fits one 16-unit section");
 Deliver(w);
 Check(Stock(w,source)==16 && receivers.All(n=>Stock(w,n)==16),"Leave the supplier's own 16 units and deliver the rest");
 w.Robots.Check();

 w=Fixture();
 var chain=w.Nodes.All.Where(n=>n.Functional).SelectMany(n=>FunctionalDeliveryRoutes.Targets(w,n.Id)
    .SelectMany(mid=>FunctionalDeliveryRoutes.Targets(w,mid).Where(end=>end!=n.Id &&
        !FunctionalDeliveryRoutes.Connected(w,n.Id,end)).Select(end=>(Source:n.Id,Mid:mid,Target:end)))).First();
 SetStock(w,chain.Source,3,5);
 Recipe(w,chain.Target,RobotParts.Head);
 Plan(w);
 var order=w.Robots.MapleShipments.Single();
 Check(order.Target==chain.Target && Stock(w,chain.Source)==8 &&
    w.Robots.Nodes[chain.Source].MaplePackages.Single().Units==8,"Combine two loose packages to cover exactly eight units");
 Deliver(w);
 int relay=w.Robots.MapleShipments.Single().Source;
 Check(relay!=chain.Source && Stock(w,relay)==8 && Stock(w,chain.Target)==0,"Long haul advances one hop per delivery");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
 Check(w.Robots.MapleShipments.Single().Computer,"Computer shipment survives save and resume");
 Plan(w);
 Check(w.Robots.MapleShipments.Count==1,"In-transit stock prevents duplicate requests");
 var targetStore=w.Robots.Nodes[chain.Target];
 while(targetStore.HasSpace)targetStore.Add(w.Robots.Create(0,RobotParts.Complete));
 Deliver(w);
 Check(w.Robots.MapleShipments.Single().Source==relay && Stock(w,relay)==8,"A full destination leaves the load waiting at the relay");
 targetStore.Remove(targetStore.Robots.Last().Id);
 Deliver(w);
 Check(w.Robots.MapleShipments.Count==0 && Stock(w,chain.Target)==8 && Stock(w,relay)==0,"Delivery resumes when the destination has space");
 w.Robots.Check();
 Console.WriteLine("PASS: computer raw shipping splits and combines stock, balances factories, protects local demand, and resumes a saved relay after a capacity wait.");
}

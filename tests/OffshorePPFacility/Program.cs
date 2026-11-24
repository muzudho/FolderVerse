using FolderVerse;using System.Reflection;using System.Text.Json;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
void Check(bool yes,string text){if(!yes)throw new Exception(text);Console.WriteLine("PASS "+text);}
var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(0,2);w.SetWorld(42);w.SetCast(2);w.SetTerritoryRights(3);foreach(int owner in w.LandOrder())w.ClaimLand(owner,new ComputerLandPlanner(w).Select(owner,null).Cells);w.CompleteLand(4,25,25);
var resource=typeof(CubeNet).GetMethod("ResourceSites",F)!;var kind=typeof(CubeNet).GetNestedType("ResourceSiteKind",BindingFlags.NonPublic)!;var sites=new List<(int Cell,int Node)>();
if(Environment.GetEnvironmentVariable("PP_FIXTURE") is string fixture)AutoSaveStore.Write(fixture,w);
foreach(var cell in w.Cells)foreach(var site in (Array)resource.Invoke(null,new object[]{w,cell,w.Terrain})!){
 var type=site!.GetType();if(type.GetProperty("Kind")!.GetValue(site)!.ToString()!="OffshoreGas")continue;
 var at=type.GetProperty("Position")!.GetValue(site)!;var node=(Node)typeof(RobotWorld).GetMethod("SiteNode",F)!.Invoke(null,new[]{w,(object)cell,at})!;
 Check(node.Terrain==TravelTerrain.Sea&&!w.IsPolarFace(cell.Face)&&!w.Routes.NearWhirlpool(node),"marine site stays in existing nonpolar safe sea domain");sites.Add((cell.Id,node.Id));
 Check((RawMaterial)typeof(RobotWorld).GetMethod("SiteMaterial",F)!.Invoke(null,new[]{type.GetProperty("Kind")!.GetValue(site)})! ==RawMaterial.PP,"marine recycler yields existing PP resource");
}
Check(sites.Count>0,"new world contains offshore recycling opportunities");
Console.WriteLine("DEPOSITS "+JsonSerializer.Serialize(w.Nodes.All.Select(n=>new{n.Id,pp=w.Robots.RawDeposit(n.Id,RawMaterial.PP)})));
int source=sites.First(s=>FunctionalDeliveryRoutes.Targets(w,s.Node).Any()).Node;var r=w.Robots;w.Nodes.All[source].Owner=0;w.Nodes.All[source].Purpose=NodePurpose.General;
// Remove packages before keepers so the material warehouse invariant stays valid.
typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(r.Nodes[source],new object[]{Array.Empty<MaplePackage>()});typeof(RobotStore).GetMethod("Replace",F)!.Invoke(r.Nodes[source],new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(r.Nodes[source],new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(r.Nodes[source],new object[]{12});
for(int i=0;i<2;i++)r.Nodes[source].Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});
typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(r.Workshops[source],null);r.Workshops[source].LimitLines(3);var worker=r.Create(0,RobotParts.Complete);r.Nodes[source].Add(worker);
Check(r.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Harvest,0),"reserve real marine harvest worker");typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(r.PartPlanning,new object[]{w});
var line=r.Workshops[source].ProductionLines.Single();int before=r.RawDeposit(source,RawMaterial.PP);line.SelectMapleHarvest(1,RawMaterial.PP);line.SetDoNotRepeat(true);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(r.RawDeposit(source,RawMaterial.PP)==before-1&&r.Nodes[source].MaplePackages.Sum(p=>p.Material==RawMaterial.PP?p.Units:0)==16,"harvest lane creates 16 PP units and consumes exactly one load");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;r.ConfigureFactories(w);Check(r.RawDeposit(source,RawMaterial.PP)==before-1,"save does not regenerate marine reserve");
{
 var blocked=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));var stock=blocked.Robots;var store=stock.Nodes[source];
 typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{store.MaplePackages.Concat(new[]{new MaplePackage(900001,16){Material=RawMaterial.Styrene},new MaplePackage(900002,16){Material=RawMaterial.Vinyl}}).ToArray()});
 while(store.HasSpace)store.Add(stock.Create(0,RobotParts.Complete));
 var harvest=stock.Workshops[source].ProductionLines.Single();harvest.SelectMapleHarvest(1,RawMaterial.PP);var packages=store.MaplePackages.ToArray();int reserve=stock.RawDeposit(source,RawMaterial.PP);
 for(int i=0;i<6;i++)stock.AdvanceIndustry(n=>blocked.Nodes.All[n].Owner,n=>n!=source);
 Check(stock.RawDeposit(source,RawMaterial.PP)==reserve&&store.MaplePackages.SequenceEqual(packages)&&harvest.MapleRemaining==1,"full marine hangar waits without consuming reserve or duplicating PP");
 blocked=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(blocked));stock=blocked.Robots;store=stock.Nodes[source];
 Check(stock.RawDeposit(source,RawMaterial.PP)==reserve&&stock.Workshops[source].ProductionLines.Single().MapleRemaining==1,"full harvest wait survives save");
 store.Remove(store.Robots.First(robot=>!robot.Stationed).Id);
 for(int i=0;i<IndustryTime.UnitsPerTurn;i++)stock.AdvanceIndustry(n=>blocked.Nodes.All[n].Owner,n=>n!=source);
 Check(stock.RawDeposit(source,RawMaterial.PP)==reserve-1&&store.MaplePackages.Where(p=>p.Material==RawMaterial.PP).Sum(p=>p.Units)==32,"freeing space resumes marine harvest once and stores second PP load");
}
{
 var export=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));export.Relations.Leader=0;int target=export.Routes.Neighbors(export.Nodes.All[source]).Select(route=>export.Nodes.At(route.Target,route.Entry)).First(node=>node.Functional).Id;export.Nodes.All[target].Owner=0;
 var stock=export.Robots;var package=stock.Nodes[source].MaplePackages.Single();
 Check(stock.ReserveMapleShipment(export,source,target,package.Id),"reserve marine PP shipment before manufacturing");
 Check(stock.ToggleReservation(source,worker.Id,HangarReservation.Worker,0),"switch export fixture to manufacturing");var factory=stock.Workshops[source].ProductionLines.Single();factory.Configure(RobotParts.Head,3);factory.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(factory,new ProductionPlan(RobotParts.Head,3){HeadMaterial=RobotMaterial.Polypropylene});
 export=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(export));stock=export.Robots;stock.ConfigureFactories(export);
 for(int i=0;i<9;i++)stock.AdvanceIndustry(n=>export.Nodes.All[n].Owner,n=>n!=source);
 Check(stock.Nodes[source].MaplePackages.Single()==package&&stock.MapleShipments.Single().PackageId==package.Id&&stock.ProductionIntakes.Count==0&&stock.Workshops[source].ProductionLines.Single().WaitingForMaple(stock,source),"saved outgoing PP is protected from factory consumption and reports material wait");
 Check(stock.CancelMapleShipment(source,package.Id),"cancel outgoing marine PP reservation");
 export=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(export));stock=export.Robots;stock.ConfigureFactories(export);
 Check(!stock.MapleShipments.Any()&&stock.Nodes[source].MaplePackages.Single()==package&&!stock.Workshops[source].ProductionLines.Single().WaitingForMaple(stock,source),"shipment cancellation survives save and clears material shortage without losing stock");
 for(int i=0;i<18;i++)stock.AdvanceIndustry(n=>export.Nodes.All[n].Owner,n=>n!=source);
 Check(stock.Nodes[source].MaplePackages.Single().Units==8&&stock.ProductionIntakes.Count==1&&stock.ProductionIntakes.Single().Product.HeadMaterial==RobotMaterial.Polypropylene,"cancelled export PP resumes local production exactly once with eight units remaining");
}
{
 var delivery=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));delivery.Relations.Leader=0;
 int destination=delivery.Routes.Neighbors(delivery.Nodes.All[source]).Select(route=>delivery.Nodes.At(route.Target,route.Entry)).First(n=>n.Functional).Id;delivery.Nodes.All[destination].Owner=0;delivery.Nodes.All[destination].Purpose=NodePurpose.General;
 var stock=delivery.Robots;var store=stock.Nodes[destination];
 typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{Array.Empty<MaplePackage>()});typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(store,new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{12});
 for(int i=0;i<2;i++)store.Add(stock.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});
 typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(stock.Workshops[destination],null);stock.Workshops[destination].LimitLines(3);
 var maker=stock.Create(0,RobotParts.Complete);store.Add(maker);
 Check(stock.PartPlanning.ReserveRole(delivery,destination,maker.Id,HangarReservation.Worker,0),"reserve actual recipient manufacturer");typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(stock.PartPlanning,new object[]{delivery});
 var factory=stock.Workshops[destination].ProductionLines.Single();factory.Configure(RobotParts.Head,3);factory.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(factory,new ProductionPlan(RobotParts.Head,3){HeadMaterial=RobotMaterial.Polypropylene});
 var package=stock.Nodes[source].MaplePackages.Single();Check(stock.ReserveMapleShipment(delivery,source,destination,package.Id),"reserve harvested marine PP to actual recipient factory");
 delivery=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(delivery));stock=delivery.Robots;stock.ConfigureFactories(delivery);
 typeof(RobotWorld).GetMethod("ResolveMapleShipments",F)!.Invoke(stock,new object[]{delivery});
 delivery=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(delivery));stock=delivery.Robots;stock.ConfigureFactories(delivery);
 Check(!stock.MapleShipments.Any()&&!stock.Nodes[source].MaplePackages.Any()&&stock.Nodes[destination].MaplePackages.Single()==package,"saved shipment arrives once with exact PP identity and source cleared");
 for(int i=0;i<18;i++)stock.AdvanceIndustry(n=>delivery.Nodes.All[n].Owner,n=>n!=destination);
 Check(stock.Nodes[destination].MaplePackages.Single().Units==8&&stock.ProductionIntakes.Single().Source==destination&&stock.ProductionIntakes.Single().Product.HeadMaterial==RobotMaterial.Polypropylene,"delivered marine PP restarts recipient manufacture with exact eight-unit recipe");
}
Check(r.ToggleReservation(source,worker.Id,HangarReservation.Worker,0),"switch harvester into manufacturer");line=r.Workshops[source].ProductionLines.Single();var plan=new ProductionPlan(RobotParts.Head,3){HeadMaterial=RobotMaterial.Polypropylene};line.Configure(RobotParts.Head,3);line.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,plan);
for(int i=0;i<18;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(r.Nodes[source].MaplePackages.Where(p=>p.Material==RawMaterial.PP).Sum(p=>p.Units)==8&&r.ProductionIntakes.Count==1,"recycled PP makes a PP head with unchanged eight-unit recipe");
var product=r.ProductionIntakes.Single().Product;Check(product.HeadMaterial==RobotMaterial.Polypropylene,"manufactured material is polypropylene");
{
 var redirected=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));var stock=redirected.Robots;redirected.Relations.Leader=0;
 int destination=redirected.Routes.Neighbors(redirected.Nodes.All[source]).Select(route=>redirected.Nodes.At(route.Target,route.Entry)).First(n=>n.Functional).Id;redirected.Nodes.All[destination].Owner=0;
 var targetStore=stock.Nodes[destination];typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(targetStore,new object[]{Array.Empty<MaplePackage>()});typeof(RobotStore).GetMethod("Replace",F)!.Invoke(targetStore,new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(targetStore,new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(targetStore,new object[]{12});
 for(int i=0;i<2;i++)targetStore.Add(stock.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});
 var intake=stock.ProductionIntakes.Single();stock.SetIntakeTarget(intake.Line,destination);
 Check(stock.ToggleReservation(source,worker.Id,HangarReservation.Worker,0)&&!stock.Workshops[source].ProductionLines.Any(),"release source manufacturer after redirecting completed product");
 redirected=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(redirected));stock=redirected.Robots;stock.ConfigureFactories(redirected);
 Check(stock.ProductionIntakes.Single().Target==destination&&stock.ProductionIntakes.Single().Product==product&&!stock.Workshops[source].ProductionLines.Any(),"redirected completed intake survives worker release and save with exact material product");
 {
  var lost=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(redirected));lost.Robots.ConfigureFactories(lost);lost.Nodes.All[destination].Owner=-1;
  typeof(RobotWorld).GetMethod("ResolveProductionIntakes",F)!.Invoke(lost.Robots,new object[]{lost});
  Check(lost.Robots.ProductionIntakes.Single().Status=="入荷待機：経路・所有国・戦闘状況"&&lost.Robots.ProductionIntakes.Single().Product==product&&!lost.Robots.Nodes[destination].Robots.Any(robot=>robot.Id==product.Id),"lost destination ownership reports route authority wait rather than capacity shortage");
  lost=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(lost));lost.Robots.ConfigureFactories(lost);
  Check(lost.Robots.ProductionIntakes.Single().Status=="入荷待機：経路・所有国・戦闘状況"&&lost.Robots.Nodes[source].MaplePackages.Single().Units==8,"ownership wait survives save without consuming material again");
  lost.Nodes.All[destination].Owner=0;typeof(RobotWorld).GetMethod("ResolveProductionIntakes",F)!.Invoke(lost.Robots,new object[]{lost});
  Check(!lost.Robots.ProductionIntakes.Any()&&lost.Robots.Nodes[destination].Robots.Single(robot=>robot.Id==product.Id)==product,"restored ownership resumes released-lane intake exactly once");
 }
 while(stock.Nodes[destination].HasSpace)stock.Nodes[destination].Add(stock.Create(0,RobotParts.Complete));
 typeof(RobotWorld).GetMethod("ResolveProductionIntakes",F)!.Invoke(stock,new object[]{redirected});
 Check(stock.ProductionIntakes.Single().Product==product&&stock.ProductionIntakes.Single().Status=="入荷待機：格納枠不足"&&!stock.Nodes[destination].Robots.Any(robot=>robot.Id==product.Id),"full redirected destination retains released-lane product with precise capacity wait reason");
 redirected=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(redirected));stock=redirected.Robots;stock.ConfigureFactories(redirected);
 Check(stock.ProductionIntakes.Single().Status=="入荷待機：格納枠不足"&&stock.Nodes[source].MaplePackages.Single().Units==8,"redirected capacity wait reason and raw balance survive save");
 if(Environment.GetEnvironmentVariable("PP_WAIT_FIXTURE") is string waitFixture)AutoSaveStore.Write(waitFixture,redirected);
 stock.Nodes[destination].Remove(stock.Nodes[destination].Robots.First(robot=>!robot.WarehouseKeeper).Id);
 typeof(RobotWorld).GetMethod("ResolveProductionIntakes",F)!.Invoke(stock,new object[]{redirected});
 Check(!stock.ProductionIntakes.Any()&&stock.Nodes[destination].Robots.Single(robot=>robot.Id==product.Id)==product&&stock.Nodes[source].MaplePackages.Single().Units==8,"released lane product arrives at new destination once without consuming more raw material");
}

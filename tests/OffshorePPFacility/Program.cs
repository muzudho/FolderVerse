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
int source=sites.First().Node;var r=w.Robots;w.Nodes.All[source].Owner=0;w.Nodes.All[source].Purpose=NodePurpose.General;
typeof(RobotStore).GetMethod("Replace",F)!.Invoke(r.Nodes[source],new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(r.Nodes[source],new object[]{Array.Empty<MaplePackage>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(r.Nodes[source],new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(r.Nodes[source],new object[]{12});
for(int i=0;i<2;i++)r.Nodes[source].Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});
typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(r.Workshops[source],null);r.Workshops[source].LimitLines(3);var worker=r.Create(0,RobotParts.Complete);r.Nodes[source].Add(worker);
Check(r.PartPlanning.ReserveRole(w,source,worker.Id,HangarReservation.Harvest,0),"reserve real marine harvest worker");typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(r.PartPlanning,new object[]{w});
var line=r.Workshops[source].ProductionLines.Single();int before=r.RawDeposit(source,RawMaterial.PP);line.SelectMapleHarvest(1,RawMaterial.PP);line.SetDoNotRepeat(true);
for(int i=0;i<IndustryTime.UnitsPerTurn;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(r.RawDeposit(source,RawMaterial.PP)==before-1&&r.Nodes[source].MaplePackages.Sum(p=>p.Material==RawMaterial.PP?p.Units:0)==16,"harvest lane creates 16 PP units and consumes exactly one load");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;r.ConfigureFactories(w);Check(r.RawDeposit(source,RawMaterial.PP)==before-1,"save does not regenerate marine reserve");
Check(r.ToggleReservation(source,worker.Id,HangarReservation.Worker,0),"switch harvester into manufacturer");line=r.Workshops[source].ProductionLines.Single();var plan=new ProductionPlan(RobotParts.Head,3){HeadMaterial=RobotMaterial.Polypropylene};line.Configure(RobotParts.Head,3);line.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,plan);
for(int i=0;i<18;i++)r.AdvanceIndustry(n=>w.Nodes.All[n].Owner,n=>n!=source);
Check(r.Nodes[source].MaplePackages.Where(p=>p.Material==RawMaterial.PP).Sum(p=>p.Units)==8&&r.ProductionIntakes.Count==1,"recycled PP makes a PP head with unchanged eight-unit recipe");
var product=r.ProductionIntakes.Single().Product;Check(product.HeadMaterial==RobotMaterial.Polypropylene,"manufactured material is polypropylene");

using FolderVerse;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
var seed=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};seed.SetParticipantCounts(0,2);seed.SetWorld(104729);seed.SetCast(1);seed.SetTerritoryRights(2);foreach(int ruler in seed.LandOrder())seed.ClaimLand(ruler,new ComputerLandPlanner(seed).Select(ruler,null).Cells);seed.CompleteLand(3,25,25);
var baseline=AutoSaveStore.Capture(seed);
try{foreach(var materials in new[]{new[]{RobotMaterial.Maple,RobotMaterial.Polystyrene,RobotMaterial.SoftVinyl},new[]{RobotMaterial.Polypropylene,RobotMaterial.Silicone,RobotMaterial.Polypropylene}}){
 var w=AutoSaveStore.Restore<WorldSetup>(baseline);var r=w.Robots;int node=w.Nodes.All.First(n=>n.Functional).Id;w.Nodes.All[node].Owner=0;r.ConfigureFactories(w);
 var store=r.Nodes[node];typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{Array.Empty<Robot>()});typeof(RobotStore).GetMethod("SetWarehouseLimit",F)!.Invoke(store,new object[]{60});typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{12});typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{Array.Empty<MaplePackage>()});store.Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});store.Add(r.Create(0,RobotParts.Complete) with{WarehouseKeeper=true});var worker=r.Create(0,RobotParts.Complete);store.Add(worker);typeof(RobotWorkshop).GetMethod("RemoveProductionLines",F)!.Invoke(r.Workshops[node],null);r.Workshops[node].LimitLines(3);if(!r.PartPlanning.ReserveRole(w,node,worker.Id,HangarReservation.Worker,0))throw new Exception("Actual worker appointment rejected");typeof(RobotPartPlanning).GetMethod("Resolve",F)!.Invoke(r.PartPlanning,new object[]{w});if(r.Workshops[node].BaseLineCount!=0||r.Workshops[node].WorkerAt(0)!=worker.Id)throw new Exception("Expected worker-created lane");w.Nodes.All[node].Purpose=NodePurpose.General;
 var plan=new ProductionPlan(RobotParts.Complete,3){HeadMaterial=materials[0],BodyMaterial=materials[1],LegsMaterial=materials[2]};var preview=plan.Preview(0);
 int Cost(RawMaterial m)=>(int)typeof(RobotWorld).GetMethod("RawCost",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{preview,m})!;
 var needs=RobotWorld.RawMaterials.Where(m=>Cost(m)>0).ToArray();var packages=needs.Select((m,i)=>new MaplePackage(990000+i,48){Material=m}).ToArray();packages[^1]=packages[^1] with{Units=Cost(needs[^1])-1};
 typeof(RobotStore).GetMethod("SetMaplePackages",F)!.Invoke(store,new object[]{packages});
 var line=r.Workshops[node].ProductionLines.Single();line.Configure(RobotParts.Complete,3);line.SetDoNotRepeat(true);typeof(RobotWorkshop).GetField("_selectedProductPlan",F)!.SetValue(line,plan);r.Workshops[node].SetReservedSlots(0);line.SetReservedSlots(0);
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
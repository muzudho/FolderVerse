using FolderVerse;
if(args.Contains("--ui")){using var game=new WorkerPlansUi();game.Run();return;}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
bool sawMixed=false;var signatures=new HashSet<string>();
{
 var(w,node)=Fixture.Create();
 w.EquipmentMode=RobotEquipmentMode.Materials;
 w.Robots.ConfigureFactories(w);
 var worker=w.Robots.Create(0,RobotParts.Complete) with {
  HeadMaterial=RobotMaterial.Cardboard,BodyMaterial=RobotMaterial.Cardboard,LegsMaterial=RobotMaterial.Tin};
 var line=Fixture.Appoint(w,node,worker);
 var catalogue=Fixture.Catalogue(line);
 Check(catalogue.Any(p=>p.HeadMaterial==RobotMaterial.Cardboard) &&
       catalogue.Any(p=>p.BodyMaterial==RobotMaterial.Cardboard),"Worker materials influence both matching parts");
 Check(catalogue.All(p=>p.LegsMaterial!=RobotMaterial.Tin),"Worker cannot manufacture tinplate");
 Check(catalogue.Any(p=>p.HeadMaterial==null && (p.Product&RobotParts.Head)!=0),"Worker keeps other material choices");
 var loaded=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
 loaded.Robots.ConfigureFactories(loaded);
 Check(Fixture.Catalogue(loaded.Robots.Workshops[node].ProductionLines.Last()).SequenceEqual(catalogue),
       "Material catalogue survives reload");
}
{
 var(w,node)=Fixture.Create();
 var factory=w.Robots.Workshops[node];
 for(int seed=0;factory.BaseLineCount!=0;seed++)factory.GenerateLines(seed);
 var worker=w.Robots.Create(0,RobotParts.Complete);
 w.Robots.Nodes[node].Add(worker);
 using var game=new Game1();
 const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
 typeof(Game1).GetField("_setup",flags)!.SetValue(game,w);
 typeof(Game1).GetField("_populationNode",flags)!.SetValue(game,node);
 var canOpen=typeof(Game1).GetProperty("CanOpenNodeFactory",flags)!;
 Check(!(bool)canOpen.GetValue(game)!,"Factory with no lanes starts disabled");
 Check(w.Robots.PartPlanning.ReserveRole(w,node,worker.Id,HangarReservation.Worker,0),"Can reserve first worker");
 Check(factory.ProductionLines.Length==0 && (bool)canOpen.GetValue(game)!,"Pending worker enables the factory tab");
 typeof(Game1).GetMethod("OpenNodeFactory",flags)!.Invoke(game,null);
 Check((bool)typeof(Game1).GetField("_nodeFactoryOpen",flags)!.GetValue(game)!,"Factory tab opens before appointment");
}
for(int seed=0;seed<24;seed++){
 var(w,node)=Fixture.Create(seed*37+123);var f=w.Robots.Workshops[node];var baseline=Fixture.Catalogue(f);var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=2,CheapParts=RobotParts.Head};
 var line=Fixture.Appoint(w,node,worker);var catalogue=Fixture.Catalogue(line);
 Check(Fixture.Catalogue(f).SequenceEqual(baseline),"Worker does not change the permanent lane");
 foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
  var recipes=catalogue.Where(p=>(p.Product&part)!=0).ToArray();int baselineRegular=new[]{0,1,2,7,8}.Count(i=>(line.PlanAt(i).Product&part)!=0);
  int regular=recipes.Count(p=>(p.EffectiveCheapParts&part)==0);
  Check(regular==(part==RobotParts.Head?(baselineRegular+1)/2:baselineRegular),"Only the cheap worker part halves regular choices, rounding up");
  int foreign=recipes.Count(p=>p.Preview(0).Origin(part)==worker.Origin(part) && worker.Origin(part)!=0);
  Check(foreign==(worker.Origin(part)==0?0:recipes.Length/2),"Exactly half the corresponding candidates inherit the worker's foreign color");
 }
 sawMixed|=catalogue.Any(p=>p.EffectiveCheapParts!=0 && p.EffectiveCheapParts!=p.Product);
 signatures.Add(string.Join(";",catalogue.Select(p=>p.ToString())));
 w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);
 Check(w.Robots.Workshops[node].ProductionLines.Length==1,"Removing the worker removes only its extra lane");
 var replacement=worker.Materialize() with {Id=w.Robots.Create(0,RobotParts.Complete).Id,Role=RobotRole.Captain,LastDomesticPersuasionTurn=333};
 line=Fixture.Appoint(w,node,replacement);
 Check(Fixture.Catalogue(line).SequenceEqual(catalogue),"Different ID, role, flags and history cannot reroll equivalent parts");
 if(seed==0){
  var loaded=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));loaded.Robots.ConfigureFactories(loaded);
  loaded.Robots.ToggleReservation(node,replacement.Id,HangarReservation.Worker);loaded.Robots.ToggleReservation(node,replacement.Id,HangarReservation.Worker);
  Check(Fixture.Catalogue(loaded.Robots.Workshops[node].ProductionLines.Last()).SequenceEqual(catalogue),"Reappointment after save/load derives the same seed and catalogue");
 }
 w.Robots.ToggleReservation(node,replacement.Id,HangarReservation.Worker);w.Robots.ToggleReservation(node,replacement.Id,HangarReservation.Worker);
 Check(Fixture.Catalogue(w.Robots.Workshops[node].ProductionLines.Last()).SequenceEqual(catalogue),"Repeated cancellation and appointment cannot reroll a catalogue");
}
Check(sawMixed && signatures.Count>2,"Mixed quality is represented and world/location conditions produce different catalogues");
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,CheapParts=RobotParts.Head};var line=Fixture.Appoint(w,node,worker);var plans=Fixture.Catalogue(line);
 line.SelectPlan(7);var recipe=line.CurrentProductionPlan;w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);int age=line.ProductionAge;
 var donor=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[node].Add(donor);w.Robots.MovePart(node,donor.Id,RobotParts.Head,worker.Id,0);
 Check(w.Robots.Nodes[node].Robots.Single(r=>r.Id==worker.Id).FactoryWorker && line.WorkerSnapshot.Origin(RobotParts.Head)==1 && Fixture.Catalogue(line).SequenceEqual(plans) && line.CurrentProductionPlan==recipe && line.ProductionAge==age,"Qualified part swap preserves appointment snapshot, candidates and current manufacturing");
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));restored.Robots.ConfigureFactories(restored);var restoredLine=restored.Robots.Workshops[node].ProductionLines.Last();
 Check(Fixture.Catalogue(restoredLine).SequenceEqual(plans) && restoredLine.CurrentProductionPlan==recipe && restoredLine.ProductionAge==age,"Save/load preserves old appointment snapshot and current production after a part swap");
 w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);
 Check(w.Robots.Workshops[node].ProductionLines.Last().WorkerSnapshot.Origin(RobotParts.Head)==0 && Fixture.Catalogue(w.Robots.Workshops[node].ProductionLines.Last()).All(p=>p.Preview(0).Origin(RobotParts.Head)==0),"Reappointment adopts the new part state");
 w.Robots.MovePart(node,donor.Id,RobotParts.Head,worker.Id,0);w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);
 Check(Fixture.Catalogue(w.Robots.Workshops[node].ProductionLines.Last()).SequenceEqual(plans),"Returning to the original part state restores the original catalogue");
}
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,CheapParts=RobotParts.Head};var line=Fixture.Appoint(w,node,worker);
 foreach(int index in Fixture.Options){
  foreach(var r in w.Robots.Nodes[node].Robots.Where(r=>r.Id!=worker.Id).ToArray())w.Robots.Nodes[node].Remove(r.Id);
  line.SelectPlan(3);line.SelectPlan(index);var recipe=line.PlanAt(index);
  for(int turn=0;turn<recipe.Turns;turn++)w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
  var product=w.Robots.Nodes[node].Robots.Single(r=>r.Id!=worker.Id);Fixture.Same(recipe,product);
  Check(!product.IsDamaged(product.Parts),"Worker damage is a seed input, not damage copied onto manufactured parts");
 }
}
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1};var line=Fixture.Appoint(w,node,worker);line.Configure(RobotParts.Head,4);w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
 var legacy=AutoSaveStore.Capture(w);
 foreach(var e in legacy.Objects)if(e.Fields!=null)foreach(string field in new[]{"_workerPlans","_workerAppointment","_selectedProductPlan","_workerPlanWorldSeed","<HeadOrigin>k__BackingField","<BodyOrigin>k__BackingField","<LegsOrigin>k__BackingField","<CheapParts>k__BackingField"}){
  if(field.StartsWith("<") && !e.Type.StartsWith("FolderVerse.ProductionPlan,"))continue;e.Fields.Remove(field);
 }
 var restored=AutoSaveStore.Restore<WorldSetup>(legacy);restored.Robots.ConfigureFactories(restored);var upgraded=restored.Robots.Workshops[node].ProductionLines.Last();
 restored.Robots.ProductionIntakeEnabled=false;
 Check(upgraded.WorkerSnapshot!=null && upgraded.ProductionAge==1 && upgraded.Product==RobotParts.Head && upgraded.ProductionPeriod==4 && upgraded.CurrentProductionPlan.Preview(0).HeadOrigin==0 && upgraded.SelectedPlan==-1,"Old worker lanes gain deterministic choices without changing the in-progress legacy recipe");
 for(int i=0;i<3;i++)restored.Robots.AdvanceIndustry(n=>restored.Nodes.All[n].Owner);
 Check(restored.Robots.Nodes[node].Robots.Any(r=>r.Id!=worker.Id && r.Parts==RobotParts.Head && r.Origin(RobotParts.Head)==0 && r.CheapParts==0),"Legacy in-progress manufacturing completes unchanged");
}
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=2,CheapParts=RobotParts.Head};var original=Fixture.Catalogue(Fixture.Appoint(w,node,worker));
 w.Robots.ToggleReservation(node,worker.Id,HangarReservation.Worker);
 var equivalent=worker.Materialize() with {Id=w.Robots.Create(1,RobotParts.Complete).Id,Owner=1};w.Relations.SetSuperior(1,0);
 Check(Fixture.Catalogue(Fixture.Appoint(w,node,equivalent)).SequenceEqual(original),"Robot ownership metadata does not reroll equal actual part colors");
 int other=w.Nodes.All.First(n=>n.Id!=node && n.Terrain!=TravelTerrain.Sea).Id;w.Nodes.All[other].Owner=0;
 var f=w.Robots.Workshops[other];f.LimitLines(3);for(int i=0;f.BaseLineCount!=1;i++)f.GenerateLines(i);
 foreach(var r in w.Robots.Nodes[other].Robots.ToArray())w.Robots.Nodes[other].Remove(r.Id);
 var moved=worker.Materialize() with {Id=w.Robots.Create(0,RobotParts.Complete).Id};
 Check(!Fixture.Catalogue(Fixture.Appoint(w,other,moved)).SequenceEqual(original),"Different locations contribute to the deterministic catalogue");
 w.Robots.ToggleReservation(node,equivalent.Id,HangarReservation.Worker);w.Nodes.All[node].Owner=1;
 w.Robots.ToggleReservation(node,equivalent.Id,HangarReservation.Worker);var changed=Fixture.Catalogue(w.Robots.Workshops[node].ProductionLines.Last());
 Check(changed.All(p=>p.Preview(1).HeadOrigin==1),"Factory country determines native versus foreign color on reappointment");
}
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,CheapParts=RobotParts.Head,DamagedParts=RobotParts.Head};var line=Fixture.Appoint(w,node,worker);
 int index=Fixture.Options.First(i=>(line.PlanAt(i).Product&RobotParts.Head)!=0);line.SelectPlan(3);line.SelectPlan(index);var recipe=line.PlanAt(index);
 for(int i=0;i<recipe.Turns;i++)w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
 var product=w.Robots.Nodes[node].Robots.Single(r=>r.Id!=worker.Id);Fixture.Same(recipe,product);Check(product.DamagedParts==0,"A damaged worker can influence the catalogue without manufacturing damaged products");
}
{
 var(w,node)=Fixture.Create();var worker=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,CheapParts=RobotParts.Head};var line=Fixture.Appoint(w,node,worker);
 for(int i=0;i<10;i++)w.Robots.Nodes[node].Add(w.Robots.Create(0,RobotParts.Body|RobotParts.Legs));
 bool regularHead=Fixture.Options.Select(line.PlanAt).Any(p=>(p.Product&RobotParts.Head)!=0 && p.Preview(0).Origin(RobotParts.Head)==0 && (p.EffectiveCheapParts&RobotParts.Head)==0);
 w.Robots.Workshops[node].Automation=NodeAutomation.Auto;w.Robots.AdjustNode(node,0,(a,b)=>false,nodeOwner:n=>w.Nodes.All[n].Owner);
 var chosen=line.CurrentProductionPlan.Preview(0);Check((chosen.Parts&RobotParts.Head)!=0 && chosen.Origin(RobotParts.Head)==0 && (!regularHead || !chosen.IsCheap(RobotParts.Head)),"Supervisor fills the actual native head shortage and prefers available regular native output");
}
Console.WriteLine("PASS: per-part 50% color/quality, permanent lanes unchanged, deterministic equivalent appointments, fixed snapshots, exact products, integrated parts, save/load and legacy manufacturing.");

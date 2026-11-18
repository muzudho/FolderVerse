using FolderVerse;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
var baseline=AutoSaveStore.Capture(AutoSaveStore.Read<WorldSetup>(args[0]));
var w=AutoSaveStore.Restore<WorldSetup>(baseline);var r=w.Robots;
int reroutes=0;
void Reroute(bool automaticAll=false){
 var lines=r.Workshops.SelectMany(x=>x.ProductionLines).ToArray();var before=lines.Select(x=>x.IntakeTarget).ToArray();
 typeof(WorldSetup).Assembly.GetType("FolderVerse.ComputerIntakePlanning")!.GetMethod("Plan",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{w,automaticAll});
 reroutes+=lines.Where((x,i)=>x.IntakeTarget!=before[i]).Count();
}void Reset(){w=AutoSaveStore.Restore<WorldSetup>(baseline);r=w.Robots;reroutes=0;foreach(var shop in r.Workshops)shop.Automation=NodeAutomation.Manual;r.Workshops[96].Automation=NodeAutomation.Gentle;}
try{
 Reset();Reroute();if(reroutes!=1 || r.Workshops[96].ProductionLines.Single().IntakeTarget!=87)throw new Exception("Expected equal-space tie-break neighbor87");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;if(r.Workshops[96].ProductionLines.Single().IntakeTarget!=87)throw new Exception("Changed target not saved");Console.WriteLine("PASS existing feasible target and save");
 Reset();r.Workshops[96].Automation=NodeAutomation.Manual;Reroute();if(reroutes!=0 || r.Workshops[96].ProductionLines.Single().IntakeTarget!=null)throw new Exception("Manual factory overwritten");Console.WriteLine("PASS manual factory protection");
 Reset();w.Nodes.All[87].Owner=-1;
 // One effective destination slot after respecting its four retained slots.
 r.Nodes[97].Add(r.Create(15,RobotParts.Complete));
 var line=r.Workshops[96].ProductionLines.Single();var second=AutoSaveStore.Restore<RobotWorkshop>(AutoSaveStore.Capture(line));
 typeof(RobotWorkshop).GetField("<ProductionLines>k__BackingField",F)!.SetValue(r.Workshops[96],new[]{line,second});
 Reroute();if(reroutes!=1 || new[]{line,second}.Count(l=>l.IntakeTarget==97)!=1)throw new Exception("Two outputs overbooked one effective slot");Console.WriteLine("PASS two failed lanes share one effective slot: exactly one reroute");
 Reset();w.Nodes.All[87].Owner=-1;var pendingLine=r.Workshops[96].ProductionLines.Single();
 var pendingProduct=pendingLine.CurrentProductionPlan.Preview(15);
 r.ProductionIntakes.Add(new(96,97,pendingProduct with {Id=long.MaxValue-10},new RobotWorkshop()));
 r.ProductionIntakes.Add(new(96,97,pendingProduct with {Id=long.MaxValue-11},new RobotWorkshop()));
 Reroute();if(reroutes!=0)throw new Exception("Existing completed intakes were ignored");Console.WriteLine("PASS existing two completed intakes consume effective destination space");
 Reset();w.SetParticipantCounts(1,19);w.HumanPlayers.Add(15);
 if(!w.ManualRulers().Contains(15))throw new Exception("Human fixture not actually controlled");
 var humanLine=r.Workshops[96].ProductionLines.Single();r.SetIntakeTarget(humanLine,18);humanLine.IntakeFailure="入荷先の格納枠が不足しています";
 Reroute();if(reroutes!=0 || humanLine.IntakeTarget!=18)throw new Exception("Human explicit destination overwritten");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;Reroute();if(reroutes!=0 || r.Workshops[96].ProductionLines.Single().IntakeTarget!=18)throw new Exception("Saved human target overwritten");
 Reroute(true);if(reroutes!=1)throw new Exception("Explicit automaticAll did not permit planning");Console.WriteLine("PASS human faction/explicit target/save protected; automaticAll override works");
 Reset();w.Campaign.RobotBattles.Encounters.Add(new RobotEncounter{Kind=BattleKind.Node,Target=96});Reroute();if(reroutes!=0)throw new Exception("Battle source changed");Console.WriteLine("PASS battle-locked source");
 Reset();foreach(int t in new[]{87,97})w.Campaign.RobotBattles.Encounters.Add(new RobotEncounter{Kind=BattleKind.Edge,Source=96,Target=t});Reroute();if(reroutes!=0)throw new Exception("Battle edge selected");Console.WriteLine("PASS locked delivery edges");
 Reset();foreach(int t in new[]{87,97})w.Campaign.RobotBattles.Encounters.Add(new RobotEncounter{Kind=BattleKind.Node,Target=t});Reroute();if(reroutes!=0)throw new Exception("Battle destination selected");Console.WriteLine("PASS battle-locked destinations");
 Reset();w.Campaign.FunctionTransfers.Orders.Add(new(96,97,15));Reroute();if(reroutes!=0)throw new Exception("Moving source factory changed");Console.WriteLine("PASS source transfer reservation");
 Reset();w.Campaign.FunctionTransfers.Orders.Add(new(87,97,15));Reroute();if(reroutes!=0)throw new Exception("Moving destination selected");Console.WriteLine("PASS destination transfer reservation");
 Reset();var keep=r.Workshops[96].ProductionLines.Single();r.SetIntakeTarget(keep,97);keep.IntakeFailure="入荷先の格納枠が不足しています";Reroute();if(reroutes!=0 || keep.IntakeTarget!=97)throw new Exception("Usable existing choice overwritten");Console.WriteLine("PASS existing usable target remains");
}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}
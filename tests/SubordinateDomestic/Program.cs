using FolderVerse;
if(args.Contains("--ui")){using var game=new SubordinateUi();game.Run();return;}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static WorldSetup Fixture(){
    var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
    w.HumanPlayers.Clear();w.HumanPlayers.Add(0);w.Relations.SetSuperior(1,0);
    foreach(var n in w.Nodes.All)n.Owner=0;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    foreach(var shop in w.Robots.Workshops){shop.SelectPlan(3);shop.AutoAssemblyDisabled=true;}
    return w;
}
static Robot Add(WorldSetup w,int node,RobotParts parts){var r=w.Robots.Create(w.Nodes.All[node].Owner,parts);w.Robots.Nodes[node].Add(r);return r;}
{
    var w=Fixture();w.Relations.SetSuperior(2,1);w.Relations.SetSuperior(3,0);w.HumanPlayers.Add(3);
    foreach(int n in new[]{0,1,2,3})w.Nodes.All[n].Owner=n==3?3:1;
    w.Nodes.All[2].FunctionUnits=0;
    Check(SubordinateDomestic.Pending(w,0,new[]{1}).SequenceEqual(new[]{0}),"Only unchecked functional subordinate points belong to this controller");
    Check(SubordinateDomestic.Pending(w,-1,Array.Empty<int>()).Length==0,"CPU/no controller is never treated as a human's subordinate");
    w.Nodes.All[4].Owner=2;
    Check(SubordinateDomestic.Pending(w,0,new[]{1}).Contains(4),"Nested nonhuman subordinates are included");
    w.Campaign.RobotBattles.Encounters.Add(new RobotEncounter{Kind=BattleKind.Node,Target=0});
    Check(!SubordinateDomestic.Pending(w,0,Array.Empty<int>()).Contains(0),"A point already in combat is excluded");
}
{
    var w=Fixture();int node=0;w.Nodes.All[node].Owner=1;
    var shop=w.Robots.Workshops[node];shop.SetDoNotRepeat(true);int selected=shop.SelectedPlan;
    Add(w,node,RobotParts.Head);Add(w,node,RobotParts.Body);Add(w,node,RobotParts.Legs);
    var result=SubordinateDomestic.Operate(w,0,node,new HashSet<int>{node});
    Check(w.Robots.Nodes[node].Count==3 && w.Robots.PartPlanning.Orders.Count==2,"Subordinate assembly is forecast-only until delivery");
    Check(shop.DoNotRepeat && shop.SelectedPlan==selected && shop.Automation==NodeAutomation.Manual,"One-shot manufacturing and permanent manual supervisor setting survive the supplement");
    Check(w.Robots.PartPlanning.Preview(w).Nodes[node].Robots.Single().Parts==RobotParts.Complete,"Forecast contains the completed subordinate robot");
    int plans=w.Robots.PartPlanning.Orders.Count;
    Check(SubordinateDomestic.Operate(w,0,node,new HashSet<int>{node})==result && w.Robots.PartPlanning.Orders.Count==plans,"Replay/skip cannot run a point twice in the same turn");
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));saved.Robots.ConfigureFactories(saved);
    Check(saved.Campaign.SubordinateResults.Single()==result && SubordinateDomestic.Pending(saved,0,Array.Empty<int>()).Length==0,"Reports and completion survive save/resume");
    var legacy=AutoSaveStore.Capture(w);
    foreach(var entry in legacy.Objects.Where(e=>e.Type.StartsWith("FolderVerse.ConquestCampaign,")))entry.Fields?.Remove("<SubordinateResults>k__BackingField");
    Check(AutoSaveStore.Restore<WorldSetup>(legacy).Campaign.SubordinateResults.Count==0,"Older saves default to an empty subordinate report list");
    w.Campaign.FinishDomestic(w);Check(w.Robots.Nodes[node].Robots.Single().Parts==RobotParts.Complete,"Reserved assembly executes in ordinary delivery");
}
{
    var w=Fixture();w.Nodes.All[0].Owner=1;var robot=Add(w,0,RobotParts.Complete);
    w.Robots.PartPlanning.Reserve(w,0,0,robot.Id,RobotParts.Head,null,1);
    var before=w.Robots.PartPlanning.Orders.ToArray();int plan=w.Robots.Workshops[0].SelectedPlan;
    var report=SubordinateDomestic.Operate(w,0,0,new HashSet<int>{0});
    Check(report.Action.Contains("手動") && before.SequenceEqual(w.Robots.PartPlanning.Orders) && w.Robots.Workshops[0].SelectedPlan==plan,"Existing human reservations and factory settings are preserved");
}
{
    var w=Fixture();var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);source.Owner=1;
    var route=w.Routes.Neighbors(source)[0];int target=w.Nodes.At(route.Target,route.Entry).Id;w.Nodes.All[target].Owner=2;
    Add(w,source.Id,RobotParts.Complete);Add(w,source.Id,RobotParts.Complete);
    var report=SubordinateDomestic.Operate(w,0,source.Id,new HashSet<int>{source.Id});
    Check(report.Action.Contains("開戦") && w.Campaign.PlannedOrders.Single().SourceNode==source.Id && w.Campaign.PlannedOrders.Single().Fighters==1,"Subordinate reserves a weaker frontier sortie while retaining one robot");
    w.Campaign.FinishDomestic(w);Check(w.Campaign.LastCancelledWars.Count==0,"Subordinate sortie survives ordinary delivery validation");
}
{
    var w=Fixture();var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);source.Owner=1;
    var route=w.Routes.Neighbors(source)[0];int target=w.Nodes.At(route.Target,route.Entry).Id;w.Nodes.All[target].Owner=1;
    for(int i=0;i<4;i++)Add(w,source.Id,RobotParts.Complete);
    SubordinateDomestic.Operate(w,0,source.Id,new HashSet<int>{source.Id});
    Check(!w.Robots.PartPlanning.Orders.Any(o=>o.Target==target),"Checked destination is never modified by an automatic direct delivery");
    w.Campaign.SubordinateResults.Clear();
    SubordinateDomestic.Operate(w,0,source.Id,new HashSet<int>{source.Id,target});
    Check(w.Robots.PartPlanning.Orders.Any(o=>o.Source==source.Id && o.Target==target),"Surplus complete robots can be reserved for another unchecked subordinate point");
}
Console.WriteLine("PASS: subordinate selection, human boundaries, forecast assembly, manual/one-shot preservation, idempotence, saved reports, guarded sorties and checked-target protection.");

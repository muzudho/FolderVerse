using FolderVerse;
using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
void Check(bool yes,string message){if(!yes)throw new Exception(message);Console.WriteLine("PASS "+message);}
WorldSetup Fixture(){
    var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Relations.Reset();w.Relations.Leader=0;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    foreach(var n in w.Nodes.All){n.Owner=0;n.Purpose=NodePurpose.General;}
    var battle=w.Campaign.RobotBattles;
    int enemy=-1,wall=-1,rear=-1;
    foreach(var n in w.Nodes.All){
        var neighbors=battle.MarchTargets(w,n.Id).ToArray();if(neighbors.Length<3)continue;
        foreach(int via in neighbors)foreach(int source in battle.MarchTargets(w,via))
            if(source!=n.Id && !neighbors.Contains(source)){enemy=n.Id;wall=via;rear=source;break;}
        if(rear>=0)break;
    }
    if(rear<0)throw new Exception("No suitable topology");
    w.Nodes.All[enemy].Owner=1;
    foreach(int source in battle.MarchTargets(w,enemy).Append(rear).Distinct())
        for(int i=0;i<3;i++)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete));
    return w;
}
WorldSetup Clone(WorldSetup w)=>AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
var initial=Fixture();if(Environment.GetEnvironmentVariable("STRATAGEM_FIXTURE") is string fixturePath)AutoSaveStore.Write(fixturePath,initial);var all=initial.Campaign.Stratagems.Find(initial,0);
foreach(var kind in new[]{StratagemKind.WallSortie,StratagemKind.WallRelief}){
    var w=Clone(initial);var plans=w.Campaign.Stratagems;var original=plans.Find(w,0).First(m=>m.Kind==kind);
    int wall=original.Wall;
    foreach(var guard in w.Robots.Nodes[wall].Robots.ToArray())Check(w.Robots.ToggleReservation(wall,guard.Id,HangarReservation.Defense),"manually staff existing wall");
    var guards=w.Robots.Nodes[wall].Robots.ToArray();
    var match=plans.Find(w,0).First(m=>m.Kind==kind && m.Wall==wall && m.Sites[0].Node==original.Sites[0].Node);
    Check(plans.Reserve(w,0,match,out _),"staffed wall remains usable for "+kind);
    var cancelled=Clone(w);cancelled.Campaign.Stratagems.Cancel(cancelled.Campaign.Stratagems.Pending.Single().Id,0);
    Check(cancelled.Robots.Nodes[wall].Robots.SequenceEqual(guards),"cancel preserves pre-existing wall guards");
    w=Clone(w);plans=w.Campaign.Stratagems;
    Check(plans.Commit(w,0,out var reason),"saved staffed wall commit: "+reason);
    Check(w.Robots.Nodes[wall].Robots.SequenceEqual(guards),"commit never toggles existing guards off");
}
Check(Enum.GetValues<StratagemKind>().All(k=>all.Any(m=>m.Kind==k)),"all three patterns match actual map topology");
Check(all.Select(m=>m.Key).Distinct().Count()==all.Length,"candidate keys are unique");
{
    var w=Clone(initial);
    foreach(var node in w.Nodes.All.Where(n=>n.Owner==0 && n.Id%17==0).Take(8))node.Owner=1;
    foreach(var node in w.Nodes.All.Where(n=>n.Owner==0))while(w.Robots.Nodes[node.Id].Count<3)w.Robots.Nodes[node.Id].Add(w.Robots.Create(0,RobotParts.Complete));
    var watch=System.Diagnostics.Stopwatch.StartNew();var many=w.Campaign.Stratagems.Find(w,0);watch.Stop();
    Check(many.Length>10,"all matches remain available beyond ten combinations");
    Check(many.Count(m=>m.Kind==StratagemKind.WallSortie)>10,"one card can list more than ten applicable arrangements");
    var first=many.First();Check(w.Campaign.Stratagems.Reserve(w,0,first,out _),"first independent card reservation");
    var next=w.Campaign.Stratagems.Find(w,0).FirstOrDefault(m=>m.Kind==first.Kind);
    Check(next!=null && w.Campaign.Stratagems.Reserve(w,0,next,out _),"second independent reservation of same card");
    var purposes=w.Nodes.All.Select(n=>n.Purpose).ToArray();var changed=next!.Troops.First();
    Check(w.Robots.ToggleReservation(changed.Node,changed.Before.Id,HangarReservation.Defense),"manual conflict in second bundle");
    Check(!w.Campaign.Stratagems.Commit(w,0,out _) && !w.Campaign.PlannedOrders.Any() && w.Nodes.All.Select(n=>n.Purpose).SequenceEqual(purposes),"whole batch validated before first bundle applies");
    long keep=w.Campaign.Stratagems.Pending[1].Id;w.Campaign.Stratagems.Cancel(w.Campaign.Stratagems.Pending[0].Id,0);
    Check(w.Campaign.Stratagems.Pending.Single().Id==keep,"individual cancellation preserves another bundle");
    Console.WriteLine($"INFO {many.Length} candidates in {watch.ElapsedMilliseconds}ms");
}
foreach(var kind in Enum.GetValues<StratagemKind>()){
    var w=Clone(initial);var plans=w.Campaign.Stratagems;var match=plans.Find(w,0).First(m=>m.Kind==kind);
    var robots=w.Robots.Nodes.SelectMany(s=>s.Robots).ToArray();var purposes=w.Nodes.All.Select(n=>n.Purpose).ToArray();
    Check(plans.Reserve(w,0,match,out _),"reserve "+kind);
    Check(w.Robots.Nodes.SelectMany(s=>s.Robots).SequenceEqual(robots) && w.Nodes.All.Select(n=>n.Purpose).SequenceEqual(purposes) && !w.Campaign.PlannedOrders.Any(),"reservation is deferred, not an immediate setup");
    Check(!plans.Reserve(w,0,match,out _),"overlapping bundle cannot be reserved twice");
    var saved=Clone(w);Check(saved.Campaign.Stratagems.Pending.Single().Match.Key==match.Key,"pending card survives save");
    Check(!saved.Campaign.Stratagems.Cancel(saved.Campaign.Stratagems.Pending.Single().Id,1),"other controller cannot cancel");
    Check(saved.Campaign.Stratagems.Cancel(saved.Campaign.Stratagems.Pending.Single().Id,0) && !saved.Campaign.Stratagems.Pending.Any() && !saved.Campaign.PlannedOrders.Any(),"cancel whole bundle after save without side effects");
    Check(plans.Commit(w,0,out _),"commit "+kind);
    Check(!plans.Pending.Any() && match.Sites.All(s=>w.Nodes.All[s.Node].Purpose==s.After),"commit applies purposes once");
    Check(match.Troops.Where(t=>t.Reservation!=HangarReservation.None).All(t=>w.Robots.Nodes[t.Node].Robots.Single(r=>r.Id==t.Before.Id) is var r && (t.Reservation==HangarReservation.Defense?r.DefenseOnly:r.InterceptOnly)),"commit applies defense/interception only to selected robots");
    Check(w.Campaign.PlannedOrders.Count==match.Marches.Length && w.Campaign.PlannedOrders.All(o=>!o.Queen && o.RobotIds!.Length==o.Fighters),"commit uses explicit ordinary march reservations, no queen");
    Check(plans.Commit(w,0,out _) && w.Campaign.PlannedOrders.Count==match.Marches.Length,"repeat commit does not duplicate orders");
}
{
    var w=Clone(initial);var plans=w.Campaign.Stratagems;var match=plans.Find(w,0).First(m=>m.Kind==StratagemKind.WallSortie);plans.Reserve(w,0,match,out _);
    var troop=match.Troops.First(t=>t.Reservation==HangarReservation.None);
    Check(w.Robots.ToggleReservation(troop.Node,troop.Before.Id,HangarReservation.Defense),"manual change after card reservation");
    Check(!plans.Commit(w,0,out var reason) && reason.Length>0 && !w.Campaign.PlannedOrders.Any() && match.Sites.All(s=>w.Nodes.All[s.Node].Purpose==s.Before),"stale bundle rejected before any partial purpose/order mutation");
    plans.Cancel(plans.Pending.Single().Id,0);Check(w.Robots.Nodes[troop.Node].Robots.Single(r=>r.Id==troop.Before.Id).DefenseOnly,"cancel preserves unrelated manual change");
}
{
    var w=Clone(initial);var plans=w.Campaign.Stratagems;var match=plans.Find(w,0).First(m=>m.Kind==StratagemKind.ConcentratedAttack);
    w.Campaign.Plan(match.Marches[0]);Check(!plans.Reserve(w,0,match,out _),"existing manual march is never overwritten");Check(w.Campaign.PlannedOrders.Count==1,"manual march survives rejected card");
    var doc=AutoSaveStore.Capture(w);foreach(var entry in doc.Objects)entry.Fields?.Remove("<Stratagems>k__BackingField");
    Check(!AutoSaveStore.Restore<WorldSetup>(doc).Campaign.Stratagems.Pending.Any(),"older Materials save defaults to no card reservations");
}
{
    var w=Clone(initial);var plans=w.Campaign.Stratagems;var match=plans.Find(w,0).First(m=>m.Kind==StratagemKind.WallSortie);plans.Reserve(w,0,match,out _);
    w.Campaign.FinishDomestic(w,deferDelivery:()=>true);
    Check(!plans.Pending.Any() && w.Campaign.PlannedOrders.Count==1 && w.Campaign.Phase==CampaignPhase.Domestic,"faction end commits bundle before deferred delivery");
}

// CPU authority must not depend on the human controller/Leader selection.
{
 var w=Clone(initial);w.SetParticipantCounts(0,2);w.Relations.Leader=-1;foreach(var shop in w.Robots.Workshops)shop.Automation=NodeAutomation.Auto;
 Check(w.Campaign.Stratagems.Find(w,0).Length==0,"human lookup grants no authority in computer-only world");
 Check(w.Campaign.Stratagems.FindComputer(w,0).Length>0,"computer-only faction discovers physical opportunities");
 foreach(var kind in Enum.GetValues<StratagemKind>()){
  var c=Clone(w);var p=c.Campaign.Stratagems;var m=p.FindComputer(c,0).First(x=>x.Kind==kind);
  bool reserved=p.ReserveComputer(c,0,m,false,out var cpuReason);Check(reserved,"CPU reserve "+kind+" "+cpuReason);c=Clone(c);p=c.Campaign.Stratagems;
  Check(p.Pending.Single().Computer,"CPU reservation authority survives save");
  Check(p.CommitComputer(c,0,false,out _),"CPU commit "+kind);Check(c.Relations.Leader==-1,"CPU does not impersonate human leader");
 }
 ComputerStratagems.Plan(w);var d=w.Campaign.StratagemDecisions.Single(x=>x.Ruler==0);
 Check(d.Candidates>0&&d.Committed==1,"CPU planner commits one actual card");
 Check(!w.Campaign.Stratagems.Pending.Any(),"CPU bundle fully consumed");
 var savedCpu=Clone(w);Check(savedCpu.Campaign.StratagemDecisions.Single(x=>x.Ruler==0).Kind==d.Kind,"decision telemetry survives save");
 int before=w.Campaign.StratagemDecisions.Count;ComputerStratagems.Plan(w);Check(w.Campaign.StratagemDecisions.Count==before,"one decision per faction per turn");
 w.Campaign.Reset();Check(w.Campaign.StratagemDecisions.Count==0,"new campaign clears old CPU decisions");
}
{
 var w=Clone(initial);ComputerStratagems.Plan(w);
 Check(w.Campaign.StratagemDecisions.Single(x=>x.Ruler==0).Reason=="manual_control","manual player is protected");
 Check(!w.Campaign.PlannedOrders.Any(),"CPU never writes manual faction marches");
}

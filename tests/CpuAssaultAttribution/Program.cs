using FolderVerse;
using System.Text.Json;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var automated=new HashSet<int>{0,2};var family=new HashSet<int>{0,2};
var orders=new[]{new March(0,2,3,1,Queen:false,RobotIds:new long[]{101,102,999}),new March(2,2,1,3,Queen:false,RobotIds:new long[]{103}),
    new March(1,2,1,4,Queen:false,RobotIds:new long[]{104}),new March(0,2,0,1,Queen:true,PocketOnly:true,Peaceful:true)};
var roster=CpuAssaultAttribution.Record(6,orders,automated);
var battle=new BattleState(1){GlobalTurn=6,Kind=BattleKind.Node};
battle.AddArmy(0,1,new[]{new Robot(101,0,RobotParts.Complete),new Robot(102,0,RobotParts.Complete)});
battle.AddArmy(2,3,new[]{new Robot(103,2,RobotParts.Complete)});
Check(roster.Length==2,"Manual and peaceful orders are excluded");
Check(CpuAssaultAttribution.Force(roster,battle,2,family)==3,"Joint force counts only IDs actually in units or army queues");
foreach(var army in battle.Armies)while(army.Waiting.TryDequeue(out var unit)){unit.Owner=1;unit.Robot=unit.Robot with{Owner=1};battle.Units.Add(unit);}
Check(CpuAssaultAttribution.Force(roster,battle,2,family)==3,"Converted soldiers keep original CPU assault attribution");
Check(battle.Units.All(u=>u.Owner==1 && u.Robot.Owner==1),"Attribution does not rewrite actual ownership");
foreach(var unit in battle.Units)unit.Fallen=true;
Check(CpuAssaultAttribution.Force(roster,battle,2,family)==3,"Casualties still count as committed force");
Check(CpuAssaultAttribution.Force(roster,battle,7,family)==0,"Another target does not inherit force");
battle.GlobalTurn=7;Check(CpuAssaultAttribution.Force(roster,battle,2,family)==0,"Another execution turn is not matched");battle.GlobalTurn=6;
Check(CpuAssaultAttribution.Force(roster,battle,2,new HashSet<int>{3})==0,"Another faction does not inherit force");
var restored=JsonSerializer.Deserialize<CpuAssaultRoster[]>(JsonSerializer.Serialize(roster))!;
Check(CpuAssaultAttribution.Force(restored,battle,2,family)==3,"Independent roster JSON preserves attribution");
Check(CpuAssaultAttribution.Force(Array.Empty<CpuAssaultRoster>(),battle,2,family)==0,"Cancelled or unrecorded orders contribute nothing");
if(args.Length>0){
    var rows=File.ReadLines(args[0]).Select(line=>JsonDocument.Parse(line).RootElement.Clone()).ToArray();
    int baseTurn=rows.First(r=>r.TryGetProperty("step",out var s) && s.GetInt32()==0).GetProperty("turn").GetInt32();
    var order=rows.First(r=>r.TryGetProperty("trace",out var t) && t.GetString()=="order" && r.GetProperty("step").GetInt32()==2 && r.GetProperty("order").GetProperty("Ruler").GetInt32()==20 && r.GetProperty("order").GetProperty("TargetNode").GetInt32()==37).GetProperty("order");
    var ids=order.GetProperty("RobotIds").EnumerateArray().Select(v=>v.GetInt64()).ToArray();
    var trace=rows.First(r=>r.TryGetProperty("trace",out var t) && t.GetString()=="battle" && r.GetProperty("step").GetInt32()==2 && r.GetProperty("Target").GetInt32()==37).GetProperty("battles")[0];
    var replay=new BattleState(1){GlobalTurn=baseTurn+2,Kind=BattleKind.Node};
    foreach(var army in trace.GetProperty("armies").EnumerateArray()){
        int owner=army.GetProperty("Owner").GetInt32(),home=army.GetProperty("HomeNode").GetInt32();
        replay.AddArmy(owner,home,army.GetProperty("ids").EnumerateArray().Select(v=>new Robot(v.GetInt64(),owner,RobotParts.Complete)));
    }
    var actual=CpuAssaultAttribution.Record(baseTurn+2,new[]{new March(20,37,ids.Length,38,Queen:false,RobotIds:ids)},new HashSet<int>{20});
    int force=CpuAssaultAttribution.Force(actual,replay,37,new HashSet<int>{20});
    Check(force==4,"Recorded four-unit real assault remains four after all conversions");
    Check(replay.Units.Concat(replay.Armies.SelectMany(a=>a.Waiting)).Count(u=>u.Owner==20 && u.Robot!=null)==0,"Real trace final ownership count is zero");
    Console.WriteLine("PASS real trace: original force 4, post-conversion count 0, attribution force 4");
}
Console.WriteLine("PASS manual/peaceful exclusion, joint forces, absent IDs, conversions, casualties, faction/target/turn isolation, independent JSON and cancelled orders");
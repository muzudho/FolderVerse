using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Scene(int id,int[] owners,int[] queens,int pieces,int turns)
{
    var b=new BattleState(id){Id=id};
    foreach(int owner in owners)b.Armies.Add(new(){Owner=owner});
    for(int i=0;i<pieces;i++)b.Units.Add(new(){Id=id*100+i,Owner=owners[i%owners.Length],Role=BattleRole.Soldier,Position=new Point(i%10,i/10)});
    foreach(int owner in queens)b.Units.Add(new(){Id=-(owner+1),Owner=owner,Role=BattleRole.Queen,Position=new Point(5,5)});
    for(int t=0;t<turns;t++){b.Turn++;b.Capture(BattlePhase.Choose);b.Capture(BattlePhase.Result);}
    return b;
}
var scenes=new[]{Scene(1,new[]{8,9},new[]{8},20,3),Scene(2,new[]{4,5},new[]{4,5},2,2),Scene(3,new[]{0,7},Array.Empty<int>(),1,1),Scene(4,new[]{1,6},Array.Empty<int>(),1,3),Scene(5,new[]{2,3},Array.Empty<int>(),30,3),Scene(6,new[]{10,11},Array.Empty<int>(),31,2)};
var replay=new RobotBattlePlayback(scenes);
Check(replay.Rounds.Length==3 && replay.Rounds.Select(r=>r.Length).SequenceEqual(new[]{6,5,3}),"Shared rounds include each battlefield once and omit ended boards");
Check(RobotBattlePlayback.Priority(scenes,replay.Rounds[0],new[]{0,1}).SequenceEqual(new[]{2,3,1,0,5,4}),"Player order, queen count and remaining pieces priority");
Check(RobotBattlePlayback.Priority(scenes,replay.Rounds[1],new[]{0,1})[0]==3,"Next player selected after earlier player's board ends");
foreach(var round in replay.Rounds)foreach(var slice in round)
    Check(scenes[slice.Scene].Frames[slice.Start].Turn==scenes[slice.Scene].Frames[slice.End].Turn && slice.End-slice.Start==1,"Every slice is exactly one combat turn");
var ties=new[]{Scene(7,new[]{1,8},new[]{8},1,1),Scene(8,new[]{3,9},new[]{3},1,1),Scene(9,new[]{6,7},Array.Empty<int>(),4,1),Scene(10,new[]{4,5},Array.Empty<int>(),4,1)};
var tieReplay=new RobotBattlePlayback(ties);
Check(RobotBattlePlayback.Priority(ties,tieReplay.Rounds[0],Array.Empty<int>()).SequenceEqual(new[]{1,0,3,2}),"Queen tie uses queen owner, ordinary tie uses minimum army owner");
Check(new RobotBattlePlayback(Array.Empty<BattleState>()).Rounds.Length==0,"Empty battle list");
Console.WriteLine("PASS: shared turns, finished-board removal, multiplayer-ready player order, queen priority and piece-count tie breakers.");

var held=new RobotBattleSlice(0,10,16);double previous=0;for(int i=0;i<100;i++){double current=RobotBattlePlayback.FramePosition(held,i*.03,.25);if(current<previous || current>17)throw new Exception("Retreat time oscillated");previous=current;}if(previous!=17)throw new Exception("Final frame failed to hold");

var waiting=Scene(20,new[]{3,7},Array.Empty<int>(),1,1);waiting.Units[0].Owner=7;waiting.Frames.Clear();waiting.Capture(BattlePhase.Choose);
var other=Scene(21,new[]{0,8},new[]{0},20,1);var pendingScenes=new[]{other,waiting};var pendingReplay=new RobotBattlePlayback(pendingScenes);
Check(RobotBattlePlayback.Priority(pendingScenes,pendingReplay.Rounds[0],new[]{3,0})[0]==1,"Player one is first even when her army is waiting rather than visible in the first frame");
Console.WriteLine("PASS: permanent player-one priority includes participating armies without a currently visible unit.");
var quiet=Scene(50,new[]{0,1},Array.Empty<int>(),2,2);var slices=new RobotBattlePlayback(new[]{quiet}).Rounds;
var quietSlice=slices[0][0];Check(RobotBattlePlayback.MovementOnly(quiet,quietSlice) && Math.Abs(RobotBattlePlayback.SecondsPerFrame(quiet,quietSlice)*(quietSlice.End-quietSlice.Start+1)-.4)<1e-8,"Entire movement-only turn lasts 0.4 seconds");
quiet.Attacks.Add(new(1,5000,null,new(0,0),new(1,0),BattleWeapon.Rifle));Check(!RobotBattlePlayback.MovementOnly(quiet,quietSlice) && RobotBattlePlayback.SecondsPerFrame(quiet,quietSlice)==.25,"Missed attacks still retain full animation speed");quiet.Attacks.Clear();
foreach(var phase in new[]{BattlePhase.Flag,BattlePhase.Fall,BattlePhase.Retreat,BattlePhase.Converted}){quiet.Events.Add(new(1,phase,0,"meaningful"));Check(!RobotBattlePlayback.MovementOnly(quiet,quietSlice),"Meaningful phase remains readable "+phase);quiet.Events.Clear();}
quiet.Persuasions.Add(new(1,-1,5000,0,1,new(0,0),new(1,0),false));Check(!RobotBattlePlayback.MovementOnly(quiet,quietSlice),"Even failed persuasion retains reading time");quiet.Persuasions.Clear();quiet.Finished=true;
Check(RobotBattlePlayback.MovementOnly(quiet,quietSlice) && !RobotBattlePlayback.MovementOnly(quiet,slices[1][0]),"Final result retains time while earlier quiet turns accelerate");
Console.WriteLine("PASS: 0.4-second movement turn, attacks including misses, persuasion, flag/fall/withdrawal/conversion and final-result exclusions.");

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

using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
Check(new BattleRules().SegmentTurns==200,"Default segment 200");
var w=new WorldSetup();w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
var c=w.Campaign.RobotBattles;c.Rules=new(){SegmentTurns=200,NoWillTurns=100000,PersuasionPercent=0};
var b=new BattleState(5,c.Rules){Kind=BattleKind.Node,Defender=1};b.Armies.Add(new(){Owner=0,HomeNode=w.Nodes.Current(0).Id,Side=0});b.Armies.Add(new(){Owner=1,HomeNode=w.Nodes.Current(1).Id,Side=2});
b.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(1,1),Daggers=0,Rifles=0});b.Units.Add(new(){Id=-2,Owner=1,Role=BattleRole.Queen,Position=new(8,8),Daggers=0,Rifles=0});
var e=new RobotEncounter{Id=123,Kind=BattleKind.Node,Target=w.Nodes.Current(1).Id};e.Battles.Add(b);c.Encounters.Add(e);
void Run()=>typeof(RobotCampaign).GetMethod("RunSegment",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(c,new object[]{w,e});
Run();Check(b.Turn==200 && !e.Finished && !b.Finished,"Nondecisive repetition remains ongoing at 200");Run();Check(b.Turn==400 && !e.Finished,"Continuation preserves cumulative battle turn");
c.RequestWithdrawal(w,e.Id,0);Check(!b.Army(0).Withdrawing && e.WithdrawalOwners.Contains(0),"Reservation does not instantly remove units");c.RequestWithdrawal(w,e.Id,0,false);Check(e.WithdrawalOwners.Count==0,"Reservation can be canceled");
try{c.RequestWithdrawal(w,e.Id,1);throw new Exception("Enemy withdrawal allowed");}catch(InvalidOperationException){}
c.Rules=new(){SegmentTurns=1};c.RequestWithdrawal(w,e.Id,0);Run();Check(b.Army(0).Withdrawing && b.Units.Single(u=>u.Owner==0).Retreating,"Withdrawal begins at next segment start");Check(e.WithdrawalOwners.Count==0,"Reservation consumed once");
var shot=new BattleState(1,new(){NoWillTurns=10000,PersuasionPercent=0});shot.Armies.Add(new(){Owner=0,Side=0,Withdrawing=true});shot.Armies.Add(new(){Owner=1,Side=2});
var runner=new BattleUnit{Id=1,Owner=0,Position=new(4,4),Shields=0};var gunner=new BattleUnit{Id=2,Owner=1,Position=new(4,7),Facing=BattleFacing.North};shot.Units.Add(runner);shot.Units.Add(gunner);
BattleTurnResolver.Advance(shot);Check(runner.Retreating && shot.Attacks.Any(a=>a.Target==runner.Id) && runner.Fallen,"Retreating robot can be shot while leaving");
Console.WriteLine("PASS: 200-turn continuation, cumulative state, withdrawal authorization, reservation/cancellation, next-segment retreat and incoming fire.");

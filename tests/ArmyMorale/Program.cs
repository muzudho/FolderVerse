using FolderVerse;
using Microsoft.Xna.Framework;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Board(){var b=new BattleState(42,new(){PersuasionPercent=0}){Kind=BattleKind.Node,Defender=0,FlagStanding=false,LastFlagOwner=1};for(int i=0;i<3;i++){b.Armies.Add(new(){Owner=i,Side=i});b.Units.Add(new(){Id=i+1,Owner=i,Role=BattleRole.Queen,Position=new(2+i*2,2+i*2),Shields=3,Daggers=0,Rifles=0});}return b;}
var b=Board();
void Still(BattleState board)=>BattleTurnResolver.Advance(board,board.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing)));
for(int turn=0;turn<9;turn++){b.Armies[1].NoWillCount=b.Armies[2].NoWillCount=0;Still(b);Check(!b.Armies[0].Withdrawing,"Defender survives first nine flag-down turns");}
Still(b);Check(b.Armies[0].Withdrawing && !b.Armies[1].Withdrawing && !b.Armies[2].Withdrawing && !b.Finished,"Only defender loses will; attackers continue");
for(int turn=0;turn<20 && b.Units[0].Active;turn++){b.Armies[1].NoWillCount=b.Armies[2].NoWillCount=0;BattleTurnResolver.Advance(b);}
Check(b.Units[0].Retreated && !b.Finished,"Defender leaves while rival attackers remain");
b.Armies[2].NoWillCount=9;b.Armies[1].NoWillCount=0;Still(b);Check(b.Armies[2].Withdrawing && !b.Armies[1].Withdrawing,"Second attacker independently withdraws");
for(int turn=0;turn<25 && !b.Finished;turn++){b.Armies[1].NoWillCount=0;BattleTurnResolver.Advance(b);}
Check(b.Finished && b.Winner==1 && b.Units[1].Active,"Last country wins and stays");
b=Board();b.FlagStanding=true;b.Armies[0].NoWillCount=9;Still(b);Check(b.Armies[0].NoWillCount==0,"Raised flag restores defender will");
Console.WriteLine("PASS: ten flag-down turns, independent attacker morale, withdrawals, continuing rival attacks, last country stays, raised flag reset.");
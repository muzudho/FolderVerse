using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Board(BattleKind kind=BattleKind.Edge,int noWill=10){var b=new BattleState(42,new(){NoWillTurns=noWill,PersuasionPercent=0}){Kind=kind,Defender=1};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});return b;}
BattleUnit Unit(BattleState b,long id,int owner,int x,int y,BattleFacing facing){var u=new BattleUnit{Id=id,Owner=owner,Position=new(x,y),Facing=facing,Shields=4,Rifles=3};b.Units.Add(u);return u;}
void Turn(BattleState b,params (BattleUnit Unit,BattleWeapon Weapon,Point Step,BattleFacing? Facing)[] actions)
{
    var orders=b.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));
    foreach(var a in actions)orders[a.Unit.Id]=new(a.Step,a.Facing??a.Unit.Facing,a.Weapon);BattleTurnResolver.Advance(b,orders);
}
var b=Board();BattleUnit a,c;
// Capture immunity requires the exact captor nation, a survivor, and a vacated flag square.
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(!b.Army(0).Withdrawing && b.Army(0).NoWillCount==0,"Captor away from flag retains morale without attacks");
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(b.Army(0).Withdrawing,"Camping on fallen flag has no morale exemption");
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=2;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(b.Army(0).Withdrawing,"Other attacker nation receives no capture exemption");
b=Board(BattleKind.Node);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,5,8,BattleFacing.North);
Unit(b,3,0,0,0,BattleFacing.South);foreach(var role in Enum.GetValues<BattleRole>()){a.Retreating=false;a.Role=role;Check(BattleAi.Choose(b,a).Step!=Point.Zero,"Every attacking role vacates fallen flag before fighting or guarding");}
a.Role=BattleRole.Soldier;a.Position=new(5,4);Check(BattleAi.Path(b,a,new(5,6)) is Point flagDetour && a.Position+flagDetour!=b.Flag,"Attacker path detours around fallen flag");c.Position=b.Flag;Check(BattleAi.Path(b,c,new(5,4))!=null,"Defender can still cross flag to restore it");
b=Board();a=Unit(b,1,0,4,2,BattleFacing.South);c=Unit(b,2,1,4,6,BattleFacing.North);Check(BattleAi.Choose(b,a).Weapon==BattleWeapon.None && a.Rifles>0,"Threatened shooter holds shield despite ammo");a.Shields=0;Check(BattleAi.Choose(b,a).Weapon==BattleWeapon.Rifle,"Without shield shooter fires");a.Rifles=0;Check(BattleAi.Choose(b,a).Weapon!=BattleWeapon.Rifle,"Empty rifle cannot fire");

b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,5,5,BattleFacing.North);Turn(b);Check(b.FlagStanding && b.Army(0).NoWillCount==1,"Restoring flag removes captor exemption immediately");
b=Board(BattleKind.Edge,2);b.FlagStanding=false;b.LastFlagOwner=0;Unit(b,1,0,4,5,BattleFacing.North);Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(b.Army(0).Withdrawing,"Edge battles never gain flag exemption");
for(int seed=0;seed<30;seed++){
 var state=new BattleState(seed){Kind=BattleKind.Node,Defender=0};
 for(int owner=0;owner<2;owner++)state.AddArmy(owner,owner,Enumerable.Range(0,3).Select(i=>new Robot(1+owner*3+i,owner,RobotParts.Complete)),true);
 state.BuildTerrain(BattleTerrain.Fort);state.Deploy();for(int turn=0;turn<1000 && !state.Finished;turn++)BattleTurnResolver.Advance(state);
 Check(state.Finished,"Flag changes still terminate seeded fort battle "+seed);state.Check();
}
Console.WriteLine("PASS: captor-away morale, camper/foreign/edge exclusions, restored flag, all-role flag evacuation, path avoidance, rifle/shield/ammo decisions and 30 terminating fort battles.");

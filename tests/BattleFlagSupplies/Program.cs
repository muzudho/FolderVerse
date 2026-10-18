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
// Captors may occupy or leave the fallen flag.
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(!b.Army(0).Withdrawing && b.Army(0).NoWillCount==0,"Captor away from flag retains morale without attacks");
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(!b.Army(0).Withdrawing,"Camping on fallen flag retains morale");
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=2;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(b.Army(0).Withdrawing,"Other attacker nation receives no capture exemption");
b=Board(BattleKind.Node);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,5,8,BattleFacing.North);
Unit(b,3,0,0,0,BattleFacing.South);a.Role=BattleRole.Queen;Check(BattleAi.Choose(b,a).Step==Point.Zero,"Queen is allowed to guard fallen flag");
a.Role=BattleRole.Soldier;a.Position=new(5,4);Check(BattleAi.Path(b,a,new(5,6))==new Point(0,1),"Attackers may cross fallen flag");c.Position=b.Flag;Check(BattleAi.Path(b,c,new(5,4))!=null,"Defender may restore flag");
b=Board();a=Unit(b,1,0,4,2,BattleFacing.South);c=Unit(b,2,1,4,6,BattleFacing.North);Check(BattleAi.Choose(b,a).Weapon==BattleWeapon.None && a.Rifles>0,"Threatened shooter holds shield despite ammo");a.Shields=0;Check(BattleAi.Choose(b,a).Weapon==BattleWeapon.Rifle,"Without shield shooter fires");a.Rifles=0;Check(BattleAi.Choose(b,a).Weapon!=BattleWeapon.Rifle,"Empty rifle cannot fire");

b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,0,4,5,BattleFacing.North);c=Unit(b,2,1,5,5,BattleFacing.North);Turn(b);Check(b.FlagStanding && b.Army(0).NoWillCount==1,"Restoring flag removes captor exemption immediately");
b=Board(BattleKind.Edge,2);b.FlagStanding=false;b.LastFlagOwner=0;Unit(b,1,0,4,5,BattleFacing.North);Unit(b,2,1,9,9,BattleFacing.North);Turn(b);Turn(b);Check(b.Army(0).Withdrawing,"Edge battles never gain flag exemption");
for(int seed=0;seed<30;seed++){
 var state=new BattleState(seed){Kind=BattleKind.Node,Defender=0};
 for(int owner=0;owner<2;owner++)state.AddArmy(owner,owner,Enumerable.Range(0,3).Select(i=>new Robot(1+owner*3+i,owner,RobotParts.Complete)),true);
 state.BuildTerrain(BattleTerrain.Fort);state.Deploy();for(int turn=0;turn<1000 && !state.Finished;turn++)BattleTurnResolver.Advance(state);
 Check(state.Finished,"Flag changes still terminate seeded fort battle "+seed);state.Check();
}
Console.WriteLine("PASS: capture morale on/away flag, foreign/edge exclusions, restored flag, free flag occupancy and crossing, rifle/shield/ammo decisions and 30 terminating fort battles.");

// A capture starts at zero: ten complete subsequent turns remain available for recovery.
b=Board(BattleKind.Node,1000);a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);
Turn(b);Check(b.FlagCaptureTurns==0 && !b.Finished,"Flag fall starts a fresh ten-turn recovery window");
for(int i=1;i<=9;i++){Turn(b);Check(!b.Finished && b.FlagCaptureTurns==i,"No premature flag capture");}
Turn(b);Check(b.Finished && b.Winner==0 && !c.Fallen && b.Frames[^1].FlagCaptureTurns==10,"Capture forces surrender with defenders still alive and snapshots timer");
b=Board(BattleKind.Node,1000);a=Unit(b,1,0,0,0,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);
for(int i=0;i<49;i++)Turn(b);Check(!b.Finished && b.FlagDefenseTurns==49,"Defense is not won before fifty turns");Turn(b);Check(b.Finished && b.Winner==1 && !a.Fallen,"Fifty continuous flag turns win defense");
b=Board(BattleKind.Node,1000);a=Unit(b,1,0,0,0,BattleFacing.North);c=Unit(b,2,1,9,9,BattleFacing.North);
for(int i=0;i<40;i++)Turn(b);a.Position=b.Flag;Turn(b);Check(b.FlagDefenseTurns==0,"Flag fall resets defense progress");
a.Position=new(4,5);for(int i=0;i<9;i++)Turn(b);c.Position=b.Flag;Turn(b);Check(!b.Finished && b.FlagStanding && b.FlagCaptureTurns==0 && b.FlagDefenseTurns==0,"Last-opportunity recapture cancels surrender and restarts defense");
for(int i=0;i<49;i++)Turn(b);Check(!b.Finished,"Recaptured defense requires a fresh fifty turns");Turn(b);Check(b.Finished && b.Winner==1,"Recapture can lead to defensive victory");
b=Board(BattleKind.Edge,1000);Unit(b,1,0,0,0,BattleFacing.North);Unit(b,2,1,9,9,BattleFacing.North);for(int i=0;i<60;i++)Turn(b);Check(!b.Finished && b.FlagDefenseTurns==0 && b.FlagCaptureTurns==0,"Edge battles do not use flag timers");
b=Board(BattleKind.Node,1000);Unit(b,1,0,0,0,BattleFacing.North);Unit(b,2,1,9,9,BattleFacing.North);for(int i=0;i<20;i++)Turn(b);
var saved=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));Check(saved.FlagDefenseTurns==20 && saved.Rules.FlagCaptureTurns==10 && saved.Rules.FlagDefenseTurns==50,"Save retains flag progress and rules");
var legacy=AutoSaveStore.Capture(b);foreach(var o in legacy.Objects)if(o.Fields!=null){o.Fields.Remove("FlagDefenseTurns");o.Fields.Remove("FlagCaptureTurns");o.Fields.Remove("<FlagDefenseTurns>k__BackingField");o.Fields.Remove("<FlagCaptureTurns>k__BackingField");}
saved=AutoSaveStore.Restore<BattleState>(legacy);Check(saved.FlagDefenseTurns==0 && saved.Rules.FlagDefenseTurns==50 && saved.Rules.FlagCaptureTurns==10,"Legacy save loads new timer defaults");
var profile=Path.Combine(AppContext.BaseDirectory,"FolderVerse-medals-"+Guid.NewGuid()+".json");
try{var medals=new TutorialAchievements(profile);Check(!medals.Has(TutorialAchievements.FirstAttack),"Fresh medals");Check(medals.Award(TutorialAchievements.FirstAttack),"First award persists");Check(!medals.Award(TutorialAchievements.FirstAttack),"Award is idempotent");medals.Award("other-achievement");medals=new(profile);Check(medals.Has(TutorialAchievements.FirstAttack),"Medal survives relaunch");medals.Clear();Check(!new TutorialAchievements(profile).Has(TutorialAchievements.FirstAttack) && new TutorialAchievements(profile).Has("other-achievement"),"Clear removes only tutorial medals and persists");}finally{File.Delete(profile);}
Console.WriteLine("PASS: ten-turn capture, fifty-turn defense, deadline recapture, edge exclusion, replay/save/legacy timers, persistent medals and tutorial-only clearing.");

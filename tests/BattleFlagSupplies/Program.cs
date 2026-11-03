using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Board(BattleKind kind=BattleKind.Edge,int noWill=10){var b=new BattleState(42,new(){NoWillTurns=noWill,PersuasionPercent=0}){BoardSize=10,Flag=new(5,5),Kind=kind,Defender=1};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});return b;}
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

// Victory retreat crosses an occupied choke point without another combat turn.
b=new BattleState(7,new(){FlagDefenseTurns=1,PersuasionPercent=0}){BoardSize=10,Flag=new(5,3),Kind=BattleKind.Node,Defender=1};
b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=1});b.Armies.Add(new(){Owner=2,Side=2});b.Armies.Add(new(){Owner=3,Side=3});b.Allied=(x,y)=>x==y || x is 1 or 3 && y is 1 or 3;
for(int x=0;x<10;x++)for(int y=0;y<10;y++)if(x!=5)b.Walls.Add(new(x,y));
a=Unit(b,1,0,5,4,BattleFacing.North);c=Unit(b,2,1,5,2,BattleFacing.South);var third=Unit(b,3,2,5,5,BattleFacing.North);var ally=Unit(b,4,3,5,8,BattleFacing.North);var fallen=Unit(b,5,0,5,1,BattleFacing.South);fallen.Fallen=true;
b.Army(0).Waiting.Enqueue(new(){Id=6,Owner=0});
Turn(b);Check(b.Finished && b.Winner==1 && b.Turn==1,"Defense remains final with no extra combat turns");
Check(a.Retreated && third.Retreated && BattleState.HomeExit(a.Position,0,b.BoardSize) && BattleState.HomeExit(third.Position,2,b.BoardSize),"All surviving hostile nations exit toward home");
Check(!c.Retreated && !ally.Retreated && c.Position==new Point(5,2) && ally.Position==new Point(5,8),"Defender and ally remain stationary to celebrate");
Check(b.Frames.Any(f=>f.Phase==BattlePhase.DefenseRetreat && f.Units.Any(u=>u.Id==a.Id && !u.Retreated && u.Position==c.Position)),"Attacker passes through foreign defender at single corridor");
Check(fallen.Fallen && !fallen.Retreated && b.Units.Single(u=>u.Id==6).Retreated && b.Army(0).Waiting.Count==0,"Fallen pieces stay; undeployed reserves return directly");
Check(b.Attacks.Count==0 && b.Persuasions.Count==0 && a.Shields==4 && a.Rifles==3,"Victory retreat consumes no supplies and causes no combat");
var playback=new RobotBattlePlayback(new[]{b});Check(playback.Rounds.Length==1 && RobotBattlePlayback.SecondsPerFrame(b,playback.Rounds[0][0])==.25,"Retreat gets visible movement pacing even without attacks");
b.Check();saved=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));Check(saved.Units.Single(u=>u.Id==a.Id).Retreated,"Returned unit state persists through save");
Console.WriteLine("PASS: defensive victory retreat, foreign-unit pass-through, multi-attacker home exits, defender allies, fallen/reserve handling, no extra combat, visible replay and save.");

if(args.Contains("--ui")){using var game=new DefenseRetreatGame(b);game.Run();}
sealed class DefenseRetreatGame(BattleState battle):Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)typeof(Game1).GetField("_setup",F)!.GetValue(this)!;w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);battle.TargetNode=w.Nodes.Current(1).Id;
  Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,"Battle"));Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotPlaying",false);Set("_robotSummary",false);Set("_lastCutInPhase","戦闘フェーズ");Set("_cutInStarted",-10d);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  int start=battle.Frames.FindIndex(f=>f.Phase==BattlePhase.DefenseRetreat);Set("_battleAge",(start+frame+0.3)*.25);base.Draw(t);
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"defense-retreat-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==6){Console.WriteLine("PASS: defense celebration and retreat frames rendered.");Exit();}
 }
}

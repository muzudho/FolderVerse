using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static BattleState Board(BattleKind kind=BattleKind.Edge){var b=new BattleState(42,new(){NoWillTurns=100,PersuasionPercent=0}){Kind=kind,Defender=1};b.Armies.Add(new(){Owner=0,HomeNode=0,Side=0});b.Armies.Add(new(){Owner=1,HomeNode=1,Side=2});return b;}
static BattleUnit Unit(BattleState b,long id,int owner,int x,int y,BattleFacing facing=BattleFacing.South){var u=new BattleUnit{Id=id,Owner=owner,Position=new(x,y),Facing=facing,Robot=new(id,owner,RobotParts.Complete)};b.Units.Add(u);return u;}
static void Turn(BattleState b,params (BattleUnit Unit,Point Step,BattleFacing Face,BattleWeapon Weapon)[] changes){var a=b.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));foreach(var c in changes)a[c.Unit.Id]=new(c.Step,c.Face,c.Weapon);BattleTurnResolver.Advance(b,a);}
var b=Board(BattleKind.Node);var north=Unit(b,10001,0,5,4);var east=Unit(b,10002,0,6,5);var west=Unit(b,10003,0,4,5);var enemy=Unit(b,10004,1,9,9);var departed=Unit(b,10005,1,0,0);departed.Retreated=true;
var waiting=new BattleUnit{Id=10006,Owner=1,Robot=new(10006,1,RobotParts.Complete)};b.Army(1).Waiting.Enqueue(waiting);
Turn(b,(north,new(0,1),BattleFacing.South,BattleWeapon.None),(east,new(-1,0),BattleFacing.West,BattleWeapon.None),(west,new(1,0),BattleFacing.East,BattleWeapon.None));
Check(b.FlagStanding && north.Position==new Point(5,4) && east.Position==new Point(6,5) && west.Position==new Point(4,5),"Without card flag collision remains intact");
Check(new[]{north,east,west,enemy}.All(u=>(u.WitnessedLessons&BattleLesson.FlagCourtesy)!=0),"All robots on the board witness collision, including enemies");Check(departed.WitnessedLessons==0 && waiting.WitnessedLessons==0,"No qualification for departed or waiting/reinforcing robots");
var ledger=new BattleLessons();north.Fallen=true;ledger.BringHome(north);Check(ledger.Pending.Count==0,"Fallen witness cannot teach");east.Retreated=true;ledger.BringHome(east);ledger.BringHome(west);ledger.BringHome(enemy);Check(ledger.Pending.Count==2 && ledger.Owned.All(x=>x==0),"Surviving retreat/winner teaches each country once, pending research");Check(ledger.Research().Length==2 && ledger.Research().Length==0,"Research grants once");

// Reproduce the reported case using normal AI and no defending robots.
foreach(bool trained in new[]{false,true}){
 var emptyFort=new BattleState(42,new(){NoWillTurns=2,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1};emptyFort.Armies.Add(new(){Owner=0,HomeNode=0});emptyFort.Armies.Add(new(){Owner=1,HomeNode=1,Side=2});
 Unit(emptyFort,10101,0,5,4);Unit(emptyFort,10102,0,6,5);Unit(emptyFort,10103,0,4,5);if(trained)emptyFort.Lessons[0]=BattleLesson.FlagCourtesy;
 for(int i=0;i<30 && !emptyFort.Finished;i++)BattleTurnResolver.Advance(emptyFort);
 Check(emptyFort.Finished,"Empty-fort AI battle terminates");
 if(trained)Check(emptyFort.Winner==0 && !emptyFort.FlagStanding,"Trained AI enters flag and wins");
 else{Check(emptyFort.FlagStanding && emptyFort.Units.All(u=>u.Retreated),"Untrained AI collides and physically retreats");var learned=new BattleLessons();foreach(var u in emptyFort.Units)learned.BringHome(u);Check(learned.Research().Single().Lesson==BattleLesson.FlagCourtesy,"Retreating eyewitnesses teach flag lesson");}
}
for(int first=0;first<4;first++){
 b=Board(BattleKind.Node);b.Lessons[0]=BattleLesson.FlagCourtesy;
 var steps=new[]{new Point(0,-1),new Point(1,0),new Point(0,1),new Point(-1,0)};
 var entrants=steps.Select((p,i)=>Unit(b,200+i,0,5+p.X,5+p.Y)).Skip(first).ToArray();b.Units.RemoveAll(u=>!entrants.Contains(u));enemy=Unit(b,210,1,9,9);
 Turn(b,entrants.Select(u=>(u,b.Flag-u.Position,BattleAi.Face(b.Flag-u.Position),BattleWeapon.None)).ToArray());
 Check(entrants[0].Position==b.Flag && entrants.Skip(1).All(u=>u.Position!=b.Flag),"N/E/S/W priority, case "+first);
 Check(b.Units.All(u=>u.WitnessedLessons==0),"Yield is not a rollback lesson");
}
// Ownership is per country; an untrained ally still follows its old routine.
b=Board(BattleKind.Node);b.Armies.Add(new(){Owner=2,HomeNode=2,Side=1});b.Allied=(a,c)=>a==c || a!=1 && c!=1;b.Lessons[0]=BattleLesson.FlagCourtesy;
north=Unit(b,301,0,5,4);east=Unit(b,302,2,6,5);enemy=Unit(b,303,1,9,9);
Turn(b,(north,new(0,1),BattleFacing.South,BattleWeapon.None),(east,new(-1,0),BattleFacing.West,BattleWeapon.None));Check(north.Position!=b.Flag && east.Position!=b.Flag,"Untrained allied country can still collide");

// Retreat route is unchanged, but the trained robot faces backwards and can use its shield.
foreach(bool trained in new[]{false,true}){
 b=Board();var runner=Unit(b,401,0,4,5);runner.Retreating=true;var shooter=Unit(b,402,1,4,8,BattleFacing.North);var witness=Unit(b,403,1,8,8);if(trained)b.Lessons[0]=BattleLesson.RetreatBackwards;
 Turn(b,(shooter,Point.Zero,BattleFacing.North,BattleWeapon.Rifle));Check(runner.Position==new Point(4,4),"Retreat follows original exit route");
 Check(runner.Facing==(trained?BattleFacing.South:BattleFacing.North) && runner.Fallen!=trained,"Backward retreat shields back shot");
 Check(((witness.WitnessedLessons&BattleLesson.RetreatBackwards)!=0)!=trained,"Only failed back-shot retreat grants lesson");
}

b=Board();var gun=Unit(b,501,0,3,5,BattleFacing.East);var friend=Unit(b,502,0,5,5);enemy=Unit(b,503,1,8,5,BattleFacing.West);
Turn(b,(gun,Point.Zero,BattleFacing.East,BattleWeapon.Rifle));Check(friend.Fallen && (enemy.WitnessedLessons&BattleLesson.FireDiscipline)!=0,"Friendly hit gives both sides qualification");
b=Board();gun=Unit(b,511,0,3,5,BattleFacing.East);friend=Unit(b,512,0,4,5);enemy=Unit(b,513,1,8,5,BattleFacing.West);b.Lessons[0]=BattleLesson.FireDiscipline;
Turn(b,(gun,Point.Zero,BattleFacing.East,BattleWeapon.Rifle));Check(b.Attacks.Count==0 && gun.Rifles==4,"Card prevents even adjacent-line friendly shooting and ammo use");
friend.Position=new(5,4);friend.Facing=BattleFacing.South;Check(!BattleLessons.SafeShot(b,gun,BattleFacing.East),"North neighbor looking into line blocks shot");friend.Facing=BattleFacing.North;Check(BattleLessons.SafeShot(b,gun,BattleFacing.East),"North neighbor looking away permits shot");friend.Position=new(5,6);friend.Facing=BattleFacing.North;Check(!BattleLessons.SafeShot(b,gun,BattleFacing.East),"South neighbor looking into line blocks shot");friend.Facing=BattleFacing.East;Check(BattleLessons.SafeShot(b,gun,BattleFacing.East),"Parallel neighbor permits shot");
friend.Position=new(5,4);friend.Facing=BattleFacing.North;Turn(b,(gun,Point.Zero,BattleFacing.East,BattleWeapon.Rifle),(friend,new(0,1),BattleFacing.South,BattleWeapon.None));Check(friend.Fallen && b.Attacks.Any(a=>a.Target==friend.Id),"A robot can still jump into a shot after the decision");

// Qualifications survive continuation/save and belong to the final owner after persuasion.
var world=new WorldSetup();world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);
b=Board();var survivor=Unit(b,30001,0,2,2);survivor.WitnessedLessons=BattleLesson.FlagCourtesy|BattleLesson.FireDiscipline;survivor.Retreated=true;survivor.Owner=1;
var restored=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));Check(restored.Units[0].WitnessedLessons==survivor.WitnessedLessons,"Continuation eligibility persists through save");
var oldBattle=AutoSaveStore.Capture(b);foreach(var o in oldBattle.Objects)if(o.Fields!=null){o.Fields.Remove("WitnessedLessons");o.Fields.Remove("Lessons");}var oldBoard=AutoSaveStore.Restore<BattleState>(oldBattle);Check(oldBoard.Units.All(u=>u.WitnessedLessons==0) && oldBoard.Lessons.All(l=>l==0),"Legacy combat state receives empty qualifications and card snapshot");
var encounter=new RobotEncounter{Kind=BattleKind.Edge,Source=0,Target=1};typeof(RobotCampaign).GetMethod("ReturnUnits",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(world.Campaign.RobotBattles,new object[]{world,encounter,b,new[]{survivor}});
Check(world.Lessons.Pending.Count==2 && world.Lessons.Pending.All(a=>a.Owner==1),"Campaign return delivers to final owner");
world.Lessons.Research();world.Lessons.Pending.Add(new(0,BattleLesson.RetreatBackwards));var save=AutoSaveStore.Capture(world);var load=AutoSaveStore.Restore<WorldSetup>(save);Check(load.Lessons.Owned[1]==(BattleLesson.FlagCourtesy|BattleLesson.FireDiscipline) && load.Lessons.Pending.Count==1,"Country cards and pending research persist");
foreach(var o in save.Objects)if(o.Fields!=null){o.Fields.Remove("<Lessons>k__BackingField");o.Fields.Remove("WitnessedLessons");o.Fields.Remove("Lessons");}
load=AutoSaveStore.Restore<WorldSetup>(save);Check(load.Lessons.Owned.All(x=>x==0) && load.Lessons.Pending.Count==0,"Old saves default to no cards");
Console.WriteLine("PASS: flag rollback and priority, per-country effects, failed/backward retreat, friendly fire and adjacent lanes, late entry into shots, survivor delivery, pending/owned saves and legacy defaults.");

foreach(bool trained in new[]{false,true}){
 var sideBoard=Board();var victim=Unit(sideBoard,20001,0,5,5,BattleFacing.North);var sideGun=Unit(sideBoard,20002,1,5,1,BattleFacing.South);var knife=Unit(sideBoard,20003,1,6,5,BattleFacing.West);knife.Rifles=0;var witness=Unit(sideBoard,20004,0,0,8);if(trained)sideBoard.Lessons[0]=BattleLesson.LeaveFiringLine;
 Turn(sideBoard,(victim,Point.Zero,BattleFacing.East,BattleWeapon.Dagger),(sideGun,Point.Zero,BattleFacing.South,BattleWeapon.Rifle));
 if(trained)Check(!victim.Fallen && victim.Position==new Point(4,5) && victim.Facing==BattleFacing.North && !sideBoard.Attacks.Any(a=>a.Attacker==victim.Id),"Learned robot leaves firing line before sideways dagger attack");
 else{Check(victim.Fallen && (witness.WitnessedLessons&BattleLesson.LeaveFiringLine)!=0 && (sideGun.WitnessedLessons&BattleLesson.LeaveFiringLine)!=0,"Sideways dagger under frontal rifle qualifies both armies' witnesses");var lessons=new BattleLessons();lessons.BringHome(victim);Check(lessons.Pending.Count==0,"Fallen victim does not teach");lessons.BringHome(witness);Check(lessons.Research().Any(a=>a.Lesson==BattleLesson.LeaveFiringLine),"Surviving witness brings home lesson four");var fourthRestored=AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(lessons));Check((fourthRestored.Owned[0]&BattleLesson.LeaveFiringLine)!=0,"Fourth card survives save");}
}
foreach(bool rear in new[]{false,true}){
 var sideBoard=Board();var victim=Unit(sideBoard,21001,0,5,5,rear?BattleFacing.South:BattleFacing.North);var sideGun=Unit(sideBoard,21002,1,5,1,BattleFacing.South);var knife=Unit(sideBoard,21003,1,6,5);var witness=Unit(sideBoard,21004,0,0,8);
 Turn(sideBoard,(victim,Point.Zero,rear?BattleFacing.East:BattleFacing.North,rear?BattleWeapon.Dagger:BattleWeapon.None),(sideGun,Point.Zero,BattleFacing.South,BattleWeapon.Rifle));Check((witness.WitnessedLessons&BattleLesson.LeaveFiringLine)==0,"Rear exposure or no dagger is excluded from lesson four");
}
var trapped=Board();var trappedUnit=Unit(trapped,22001,0,5,5,BattleFacing.North);Unit(trapped,22002,1,5,1,BattleFacing.South);Unit(trapped,22003,1,6,5);Unit(trapped,22004,0,4,5);trapped.Lessons[0]=BattleLesson.LeaveFiringLine;var safe=BattleAi.Choose(trapped,trappedUnit);Check(safe.Step==Point.Zero && safe.Weapon==BattleWeapon.None && safe.Facing==BattleFacing.North,"No escape: keep frontal shield instead of sideways dagger");
Console.WriteLine("PASS: fourth lesson actual sideways hit, rear/no-dagger exclusions, both armies, survivor/save, sidestep and blocked escape.");

var delayed=Board();var delayedVictim=Unit(delayed,23001,0,5,5,BattleFacing.North);var delayedGun=Unit(delayed,23002,1,5,1,BattleFacing.South);var delayedKnife=Unit(delayed,23003,1,6,5);delayedKnife.Rifles=0;var delayedWitness=Unit(delayed,23004,0,0,8);
Turn(delayed,(delayedVictim,Point.Zero,BattleFacing.East,BattleWeapon.Dagger));Check((delayedWitness.WitnessedLessons&BattleLesson.LeaveFiringLine)==0,"Merely swinging in a line without an actual rifle hit does not award lesson four");

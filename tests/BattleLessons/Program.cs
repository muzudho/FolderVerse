using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static BattleState Board(BattleKind kind=BattleKind.Edge){var b=new BattleState(42,new(){NoWillTurns=100,PersuasionPercent=0}){Kind=kind,Defender=1,BoardSize=10,Flag=new(5,5)};b.Armies.Add(new(){Owner=0,HomeNode=0,Side=0});b.Armies.Add(new(){Owner=1,HomeNode=1,Side=2});return b;}
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
 var emptyFort=new BattleState(42,new(){NoWillTurns=2,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1,BoardSize=10,Flag=new(5,5)};emptyFort.Armies.Add(new(){Owner=0,HomeNode=0});emptyFort.Armies.Add(new(){Owner=1,HomeNode=1,Side=2});
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

// Lesson five relaxes only the skipped adjacent cell, with a forward-facing shield bearer.
b=Board();gun=Unit(b,51001,0,3,5,BattleFacing.East);friend=Unit(b,51002,0,4,5,BattleFacing.East);friend.Shields=3;enemy=Unit(b,51003,1,8,5,BattleFacing.West);
b.Lessons[0]=BattleLesson.FireDiscipline;
Turn(b,(gun,Point.Zero,BattleFacing.East,BattleWeapon.Rifle));
Check(b.Attacks.Count==0 && (gun.WitnessedLessons&BattleLesson.ShieldSupport)!=0,"Withheld supporting shot teaches lesson five");
b.Lessons[0]|=BattleLesson.ShieldSupport;
Check(BattleLessons.SafeShot(b,gun,BattleFacing.East),"Trained shooter can skip adjacent shield bearer");
Turn(b,(gun,Point.Zero,BattleFacing.East,BattleWeapon.Rifle));
Check(b.Attacks.Any(a=>a.Attacker==gun.Id && a.Target==enemy.Id) && !friend.Fallen,"Support shot reaches enemy beyond shield bearer");
friend.Position=new(5,5);Check(!BattleLessons.SafeShot(b,gun,BattleFacing.East),"Friend two cells ahead still blocks");
friend.Position=new(4,5);friend.Shields=0;Check(!BattleLessons.SafeShot(b,gun,BattleFacing.East),"Adjacent friend without shield remains protected by discipline");

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

foreach(bool trained in new[]{false,true}){
 var duel=Board();duel.BoardSize=13;var attacker=Unit(duel,60001,0,3,5,BattleFacing.East);var defender=Unit(duel,60002,1,6,5,BattleFacing.West);
 if(trained)duel.Lessons[0]=BattleLesson.CloseForDagger;
 var action=BattleAi.Choose(duel,attacker);
 Check(action.Step==(trained?new Point(1,0):Point.Zero) && action.Weapon==BattleWeapon.None && action.Facing==BattleFacing.East,"Lesson six advances into shielded standoff");
 BattleTurnResolver.Advance(duel);
 if(!trained){Check((attacker.WitnessedLessons&BattleLesson.CloseForDagger)!=0,"Unresolved standoff is witnessed");var learned=new BattleLessons();attacker.Retreated=true;learned.BringHome(attacker);Check(learned.Research().Any(a=>a.Lesson==BattleLesson.CloseForDagger),"Survivor brings sixth lesson home");}
 else{BattleTurnResolver.Advance(duel);action=BattleAi.Choose(duel,attacker);Check(action.Weapon==BattleWeapon.Dagger,"Approach ends with dagger at adjacent range");}
}
var unarmedDuel=Board();var noKnife=Unit(unarmedDuel,60101,0,3,5,BattleFacing.East);Unit(unarmedDuel,60102,1,6,5,BattleFacing.West);noKnife.Daggers=0;unarmedDuel.Lessons[0]=BattleLesson.CloseForDagger;
Check(BattleAi.Choose(unarmedDuel,noKnife).Step==Point.Zero,"Lesson six cannot charge without a dagger");
Console.WriteLine("PASS: lesson six standoff, survivor research, frontal approach, adjacent dagger and no-dagger exclusion.");

// Lesson seven: opposed movement into one empty cell teaches both sides about tempo.
BattleState Tempo(){var t=new BattleState(7,new(){NoWillTurns=100,PersuasionPercent=0}){BoardSize=10,Kind=BattleKind.Node,Defender=1,Flag=new(8,8)};t.Armies.Add(new(){Owner=0,Side=0});t.Armies.Add(new(){Owner=1,Side=2});return t;}
var collision=Tempo();var advance=Unit(collision,70001,0,3,5,BattleFacing.East);var guard=Unit(collision,70002,1,5,5,BattleFacing.West);
Turn(collision,(advance,new(1,0),BattleFacing.East,BattleWeapon.None),(guard,new(-1,0),BattleFacing.West,BattleWeapon.None));
Check(advance.Position==new Point(3,5) && guard.Position==new Point(5,5) && new[]{advance,guard}.All(u=>(u.WitnessedLessons&BattleLesson.CollisionTempo)!=0),"Opposing one-gap collision rolls back and is witnessed by both sides");
var tempoResearch=new BattleLessons();advance.Retreated=true;tempoResearch.BringHome(advance);Check(tempoResearch.Research().Any(a=>a.Lesson==BattleLesson.CollisionTempo),"Surviving collision witness brings the new card home");
foreach(bool wall in new[]{false,true}){
 var excluded=Tempo();var x=Unit(excluded,70101,0,3,5);var y=Unit(excluded,70102,wall?1:0,5,5);
 if(wall)excluded.Walls.Add(new(4,5));Turn(excluded,(x,new(1,0),BattleFacing.East,BattleWeapon.None),(y,new(-1,0),BattleFacing.West,BattleWeapon.None));
 Check((x.WitnessedLessons&BattleLesson.CollisionTempo)==0,"Allied collision or impassable wall does not teach opposed empty-cell collision");
}
var trainedTempo=Tempo();var knifeWait=Unit(trainedTempo,70201,0,3,5,BattleFacing.East);var timeGuard=Unit(trainedTempo,70202,1,5,5,BattleFacing.West);
trainedTempo.Lessons[0]=trainedTempo.Lessons[1]=BattleLesson.CollisionTempo;timeGuard.Shields=0;
var attackChoice=BattleAi.Choose(trainedTempo,knifeWait);var defenseChoice=BattleAi.Choose(trainedTempo,timeGuard);
Check(attackChoice.Step==Point.Zero && attackChoice.Weapon==BattleWeapon.Dagger && defenseChoice.Step==new Point(-1,0),"Trained attacker waits with dagger while defender advances into one-gap cell");
BattleTurnResolver.Advance(trainedTempo);Check(knifeWait.Position==new Point(3,5) && timeGuard.Position==new Point(4,5) && timeGuard.Fallen,"Waiting dagger actually catches the advancing unshielded defender");
var rangedTempo=Tempo();var ranged=Unit(rangedTempo,70301,0,3,5,BattleFacing.East);var rangedGuard=Unit(rangedTempo,70302,1,6,5,BattleFacing.West);rangedGuard.Shields=0;rangedTempo.Lessons[0]=BattleLesson.CollisionTempo;
var shot=BattleLessons.CollisionTempoAction(rangedTempo,ranged,new(new(1,0),BattleFacing.East));Check(shot.Step==Point.Zero && shot.Weapon==BattleWeapon.Rifle,"Two empty cells select rifle instead of advancing");
Turn(rangedTempo,(ranged,new(1,0),BattleFacing.East,BattleWeapon.None));Check(ranged.Position==new Point(3,5) && rangedGuard.Fallen && ranged.Rifles==3,"New ranged tactic fires and consumes ammunition");
var savedTempo=AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(tempoResearch));Check((savedTempo.Owned[0]&BattleLesson.CollisionTempo)!=0,"Seventh card survives save/load");
Console.WriteLine("PASS: opposed empty-cell rollback, friendly/wall exclusions, survivor research/save, defending advance, attacking dagger ambush and two-gap rifle.");

var edgeTempo=Tempo();edgeTempo.Kind=BattleKind.Edge;var edgeApproach=Unit(edgeTempo,70401,0,3,5,BattleFacing.East);Unit(edgeTempo,70402,1,5,5,BattleFacing.West);edgeTempo.Lessons[0]=BattleLesson.CollisionTempo;
var originalEdgeAction=new BattleAction(new(1,0),BattleFacing.East);Check(BattleLessons.CollisionTempoAction(edgeTempo,edgeApproach,originalEdgeAction)==originalEdgeAction,"No defending faction on edge boards: avoid making both attackers wait forever");

// Lesson eight: an empty-handed adjacent standoff wastes the attacking side's time.
var emptyFront=Tempo();var stalled=Unit(emptyFront,80001,0,3,5,BattleFacing.East);var stalledGuard=Unit(emptyFront,80002,1,4,5,BattleFacing.West);stalled.Daggers=stalledGuard.Daggers=0;
Turn(emptyFront);Check(new[]{stalled,stalledGuard}.All(u=>(u.WitnessedLessons&BattleLesson.RelieveFront)!=0),"Adjacent empty-dagger standoff is witnessed by both sides");
var reliefResearch=new BattleLessons();stalled.Retreated=true;reliefResearch.BringHome(stalled);Check(reliefResearch.Research().Any(a=>a.Lesson==BattleLesson.RelieveFront),"Survivor brings eighth lesson home");
Check((AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(reliefResearch)).Owned[0]&BattleLesson.RelieveFront)!=0,"Eighth lesson survives save/load");
foreach(bool moving in new[]{false,true}){
 var noStall=Tempo();var x=Unit(noStall,80101,0,3,5,BattleFacing.East);var y=Unit(noStall,80102,1,4,5,BattleFacing.West);x.Daggers=0;
 Turn(noStall,(x,moving?new Point(-1,0):Point.Zero,BattleFacing.East,BattleWeapon.None),(y,Point.Zero,BattleFacing.West,moving?BattleWeapon.None:BattleWeapon.Dagger));
 Check((x.WitnessedLessons&BattleLesson.RelieveFront)==0,"Moving apart or actually hitting the opponent is not an idle standoff");
}
var support=Tempo();var reliefShooter=Unit(support,80201,0,3,5,BattleFacing.East);var closeEnemy=Unit(support,80202,1,4,5,BattleFacing.West);var screen=Unit(support,80203,0,4,6,BattleFacing.East);var screenEnemy=Unit(support,80204,1,5,6,BattleFacing.West);reliefShooter.Daggers=0;reliefShooter.Rifles=2;screen.Shields=0;screenEnemy.Shields=0;support.Lessons[0]=BattleLesson.RelieveFront|BattleLesson.FireDiscipline;
var takeRear=BattleAi.Choose(support,reliefShooter);Check(takeRear.Step==new Point(0,1) && takeRear.Weapon==BattleWeapon.None,"Short-of-daggers attacker moves behind a fighting ally");
Turn(support,(reliefShooter,takeRear.Step,takeRear.Facing,takeRear.Weapon));var supportShot=BattleAi.Choose(support,reliefShooter);Check(supportShot.Weapon==BattleWeapon.Rifle && BattleLessons.SafeShot(support,reliefShooter,BattleFacing.East),"Eighth lesson permits support past the adjacent same-facing ally even without her shield");
Turn(support,(reliefShooter,supportShot.Step,supportShot.Facing,supportShot.Weapon));Check(screenEnemy.Fallen && !screen.Fallen && reliefShooter.Rifles==1,"Support shot actually passes the screen, hits enemy and spends ammunition");
var obstruction=Tempo();var obGun=Unit(obstruction,80301,0,2,5,BattleFacing.East);Unit(obstruction,80302,0,3,5,BattleFacing.East);Unit(obstruction,80303,0,4,5,BattleFacing.East);Unit(obstruction,80304,1,6,5,BattleFacing.West);obGun.Daggers=0;obstruction.Lessons[0]=BattleLesson.RelieveFront|BattleLesson.FireDiscipline;
Check(!BattleLessons.SafeShot(obstruction,obGun,BattleFacing.East),"A second friendly unit farther down the line still prohibits fire");
var closedGate=Tempo();var gateGun=Unit(closedGate,80401,0,2,5,BattleFacing.East);Unit(closedGate,80402,0,3,5,BattleFacing.East);Unit(closedGate,80403,1,5,5,BattleFacing.West);gateGun.Daggers=0;closedGate.Lessons[0]=BattleLesson.RelieveFront;closedGate.Fortifications.Add(new(new(4,5),FortificationKind.Gate,1,BattleFacing.West));
Check(BattleAi.FirstTarget(closedGate,gateGun,BattleFacing.East)==null && BattleAi.Choose(closedGate,gateGun).Weapon!=BattleWeapon.Rifle,"Closed gate cannot become an imaginary support firing line");
var backOff=Tempo();var aloneGun=Unit(backOff,80501,0,3,5,BattleFacing.East);Unit(backOff,80502,1,4,5,BattleFacing.West);aloneGun.Daggers=0;backOff.Lessons[0]=BattleLesson.RelieveFront;Check(BattleAi.Choose(backOff,aloneGun).Step==new Point(-1,0),"No support position: back away while facing opponent");
var replace=Tempo();var empty=Unit(replace,80601,0,3,5,BattleFacing.East);Unit(replace,80602,1,4,5,BattleFacing.West);Unit(replace,80603,0,1,8,BattleFacing.East);empty.Daggers=empty.Rifles=0;replace.Lessons[0]=BattleLesson.RelieveFront;replace.Army(0).Limit=2;replace.Army(0).Waiting.Enqueue(new(){Id=80604,Owner=0,Robot=new(80604,0,RobotParts.Complete)});
BattleAi.Choose(replace,empty);Check(empty.Retreating && !replace.Army(0).Withdrawing,"Only unarmed individual yields the front, not whole army");for(int t=0;t<9;t++)Turn(replace);
Check(empty.Retreated && replace.Units.Any(u=>u.Id==80604 && u.Active),"Individual withdrawal actually releases a reinforcement slot");
var defendHold=Tempo();var defenderOnly=Unit(defendHold,80701,1,3,5,BattleFacing.East);Unit(defendHold,80702,0,4,5,BattleFacing.West);defenderOnly.Daggers=defenderOnly.Rifles=0;defendHold.Lessons[1]=BattleLesson.RelieveFront;
var hold=new BattleAction(Point.Zero,BattleFacing.East);Check(BattleLessons.RelieveFrontAction(defendHold,defenderOnly,hold)==hold && !defenderOnly.Retreating,"Defender's useful time-wasting remains unchanged");
Console.WriteLine("PASS: eighth-lesson standoff/research/save, real move-to-support and shot, farther-friendly/gate exclusions, fallback backstep, individual relief/reinforcement and unchanged defender.");
var capturedHold=Tempo();capturedHold.Flag=new(3,5);capturedHold.FlagStanding=false;capturedHold.LastFlagOwner=0;var flagHolder=Unit(capturedHold,80801,0,3,5,BattleFacing.East);Unit(capturedHold,80802,1,4,5,BattleFacing.West);flagHolder.Daggers=flagHolder.Rifles=0;capturedHold.Lessons[0]=BattleLesson.RelieveFront;
Check(BattleLessons.RelieveFrontAction(capturedHold,flagHolder,hold)==hold && !flagHolder.Retreating,"Keeping a captured flag remains useful despite empty weapons");
var productiveGate=Tempo();var gateWorker=Unit(productiveGate,80901,0,3,5,BattleFacing.East);var gateOccupant=Unit(productiveGate,80902,1,4,5,BattleFacing.West);gateWorker.Daggers=gateOccupant.Daggers=0;productiveGate.Fortifications.Add(new(new(4,5),FortificationKind.Gate,1,BattleFacing.West));
Turn(productiveGate,(gateWorker,Point.Zero,BattleFacing.East,BattleWeapon.Rifle));Check(productiveGate.GateAt(new(4,5))?.Health==1 && (gateWorker.WitnessedLessons&BattleLesson.RelieveFront)==0,"Making progress by damaging a gate is not an idle standoff");

// Lesson nine: the enemy sidesteps and an allied pursuer enters a waiting dagger.
foreach(bool trained in new[]{false,true}){
 var ambush=Tempo();var stabber=Unit(ambush,90001,0,3,5,BattleFacing.East);var runner=Unit(ambush,90002,1,4,5,BattleFacing.North);var pursuer=Unit(ambush,90003,0,4,6,BattleFacing.North);
 if(trained)ambush.Lessons[0]=BattleLesson.WaitingDaggerSafety;
 Turn(ambush,(stabber,Point.Zero,BattleFacing.East,BattleWeapon.Dagger),(runner,new(0,-1),BattleFacing.North,BattleWeapon.None),(pursuer,new(0,-1),BattleFacing.North,BattleWeapon.None));
 Check(runner.Position==new Point(4,4) && pursuer.Position==new Point(4,5),"Enemy sidesteps and ally actually pursues into the former enemy cell");
 if(!trained){
  Check(pursuer.Fallen && stabber.Daggers==2 && (stabber.WitnessedLessons&BattleLesson.WaitingDaggerSafety)!=0,"Untrained waiting dagger hits allied pursuer and teaches ninth lesson");
  var ninth=new BattleLessons();ninth.BringHome(pursuer);Check(ninth.Pending.Count==0,"Fallen friendly victim cannot bring lesson home");stabber.Retreated=true;ninth.BringHome(stabber);Check(ninth.Research().Any(a=>a.Lesson==BattleLesson.WaitingDaggerSafety),"Surviving witness researches ninth lesson");
  Check((AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(ninth)).Owned[0]&BattleLesson.WaitingDaggerSafety)!=0,"Ninth lesson persists through save/load");
 }else Check(!pursuer.Fallen && stabber.Daggers==3 && !ambush.Attacks.Any(a=>a.Attacker==stabber.Id),"Trained robot refrains before movement and protects pursuer without spending dagger");
}
var careful=Tempo();var carefulStab=Unit(careful,90101,0,3,5,BattleFacing.East);var possibleFriend=Unit(careful,90102,0,4,6,BattleFacing.South);careful.Lessons[0]=BattleLesson.WaitingDaggerSafety;
var waitingSlash=new BattleAction(Point.Zero,BattleFacing.East,BattleWeapon.Dagger);
Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash).Weapon==BattleWeapon.None,"Possible backward entry by a friend is enough; no future-action knowledge");
possibleFriend.Position=new(4,7);Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash)==waitingSlash,"Friend two steps away cannot enter this turn, so dagger remains available");
possibleFriend.Position=new(4,6);careful.Walls.Add(new(4,5));Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash)==waitingSlash,"Impassable terrain does not create a false friend-entry risk");careful.Walls.Clear();
careful.Fortifications.Add(new(new(4,5),FortificationKind.Gate,1,BattleFacing.West));Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash)==waitingSlash,"Friendly unit cannot enter enemy gate; breaching remains allowed");careful.Fortifications.Clear();
possibleFriend.Owner=1;Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash)==waitingSlash,"Enemy entry does not suppress waiting dagger");careful.Allied=(a,c)=>true;
Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash).Weapon==BattleWeapon.None,"Allied different-owner unit is protected too");
Check(BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash with {Weapon=BattleWeapon.Rifle}).Weapon==BattleWeapon.Rifle && BattleLessons.WaitingDaggerAction(careful,carefulStab,waitingSlash with {Step=new(1,0)}).Weapon==BattleWeapon.Dagger,"Only stationary dagger is constrained, not rifle or advancing attack");
var noChase=Tempo();var oldFriendHit=Unit(noChase,90201,0,3,5,BattleFacing.East);Unit(noChase,90202,0,4,5,BattleFacing.North);Turn(noChase,(oldFriendHit,Point.Zero,BattleFacing.East,BattleWeapon.Dagger));
Check((oldFriendHit.WitnessedLessons&BattleLesson.WaitingDaggerSafety)==0,"Existing friendly target without fleeing enemy is not the specific ninth-lesson event");
Console.WriteLine("PASS: ninth-lesson sidestep/chase friendly hit, acquisition and save, conservative pre-move prevention, terrain/enemy-gate exclusions, allied ownership, stationary-only scope and unrelated-hit exclusion.");

// Lesson ten: perpendicular immovable neighbors create a corner, not a straight corridor.
var boxed=Tempo();var boxedVictim=Unit(boxed,100001,0,5,5,BattleFacing.North);var boxNorth=Unit(boxed,100002,1,5,4,BattleFacing.South);var boxWest=Unit(boxed,100003,1,4,5,BattleFacing.East);var cornerWitness=Unit(boxed,100004,0,2,2,BattleFacing.East);boxed.Walls.Add(new(6,5));boxed.Walls.Add(new(5,6));
Check(BattleLessons.IsCorner(boxed,boxedVictim.Position,0) && BattleLessons.CornerSideTrap(boxed,boxedVictim),"Perpendicular walls and enemies in both open directions form the reported trap");
Turn(boxed,(boxWest,Point.Zero,BattleFacing.East,BattleWeapon.Dagger));Check(boxedVictim.Fallen && (cornerWitness.WitnessedLessons&BattleLesson.CornerEscape)!=0,"Actual sideways dagger in surrounded corner teaches tenth lesson");
var cornerResearch=new BattleLessons();cornerResearch.BringHome(boxedVictim);Check(cornerResearch.Pending.Count==0,"Fallen corner victim cannot teach");cornerWitness.Retreated=true;cornerResearch.BringHome(cornerWitness);Check(cornerResearch.Research().Any(a=>a.Lesson==BattleLesson.CornerEscape),"Surviving observer brings corner lesson home");
Check((AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(cornerResearch)).Owned[0]&BattleLesson.CornerEscape)!=0,"Tenth lesson survives save/load");
var leaveCorner=Tempo();var escaper=Unit(leaveCorner,100101,0,5,5,BattleFacing.North);Unit(leaveCorner,100102,1,5,3,BattleFacing.South);Unit(leaveCorner,100103,1,3,5,BattleFacing.East);leaveCorner.Walls.Add(new(6,5));leaveCorner.Walls.Add(new(5,6));leaveCorner.Lessons[0]=BattleLesson.CornerEscape;
Check(BattleLessons.EnemiesWithinTwo(leaveCorner,escaper.Position,0)==2,"Two approaching enemies are reachable within two walking steps");var escape=BattleAi.Choose(leaveCorner,escaper);Check(escape.Step!=Point.Zero && !BattleLessons.IsCorner(leaveCorner,escaper.Position+escape.Step,0),"Trained AI leaves corner before enemies close both exits");
Turn(leaveCorner);Check(!BattleLessons.IsCorner(leaveCorner,escaper.Position,0),"Learned escape actually moves out of the corner");
var avoidCorner=Tempo();var approach=Unit(avoidCorner,100201,0,4,5,BattleFacing.East);Unit(avoidCorner,100202,1,5,4,BattleFacing.South);Unit(avoidCorner,100203,1,4,4,BattleFacing.South);avoidCorner.Walls.Add(new(6,5));avoidCorner.Walls.Add(new(5,6));avoidCorner.Lessons[0]=BattleLesson.CornerEscape;
var forced=new BattleAction(new(1,0),BattleFacing.East);Check(BattleLessons.CornerAction(avoidCorner,approach,forced).Step==Point.Zero,"Trained unit refuses forced entry into a threatened adjacent corner");
Check(BattleAi.Path(avoidCorner,approach,new(7,5)) is {} detour && detour!=new Point(1,0),"Pathfinding chooses an available route around the dangerous corner");
var walkingDistance=Tempo();Unit(walkingDistance,100301,0,5,5);Unit(walkingDistance,100302,1,5,4);Unit(walkingDistance,100303,1,7,5);walkingDistance.Walls.Add(new(6,5));walkingDistance.Walls.Add(new(5,6));
Check(BattleLessons.EnemiesWithinTwo(walkingDistance,new(5,5),0)==1 && !BattleLessons.DangerousCorner(walkingDistance,new(5,5),0),"Enemy close by Manhattan distance but behind a wall is excluded");
var corridor=Tempo();corridor.Walls.Add(new(5,4));corridor.Walls.Add(new(5,6));Check(!BattleLessons.IsCorner(corridor,new(5,5),0),"Opposing blocked directions are a corridor, not a corner");Check(BattleLessons.IsCorner(corridor,new(0,0),0),"Map boundary counts as impassable terrain");
var gateCorner=Tempo();gateCorner.Walls.Add(new(5,6));gateCorner.Fortifications.Add(new(new(6,5),FortificationKind.Gate,0,BattleFacing.East));Check(!BattleLessons.IsCorner(gateCorner,new(5,5),0) && BattleLessons.IsCorner(gateCorner,new(5,5),1),"Gate access changes corner geometry for owner and enemy");
boxed.Lessons[0]=BattleLesson.CornerEscape;boxedVictim.Fallen=false;var trappedAction=BattleLessons.CornerAction(boxed,boxedVictim,new(Point.Zero,BattleFacing.North));Check(trappedAction.Step==Point.Zero,"Already surrounded unit cannot escape through occupied enemy squares");
Console.WriteLine("PASS: tenth-lesson actual corner flank, survivor research/save, early escape, denied entry, rerouting, true walking distance, corridor/boundary/gate geometry and occupied-exit safety.");

Check(BattleLessons.CornerAction(boxed,boxedVictim,new(Point.Zero,BattleFacing.North,BattleWeapon.Dagger)).Weapon==BattleWeapon.Dagger,"No legal exit: retain fighting action rather than freezing the surrounded unit");

// Leave an occupied corner at walking radius three; entering still uses radius two.
var earlyCorner=Tempo();var earlyEscaper=Unit(earlyCorner,101001,0,5,5,BattleFacing.North);Unit(earlyCorner,101002,1,5,2,BattleFacing.South);Unit(earlyCorner,101003,1,3,5,BattleFacing.East);earlyCorner.Walls.Add(new(6,5));earlyCorner.Walls.Add(new(5,6));earlyCorner.Lessons[0]=BattleLesson.CornerEscape;
Check(BattleLessons.EnemiesWithinTwo(earlyCorner,new(5,5),0)==1 && BattleLessons.EnemiesWithinThree(earlyCorner,new(5,5),0)==2,"Radius three includes both nearer enemies and exactly-three-step enemies");
Check(!BattleLessons.DangerousCorner(earlyCorner,new(5,5),0),"Entry avoidance remains radius two");
var earlyExit=BattleLessons.CornerAction(earlyCorner,earlyEscaper,new(Point.Zero,BattleFacing.North));Check(earlyExit.Step!=Point.Zero && !BattleLessons.IsCorner(earlyCorner,earlyEscaper.Position+earlyExit.Step,0),"Existing corner is left before the enemy at three steps closes in");
var fourAway=Tempo();var cautious=Unit(fourAway,101101,0,5,5,BattleFacing.North);Unit(fourAway,101102,1,5,1,BattleFacing.South);Unit(fourAway,101103,1,3,5,BattleFacing.East);fourAway.Walls.Add(new(6,5));fourAway.Walls.Add(new(5,6));fourAway.Lessons[0]=BattleLesson.CornerEscape;
var stay=new BattleAction(Point.Zero,BattleFacing.North);Check(BattleLessons.EnemiesWithinThree(fourAway,new(5,5),0)==1 && BattleLessons.CornerAction(fourAway,cautious,stay)==stay,"Four steps is outside the early-exit radius");
Console.WriteLine("PASS: corner entry at radius two, early exit at radius three inclusive, four-step exclusion.");

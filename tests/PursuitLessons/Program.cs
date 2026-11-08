using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static string State(BattleState b)=>string.Join(";",b.Units.Where(u=>u.Active).OrderBy(u=>u.Id).Select(u=>$"{u.Id}:{u.Position}:{u.Facing}:{u.Daggers},{u.Rifles},{u.Shields}"));
static BattleState Copy(BattleState b){var copy=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));copy.Allied=b.Allied;return copy;}
void Verify(BattleState original,int period){
    const BattleLesson both=BattleLesson.PursuitLoop|BattleLesson.RifleSpacing;
    var lesson=period==4?BattleLesson.PursuitLoop:BattleLesson.RifleSpacing;var other=period==4?BattleLesson.RifleSpacing:BattleLesson.PursuitLoop;
    var untrained=Copy(original);foreach(var owner in untrained.Armies.Select(a=>a.Owner))untrained.Lessons[owner]&=~both;
    string initial=State(untrained);
    for(int i=0;i<period;i++)BattleTurnResolver.Advance(untrained);
    Check(!untrained.Finished && State(untrained)==initial,$"Untrained {period}-turn loop is restored");
    Check(untrained.Units.All(u=>(u.WitnessedLessons&both)==0),"No premature qualification before one full loop");
    untrained=Copy(untrained);Check(untrained.PursuitHistory.Count==period,"History survives save/resume");
    BattleTurnResolver.Advance(untrained);
    Check(untrained.Units.Any(u=>u.Retreating && (u.WitnessedLessons&lesson)!=0),"Repeating patrol witnesses only its own failure and starts retreat");
    Check(untrained.Units.All(u=>(u.WitnessedLessons&other)==0),"The other pursuit lesson is not awarded");
    for(int i=0;i<150 && !untrained.Finished;i++)BattleTurnResolver.Advance(untrained);
    Check(untrained.Finished,"Untrained failure ends through ordinary retreat, not endless waiting");
    var ledger=new BattleLessons();foreach(var unit in untrained.Units)ledger.BringHome(unit);
    Check(ledger.Pending.Any(a=>a.Lesson==lesson) && ledger.Pending.All(a=>a.Lesson!=other) && ledger.Owned.All(l=>l==0),"Survivor brings only the matching qualification home before research");
    ledger.Research();Check(ledger.Owned.Any(l=>(l&lesson)!=0) && ledger.Owned.All(l=>(l&other)==0),"Research grants only the matching pursuit card");
    var trained=Copy(original);foreach(var owner in trained.Armies.Select(a=>a.Owner))trained.Lessons[owner]=(trained.Lessons[owner]&~both)|lesson;
    for(int i=0;i<period && !trained.Finished;i++)BattleTurnResolver.Advance(trained);
    Check(trained.Finished || State(trained)!=initial,"The matching tactic breaks its original loop without knowing the other card");
    for(int i=0;i<150 && !trained.Finished;i++)BattleTurnResolver.Advance(trained);
    Check(trained.Finished && trained.Units.All(u=>(u.WitnessedLessons&lesson)==0),"The known failure is not taught again; a different failure may still occur");
    var wrong=Copy(original);foreach(var owner in wrong.Armies.Select(a=>a.Owner))wrong.Lessons[owner]=(wrong.Lessons[owner]&~both)|other;
    for(int i=0;i<period;i++)BattleTurnResolver.Advance(wrong);
    Check(State(wrong)==initial && !wrong.Finished,"Knowing the other card does not remove this loop");BattleTurnResolver.Advance(wrong);
    Check(wrong.Units.Any(u=>(u.WitnessedLessons&lesson)!=0) && wrong.Units.All(u=>(u.WitnessedLessons&other)==0),"The remaining failure can be learned independently");
    Console.WriteLine($"PASS period={period}: independent qualification/research/effect, wrong-card repetition, retreat and save/resume");
}
var b=new BattleState(1,new(){PersuasionPercent=0,NoWillTurns=100}){Kind=BattleKind.Edge};
b.AddArmy(0,0,new[]{new Robot(1484,0,RobotParts.Complete)});b.AddArmy(1,1,new[]{new Robot(1613,1,RobotParts.Complete)});b.Reinforce();
var a=b.Units.Single(u=>u.Owner==0);var enemy=b.Units.Single(u=>u.Owner==1);
a.Role=enemy.Role=BattleRole.Soldier;a.Position=new(6,5);a.Facing=BattleFacing.South;a.Daggers=1;a.Rifles=1;a.Shields=2;
enemy.Position=new(7,6);enemy.Facing=BattleFacing.North;enemy.Daggers=0;enemy.Rifles=2;enemy.Shields=1;
Verify(b,4);
var legacy=AutoSaveStore.Capture(b);foreach(var item in legacy.Objects)if(item.Fields!=null)item.Fields.Remove("<PursuitHistory>k__BackingField");
Check(AutoSaveStore.Restore<BattleState>(legacy).PursuitHistory.Count==0,"Legacy saves default to empty pursuit history");
var priorCard=new BattleLessons();priorCard.Owned[0]=BattleLesson.PursuitLoop;
var carried=AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(priorCard));
Check(carried.Owned[0]==BattleLesson.PursuitLoop && (carried.Owned[0]&BattleLesson.RifleSpacing)==0,"A saved former common card retains only card thirteen, never silently grants fourteen");
var dead=new BattleUnit{Owner=0,Role=BattleRole.Soldier,Robot=new(99,0,RobotParts.Complete),Fallen=true,WitnessedLessons=BattleLesson.PursuitLoop};
var empty=new BattleLessons();empty.BringHome(dead);Check(empty.Pending.Count==0,"A fallen witness cannot teach the lesson");
var still=Copy(b);still.Units.Clear();still.Armies.Clear();still.Armies.Add(new(){Owner=0});still.Armies.Add(new(){Owner=1,Side=2});
still.Units.Add(new(){Id=1,Owner=0,Robot=new(1,0,RobotParts.Complete),Position=new(1,1)});still.Units.Add(new(){Id=2,Owner=1,Robot=new(2,1,RobotParts.Complete),Position=new(9,9)});
for(int i=0;i<50;i++)BattleTurnResolver.Advance(still,still.Units.ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing)));
Check(still.Units.All(u=>(u.WitnessedLessons&BattleLesson.PursuitLoop)==0) && still.PursuitHistory.Count==0,"Explicit stationary orders do not grant a pursuit lesson");
var idle=new BattleState(2,new(){NoWillTurns=1000,PersuasionPercent=0}){Kind=BattleKind.Edge};idle.Armies.Add(new(){Owner=0});idle.Armies.Add(new(){Owner=1,Side=2});
foreach(var (owner,point) in new[]{(0,new Point(1,1)),(1,new Point(9,9))}){
    idle.Units.Add(new(){Id=owner+1,Owner=owner,Robot=new(owner+1,owner,RobotParts.Complete),Position=point,Daggers=0,Rifles=0,Shields=3});
    foreach(var step in new[]{new Point(1,0),new Point(-1,0),new Point(0,1),new Point(0,-1)})idle.Walls.Add(point+step);
}
for(int i=0;i<50;i++)BattleTurnResolver.Advance(idle);
Check(idle.Units.All(u=>(u.WitnessedLessons&BattleLesson.PursuitLoop)==0) && idle.PursuitHistory.Count<=41,"Automatic stationary soldiers neither earn the pursuit card nor grow history without bound");
if(args.Length>0){
    object saved=AutoSaveStore.Read<object>(args[0]);var world=(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
    foreach(var (id,period) in new[]{(1645,40),(2397,4)}){
        var original=world.Campaign.RobotBattles.Encounters.Single(e=>e.Id==id).Battles.Single();original.Allied=world.Relations.Allied;Verify(original,period);
    }
}

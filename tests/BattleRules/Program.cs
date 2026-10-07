using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Board(BattleKind kind=BattleKind.Edge,int noWill=10){var b=new BattleState(42,new(){NoWillTurns=noWill,PersuasionPercent=0}){Kind=kind,Defender=1};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});return b;}
BattleUnit Unit(BattleState b,long id,int owner,int x,int y,BattleFacing facing){var u=new BattleUnit{Id=id,Owner=owner,Position=new(x,y),Facing=facing};b.Units.Add(u);return u;}
void Turn(BattleState b,params (BattleUnit Unit,BattleWeapon Weapon,Point Step,BattleFacing? Facing)[] actions)
{
    var orders=b.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));
    foreach(var a in actions)orders[a.Unit.Id]=new(a.Step,a.Facing??a.Unit.Facing,a.Weapon);BattleTurnResolver.Advance(b,orders);
}
var b=Board();var a=Unit(b,1,0,4,4,BattleFacing.South);var c=Unit(b,2,1,4,5,BattleFacing.North);
Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null),(c,BattleWeapon.Dagger,Point.Zero,null));Check(a.Daggers==2 && c.Daggers==2 && a.Shields==4 && c.Shields==4 && !a.Fallen && !c.Fallen,"Dagger clash");
Check(BattleAttackArt.Clashes(b.Attacks[0],b.Attacks[1]),"Frontal dagger hit records produce clash art");
Check(!BattleAttackArt.Clashes(b.Attacks[0],b.Attacks[1] with {Weapon=BattleWeapon.Rifle}),"Rifle cannot produce dagger sparks");
Check(!BattleAttackArt.Clashes(b.Attacks[0],b.Attacks[1] with {Turn=2}),"Different turn attacks do not clash");
Check(!BattleAttackArt.Clashes(b.Attacks[0],b.Attacks[1] with {Target=null}),"Missed dagger does not clash");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null));Check(a.Daggers==2 && c.Shields==3 && !c.Fallen,"Passive shield");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);c.Shields=0;Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null));Check(c.Fallen,"No shield dagger");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null),(c,BattleWeapon.Rifle,Point.Zero,null));Check(c.Fallen && !a.Fallen && a.Daggers==2 && c.Rifles==2 && c.Shields==4,"Adjacent dagger vs rifle");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.Rifle,Point.Zero,null),(c,BattleWeapon.Rifle,Point.Zero,null));Check(!a.Fallen && !c.Fallen && a.Rifles==2 && c.Rifles==2,"Adjacent rifles jump");
b=Board();a=Unit(b,1,0,4,3,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null),(c,BattleWeapon.Rifle,Point.Zero,null));Check(!a.Fallen && a.Shields==3 && a.Daggers==3,"Distant dagger misses, shield blocks");
b=Board();a=Unit(b,1,0,4,3,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.Rifle,Point.Zero,null),(c,BattleWeapon.Rifle,Point.Zero,null));Check(a.Fallen && c.Fallen && a.Rifles==2 && c.Rifles==2,"Simultaneous rifle falls");
b=Board();a=Unit(b,1,0,3,5,BattleFacing.East);c=Unit(b,2,1,4,5,BattleFacing.North);var d=Unit(b,3,0,4,3,BattleFacing.North);Turn(b,(a,BattleWeapon.Dagger,Point.Zero,null),(c,BattleWeapon.Rifle,Point.Zero,null));Check(c.Fallen && d.Fallen && c.Rifles==2,"Fallen unit still shoots and friendly fire");
b=Board();a=Unit(b,1,0,4,2,BattleFacing.South);d=Unit(b,3,0,4,3,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);c.Shields=1;Turn(b,(a,BattleWeapon.Rifle,Point.Zero,null),(d,BattleWeapon.Rifle,Point.Zero,null));Check(c.Fallen && c.Shields==0,"Two frontal shots shield shortage");
b=Board();a=Unit(b,1,0,4,2,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);b.Walls.Add(new(4,3));Turn(b,(a,BattleWeapon.Rifle,Point.Zero,null));Check(!c.Fallen && c.Shields==4 && a.Rifles==2,"Adjacent wall blocks jump");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,1,4,5,BattleFacing.North);Turn(b,(a,BattleWeapon.None,new(0,1),null),(c,BattleWeapon.None,new(0,-1),null));Check(a.Position==new Point(4,4) && c.Position==new Point(4,5),"Enemy exchange blocked");
b=Board();a=Unit(b,1,0,4,4,BattleFacing.South);c=Unit(b,2,0,4,5,BattleFacing.North);Unit(b,3,1,9,9,BattleFacing.North);Turn(b,(a,BattleWeapon.None,new(0,1),null),(c,BattleWeapon.None,new(0,-1),null));Check(a.Position==new Point(4,5) && c.Position==new Point(4,4),"Friendly exchange");
b=Board();a=Unit(b,1,0,3,4,BattleFacing.East);c=Unit(b,2,1,5,4,BattleFacing.West);d=Unit(b,3,0,2,4,BattleFacing.East);Turn(b,(a,BattleWeapon.None,new(1,0),null),(c,BattleWeapon.None,new(-1,0),null),(d,BattleWeapon.None,new(1,0),null));Check(a.Position==new Point(3,4) && c.Position==new Point(5,4) && d.Position==new Point(2,4),"Chain rollback");
b=Board(BattleKind.Node);a=Unit(b,1,0,5,4,BattleFacing.South);c=Unit(b,2,1,6,5,BattleFacing.West);Turn(b,(a,BattleWeapon.None,new(0,1),null),(c,BattleWeapon.Dagger,Point.Zero,null));Check(a.Fallen && b.FlagStanding,"Fallen flag occupant cannot capture");
b=Board(BattleKind.Node,2);a=Unit(b,1,0,5,5,BattleFacing.North);c=Unit(b,2,1,8,8,BattleFacing.North);Turn(b);Check(!b.FlagStanding && b.LastFlagOwner==0,"Capture flag");Turn(b);Check(b.Finished && b.Winner==0,"No will flag victory");
b=Board(BattleKind.Node,2);b.FlagStanding=false;b.LastFlagOwner=0;Unit(b,1,1,8,8,BattleFacing.North);b.Armies.Add(new(){Owner=2,Side=1});Unit(b,2,2,5,5,BattleFacing.North);Turn(b);Check(b.LastFlagOwner==0,"Other attacker does not replace last captor");Turn(b);Check(b.Finished && b.Winner==-1,"Absent captor neutralizes node");
b=Board(BattleKind.Node);b.FlagStanding=false;b.LastFlagOwner=0;a=Unit(b,1,1,5,5,BattleFacing.North);Unit(b,2,0,8,8,BattleFacing.North);Turn(b);Check(b.FlagStanding,"Defender restores flag");
b=Board(noWill:2);a=Unit(b,1,0,0,0,BattleFacing.South);c=Unit(b,2,1,9,9,BattleFacing.North);b.Armies[0].Waiting.Enqueue(new(){Id=3,Owner=0});b.NoWillCount=1;Turn(b);Check(b.NoWillCount==0 && b.Units.Count==3,"Reinforcement resets will before judgment");
b=Board(noWill:2);a=Unit(b,1,0,0,0,BattleFacing.South);c=Unit(b,2,1,9,9,BattleFacing.North);foreach(var p in b.Region(0).Where(p=>p.Y==0))b.Walls.Add(p);b.Walls.Remove(a.Position);b.Armies[0].Waiting.Enqueue(new(){Id=3,Owner=0});Turn(b);Turn(b);Check(b.Finished && b.Winner==-1 && b.Armies[0].Waiting.Count==0,"Blocked reinforcement expires");
for(int count=1;count<=12;count++)
{
    var state=new BattleState(1);for(int owner=0;owner<4;owner++)state.AddArmy(owner,owner,Enumerable.Range(0,count).Select(i=>new Robot(1+owner*12+i,owner,RobotParts.Complete)),true);
    state.BuildTerrain(BattleTerrain.Sea);state.Deploy();state.Check();Check(state.Units.Count+state.Armies.Sum(ar=>ar.Waiting.Count)==4*(count+1),"Deployment preserves units");
}
b=new BattleState(42,new(){PersuasionPercent=100,PersuasionRadius=4}){Kind=BattleKind.Edge};b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});a=Unit(b,-1,0,3,3,BattleFacing.East);a.Role=BattleRole.Queen;a.Daggers=0;a.Rifles=0;c=Unit(b,1,1,4,3,BattleFacing.North);c.Robot=new(1,1,RobotParts.Complete);Turn(b);Check(c.Owner==0 && c.Robot.Owner==0,"Persuasion changes individual ownership");
b=Board();a=Unit(b,1,0,5,5,BattleFacing.South);a.Daggers=0;a.Rifles=0;c=Unit(b,2,1,8,8,BattleFacing.North);
for(int t=0;t<8 && !b.Finished;t++){var action=BattleAi.Choose(b,a);BattleTurnResolver.Advance(b,new Dictionary<long,BattleAction>{{a.Id,action},{c.Id,new(Point.Zero,c.Facing)}});}
Check(a.Retreating && a.Retreated,"Exhausted soldier follows retreat to home edge");
b=Board();a=Unit(b,-1,0,3,3,BattleFacing.South);a.Role=BattleRole.Queen;a.Daggers=0;a.Rifles=0;c=Unit(b,1,1,3,6,BattleFacing.North);
Check(BattleAi.Choose(b,a).Step==Point.Zero,"Queen stays within persuasion range rather than approaching danger");
c.Position=new(3,4);Check(BattleAi.Distance(a.Position+BattleAi.Choose(b,a).Step,c.Position)>1,"Queen retreats from adjacent enemy");
c.Position=new(3,9);Check(BattleAi.Choose(b,a).Step!=Point.Zero,"Queen approaches from beyond persuasion range");
string Replay()
{
    var state=new BattleState(314,new(){PersuasionPercent=5});state.AddArmy(0,0,Enumerable.Range(1,3).Select(i=>new Robot(i,0,RobotParts.Complete)),true);state.AddArmy(1,1,Enumerable.Range(4,3).Select(i=>new Robot(i,1,RobotParts.Complete)),true);state.Deploy();
    for(int t=0;t<20;t++)BattleTurnResolver.Advance(state);
    return string.Join(";",state.Units.OrderBy(u=>u.Id).Select(u=>$"{u.Id},{u.Owner},{u.Position},{u.Facing},{u.Daggers},{u.Rifles},{u.Shields},{u.Fallen},{u.Retreated}"))+$"/{state.Turn}/{state.NoWillCount}/{state.Winner}";
}
Check(Replay()==Replay(),"Seeded combat is reproducible");
int cases=0,turns=0,longest=0,undecided=0;
foreach(var terrain in Enum.GetValues<BattleTerrain>())
foreach(int armyCount in new[]{2,4})
foreach(int robotCount in new[]{1,3,12})
foreach(bool queens in new[]{false,true})
for(int seed=0;seed<10;seed++)
{
    var state=new BattleState(seed){Kind=terrain==BattleTerrain.Fort?BattleKind.Node:BattleKind.Edge,Defender=0};
    for(int owner=0;owner<armyCount;owner++)state.AddArmy(owner,owner,Enumerable.Range(0,robotCount).Select(i=>new Robot(1+owner*12+i,owner,RobotParts.Complete)),queens);
    state.BuildTerrain(terrain);state.Deploy();
    for(int t=0;t<1000 && !state.Finished;t++)BattleTurnResolver.Advance(state);
    Check(state.Finished,$"Combat terminates: {terrain}, {armyCount} armies, {robotCount} robots, queen={queens}, seed={seed}");
    Check(state.Units.Count+state.Armies.Sum(ar=>ar.Waiting.Count)==armyCount*(robotCount+(queens?1:0)),"Sweep preserves every individual including fallen and retreated units");
    cases++;turns+=state.Turn;longest=Math.Max(longest,state.Turn);if(state.Winner<0)undecided++;
}
Console.WriteLine($"Combat sweep: {cases} encounters, mean {turns/(double)cases:F2} turns, max {longest}, no winner {undecided}.");
Console.WriteLine("PASS: simultaneous combat, weapon examples, walls, movement rollback, flags, no-will outcomes, reinforcements and 1–12 / four-army deployment.");

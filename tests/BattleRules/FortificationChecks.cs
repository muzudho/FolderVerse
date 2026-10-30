using FolderVerse;
using Microsoft.Xna.Framework;
static class FortificationChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static BattleState Board()=>new(77,new(){NoWillTurns=100,FlagDefenseTurns=500,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1,Flag=new(10,9)};
 static BattleUnit Add(BattleState b,long id,int owner,Point p){if(!b.Armies.Any(a=>a.Owner==owner))b.Armies.Add(new(){Owner=owner,Side=owner==1?2:0});var u=new BattleUnit{Id=id,Owner=owner,Position=p,Facing=BattleFacing.South,Shields=0};b.Units.Add(u);return u;}
 static void Turn(BattleState b,params (BattleUnit Unit,BattleAction Action)[] actions){var orders=b.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));foreach(var a in actions)orders[a.Unit.Id]=a.Action;BattleTurnResolver.Advance(b,orders);}
 public static void Run(){
  foreach(bool sea in new[]{false,true})for(int count=0;count<=12;count++){
   var b=Board();b.Defender=1;b.BuildTerrain(sea?BattleTerrain.Sea:BattleTerrain.Fort);
   var original=b.Walls.ToHashSet();b.BuildFortifications(count);
   Check(b.Fortifications.Count==count,"Every defense reservation supplies one obstacle on an available board");
   Check(original.IsSubsetOf(b.Walls),"Existing fort walls and sea layout stay intact");
   Check(!sea || b.Fortifications.All(f=>f.Kind is FortificationKind.Barrel or FortificationKind.Gate),"Sea has barrels/gates, never moats");
   Check(b.Fortifications.All(f=>f.Position!=b.Flag),"Flag stays clear");
   foreach(var f in b.Fortifications.Where(f=>f.Kind==FortificationKind.Moat))Check(!b.Walkable(f.Position) && !b.BlocksShot(f.Position),"Moat blocks walking but passes every rifle direction");
   var seen=new HashSet<Point>{b.Flag};var q=new Queue<Point>();q.Enqueue(b.Flag);
   while(q.TryDequeue(out var p))foreach(BattleFacing face in Enum.GetValues<BattleFacing>()){var n=p+BattleState.Forward(face);if(b.Walkable(n) && seen.Add(n))q.Enqueue(n);}
   Check(seen.Count==Enumerable.Range(0,13).Sum(y=>Enumerable.Range(0,13).Count(x=>b.Walkable(new(x,y)))),"All remaining terrain stays connected through traversable/breakable gates");
   b.BuildFortifications(12);Check(b.Fortifications.Count==count && b.InitialDefenseCount==count,"Reinforcements cannot generate extra defenses");
  }
  {
   var crowded=new BattleState(7){Kind=BattleKind.Node,Defender=0};crowded.BuildTerrain(BattleTerrain.Sea);
   var robots=new RobotWorld();robots.Initialize(1,5);
   for(int owner=0;owner<5;owner++)crowded.AddArmy(owner,0,Enumerable.Range(0,12).Select(_=>robots.Create(owner,RobotParts.Complete)));
   crowded.Deploy();crowded.BuildFortifications(12);crowded.Check();
   Check(crowded.Fortifications.Count==12,"Full five-army ship still fits twelve defenses using allied-passable gates");
   Check(crowded.Units.All(u=>crowded.WalkableFor(u.Position,u.Owner)),"No gate traps an initial enemy deployment");
  }
  var gate=new BattleFortification(new(6,6),FortificationKind.Gate,1,BattleFacing.North);
  foreach(var weapon in new[]{BattleWeapon.Dagger,BattleWeapon.Rifle}){
   var b=Board();b.Fortifications.Add(gate);var a=Add(b,1,0,new(6,5));var defender=Add(b,2,1,new(6,8));
   var attack=new BattleAction(Point.Zero,BattleFacing.South,weapon);
   Turn(b,(a,attack));Check(b.GateAt(gate.Position)?.Health==1 && !defender.Fallen,"First attack bends gate and protects unit behind it");
   Check(b.Frames.First(f=>f.Phase==BattlePhase.Attack).Fortifications[0].Health==2 && b.Frames.First(f=>f.Phase==BattlePhase.Receive).Fortifications[0].Health==1,"Playback snapshots preserve the intact and bent phases");
   Turn(b,(a,attack));Check(b.GateAt(gate.Position)==null && !b.BlocksShot(gate.Position),"Second attack destroys gate");
   Check(weapon==BattleWeapon.Dagger?a.Daggers==1:a.Rifles==2,"Gate attacks consume real ammunition");
  }
  {
   var b=Board();b.Fortifications.Add(gate);b.Allied=(a,c)=>a==c || a is 1 or 2 && c is 1 or 2;
   Check(b.WalkableFor(gate.Position,1) && b.WalkableFor(gate.Position,2) && !b.WalkableFor(gate.Position,0),"Owner and allied armies pass; enemies cannot");
   var friend=Add(b,1,1,new(6,5));var enemy=Add(b,2,0,new(6,9));
   Turn(b,(friend,new(Point.Zero,BattleFacing.South,BattleWeapon.Rifle)));Check(b.GateAt(gate.Position)?.Health==2 && !enemy.Fallen,"Friendly rifle stops at gate without damaging it");
   Turn(b,(friend,new(new(0,1),BattleFacing.South)));Check(friend.Position==gate.Position,"Friendly movement actually enters gate tile");
  }
  {
   var b=Board();b.Fortifications.Add(gate);var a=Add(b,1,0,new(6,5));a.Rifles=0;Add(b,2,1,new(6,9));
   for(int x=0;x<13;x++)if(x!=6)b.Walls.Add(new(x,6));
   for(int i=0;i<2;i++){var action=BattleAi.Choose(b,a);Check(action.Weapon==BattleWeapon.Dagger && action.Step==Point.Zero,"AI attacks gate on the route to its objective");Turn(b,(a,action));}
   var move=BattleAi.Choose(b,a);Check(move.Step==new Point(0,1),"AI walks through the destroyed gate: "+move+" / HP="+b.Fortifications[0].Health+" / pos="+a.Position+" / retreat="+a.Retreating);Turn(b,(a,move));Check(a.Position==new Point(6,6),"Destroyed gate is truly passable");
   var copy=Board();copy.InheritFortifications(b);Check(copy.GateAt(gate.Position)==null,"Continuing/merged battle does not repair destroyed gate");
  }
  {
   var b=Board();b.Fortifications.Add(gate);var a=Add(b,1,0,new(6,5));Add(b,2,1,new(6,9));
   Check(BattleAi.Path(b,a,new(6,7))!=new Point(0,1),"AI takes a cheaper detour rather than always spending ammunition");
   for(int x=0;x<13;x++)if(x!=6)b.Walls.Add(new(x,6));a.Daggers=a.Rifles=0;
   Check(BattleAi.Path(b,a,new(6,7))==null,"Unarmed unit cannot plan to break a gate");
  }
  {
   var b=Board();b.Fortifications.Add(gate);var a=Add(b,1,0,new(6,5));var c=Add(b,3,0,new(7,5));Add(b,2,1,new(6,9));
   for(int x=0;x<13;x++)if(x!=6)b.Walls.Add(new(x,6));
   a.Daggers=c.Daggers=1;a.Rifles=c.Rifles=0;
   var first=BattleAi.Choose(b,a);Check(first.Weapon==BattleWeapon.Dagger,"One remaining weapon contributes to a shared gate breach");Turn(b,(a,first));
   Turn(b,(a,new(new Point(-1,0),BattleFacing.South)),(c,new(new Point(-1,0),BattleFacing.South)));
   var second=BattleAi.Choose(b,c);Check(second.Weapon==BattleWeapon.Dagger,"Another soldier finishes the damaged gate");Turn(b,(c,second));Check(b.GateAt(gate.Position)==null,"Two short-of-ammo allies cooperate on a gate");
  }
  {
   var b=Board();b.Fortifications.Add(gate);var a=Add(b,1,0,new(6,5));var c=Add(b,3,0,new(5,6));var victim=Add(b,2,1,new(6,8));
   Turn(b,(a,new(Point.Zero,BattleFacing.South,BattleWeapon.Rifle)),(c,new(Point.Zero,BattleFacing.East,BattleWeapon.Dagger)));
   Check(b.GateAt(gate.Position)==null && !victim.Fallen,"Two simultaneous hits destroy gate without letting shots from the same phase pass through");
   var saved=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));Check(saved.Fortifications.Single().Health==0,"Destroyed gate survives a save/load");
   var legacy=AutoSaveStore.Capture(Board());foreach(var entry in legacy.Objects.Where(e=>e.Fields!=null))foreach(string key in new[]{"<Fortifications>k__BackingField","<GateHits>k__BackingField","FortificationsBuilt","InitialDefenseCount"})entry.Fields.Remove(key);
   var restored=AutoSaveStore.Restore<BattleState>(legacy);Check(restored.Fortifications.Count==0 && restored.GateHits.Count==0,"Older saves receive empty defensive additions");
  }
  {
   var w=new WorldSetup();w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);w.Relations.Reset();
   w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);
   var target=w.Nodes.All.First(n=>n.Terrain!=TravelTerrain.Sea && w.Routes.Neighbors(n).Length>0);
   var route=w.Routes.Neighbors(target)[0];var source=w.Nodes.At(route.Target,route.Entry);
   foreach(var n in w.Nodes.All)n.Owner=-1;source.Owner=0;target.Owner=1;
   for(int owner=0;owner<w.ActiveCount;owner++)w.Relations.Powered[owner]=owner<2;
   w.ConquerorLocations[0]=source.Cell;w.ConquerorPoints[0]=source.Center;w.ConquerorLocations[1]=target.Cell;w.ConquerorPoints[1]=target.Center;
   var ids=new List<long>();for(int i=0;i<3;i++){
    var robot=w.Robots.Create(0,RobotParts.Complete);ids.Add(robot.Id);w.Robots.Nodes[source.Id].Add(robot);
    w.Robots.Nodes[target.Id].Add(w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true});
   }
   w.Nodes.Recount();w.Robots.ConfigureFactories(w);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=100,FlagDefenseTurns=500,PersuasionPercent=0};
   w.Campaign.Resolve(w,new[]{new March(0,target.Id,3,source.Id,false,false,ids.ToArray())});
   var state=w.Campaign.RobotBattles.Encounters.Single(e=>e.Kind==BattleKind.Node && e.Target==target.Id).Battles.Single();
   Check(state.InitialDefenseCount==3 && state.Fortifications.Count==3,"Actual node encounter snapshots all three defense reservations before departure");
   var original=state.Fortifications.ToArray();foreach(var u in state.Units.Where(u=>u.Robot?.DefenseOnly==true))u.Fallen=true;
   state.BuildFortifications(0);Check(state.Fortifications.SequenceEqual(original),"Fallen defense robots do not remove their constructions");
  }
  Console.WriteLine("PASS: 0..12 land/sea defenses, preserved forts, connected terrain, fixed construction count, allied gate passage, shot blocking, two-hit damage, ammo, AI breach/detour, frame history and save compatibility.");
 }
}

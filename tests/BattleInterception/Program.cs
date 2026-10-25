using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var board=new BattleState(42,new(){PersuasionPercent=0}){Kind=BattleKind.Node,Defender=4};
for(int owner=0;owner<5;owner++)board.AddArmy(owner,owner,Enumerable.Range(0,12).Select(i=>new Robot(1+owner*12+i,owner,RobotParts.Complete)),true);
board.BuildTerrain(BattleTerrain.Fort);board.ChooseLayout();board.Deploy();
Check(board.BoardSize==13 && board.Units.Count==65 && board.Armies.All(a=>a.Waiting.Count==0),"Five armies deploy 12 robots and a queen each");
Check(board.Units.Select(u=>u.Position).Distinct().Count()==65 && board.Units.Single(u=>u.Owner==4 && u.Role==BattleRole.Queen).Position==new Point(6,6),"Central defender queen occupies G7 with no overlap");
Check(board.Region(0).ToHashSet().SetEquals(Enumerable.Range(3,7).Select(x=>new Point(x,0)).Concat(Enumerable.Range(3,6).Select(x=>new Point(x,1)))),"North deployment matches the approved 13-cell pattern");
for(int side=0;side<5;side++)Check(board.Region(side).Count()==13 && board.Region(side).All(board.Walkable),"Every deployment region is clear of fort walls");
Check(board.Region(4).All(p=>Math.Abs(p.X-6)+Math.Abs(p.Y-6)<=2),"Defender deployment forms the approved central diamond");
board.Check();for(int i=0;i<10;i++)BattleTurnResolver.Advance(board);board.Check();
Check(board.Frames.Count>0 && new RobotBattlePlayback(new[]{board}).Rounds.Length>0,"Five-army battle produces replay frames");
var sea=new BattleState(3){Kind=BattleKind.Node,Defender=4};for(int owner=0;owner<5;owner++)sea.AddArmy(owner,owner,Enumerable.Range(0,12).Select(i=>new Robot(1+owner*12+i,owner,RobotParts.Complete)),true);sea.BuildTerrain(BattleTerrain.Sea);sea.Deploy();Check(sea.Units.Count==65 && sea.Units.All(u=>sea.Walkable(u.Position)),"Five-army naval deployment also fits");
var shot=new BattleState(4,new(){PersuasionPercent=0,NoWillTurns=1000}){Kind=BattleKind.Edge};shot.Armies.Add(new(){Owner=0,Side=3});shot.Armies.Add(new(){Owner=1,Side=1});
var shooter=new BattleUnit{Id=1,Owner=0,Position=new(0,12),Facing=BattleFacing.East};var target=new BattleUnit{Id=2,Owner=1,Position=new(12,12),Facing=BattleFacing.North,Shields=0};shot.Units.AddRange(new[]{shooter,target});
Check(BattleAi.FirstTarget(shot,shooter,BattleFacing.East)==target,"Rifle targeting reaches the new far edge");BattleTurnResolver.Advance(shot,new Dictionary<long,BattleAction>{{1,new(Point.Zero,BattleFacing.East,BattleWeapon.Rifle)},{2,new(Point.Zero,BattleFacing.North)}});Check(target.Fallen,"Rifle resolution reaches distance 12");
var retreat=new BattleState(5,new(){PersuasionPercent=0}){Kind=BattleKind.Edge};retreat.Armies.Add(new(){Owner=0,Side=2,Withdrawing=true});retreat.Armies.Add(new(){Owner=1,Side=0});var runner=new BattleUnit{Id=1,Owner=0,Position=new(6,12)};retreat.Units.Add(runner);retreat.Units.Add(new(){Id=2,Owner=1,Position=new(6,0)});BattleTurnResolver.Advance(retreat);Check(runner.Retreated && runner.Position.Y==13,"Retreat crosses the new board edge");
var legacy=new BattleState(7){BoardSize=10};var legacyDoc=AutoSaveStore.Capture(legacy);foreach(var entry in legacyDoc.Objects)entry.Fields?.Remove("BoardSize");var old=AutoSaveStore.Restore<BattleState>(legacyDoc);Check(old.BoardSize==10 && old.Contains(new(9,9)) && !old.Contains(new(10,10)),"Legacy battle saves keep their 10x10 dimensions");
WorldSetup World(){var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);foreach(var n in w.Nodes.All)n.Owner=-1;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};return w;}
(Node Home,Node A,Node B) Sites(WorldSetup w){var home=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().Count()>=2);var sources=w.Routes.Neighbors(home).Select(r=>w.Nodes.At(r.Target,r.Entry)).DistinctBy(n=>n.Id).Take(2).ToArray();home.Owner=0;sources[0].Owner=1;sources[1].Owner=2;return (home,sources[0],sources[1]);}
Robot Add(WorldSetup w,Node n,int owner,bool intercept=false){var r=w.Robots.Create(owner,RobotParts.Complete) with {InterceptOnly=intercept};w.Robots.Nodes[n.Id].Add(r);return r;}
var world=World();var sites=Sites(world);var guard=Add(world,sites.Home,0,true);var resident=Add(world,sites.Home,0);Add(world,sites.A,1);var campaign=world.Campaign.RobotBattles;
campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false)});var edge=campaign.Encounters.Single();
Check(edge.Kind==BattleKind.Edge && edge.InterceptOwner==0 && campaign.LockedEdge(sites.A.Id,sites.Home.Id) && !campaign.LockedNode(sites.Home.Id),"One incoming edge creates interception rather than node battle");
Check(who(edge,guard.Id) && !who(edge,resident.Id) && world.Robots.Nodes[sites.Home.Id].Robots.Any(r=>r.Id==resident.Id),"All eligible interception robots deploy, remaining guards stay home");
var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));Check(restored.Campaign.RobotBattles.Encounters.Single().InterceptOwner==0 && restored.Campaign.RobotBattles.Encounters.Single().Battles.All(b=>b.BoardSize==13),"Interception metadata and new board size survive saves");
Add(world,sites.B,2);campaign.Advance(world,new[]{new March(2,sites.Home.Id,1,sites.B.Id,Queen:false)});
Check(campaign.Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Edge) && campaign.Encounters.Any(e=>!e.Finished && e.Kind==BattleKind.Node) && campaign.Encounters.Where(e=>e.Kind==BattleKind.Node).All(e=>!who(e,guard.Id)),"Later attack from another edge starts a separate node battle without deployed interceptors");campaign.Check(world);
bool who(RobotEncounter e,long id)=>e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Any(u=>u.Id==id);
world=World();sites=Sites(world);guard=Add(world,sites.Home,0,true);Add(world,sites.A,1);Add(world,sites.B,2);campaign=world.Campaign.RobotBattles;
campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false),new March(2,sites.Home.Id,1,sites.B.Id,Queen:false)});
Check(campaign.Encounters.Count==1 && campaign.Encounters[0].Kind==BattleKind.Node && who(campaign.Encounters[0],guard.Id),"Two simultaneous edges keep all interceptors in the node battle");
world=World();sites=Sites(world);guard=Add(world,sites.Home,0,true);sites.B.Owner=1;Add(world,sites.A,1);Add(world,sites.B,1);campaign=world.Campaign.RobotBattles;
campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false),new March(1,sites.Home.Id,1,sites.B.Id,Queen:false)});
Check(campaign.Encounters.Single().Kind==BattleKind.Node && who(campaign.Encounters.Single(),guard.Id),"Two edges from the same enemy country still prevent interception");
world=World();sites=Sites(world);guard=Add(world,sites.Home,0,true);sites.B.Owner=0;var arrival=Add(world,sites.B,0);Add(world,sites.A,1);campaign=world.Campaign.RobotBattles;
campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false),new March(0,sites.Home.Id,1,sites.B.Id,Queen:false)});
Check(campaign.Encounters.Single().InterceptOwner==0 && world.Robots.Nodes[sites.Home.Id].Robots.Any(r=>r.Id==arrival.Id) && !who(campaign.Encounters.Single(),arrival.Id),"Friendly arrivals do not count as an enemy edge or disappear during interception");campaign.Check(world);
world=World();sites=Sites(world);Add(world,sites.Home,0);Add(world,sites.A,1);campaign=world.Campaign.RobotBattles;campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false)});Check(campaign.Encounters.Single().Kind==BattleKind.Node,"No eligible interceptors means a node battle");
foreach(bool defenseWins in new[]{true,false}){
 world=World();sites=Sites(world);guard=Add(world,sites.Home,0,true);var attacker=Add(world,sites.A,1);campaign=world.Campaign.RobotBattles;campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false)});edge=campaign.Encounters.Single();
 foreach(var unit in edge.Battles.SelectMany(b=>b.Units))if(unit.Owner==(defenseWins?1:0))unit.Fallen=true;
 foreach(var b in edge.Battles){b.Winner=defenseWins?0:1;b.Finished=true;}
 typeof(RobotCampaign).GetMethod("Complete",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(campaign,new object[]{world,edge});
 Check(sites.Home.Owner==0 && sites.A.Owner==1 && campaign.Encounters.Count==1,"Interception completion never captures either endpoint or advances immediately");
 Check(world.Robots.Nodes[sites.Home.Id].Robots.Any(r=>r.Id==guard.Id) && world.Robots.Nodes[sites.A.Id].Robots.Any(r=>r.Id==attacker.Id),"Survivors and damaged casualties return to their home nodes");campaign.Check(world);
}
world=World();sites=Sites(world);guard=Add(world,sites.Home,0);Check(world.Robots.ToggleReservation(sites.Home.Id,guard.Id,HangarReservation.Intercept),"Interception reservation can be enabled");guard=world.Robots.Nodes[sites.Home.Id].Robots.Single();Check(guard.InterceptOnly && guard.Stationed && !world.Robots.CanShip(sites.Home.Id,guard),"Interception reservation excludes automatic shipments");Check(world.Robots.ToggleReservation(sites.Home.Id,guard.Id,HangarReservation.Defense) && !world.Robots.Nodes[sites.Home.Id].Robots.Single().InterceptOnly,"Defense and interception reservations are distinct and exclusive");
// The queen represents the conqueror, never a robot stored in either hangar.
world=World();sites=Sites(world);world.ConquerorLocations[0]=sites.Home.Cell;world.ConquerorPoints[0]=sites.Home.Center;Add(world,sites.Home,0);Add(world,sites.A,1);campaign=world.Campaign.RobotBattles;
campaign.Advance(world,new[]{new March(1,sites.Home.Id,1,sites.A.Id,Queen:false)});var queenBattle=campaign.Encounters.Single().Battles.Single();
Check(queenBattle.Units.Any(u=>u.Owner==0 && u.Role==BattleRole.Queen && u.Robot==null),"Resident defender queen is a conqueror icon with no physical robot");
var beforeRobotCount=world.Robots.Nodes.Concat(world.Robots.Retinues).Sum(s=>s.Count)+queenBattle.Units.Count(u=>u.Robot!=null);
foreach(var unit in queenBattle.Units.Where(u=>u.Owner!=0))unit.Fallen=true;queenBattle.Winner=0;queenBattle.Finished=true;
typeof(RobotCampaign).GetMethod("Complete",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(campaign,new object[]{world,campaign.Encounters.Single()});
Check(world.Nodes.Current(0).Id==sites.Home.Id && world.Robots.Nodes.Concat(world.Robots.Retinues).SelectMany(s=>s.Robots).All(r=>r.Id>0) && world.Robots.Nodes.Concat(world.Robots.Retinues).Sum(s=>s.Count)+campaign.Reserves.Count==beforeRobotCount,"Queen updates the conqueror location without ever returning to a robot store");
Console.WriteLine("PASS: 13x13 five-army deployments, naval/fort layouts, long-range fire, retreat, legacy/new saves, interception branches, continuation, return and role reservations.");
using var game=new Preview();game.Run();
sealed class Preview:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;BattleState battle=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();var w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);battle=new BattleState(11,new(){PersuasionPercent=0}){Kind=BattleKind.Node,Defender=0,TargetNode=w.Nodes.Current(0).Id};for(int owner=0;owner<5;owner++)battle.AddArmy(owner,owner,Enumerable.Range(0,12).Select(i=>new Robot(1+owner*12+i,owner,RobotParts.Complete)),true);battle.BuildTerrain(BattleTerrain.Fort);battle.Deploy();battle.Capture(BattlePhase.Place);Set("_screen",Enum.Parse(Get("_screen").GetType(),"Battle"));Set("_robotScenes",new[]{battle});Set("_robotPlayback",new RobotBattlePlayback(new[]{battle}));Set("_robotPlaying",false);Set("_robotSummary",false);Set("_lastCutInPhase","戦闘フェーズ");Set("_cutInStarted",-10d);}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==7){
   var w=(WorldSetup)Get("_setup");var source=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Any());var route=w.Routes.Neighbors(source)[0];var target=w.Nodes.At(route.Target,route.Entry);
   w.Campaign.RobotBattles.Encounters.Add(new(){Id=1,Source=source.Id,Target=target.Id,Kind=BattleKind.Edge,InterceptOwner=0});
   w.Campaign.RobotBattles.Encounters.Add(new(){Id=2,Source=target.Id,Target=target.Id,Kind=BattleKind.Node});
   typeof(Game1).GetMethod("ResetInspector",F)!.Invoke(this,null);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",false);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
   ((WorldPreview)Get("_world")).ShowSetup(w,true);var net=(CubeNet)Get("_net");net.Home(w);net.FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!);
  }
  if(frame>=7)Set("_animationTime",frame*.4f);
  Set("_newspaperEdition",frame%7);base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"battle13-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);if(frame++==8){Console.WriteLine("PASS: 13x13 battlefield, all newspaper editions and ongoing map battle animations render.");Exit();}}
}

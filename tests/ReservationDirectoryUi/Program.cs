using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new DirectoryGame();game.Run();
sealed class DirectoryGame:Game1 {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 static Rectangle Rect(string n)=>(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
 static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 WorldSetup w=null!;CubeNet net=null!;int source,enemy,frame;
 bool[] Before()=>new[]{net.ShowMapSymbols,net.ShowEquator,net.ShowRobots,net.ShowBatteries,net.ShowCountryColors,net.ShowRoutes,net.ShowOtherCountries,((WorldPreview)Get("_world")).CountryColors};
 bool[] before=null!;
 void Click(Point p){
  var c=(Rectangle)Call("CanvasBounds");
  void Input(ButtonState b)=>Call("HandleInput",new MouseState(c.X+p.X*c.Width/1920,c.Y+p.Y*c.Height/1080,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.Zero),true);
  Input(ButtonState.Released);Input(ButtonState.Pressed);Input(ButtonState.Released);
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  net=(CubeNet)Get("_net");((WorldPreview)Get("_world")).ShowSetup(w,true);net.Home(w);
  source=w.Nodes.All.First(n=>n.Terrain!=TravelTerrain.Sea).Id;w.Nodes.All[source].Owner=0;
  var shop=w.Robots.Workshops[source];shop.LimitLines(3);for(int seed=0;shop.BaseLineCount!=1;seed++)shop.GenerateLines(seed);foreach(var r in w.Robots.Nodes[source].Robots.ToArray())w.Robots.Nodes[source].Remove(r.Id);
  var attack=Enumerable.Range(0,3).Select(_=>w.Robots.Create(0,RobotParts.Complete)).ToArray();
  foreach(var r in attack)w.Robots.Nodes[source].Add(r);
  w.Campaign.Plan(new(0,(source+1)%w.Nodes.All.Length,3,source,false,false,attack.Select(r=>r.Id).ToArray()));
  w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete) with {DefenseOnly=true});
  for(int i=0;i<2;i++)w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete) with {FactoryWorker=true});
  w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Head) with {Disposal=true});
  w.Robots.Nodes[source].Add(w.Robots.Create(0,RobotParts.Complete) with {InterceptOnly=true});
  var peaceful=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[source].Add(peaceful);
  w.Campaign.Plan(new(0,(source+2)%w.Nodes.All.Length,1,source,false,false,new[]{peaceful.Id},true));
  foreach(var parts in new[]{RobotParts.Complete,RobotParts.Complete,RobotParts.Head}){
   var cargo=w.Robots.Create(0,parts);w.Robots.Nodes[source].Add(cargo);
   w.Robots.DirectDelivery.Orders.Add(new(w.Robots.DirectDelivery.Orders.Count+1,source,(source+1)%w.Nodes.All.Length,cargo.Id,parts,null,0,0));
  }
  enemy=w.Nodes.All.Where(n=>n.Id!=source).OrderBy(n=>Vector3.DistanceSquared(w.Routes.Position(n.Cell,n.Center),w.Routes.Position(w.Nodes.All[source].Cell,w.Nodes.All[source].Center))).First().Id;
  w.Nodes.All[enemy].Owner=1;foreach(var r in w.Robots.Nodes[enemy].Robots.ToArray())w.Robots.Nodes[enemy].Remove(r.Id);
  w.Robots.Nodes[enemy].Add(w.Robots.Create(1,RobotParts.Complete) with {InterceptOnly=true});
  Check(((int[])Call("ReservationDirectoryCounts",enemy))[1]==1,"Enemy reservations are included");
  var formatter=typeof(Game1).GetMethod("DirectoryTransportAmount",BindingFlags.Static|BindingFlags.NonPublic)!;
  foreach(var pair in new[]{(0,"0"),(1,"1/3"),(2,"2/3"),(6,"2"),(7,"2 + 1/3"),(8,"2 + 2/3")})Check((string)formatter.Invoke(null,new object[]{pair.Item1})! ==pair.Item2,"Exact mixed fraction "+pair.Item2);
  Check(!(bool)Call("DirectorySeaTransport",source) && (bool)Call("DirectorySeaTransport",w.Nodes.All.First(n=>!w.Routes.Land(n.Cell,n.Center)).Id),"Truck/ferry follows node terrain");
  Check(((int[])Call("ReservationDirectoryCounts",source)).SequenceEqual(new[]{3,1,1,2,7,1}),"Counts include intercept and transport thirds, exclude peaceful movement");
  Check(w.Robots.MarchReserved(source,peaceful) && !w.Robots.CombatMarchReserved(source,peaceful),"Existing assembly locks still include peaceful movement");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",false);
  Check((string)Call("DirectoryTurnStatus",0)=="TURN","Current controller is TURN");
  int controller=w.ControllerOf(0);
  var submitted=(HashSet<int>)typeof(ConquestCampaign).GetField("_submitted",F)!.GetValue(w.Campaign)!;
  submitted.Add(controller);Check((string)Call("DirectoryTurnStatus",0)=="DONE","Submitted controller is DONE");submitted.Remove(controller);
  Check((string)Call("DirectoryTurnStatus",-1)=="","Unowned locations have no turn badge");
  int waiting=Enumerable.Range(0,w.ActiveCount).First(r=>w.Relations.CanAct(r) && w.ControllerOf(r)!=w.TurnPlayer);
  Check((string)Call("DirectoryTurnStatus",waiting)=="WAIT","Unsubmitted other faction is WAIT");
  net.ShowCountryColors=true;net.ShowRoutes=false;net.ShowOtherCountries=false;before=Before();
  Click(Rect("ReservationDirectoryButton").Center);
  Check((bool)Get("_reservationDirectory") && net.DirectoryOnly && net.ShowRoutes && !net.ShowRobots && !net.ShowBatteries && !net.ShowCountryColors && !net.ShowEquator && !net.ShowMapSymbols,"Real toolbar input enters terrain/roads/directory mode");
  net.FitNodesAtSource(w,w.Nodes.All[source],(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_statusGlobe",true);Call("FocusGlobePosition",w.Routes.Position(w.Nodes.All[source].Cell,w.Nodes.All[source].Center));}
  if(frame==2){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  Set("_animationTime",frame*.6f);base.Draw(t);
  var hits=(List<(Rectangle Bounds,int Node)>)Get("_directoryHits");
  Check(hits.Any(h=>h.Node==source),"Visible reserved source has a board in net/globe/half-size view at frame "+frame);
  Check(((int[])Call("ReservationDirectoryCounts",source)).SequenceEqual(new[]{3,1,1,2,7,1}),"Displayed counts remain 3/1/1/2/7 thirds/1");
  Check(hits.All(h=>((int[])Call("ReservationDirectoryCounts",h.Node)).Any(n=>n>0)),"Zero-only nodes have no signpost");
  if(frame==0)Check(hits.Any(h=>h.Node==enemy),"Enemy board is visible even though other-country display was off before entry");
  Check(!hits.First(h=>h.Node==source).Bounds.Intersects(hits.First(h=>h.Node==enemy).Bounds),"Nearby enemy and own signs do not overlap");
  Check(net.RobotBadges.Count==0,"Robot rendering and robot hit targets are hidden");
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"directory-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame==2){
   var pulseProperty=typeof(Game1).GetProperty("DirectoryButtonPulse",F)!;Set("_animationTime",0f);float first=(float)pulseProperty.GetValue(this)!;Set("_animationTime",.6f);Check((float)pulseProperty.GetValue(this)!>first,"Active button slowly pulses");
   var board=hits.First(h=>h.Node==source).Bounds;Click(board.Center);Check(!(bool)Get("_reservationDirectory") && (int)Get("_populationNode")==source && Before().SequenceEqual(before),"Board opens its node and restores display settings");Call("ResetInspector");Call("UpdateDomesticSlide",1f);
   Click(Rect("ReservationDirectoryButton").Center);Click(Rect("ReservationDirectoryClose").Center);Check(!(bool)Get("_reservationDirectory") && !net.DirectoryOnly && Before().SequenceEqual(before),"Close restores every original display flag");
   Click(Rect("ReservationDirectoryButton").Center);Call("ResetInspector");Check(!(bool)Get("_reservationDirectory") && Before().SequenceEqual(before),"Opening another mode restores display settings");
   Console.WriteLine("PASS: six roles, enemy boards, mixed fractions, land/sea icons, peaceful exclusion, existing reservation locks, real toolbar input, mode exit/restoration, net/globe/half-size board rendering and hidden robot hit targets.");Exit();
  }
  frame++;
 }
}

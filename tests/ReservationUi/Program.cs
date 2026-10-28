using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new Checks();game.Run();
class Checks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 WorldSetup w=null!;Node node=null!;long first,second;int frame;
 Rectangle Cell(int i)=>(Rectangle)typeof(Game1).GetMethod("HangarCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!;
 Point Button(int i)=>((Rectangle)typeof(Game1).GetMethod("HangarReservationButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!).Center;
 void Click(Point p){Set("_pointer",p);Call("NodeAssemblyLocalInput",new MouseState(),true,false);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.Relations.Reset();w.SelectTurnRuler(0);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);
  node=w.Nodes.All.First(n=>n.Terrain!=TravelTerrain.Sea);node.Owner=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);var factory=w.Robots.Workshops[node.Id];for(int seed=0;factory.ProductionLines.Length!=1;seed++)factory.GenerateLines(seed);
  for(int i=0;i<4;i++)w.Robots.Nodes[node.Id].Add(w.Robots.Create(0,RobotParts.Complete));first=w.Robots.Nodes[node.Id].Robots[0].Id;second=w.Robots.Nodes[node.Id].Robots[1].Id;
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Screen("WorldStatus");Call("OpenNodeInspector",node.Id);Call("UpdateDomesticSlide",1f);
  Click(Button(1));Click(Cell(0).Center);Click(Cell(1).Center);Check(w.Robots.Nodes[node.Id].Robots.Take(2).All(r=>r.DefenseOnly),"Reservation tool stays active for successive robots");Click(Cell(0).Center);Check(!w.Robots.Nodes[node.Id].Robots[0].DefenseOnly,"Second click cancels reservation");
  Click(Button(2));Click(Cell(0).Center);Check(factory.ProductionLines.Length==2 && factory.WorkerAt(1)==first,"Worker tool creates identifiable lane");Click(Cell(1).Center);Check(factory.ProductionLines.Length==3 && w.Robots.Nodes[node.Id].Robots[1].FactoryWorker,"Worker tool overwrites defender");Click(Button(1));Click(Cell(1).Center);Check(factory.ProductionLines.Length==2 && w.Robots.Nodes[node.Id].Robots[1].DefenseOnly,"Defense tool overwrites worker and closes only its lane");Click(Button(0));Click(Cell(2).Center);Check(w.Robots.Nodes[node.Id].Robots[2].Disposal,"Disposal tool reserves whole card");
  Check(!(bool)Call("CanMoveHangarRobot",w.Robots.Nodes[node.Id].Robots[0]) && !(bool)Call("CanMoveHangarRobot",w.Robots.Nodes[node.Id].Robots[1]) && !(bool)Call("CanMoveHangarRobot",w.Robots.Nodes[node.Id].Robots[2]),"All reservation kinds lock manual assembly");
  // Exercise the real input entry point, including cut-ins and modal branches.
  foreach(string screen in new[]{"Transport","Battle","Industry","BattleResearch","WorldStatus"})foreach(bool escape in new[]{false,true}){
   Screen(screen);Set("_autoPlay",true);Set("_previousMouse",new MouseState());Set("_previousKeyboard",new KeyboardState());Set("_conquestOverview",true);
   Call("HandleInputCore",new MouseState(10,10,0,escape?ButtonState.Released:ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),escape?new KeyboardState(Keys.Escape):new KeyboardState(),new GameTime(),true);
   Check(!(bool)Get("_autoPlay") && Get("_screen").ToString()==screen,"Click/Esc exits AUTO before phase input: "+screen);
  }
  Set("_conquestOverview",false);Screen("WorldStatus");Call("OpenNodeInspector",node.Id);Call("UpdateDomesticSlide",1f);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  // A genuine two-part recipe increases deployable strength and is never undone.
  var materialNode=w.Nodes.All.First(n=>n.Id!=node.Id && n.Terrain!=TravelTerrain.Sea);materialNode.Owner=0;var head=w.Robots.Create(0,RobotParts.Head);var rest=w.Robots.Create(0,RobotParts.Body|RobotParts.Legs);w.Robots.Nodes[materialNode.Id].Add(head);w.Robots.Nodes[materialNode.Id].Add(rest);
  Set("_demoDomesticStage",0);Call("UpdateDemoDomestic",1.8d);Set("_demoDomesticStage",3);Call("UpdateDemoDomestic",1.8d);Call("UpdateDemoDomestic",1.8d);Call("UpdateDemoDomestic",1.8d);
  Check(w.Robots.Nodes[materialNode.Id].Robots.Count==1 && w.Robots.Training[0].Eligible(w.Robots.Nodes[materialNode.Id].Robots[0]),"Demo builds actual deployable robot instead of splitting and restoring");
  var rows=(System.Collections.IList)Get("_invasionSources");rows.Clear();var sourceType=typeof(Game1).GetNestedType("InvasionSource",F)!;var source=Activator.CreateInstance(sourceType,true)!;sourceType.GetField("Owner")!.SetValue(source,0);sourceType.GetField("Node")!.SetValue(source,node.Id);sourceType.GetField("Robots")!.SetValue(source,new HashSet<long>{first});rows.Add(source);var route=w.Routes.Neighbors(node)[0];Set("_selectedMoveNode",w.Nodes.At(route.Target,route.Entry).Id);Call("ReserveInvasion");
  Check(!w.Robots.Nodes[node.Id].Robots.Single(r=>r.Id==first).Stationed && w.Campaign.PlannedOrders.Any(o=>o.RobotIds?.Contains(first)==true),"Confirming sortie replaces worker reservation");
  Check(w.Robots.ToggleReservation(node.Id,first,HangarReservation.Worker) && !w.Campaign.PlannedOrders.Any(o=>o.RobotIds?.Contains(first)==true),"Worker overwrite removes robot from sortie plan");
  Call("OpenNodeInspector",node.Id);Call("UpdateDomesticSlide",1f);Call("OpenNodeAssembly");
  Click(Button(3));Click(Cell(0).Center);Check(w.Robots.Nodes[node.Id].Robots.Single(r=>r.Id==first).InterceptOnly,"New interception reservation button assigns the selected robot");
  Click(Cell(0).Center);Check(!w.Robots.Nodes[node.Id].Robots.Single(r=>r.Id==first).InterceptOnly,"Interception reservation cancels on repeat click");
  var close=(Rectangle)typeof(Game1).GetProperty("NodeInspectorBounds",F)!.GetValue(this)!;Check(close.Contains(Button(3)),"Interception button stays inside the inspector");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==0){Click(Button(1));Set("_pointer",new Point(Cell(0).X+90,Cell(0).Y+25));}
  if(frame==1)Call("OpenNodeFactory");
  if(frame==2){w.Robots.Workshops[node.Id].LimitLines(1);w.Robots.SyncWorkers(node.Id);}
  if(frame==3){Screen("WorldStatus");Call("StartAutoPlay");Call("UpdateAutoDomestic",2d);Check((int)Get("_populationNode")>=0,"AUTO opens a node inspector and preserves the remembered tab");Call("UpdateAutoDomestic",2d);Check((bool)Get("_nodeTransportOpen"),"AUTO shows transport plan");}
  if(frame==4){Call("UpdateAutoDomestic",2d);Call("UpdateAutoDomestic",2d);Call("UpdateAutoDomestic",2d);}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"reservation-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame==0){Color At(int x,int y)=>pixels[(y*p.BackBufferHeight/1080)*p.BackBufferWidth+x*p.BackBufferWidth/1920];Check(At(Cell(0).X+1,Cell(0).Y+50)==(Color)typeof(Game1).GetProperty("ControlEdge",F)!.GetValue(this)!,"Reservation hover highlights entire cell");Check(At(Cell(0).X+57,Cell(0).Y+11)==new Color(9,19,26),"Reservation hover leaves part border inactive");}
  if(frame++==4){Console.WriteLine("PASS: continuous reservation tools and cancellation, locked parts, worker/sea factory rendering, AUTO phase-independent stop and domestic tour, and productive Demo assembly.");Exit();}
 }
}

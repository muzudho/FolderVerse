using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new Checks();game.Run();
class Checks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;int a,b;long original;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static Rectangle Part(int side,int slot,int row)=>(Rectangle)typeof(Game1).GetMethod("DirectPart",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{side,slot,row})!;
 static Rectangle Cell(int side,int slot)=>(Rectangle)typeof(Game1).GetMethod("DirectCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{side,slot})!;
 static MouseState Mouse(Point p,ButtonState state)=>new(p.X,p.Y,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
 void Drag(int side,int slot,int row,int destinationSide,int destinationSlot){var from=Part(side,slot,row).Center;Set("_pointer",from);Call("DirectPairInput",Mouse(from,ButtonState.Pressed),true,false);var to=Cell(destinationSide,destinationSlot).Center;Set("_previousMouse",Mouse(from,ButtonState.Pressed));Set("_pointer",to);Call("DirectPairInput",Mouse(to,ButtonState.Released),false,false);Set("_previousMouse",new MouseState());}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var node=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(node)[0];a=node.Id;b=w.Nodes.At(route.Target,route.Entry).Id;foreach(var n in w.Nodes.All)n.Owner=1;w.Nodes.All[a].Owner=w.Nodes.All[b].Owner=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
  var robot=w.Robots.Create(0,RobotParts.Complete);original=robot.Id;w.Robots.Nodes[a].Add(robot);w.Robots.Nodes[b].Add(w.Robots.Create(0,RobotParts.Legs));((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  Set("_statusGlobe",false);((CubeNet)Get("_net")).FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1);Call("OpenDirectMap");
  Call("OpenDirectPair",a,b);Drag(0,0,0,1,0);Check(w.Robots.DirectDelivery.Orders.Count==1 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Cross-pane drag reserves without moving physical part");
  Drag(0,0,1,0,1);Check(w.Robots.Nodes[a].Count==2 && w.Robots.DirectDelivery.Orders.Count==1,"Other unreserved body moves within source pane");
  var ghost=Part(1,0,0).Center;Set("_pointer",ghost);Call("DirectPairInput",Mouse(ghost,ButtonState.Pressed),true,false);Check(w.Robots.DirectDelivery.Orders.Count==0,"Click arrival ghost cancels");
  Drag(0,0,0,1,0);Drag(0,1,1,1,1);Check(w.Robots.DirectDelivery.Orders.Count==2,"Independent parts can use different destination cells");
  var doc=AutoSaveStore.Capture(w);Check(AutoSaveStore.Restore<WorldSetup>(doc).Robots.DirectDelivery.Orders.Count==2,"UI reservations are saved");
  Set("_pointer",new Point(1800,1000));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_directPairOpen",false);}
  if(frame==2){var hit=((System.Collections.IList)Get("_directTrucks"))[0]!;var bounds=(Rectangle)hit.GetType().GetField("Item1")!.GetValue(hit)!;Call("DirectMapClick",bounds.Center);Check((bool)Get("_directPairOpen"),"Clicking truck opens pair inspector");}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"direct-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame==1){var hits=(System.Collections.IList)Get("_directTrucks");Check(hits.Count>0,"Visible adjacent edge has a truck");}
  if(frame++==3){Console.WriteLine("PASS: edge trucks, two hangars, cross-pane reservations, same-pane part edits, ghost cancellation, multiple destinations, saved reservations and rendering.");Exit();}
 }
}

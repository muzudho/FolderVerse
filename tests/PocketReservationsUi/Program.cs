using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new PocketChecks();game.Run();
sealed class PocketChecks:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 WorldSetup world=null!;int node,frame;long movable;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 Rectangle Card(int i)=>(Rectangle)typeof(Game1).GetMethod("PocketCard",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i,true})!;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  world=(WorldSetup)Get("_setup");world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.Relations.Reset();world.SelectPlayer(0);
  var home=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Length>0);node=home.Id;home.Owner=0;
  world.ConquerorLocations[0]=home.Cell;world.ConquerorPoints[0]=home.Center;
  world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);world.Robots.ConfigureFactories(world);
  Robot Add(Robot r){world.Robots.Nodes[node].Add(r);return r;}
  var normal=Add(world.Robots.Create(0,RobotParts.Complete));movable=normal.Id;
  var defense=Add(world.Robots.Create(0,RobotParts.Complete) with {DefenseOnly=true});
  var intercept=Add(world.Robots.Create(0,RobotParts.Complete) with {InterceptOnly=true});
  var worker=Add(world.Robots.Create(0,RobotParts.Complete) with {FactoryWorker=true});
  var disposal=Add(world.Robots.Create(0,RobotParts.Complete) with {Disposal=true});
  var assembly=Add(world.Robots.Create(0,RobotParts.Head));world.Robots.PendingEdits.Add(new(node,new[]{assembly.Id},null));
  var delivery=Add(world.Robots.Create(0,RobotParts.Complete));var route=world.Routes.Neighbors(home)[0];var target=world.Nodes.At(route.Target,route.Entry);target.Owner=0;
  world.Robots.DirectDelivery.Reserve(world,node,target.Id,delivery.Id,RobotParts.Head,null,0);
  var foreign=Add(world.Robots.Create(1,RobotParts.Complete));
  world.Robots.Training[1].DeploymentPolicy=1;
  var foreignWork=Add(world.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=0,DefenseOnly=true,DefenseFlag=new(1,node,0)});
  Call("OpenPocket");Set("_cutInStarted",-10d);
  foreach(var sample in new[]{(assembly,"組立予約"),(foreign,"自国のパーツがありません"),(foreignWork,"他国の仕事予約")}){
   Check((string)Call("PocketRobotLockReason",sample.Item1,true)==sample.Item2 && !(bool)Call("PocketRobotSelectable",sample.Item1,true),"Lock label matches transfer restriction");
  }
  foreach(var own in new[]{defense,intercept,worker,disposal,delivery})Check((bool)Call("PocketRobotSelectable",own,true),"Own commands and part reservations can be overwritten by an immediate pocket transfer");
  Set("_pointer",Card(1).Center);Call("PocketInput",true,false);
  Check((long?)Get("_pocketRobot")==defense.Id && (bool)Call("PocketTransferEnabled",true),"Own shoes do not prevent pocket selection");
  Set("_pointer",Card(0).Center);Call("PocketInput",true,false);
  Check((bool)Call("PocketTransferEnabled",true),"Unreserved robot remains transferable");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  Set("_pointer",Card(1).Center);base.Draw(time);
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"pocket-reservations-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==0){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();return;}
  Set("_pointer",((Rectangle)typeof(Game1).GetMethod("PocketTransferButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{true})!).Center);Call("PocketInput",true,false);
  Check(world.Robots.Retinues[0].Robots.Any(r=>r.Id==movable),"Normal pocket transfer still succeeds");
  Console.WriteLine("PASS: reservation reasons, blocked selection and message, normal transfer, full/half-size pocket rendering.");Exit();
 }
}

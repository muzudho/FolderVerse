using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CheckGame();game.Run();
class CheckGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 int target;
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  var w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
  var source=w.Nodes.Current(0);Set("_populationNode",source.Id);Set("_populationCell",source.Cell);
  Set("_nodeTransportOpen",true);Set("_nodeTransportPart",RobotParts.Complete);
  target=((int[])Call("DestinationNodes"))[0];Set("_rateTarget",target);
  Call("SetDestinationRate",target,120);
  int count=1;foreach(var pair in new[]{(0,8),(1,9),(2,10),(3,12)})for(int col=0;col<pair.Item2;col++)
  {
   if(!(bool)typeof(Game1).GetMethod("RatePresetUnique",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{pair.Item1,col})!)continue; var rect=(Rectangle)typeof(Game1).GetMethod("RatePresetBounds",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{pair.Item1,col})!;
   Call("RatePieClick",rect.Center);int expected=(col+1)*360/pair.Item2;
   if((int)Call("Rate",target)!=expected)throw new Exception("Preset mismatch");count++;
  }
  if(count!=31)throw new Exception("Preset count");
  Call("SetDestinationRate",target,120);
  var method=typeof(Game1).GetMethod("TransportTriangleSize",BindingFlags.Static|BindingFlags.NonPublic)!;
  foreach(var pair in new[]{(0,30f),(120,60f),(180,75f),(360,120f)})
    if(Math.Abs((float)method.Invoke(null,new object[]{pair.Item1})!-pair.Item2)>.0001f)throw new Exception("Arrow threshold");
  Call("OpenTransportOverview");if(!(bool)Get("_transportOverview") || (int)Get("_populationCell")>=0)throw new Exception("Ruler transport entry");
  Set("_populationNode",source.Id);Set("_rateTarget",target);Call("SetDestinationRate",target,120);
  Call("RatePieClick",new Point(1800,970));if(!(bool)Get("_transportOverview") || (int)Get("_rateTarget")>=0)throw new Exception("Dialog close lost overview");
  Call("CloseTransportOverview");
  Console.WriteLine("PASS: fractions, arrow thresholds, ruler mode and continuing after dialog close");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  GraphicsDevice.Clear(new Color(238,225,199));var batch=(SpriteBatch)Get("_spriteBatch");batch.Begin();Call("OpenTransportOverview");Set("_statusGlobe",false);((CubeNet)Get("_net")).SetCenter((WorldSetup)Get("_setup"),0);Call("PrepareDestinationMap");((CubeNet)Get("_net")).Draw((UiPainter)Get("_ui"),(WorldSetup)Get("_setup"),new Rectangle(1040,240,790,580),-1,0);Call("DrawTransportOverviewMap");Call("DrawTransportOverviewPanel");Set("_rateTarget",target);Call("DrawRatePieDialog");batch.End();
  var colors=new Color[GraphicsDevice.Viewport.Width*GraphicsDevice.Viewport.Height];GraphicsDevice.GetBackBufferData(colors);
  using var tex=new Texture2D(GraphicsDevice,GraphicsDevice.Viewport.Width,GraphicsDevice.Viewport.Height);tex.SetData(colors);
  using var output=File.Create(Path.Combine(AppContext.BaseDirectory,"rate-pie.png"));tex.SaveAsPng(output,tex.Width,tex.Height);Exit();
 }
}

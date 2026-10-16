using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using var game=new ThemeGame();game.Run();
sealed class ThemeGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 WorldSetup w=null!;
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);}
 protected override void Update(GameTime t){}
 Color[] Pixels(){var p=GraphicsDevice.PresentationParameters;var a=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(a);return a;}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");var pp=GraphicsDevice.PresentationParameters;var bg=new Color(72,83,95);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"Review"));Check((Color)Prop("Accent")==new Color(45,135,142),"World setup keeps green");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var glyph=typeof(Game1).GetMethods(F).Single(m=>m.Name=="DrawRobotGlyph" && m.GetParameters().Length==8);
  var node=w.Nodes.All.First(n=>n.Owner==0);Set("_populationNode",node.Id);Set("_populationCell",node.Cell);
  var store=w.Robots.Nodes[node.Id];foreach(var existing in store.Robots.ToArray())store.Remove(existing.Id);var robot=w.Robots.Create(0,RobotParts.Complete);store.Add(robot);
  var pressed=new MouseState(0,0,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
  foreach(float slide in new[]{1f,.75f})foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})foreach(int grabX in new[]{22,45}){
   Set("_domesticSlide",slide);Call("OpenNodeAssembly");int row=part==RobotParts.Head?0:part==RobotParts.Body?1:2;
   int panelOffset=(int)Prop("NodeInspectorOffset");var press=new Point(140+grabX,310+row*38+12);
   Set("_pointer",new Point(press.X+panelOffset,press.Y));Call("NodeAssemblyInput",pressed,true,false);
   Check((long?)Get("_hangarDrag")==robot.Id,"Actual press starts drag for "+part);
   foreach(var delta in new[]{Point.Zero,new Point(350,120)}){
    Set("_pointer",press+delta);GraphicsDevice.Clear(bg);batch.Begin();Call("DrawHangarDrag",robot);batch.End();var actual=Pixels();
    GraphicsDevice.Clear(bg);batch.Begin();glyph.Invoke(this,new object[]{new Rectangle(141+delta.X,312+delta.Y,68,102),0,part,false,1f,true,false,false});batch.End();var expected=Pixels();
    Check(actual.SequenceEqual(expected),"Grabbed pixel and transparent equal-size glyph stay aligned: "+part+" / "+slide+" / "+grabX+" / "+delta);
   }
  }
  var ui=(UiPainter)Get("_ui");Set("_pointer",Point.Zero);ui.Pointer=Point.Zero;
  for(int owner=0;owner<w.ActiveCount;owner++){
   w.SelectPlayer(owner);Check((Color)Prop("Accent")==w.OwnerColor(owner),"Accent follows player");GraphicsDevice.Clear(bg);batch.Begin();ui.Button(new(100,100,240,64),"全体作戦",(Color)Prop("Accent"),.6f);Call("DrawRouteLayerButtons");batch.End();
   var pixels=Pixels();Check(pixels[110*pp.BackBufferWidth+110]==w.OwnerColor(owner),"Actual button fill uses country color");
   Check(UiPainter.ContrastInk(w.OwnerColor(owner))==(Color)Prop("SelectionInk"),"Text and icon contrast agree");
   if(owner is 0 or 12){using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"theme-{owner}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);}
  }
  Check(UiPainter.ContrastInk(Color.White)==new Color(12,18,24) && UiPainter.ContrastInk(Color.Black)==Color.White,"Bright and dark themes choose readable text");
  Console.WriteLine("PASS: actual mouse grabs and pixel-perfect drag at two panel positions for all parts, setup color, all country button fills, text and icon contrast.");Exit();
 }
}

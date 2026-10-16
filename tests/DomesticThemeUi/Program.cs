using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
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
  foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){
   var robot=w.Robots.Create(0,RobotParts.Complete);Set("_hangarPart",part);Set("_pointer",new Point(500,500));GraphicsDevice.Clear(bg);batch.Begin();
   glyph.Invoke(this,new object[]{new Rectangle(100,100,68,102),0,part,false,1f,true,false,false});Call("DrawHangarDrag",robot);batch.End();var pixels=Pixels();
   Rectangle Bounds(int x0,int y0,int x1,int y1){var points=new List<Point>();for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*pp.BackBufferWidth+x]!=bg)points.Add(new(x,y));return new(points.Min(p=>p.X),points.Min(p=>p.Y),points.Max(p=>p.X)-points.Min(p=>p.X)+1,points.Max(p=>p.Y)-points.Min(p=>p.Y)+1);}
   var source=Bounds(80,80,200,240);var drag=Bounds(500,490,620,640);Check(source.Size==drag.Size,"Drag preserves source dimensions for "+part);Check(pixels[512*pp.BackBufferWidth+516]==bg,"No background rectangle behind drag");
   using var dragImage=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);dragImage.SetData(pixels);using var dragFile=File.Create(Path.Combine(AppContext.BaseDirectory,$"drag-{part}.png"));dragImage.SaveAsPng(dragFile,dragImage.Width,dragImage.Height);
  }
  var ui=(UiPainter)Get("_ui");Set("_pointer",Point.Zero);ui.Pointer=Point.Zero;
  for(int owner=0;owner<w.ActiveCount;owner++){
   w.SelectPlayer(owner);Check((Color)Prop("Accent")==w.OwnerColor(owner),"Accent follows player");GraphicsDevice.Clear(bg);batch.Begin();ui.Button(new(100,100,240,64),"全体作戦",(Color)Prop("Accent"),.6f);Call("DrawRouteLayerButtons");batch.End();
   var pixels=Pixels();Check(pixels[110*pp.BackBufferWidth+110]==w.OwnerColor(owner),"Actual button fill uses country color");
   Check(UiPainter.ContrastInk(w.OwnerColor(owner))==(Color)Prop("SelectionInk"),"Text and icon contrast agree");
   if(owner is 0 or 12){using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"theme-{owner}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);}
  }
  Check(UiPainter.ContrastInk(Color.White)==new Color(12,18,24) && UiPainter.ContrastInk(Color.Black)==Color.White,"Bright and dark themes choose readable text");
  Console.WriteLine("PASS: transparent equal-size drag for all parts, setup color, all country button fills, text and icon contrast.");Exit();
 }
}

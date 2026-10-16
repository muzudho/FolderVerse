using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new InspectorGame();game.Run();
sealed class InspectorGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 WorldSetup w=null!;int node;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);
  ((CubeNet)Get("_net")).Home(w);node=w.Nodes.Current(0).Id;Call("OpenNodeInspector",node);
  Check((int)Prop("NodeInspectorOffset")==-960,"Panel begins outside left edge");Call("UpdateDomesticSlide",.2f);
  Check((float)Get("_domesticSlide")==.5f && (int)Prop("NodeInspectorOffset")==-480,"Shared half transition");
  var map=(Rectangle)Prop("NetPanel");Check(map.X==520 && map.Right==1880 && map.Y==142 && map.Height==820,"Map ratio changes horizontally with panel");
  Call("UpdateDomesticSlide",.2f);Check((float)Get("_domesticSlide")==1,"Panel enters fully");
  var factory=w.Robots.Workshops[node];for(int seed=0;factory.ProductionLines.Length==0;seed++)factory.GenerateLines(seed);Call("OpenNodeFactory");
  Set("_factoryLine",0);var button=(Rectangle)Call("FactoryDropButton",0);Call("NodeDialogClick",button.Center,new KeyboardState());
  Check((int)Get("_factoryDropDown")==0,"Repeat menu opens");var option=(Rectangle)Call("FactoryDropOption",0,1);
  Check(new Rectangle(84,169,230,60).Contains(option.Center),"Regression option overlaps factory tab");
  Call("NodeDialogClick",option.Center,new KeyboardState());Check(factory.ProductionLines[0].RestEvery==2 && (bool)Get("_nodeFactoryOpen"),"Repeat option beats underlying factory tab");
  Check(!(bool)Call("NodeDialogClick",new Point(1500,500),new KeyboardState()),"Inspector passes map clicks through");
  int other=w.Nodes.All.First(n=>n.Id!=node).Id;Call("OpenNodeInspector",other);
  Check((int)Get("_populationNode")==other && (float)Get("_domesticSlide")==1 && (bool)Get("_nodeAssemblyOpen"),"Select another node without closing");Call("OpenNodeInspector",node);
  var expand=(Rectangle)Prop("MapExpandButton");Check(expand.X==1276,"Portrait and expand button slide without shrinking");Call("DomesticViewClick",expand.Center);Call("UpdateDomesticSlide",.2f);
  Check((int)Get("_populationCell")>=0 && (float)Get("_domesticSlide")==.5f,"Closing keeps panel while sliding");Call("UpdateDomesticSlide",.2f);Check((int)Get("_populationCell")==-1,"Closing clears inspector only when offscreen");
  for(int count=1;count<=12;count++)for(int i=0;i<count;i++)Check(CubeNet.RobotRingSlot(i,count).Length()>=47.99f,"Robots surround node with clear center");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");Set("_statusGlobe",false);
  foreach(float slide in new[]{0f,.5f,1f}){
   Call("OpenNodeInspector",node);Set("_domesticSlide",slide);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawStatusUi");batch.End();
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
   using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"inspector-{slide:0.0}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  Console.WriteLine("PASS: repeat menu priority, map input, continuous selection, linked slide/close, portrait position, robot ring and panel rendering.");Exit();
 }
}

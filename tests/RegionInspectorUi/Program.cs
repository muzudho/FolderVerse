using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new RegionChecks();game.Run();
sealed class RegionChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;int node,cell;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);node=w.Nodes.Current(0).Id;cell=w.Nodes.All[node].Cell;
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  var world=(WorldPreview)Get("_world");world.ShowSetup(w,true);var net=(CubeNet)Get("_net");net.Home(w);net.ShowRoutes=false;Set("_statusGlobe",true);Call("FocusStatusGlobe");
  var surface=w.Cells[cell];Check(world.ProjectVisible(surface.Center,surface.Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),(Rectangle)Prop("StatusGlobeArea"),out var at),"Region fixture visible");
  Set("_pointer",at.ToPoint());Call("GlobeCellClick",new MouseState(),true,true,false);
  Check((int)Get("_regionCell")==cell && (int)Get("_populationCell")==-1 && (bool)Get("_statusGlobe"),"Globe region click opens its inspector without switching view or opening a node");
  Call("UpdateDomesticSlide",.4f);
  Check((bool)Prop("InspectorVisible"),"Region inspector shares the panel animation");
  var nodes=w.Nodes.InCell(cell).OrderByDescending(n=>n.Population.Total).ThenBy(n=>n.Id).ToArray();
  var r=(Rectangle)typeof(Game1).GetMethod("RegionNodeButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!;
  Call("RegionInspectorClick",r.Center);int representative=(int)Get("_inspectedNode");Check(representative==nodes[0].Id,"Region switches the automation subject explicitly");
  w.Nodes.All[representative].Owner=0;
  Check((bool)Call("NodeAutomationClick",new Point(200,940)) && (int)Get("_supervisorDialog")!=0,"Region supervisor opens leave/recall controls");
  Call("SupervisorDialogInput",false,true,new KeyboardState());Check((int)Get("_supervisorDialog")==0,"Escape dismisses supervisor dialog");
  int foreign=Enumerable.Range(1,w.ActiveCount-1).First(n=>!w.Relations.Controls(n));w.Nodes.All[representative].Owner=foreign;
  Call("NodeAutomationClick",new Point(200,940));Check((int)Get("_supervisorDialog")==0,"Foreign regional supervisor remains read only");w.Nodes.All[representative].Owner=0;
  Call("OpenNodeInspector",node);Check((int)Get("_regionCell")==-1 && (int)Get("_populationNode")==node && (bool)Get("_nodeAssemblyOpen"),"Point selection retains the hangar inspector");
  Call("OpenRegionInspector",cell);Call("UpdateDomesticSlide",1f);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",false);
   var net=(CubeNet)Get("_net");net.Home(w);net.ShowRoutes=false;
   var chosen=w.Cells.First(c=>c.Face==net.Faces[0].Face);var panel=(Rectangle)Prop("NetPanel");var fit=net.Fit(panel);
   var at=net.RotateScreen(net.Faces[0].Project(chosen.Center)*fit.Scale+fit.Origin,panel).ToPoint();
   var canvas=(Rectangle)Call("CanvasBounds");Set("_previousMouse",new MouseState());((HashSet<int>)Get("_homePersuasionShown")).Add(0);
   var mouse=new MouseState(canvas.X+at.X*canvas.Width/1920,canvas.Y+at.Y*canvas.Height/1080,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
   Call("HandleInput",mouse,new KeyboardState(),new GameTime(),true);
   Check((int)Get("_regionCell")==chosen.Id && !(bool)Get("_statusGlobe"),"Actual unfolded-map region click opens its inspector and preserves view mode");Call("UpdateDomesticSlide",.4f);
  }
  if(frame==2){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using(var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight)){tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"region-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);}
  if(frame++==2){Call("CloseNodeInspector");Call("UpdateDomesticSlide",.4f);Check((int)Get("_regionCell")==-1 && !(bool)Prop("InspectorVisible"),"Close clears region inspector");Console.WriteLine("PASS: globe region click, distinct point/region inspectors, supervisor selection, permission checks, dialogs, close and full/half-size rendering.");Exit();}
 }
}

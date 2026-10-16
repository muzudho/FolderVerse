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
 WorldSetup w=null!;int node,foreignNode;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);((WorldPreview)Get("_world")).ShowSetup(w,true);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));((CubeNet)Get("_net")).Home(w);node=w.Nodes.Current(0).Id;Call("OpenNodeInspector",node);
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
  var close=(Rectangle)Prop("InspectorClose");Check(close.X==912,"Handle sits on inspector right edge");Call("DomesticViewClick",close.Center);Call("UpdateDomesticSlide",.2f);
  Check((int)Get("_populationCell")>=0 && (float)Get("_domesticSlide")==.5f,"Closing keeps panel while sliding");Call("UpdateDomesticSlide",.2f);Check((int)Get("_populationCell")==-1,"Closing clears inspector only when offscreen");
  foreach(string launcher in new[]{"StrategyToggle","WarButton","TransportOverviewButton"}){
   var launchButton=(Rectangle)Prop(launcher);
   Check((bool)Call("DomesticViewClick",launchButton.Center),"Large map launcher works: "+launcher);
   Check((bool)Prop("InspectorVisible"),"All inspector types are visible");Call("UpdateDomesticSlide",.2f);Check((float)Get("_domesticSlide")==.5f,"All panels share slide");
   if(launcher=="WarButton"){
    Set("_selectedMoveNode",-1);Call("MovementClick",new Point(320,700),new KeyboardState());Check((int)Get("_selectedMoveNode")==-1,"Sliding panel blank area does not select map target");
   }
   if(launcher=="TransportOverviewButton"){
    Set("_populationNode",-1);Call("TransportOverviewClick",new Point(320,700));Check((int)Get("_populationNode")==-1,"Sliding transport blank area does not select map source");
   }
   Call("UpdateDomesticSlide",.2f);Call("DomesticViewClick",((Rectangle)Prop("InspectorClose")).Center);Call("UpdateDomesticSlide",.2f);
   Check((bool)Prop("InspectorVisible"),"Closing keeps panel visible");Call("UpdateDomesticSlide",.2f);Check(!(bool)Prop("InspectorVisible"),"Shared handle closes every type");
  }
  int foreign=w.Nodes.All.First(n=>n.Owner>=0 && n.Owner!=0 && !(bool)Call("CanManageNodeFactory",n.Owner)).Id;
  foreignNode=foreign;Call("OpenNodeInspector",foreign);Check(!(bool)Prop("CanOpenHangar"),"Foreign node is read only");Call("OpenNodeFactory");Check(!(bool)Get("_nodeFactoryOpen"),"Foreign factory cannot be edited");Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);
  var net=(CubeNet)Get("_net");var world=(WorldPreview)Get("_world");net.Turn(w,1);
  bool attached=false;foreach(var face in net.Faces.ToArray()){
   for(int side=0;side<4;side++)if(net.AnimateAttachment(face.Face,side)){net.UpdateAnimation(1);attached=true;break;}
   if(attached)break;
  }
  Check(attached,"Use a rearranged face layout for camera roundtrip");net.Drag(new Vector2(37,-49));net.ZoomAt((Rectangle)Prop("NetPanel"),new Point(1200,500),240);
  foreach(bool globe in new[]{false,true}){
   var view=net.CaptureView();Set("_statusGlobe",globe);Set("_yawAngle",.7f);Set("_pitchAngle",-.3f);world.GlobeZoom=2.5f;world.GlobeAnchor=new Vector3(1,2,3);world.ManualOrientation=Quaternion.CreateFromYawPitchRoll(.4f,.2f,.1f);
   var orientation=world.ManualOrientation;Call("SaveDomesticCamera");net.Home(w);Set("_statusGlobe",!globe);world.ManualOrientation=null;world.GlobeZoom=1;world.GlobeAnchor=null;
   Call("RestoreDomesticCamera");var restored=net.CaptureView();
   Check(restored.Faces.SequenceEqual(view.Faces) && restored.Pan==view.Pan && restored.Zoom==view.Zoom && restored.Rotation==view.Rotation,"Net connections, rotation, zoom and pan restored");
   Check((bool)Get("_statusGlobe")==globe && world.ManualOrientation==orientation && world.GlobeZoom==2.5f && world.GlobeAnchor==new Vector3(1,2,3),"Globe camera and selected view restored");
  }
  Check(((Rectangle)Prop("PopulationTurnButton")).Height==96,"Turn-end button is 1.5 times taller");
  for(int count=1;count<=12;count++)for(int i=0;i<count;i++)Check(CubeNet.RobotRingSlot(i,count).Length()>=47.99f,"Robots surround node with clear center");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");Set("_statusGlobe",false);
  foreach(string kind in new[]{"node","foreign","war","transport","strategy","map"})foreach(float slide in new[]{0f,.5f,1f}){
   Call("ResetInspector");if(kind=="map"){}else if(kind=="foreign")Call("OpenNodeInspector",foreignNode);else if(kind=="node")Call("OpenNodeInspector",node);else if(kind=="war")Call("OpenWarPlanning");else if(kind=="transport")Call("OpenTransportOverview");else Call("OpenStrategyInspector");Set("_domesticSlide",kind=="map"?0f:slide);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawStatusUi");batch.End();
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
   int borderX=(int)((176+(kind=="map"?0:960*slide)+125)*pp.BackBufferWidth/1920f),borderY=(int)(820*pp.BackBufferHeight/1080f);
   Check(pixels[borderY*pp.BackBufferWidth+borderX]==w.OwnerColor(w.PlayerSlot),"Portrait border displays current country color: "+kind);
   using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"inspector-{kind}-{slide:0.0}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  }
  foreach(var kind in new[]{BattleKind.Node,BattleKind.Edge}){
   Call("ResetInspector");var net=(CubeNet)Get("_net");net.Home(w);net.ResetView();
   int target=w.Routes.Neighbors(w.Nodes.All[node]).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First();
   var e=new RobotEncounter{Id=900+(int)kind,Kind=kind,Source=node,Target=target};var state=new BattleState(4);state.Armies.Add(new(){Owner=0});state.Units.Add(new(){Id=1,Owner=0,Position=new(1,1)});e.Battles.Add(state);w.Campaign.RobotBattles.Encounters.Add(e);
   Call("OpenWarPlanning");Set("_domesticSlide",1f);Call("FitDestinationView");var badge=(Rectangle)Call("ContinuationBadge",e);Check(!badge.IsEmpty,"Continuation badge visible for "+kind);
   Call("MovementClick",badge.Center,new KeyboardState());Check((int)Get("_withdrawalEncounter")==e.Id,"Click selects continuation battlefield");
   Call("MovementClick",new Point(400,380),new KeyboardState());Check(e.WithdrawalOwners.Contains(0),"Withdrawal button reserves controlled army");Call("MovementClick",new Point(400,380),new KeyboardState());Check(e.WithdrawalOwners.Count==0,"Withdrawal reservation cancel works");
   GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawStatusUi");batch.End();
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"withdrawal-{kind}.png"));tex.SaveAsPng(output,tex.Width,tex.Height);w.Campaign.RobotBattles.Encounters.Remove(e);
  }
  Call("ResetInspector");Call("OpenStrategyInspector");Set("_domesticSlide",1f);Set("_trainingOpen",true);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawStatusUi");batch.End();
  {
   var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);Check(pixels[(int)(205*pp.BackBufferHeight/1080f)*pp.BackBufferWidth+(int)(920*pp.BackBufferWidth/1920f)]==new Color(27,49,62),"Training dialog covers inspector handles");
  }
  Console.WriteLine("PASS: repeat menu priority, map input, continuous selection, linked slide/close, portrait position, robot ring and panel rendering.");Exit();
 }
}

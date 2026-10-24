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
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));((CubeNet)Get("_net")).Home(w);node=w.Nodes.Current(0).Id;
  foreach(bool globe in new[]{false,true}){
   Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",globe);Call("FocusStatusGlobe");
   var cameraNet=(CubeNet)Get("_net");var cameraWorld=(WorldPreview)Get("_world");var cameraPanel=(Rectangle)Prop("NetPanel");
   float scale=cameraNet.Fit(cameraPanel).Scale,zoom=cameraWorld.GlobeZoom;var orientation=cameraWorld.ManualOrientation;
   Call("OpenWarPlanning");Check((bool)Get("_statusGlobe")==globe && cameraWorld.GlobeZoom==zoom && cameraWorld.ManualOrientation==orientation,"War planning preserves view and globe camera");
   foreach(float dt in new[]{.1f,.1f,.2f}){
    Call("UpdateDomesticSlide",dt);var projection=(Rectangle)Prop("NetPanel");
    Check(projection.Width==1840 && Math.Abs(cameraNet.Fit(projection).Scale-scale)<.001f && cameraWorld.GlobeZoom==zoom,"Inspector slides without shrinking globe, net or nodes");
   }
   var viewport=(Rectangle)Prop("MapViewBounds");float baseLeft=(float)Get("_domesticMapLeft");
   Check(Math.Abs(baseLeft+((Rectangle)Prop("NetPanel")).X-40-viewport.Left-15)<1.1f,"Map waits then slides with 15-pixel left padding");
  }
  Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",false);
  var fixedPanel=(Rectangle)Prop("NetPanel");var fixedNet=(CubeNet)Get("_net");var fixedPosition=fixedNet.NodePosition(w,w.Nodes.All[node],fixedPanel);
  Call("OpenNodeInspector",node);
  foreach(float dt in new[]{.1f,.1f,.2f}){
   Call("UpdateDomesticSlide",dt);
   var slidingPanel=(Rectangle)Prop("NetPanel");var slidingPosition=fixedNet.NodePosition(w,w.Nodes.All[node],slidingPanel);
   Check(slidingPanel.Y==fixedPanel.Y && slidingPanel.Size==fixedPanel.Size && Math.Abs(slidingPosition.Y-fixedPosition.Y)<.001f && Math.Abs(slidingPosition.X-fixedPosition.X-(slidingPanel.X-fixedPanel.X))<.001f,"Opening a net node slides only on X without scaling or centering");
  }
  var openedNetPanel=(Rectangle)Prop("NetPanel");Check(openedNetPanel.X>fixedPanel.X,"Net slides right when inspector opens");
  var netStart=fixedNet.NodePosition(w,w.Nodes.All[node],openedNetPanel);float netZoom=fixedNet.Zoom;
  int netCenter=((Rectangle)Prop("MapViewBounds")).Center.X;
  Call("UpdateDomesticSlide",.225f);var netHalf=fixedNet.NodePosition(w,w.Nodes.All[node],(Rectangle)Prop("NetPanel"));
  Check(Math.Abs(netHalf.X-(netStart.X+netCenter)/2)<.01f && Math.Abs(netHalf.Y-netStart.Y)<.01f,"Net recenters smoothly on X only after inspector opens");
  Call("UpdateDomesticSlide",.225f);var netEnd=fixedNet.NodePosition(w,w.Nodes.All[node],(Rectangle)Prop("NetPanel"));
  Check(Math.Abs(netEnd.X-netCenter)<.01f && Math.Abs(netEnd.Y-netStart.Y)<.01f && fixedNet.Zoom==netZoom,"Net selection reaches horizontal center without changing zoom or Y");
  Call("OpenNodeInspector",w.Nodes.All.First(n=>n.Id!=node).Id);
  Check((Rectangle)Prop("NetPanel")==openedNetPanel,"Selecting another net node does not recenter");
  Call("CloseNodeInspector");Call("UpdateDomesticSlide",.2f);Check(((Rectangle)Prop("NetPanel")).X<openedNetPanel.X && ((Rectangle)Prop("NetPanel")).Y==fixedPanel.Y,"Closing slides the net left without changing Y");Call("UpdateDomesticSlide",.2f);
  int closedNode=w.Nodes.All.First(n=>n.Id!=node).Id;var closedNetStart=fixedNet.NodePosition(w,w.Nodes.All[closedNode],(Rectangle)Prop("NetPanel"));
  Call("UpdateDomesticSlide",.45f);var closedNetEnd=fixedNet.NodePosition(w,w.Nodes.All[closedNode],(Rectangle)Prop("NetPanel"));
  Check(Math.Abs(closedNetEnd.X-((Rectangle)Prop("MapViewBounds")).Center.X)<.01f && Math.Abs(closedNetEnd.Y-closedNetStart.Y)<.01f && fixedNet.Zoom==netZoom,"Net recenters selected node on X after inspector closes");
  if(Environment.GetCommandLineArgs().Contains("--camera-only")){
   Set("_statusGlobe",true);Call("FocusStatusGlobe");
   var globeWorld=(WorldPreview)Get("_world");var globePanel=(Rectangle)Prop("StatusGlobeArea");
   globeWorld.GlobeAnchor=globeWorld.GlobeAnchor.GetValueOrDefault()+new Vector3(.13f,.07f,0);
   globeWorld.GlobePan=new Vector2(.03f,-.02f);
   var anchor=globeWorld.GlobeAnchor;var globePan=globeWorld.GlobePan;var globeZoom=globeWorld.GlobeZoom;var globeOrientation=globeWorld.ManualOrientation;
   var point=w.Routes.Position(w.Nodes.All[node].Cell,w.Nodes.All[node].Center);var normal=w.Cells[w.Nodes.All[node].Cell].Normal;
   Check(globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),globePanel,out var globeAt),"Globe click fixture visible");
   Set("_pointer",globeAt.ToPoint());Call("GlobeCellClick",new MouseState(),true,true,false);
   Check((int)Get("_populationNode")==node,"Actual globe click opens the node inspector");
   foreach(float dt in new[]{.1f,.1f,.2f}){
    Call("UpdateDomesticSlide",dt);
    var slidingGlobe=(Rectangle)Prop("StatusGlobeArea");
    Check(slidingGlobe.Y==globePanel.Y && slidingGlobe.Size==globePanel.Size && globeWorld.GlobeAnchor==anchor && globeWorld.GlobePan==globePan && globeWorld.GlobeZoom==globeZoom && globeWorld.ManualOrientation==globeOrientation,"Globe node click preserves Y, size, anchor, pan, zoom and orientation");
    Check(globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),slidingGlobe,out var current) && Math.Abs(current.Y-globeAt.Y)<.001f && Math.Abs(current.X-globeAt.X-(slidingGlobe.X-globePanel.X))<.001f,"Globe slides only horizontally without centering on the clicked node");
   }
   var openedGlobePanel=(Rectangle)Prop("StatusGlobeArea");Check(openedGlobePanel.X>globePanel.X,"Globe slides right when inspector opens");
   globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),openedGlobePanel,out var globeStart);
   int globeCenter=((Rectangle)Prop("MapViewBounds")).Center.X;
   Call("UpdateDomesticSlide",.225f);globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),openedGlobePanel,out var globeHalf);
   Check(Math.Abs(globeHalf.X-(globeStart.X+globeCenter)/2)<.01f && Math.Abs(globeHalf.Y-globeStart.Y)<.01f,"Globe recenters smoothly on X only after inspector opens");
   Call("UpdateDomesticSlide",.225f);globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),openedGlobePanel,out var globeEnd);
   Check(Math.Abs(globeEnd.X-globeCenter)<.01f && Math.Abs(globeEnd.Y-globeStart.Y)<.01f && globeWorld.GlobeZoom==globeZoom && globeWorld.ManualOrientation==globeOrientation && globeWorld.GlobeAnchor==anchor,"Globe horizontal recenter preserves Y, zoom, orientation and anchor");
   Call("OpenNodeInspector",w.Nodes.All.First(n=>n.Id!=node).Id);Check((Rectangle)Prop("StatusGlobeArea")==openedGlobePanel,"Switching globe nodes does not recenter");
   Call("OpenNodeInspector",node);
   Call("CloseNodeInspector");Call("UpdateDomesticSlide",.2f);Check(((Rectangle)Prop("StatusGlobeArea")).X<openedGlobePanel.X && ((Rectangle)Prop("StatusGlobeArea")).Y==globePanel.Y,"Closing slides globe left without changing Y");Call("UpdateDomesticSlide",.2f);
   var closedGlobePanel=(Rectangle)Prop("StatusGlobeArea");globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),closedGlobePanel,out var closedGlobeStart);
   Call("UpdateDomesticSlide",.45f);globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),closedGlobePanel,out var closedGlobeEnd);
   Check(Math.Abs(closedGlobeEnd.X-((Rectangle)Prop("MapViewBounds")).Center.X)<.01f && Math.Abs(closedGlobeEnd.Y-closedGlobeStart.Y)<.01f && globeWorld.GlobeZoom==globeZoom,"Globe recenters selected node on X after inspector closes");
   foreach(bool globe in new[]{false,true}){
    Call("ResetInspector");Set("_statusGlobe",globe);Call("QueueInspectorCenter",node);
    if(globe){Call("FocusStatusGlobe");globeWorld.GlobePan+=new Vector2(.12f,-.08f);}
    else{fixedNet.ScreenAngle=.61f;fixedNet.Drag(new Vector2(83,-57));}
    var savedOrientation=globeWorld.ManualOrientation;float savedAngle=fixedNet.ScreenAngle;var savedAnchor=globeWorld.GlobeAnchor;
    Vector2 SelectedAt(){
     if(!globe)return fixedNet.NodePosition(w,w.Nodes.All[node],(Rectangle)Prop("NetPanel"));
     Check(globeWorld.ProjectVisible(point,normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),(Rectangle)Prop("StatusGlobeArea"),out var at),"Selected globe point remains front facing");return at;
    }
    foreach(int wheel in new[]{120,-120,960,-960,12000,120,-24000,-120}){
     var beforeZoom=SelectedAt();Call("ZoomSelectedPoint",wheel);var afterZoom=SelectedAt();
     Check(Vector2.Distance(beforeZoom,afterZoom)<.02f,"Wheel zoom pins selected point at the same screen X and Y, including zoom limits");
     Check(globeWorld.ManualOrientation==savedOrientation && globeWorld.GlobeAnchor==savedAnchor && fixedNet.ScreenAngle==savedAngle,"Pinned zoom preserves orientation and globe anchor");
    }
   }
   Console.WriteLine("PASS: two-stage horizontal inspector slides preserve Y/scale; globe and rotated net wheel zoom pin selected points through zoom limits.");Exit();return;
  }
  Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",true);Call("OpenNodeInspector",node);
  Check((int)Prop("NodeInspectorOffset")==-960,"Panel begins outside left edge");Call("UpdateDomesticSlide",.2f);
  Check((float)Get("_domesticSlide")==.5f && (int)Prop("NodeInspectorOffset")==-480,"Shared half transition");
  var map=(Rectangle)Prop("MapViewBounds");Check(map.X==520 && map.Right==1880 && map.Y==142 && map.Height==820,"Map ratio changes horizontally with panel");
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

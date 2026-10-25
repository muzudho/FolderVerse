using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new RotationGame();game.Run();
sealed class RotationGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 WorldSetup w=null!;CubeNet net=null!;WorldPreview world=null!;int frame;
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  for(int axis=0;axis<3;axis++){
   var direction=ToyOrientationRenderer.AxisDirection(axis);var screw=ToyOrientationRenderer.ScrewCenter(axis);
   Check(Vector3.Dot(screw,direction)<-2 && Vector3.Cross(screw,direction).LengthSquared()<.00001f,"Each screw is separated along its negative axis");
   var tint=ToyOrientationRenderer.AxisColor(axis);Check(axis==0?tint.R>tint.G && tint.R>tint.B:axis==1?tint.G>tint.R && tint.G>tint.B:tint.B>tint.R && tint.B>tint.G,"Axis screw colors follow RGB");
   Check(tint==((Color[])typeof(Game1).GetField("AxisColors",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!)[axis],"Screws and rotation controls share their colors");
  }
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  world=(WorldPreview)Get("_world");world.ShowSetup(w,true);net=(CubeNet)Get("_net");net.Home(w);
  var baseOrientation=world.ManualOrientation;
  var rockA=Game1.GlobeCradle(0);var rockB=Game1.GlobeCradle(3);
  Check(rockA!=Matrix.Identity && rockA!=rockB && Math.Abs(rockA.Determinant()-1)<.0001,"Slow cradle is an initial three-dimensional rotation and preserves shape");
  world.GlobeRock=rockB;Check(world.DisplayOrientation(0,0)==world.ViewOrientation(0,0)*rockB && world.ManualOrientation==baseOrientation,"Cradle does not overwrite saved manual orientation");world.GlobeRock=Matrix.Identity;
  var occupied=w.Nodes.All.Where(n=>w.Robots.Nodes[n.Id].Robots.Count>0).Take(2).Select(n=>n.Id).ToArray();
  Check(occupied.Length==2,"Visibility fixture has two occupied nodes");
  using(var target=new RenderTarget2D(GraphicsDevice,1920,1080)){
   var previous=GraphicsDevice.GetRenderTargets();GraphicsDevice.SetRenderTarget(target);var batch=(SpriteBatch)Get("_spriteBatch");
   var drawLayer=typeof(CubeNet).GetMethod("DrawRobotLayer",BindingFlags.NonPublic|BindingFlags.Instance)!;
   foreach(var robotAt in new[]{new Vector2(300,300),new Vector2(300,104)}){
    batch.Begin();drawLayer.Invoke(net,new object[]{Get("_ui"),w,new Rectangle(100,100,400,400),(Func<Node,Vector2?>)(n=>occupied.Contains(n.Id)?robotAt:null),1f});batch.End();
    Check(net.RobotBadges.Select(b=>b.Node).Distinct().Count()==2,"Overlapping robot groups and partially clipped edge groups both remain visible");
   }
   GraphicsDevice.SetRenderTargets(previous);net.ClearGlobeRobotBadges();
  }
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",true);Call("FocusStatusGlobe");
  var panel=(Rectangle)Prop("NetPanel");var node=w.Nodes.Current(0);var position=w.Routes.Position(node.Cell,node.Center);
  Check(world.ProjectVisible(position,w.Cells[node.Cell].Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),panel,out var at),"Current location visible");
  Check(Vector2.Distance(at,new Vector2(panel.Center.X,panel.Center.Y))<.01f,"Current location centered");
  var cameraBeforeRock=world.ManualOrientation;Call("UpdateGlobeRock",.5d);
  Check(world.GlobeRock!=Matrix.Identity && world.ManualOrientation==cameraBeforeRock,"Idle globe rocks without modifying the user's camera");
  Check(world.ProjectVisible(position,w.Cells[node.Cell].Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),panel,out var rockedAt) && (int)Call("HitGlobeNode",rockedAt.ToPoint(),new[]{node.Id})==node.Id,"Rocked globe's projected nodes remain clickable");
  Set("_statusGlobe",false);Call("UpdateGlobeRock",.1d);Check(world.GlobeRock==Matrix.Identity,"Rocking stops in unfolded view");Set("_statusGlobe",true);
  var toy=((ValueTuple<Rectangle,Rectangle,Rectangle>)typeof(Game1).GetMethod("OrientationAreas",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{panel,false})!).Item2;
  foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1}){
   var from=Quaternion.CreateFromRotationMatrix(world.ViewOrientation((float)Get("_yawAngle"),(float)Get("_pitchAngle")));
   var button=(Rectangle)Call("AxisButton",toy,axis,sign);Check((bool)Call("OrientationControlClick",button.Center),"Axis click consumed");
   Call("UpdateOrientationRotation",.2f);var half=world.ManualOrientation!.Value;Check(Math.Abs(Quaternion.Dot(from,half))<.999f,"Rotation animates");
   Call("UpdateOrientationRotation",.2f);var end=world.ManualOrientation!.Value;float angle=2*MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(from,end)),0,1));
   Check(Math.Abs(angle-MathHelper.ToRadians(30))<.001f,"Exactly 30 degrees on each axis and sign");
  }
  Set("_statusGlobe",false);
  var originalNet=net.CaptureView();int movingBelly=0;
  var bellyNodes=w.Nodes.All.Where(n=>w.Cells[n.Cell].Face==4).Take(2).ToArray();
  var bellyDirection=w.Routes.Position(bellyNodes[1].Cell,bellyNodes[1].Center)-w.Routes.Position(bellyNodes[0].Cell,bellyNodes[0].Center);
  for(int center=0;center<6;center++){
   net.SetCenter(w,center);net.ScreenAngle=.37f;var layout=net.CaptureView();
   for(int face=0;face<6;face++)for(int side=0;side<4;side++){
    net.RestoreView(layout);if(!net.AnimateAttachment(face,side))continue;
    var start=net.BellyOrientation;
    for(int sample=0;sample<=4;sample++){
     var a=net.AnimatedNodePosition(w,bellyNodes[0],panel);var b=net.AnimatedNodePosition(w,bellyNodes[1],panel);
     var direction=Vector3.TransformNormal(bellyDirection,net.BellyOrientation);
     Check(Vector2.Distance(Vector2.Normalize(b-a),Vector2.Normalize(new Vector2(direction.X,-direction.Y)))<.001f,"Robot belly matches animated belly face and screen rotation throughout attachment");
     if(sample<4)net.UpdateAnimation(.85f/4);
    }
    if(Vector3.Distance(Vector3.TransformNormal(Vector3.UnitX,start),Vector3.TransformNormal(Vector3.UnitX,net.BellyOrientation))>.1f)movingBelly++;
   }
  }
  Check(movingBelly>0,"Attachment tests exercise rotating belly faces");net.RestoreView(originalNet);
  for(int i=0;i<12;i++){
   Call("OrientationControlClick",((Rectangle)Call("AxisButton",toy,2,1)).Center);Call("UpdateOrientationRotation",.4f);
   var point=net.NodePosition(w,node,panel);Check(net.HitNode(w,panel,point.ToPoint())==node.Id,"Rotated node hit");
   Check(net.Hit(w,panel,point.ToPoint())==node.Cell,"Rotated cell hit");
   Check(net.MicroPoint(w,node.Cell,panel,point.ToPoint())==node.Center,"Rotated microcell hit");
  }
  Check(Math.Abs(net.ScreenAngle-MathHelper.TwoPi)<.00001f,"12 steps complete a circle");
  net.ScreenAngle=.5235988f;var saved=net.CaptureView();net.Home(w);net.RestoreView(saved);Check(net.ScreenAngle==saved.ScreenAngle,"Net angle restored");
  Set("_statusGlobe",true);Call("FocusStatusGlobe");
  foreach(var screen in new[]{"Rolling","CastRolling","PlacementRolling"}){
   Set("_screen",Enum.Parse(Get("_screen").GetType(),screen));
   Check(!(bool)Prop("OrientationControlsVisible"),"Slot previews hide orientation controls");
   var previewToy=(Rectangle)Prop("OrientationToyArea");var previewOrientation=world.ManualOrientation;
   foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1})
    Check(!(bool)Call("OrientationControlClick",((Rectangle)Call("AxisButton",previewToy,axis,sign)).Center),"Hidden axis buttons ignore clicks");
   Check(!(bool)Call("OrientationControlClick",((Rectangle)Prop("OrientationResetButton")).Center) && world.ManualOrientation==previewOrientation,"Hidden reset ignores clicks");
  }
  foreach(var screen in new[]{"Review","CastReview","PlacementReview","WorldStatus"}){
   Set("_screen",Enum.Parse(Get("_screen").GetType(),screen));
   Check((bool)Prop("OrientationControlsVisible"),"Controls return after the slot stops");
  }
  foreach(int sign in new[]{-1,1}){
   Set("_screen",Enum.Parse(Get("_screen").GetType(),"CastReview"));Set("_yawAngle",0f);Set("_pitchAngle",0f);world.ManualOrientation=null;
   var autoToy=(Rectangle)Prop("OrientationToyArea");
   Check((bool)Call("OrientationControlClick",((Rectangle)Call("AxisButton",autoToy,1,sign)).Center),"Automatic speed button consumed");
   Check(!(bool)Prop("OrientationRotating"),"Automatic rotation continues instead of pausing");
   float previous=0;
   for(int i=0;i<40;i++){
    Call("AdvanceAutomaticOrientation",.1f);var matrix=world.ViewOrientation(0,0);float yaw=MathF.Atan2(-matrix.M13,matrix.M11);
    float speed=(yaw-previous)/.1f;Check(sign>0?speed>=.2999f:speed>0 && speed<=.3001f,"Smooth acceleration/deceleration preserves direction");previous=yaw;
   }
   Check(Math.Abs(previous-(1.2f+sign*MathHelper.ToRadians(30)))<.0001f,"Speed pulse adds/subtracts exactly 30 degrees");
   Call("AdvanceAutomaticOrientation",1f);var final=world.ViewOrientation(0,0);float finalYaw=MathF.Atan2(-final.M13,final.M11);
   Check(Math.Abs((finalYaw-previous)-.3f)<.0001f,"Normal speed resumes");
  }
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var reset=(Rectangle)Prop("OrientationResetButton");Check(reset.Size==new Point(84,28) && reset.Top>((Rectangle)Prop("OrientationToyArea")).Bottom,"Small reset below circle");
  Call("OrientationControlClick",reset.Center);Check(world.ViewOrientation(0,0)==Matrix.Identity,"Compact reset restores orientation");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"CastReview"));Set("_yawAngle",0f);Set("_pitchAngle",0f);world.ManualOrientation=null;
  var queueToy=(Rectangle)Prop("OrientationToyArea");var queuedButton=(Rectangle)Call("AxisButton",queueToy,1,1);
  Call("OrientationControlClick",queuedButton.Center);Call("OrientationControlClick",queuedButton.Center);
  for(int i=0;i<70;i++)Call("AdvanceAutomaticOrientation",.1f);
  Check(Math.Abs(Quaternion.Dot(world.ManualOrientation!.Value,Quaternion.CreateFromAxisAngle(Vector3.UnitY,2.1f+MathHelper.ToRadians(60))))>.99999f,"Two clicks queue two complete 30-degree pulses");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var xPlus=(Rectangle)Call("AxisButton",(Rectangle)Prop("OrientationToyArea"),0,1);var xMinus=(Rectangle)Call("AxisButton",(Rectangle)Prop("OrientationToyArea"),0,-1);Check(xPlus.Y>xMinus.Y,"Only X button positions swapped");
  Rectangle Layer(int i)=>(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!;
  Call("RouteLayerClick",Layer(5).Center);Check(!net.ShowRobots,"Robot display off");Call("RouteLayerClick",Layer(5).Center);Check(net.ShowRobots,"Robot display on");
  Call("RouteLayerClick",Layer(6).Center);Check(!net.ShowOtherCountries && net.ControlledCountryColors && world.ControlledCountryColors,"Other country toggle controls both color renderers");
  var visiblePanel=(Rectangle)Prop("NetPanel");
  foreach(var post in w.Nodes.All.Where(n=>n.Owner>=0 && !w.Relations.Controls(n.Owner))){
   Check(!net.NodeVisible(w,post),"Foreign node hidden");var hiddenAt=net.NodePosition(w,post,visiblePanel);
   if(visiblePanel.Contains(hiddenAt.ToPoint()))Check(net.HitNode(w,visiblePanel,hiddenAt.ToPoint(),new[]{post.Id})==-1,"Foreign node cannot be clicked in net");
   if(world.ProjectVisible(w.Routes.Position(post.Cell,post.Center),w.Cells[post.Cell].Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),visiblePanel,out var globeAt))Check((int)Call("HitGlobeNode",globeAt.ToPoint(),new[]{post.Id})==-1,"Foreign node cannot be clicked on globe");
  }
  Check(w.Nodes.All.Where(n=>n.Owner<0 || w.Relations.Controls(n.Owner)).All(n=>net.NodeVisible(w,n)),"Own, subordinate and unowned nodes stay visible");

  foreach(var c in w.Cells){int owner=w.Nodes.FullOwner(c.Id);if(owner>=0 && !w.Relations.Controls(owner))Check(net.TerrainColor(w,c.Id,.6f)==WorldTerrain.ColorAt(.6f,c.Normal),"Foreign color hidden");}
  Call("RouteLayerClick",Layer(6).Center);Check(net.ShowOtherCountries && !net.ControlledCountryColors && !world.ControlledCountryColors,"Foreign colors restored");
  net.ShowMapSymbols=net.ShowEquator=true;
  Call("RouteLayerClick",Layer(0).Center);Check(!net.ShowMapSymbols && net.ShowEquator,"Coordinates independent from equator");
  Call("RouteLayerClick",Layer(4).Center);Check(!net.ShowMapSymbols && !net.ShowEquator,"Equator independent from coordinates");
  Set("_statusGlobe",false);net.ShowMapSymbols=true;
  Check(!(bool)Call("MapOverlayAvailable",0),"Net coordinate toggle disabled");Call("RouteLayerClick",Layer(0).Center);Check(net.ShowMapSymbols,"Disabled click preserves coordinate setting");
  var netReset=(Rectangle)Prop("OrientationResetButton");var netToy=(Rectangle)Prop("OrientationToyArea");
  Check(netReset.Width==28 && netReset.Center.X==netToy.Center.X && netReset.Bottom<netToy.Top,"Net zero at 12 o'clock outside circle");
  var before=net.CaptureView();net.ScreenAngle=.5f;Call("OrientationControlClick",netReset.Center);
  Check(net.ScreenAngle==0 && net.Faces.SequenceEqual(before.Faces) && net.Pan==before.Pan && net.Zoom==before.Zoom,"Net zero preserves layout and camera");
  foreach(int sign in new[]{-1,1})Check(Vector2.Distance(((Rectangle)Call("AxisButton",netToy,2,sign)).Center.ToVector2(),netToy.Center.ToVector2())>netToy.Width/2+20,"Net buttons outside circle");
  Set("_statusGlobe",true);net.ShowEquator=true;Call("FocusStatusGlobe");
  Check(!(bool)Prop("MapDragSlide"),"Globe defaults to rotation");
  var dragButton=(Rectangle)typeof(Game1).GetField("MapDragButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  void SelectDrag(int index){Call("RouteLayerClick",dragButton.Center);Call("RouteLayerClick",((Rectangle)typeof(Game1).GetMethod("MapDragOption",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{index})!).Center);}
  void Drag(int dx,int dy){
   var canvas=(Rectangle)Call("CanvasBounds");var start=((Rectangle)Prop("MapViewBounds")).Center;
   Microsoft.Xna.Framework.Input.MouseState Mouse(Point p)=>new(canvas.X+(int)Math.Round(p.X*canvas.Width/1920d),canvas.Y+(int)Math.Round(p.Y*canvas.Height/1080d),0,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Pressed,Microsoft.Xna.Framework.Input.ButtonState.Released,Microsoft.Xna.Framework.Input.ButtonState.Released);
   Set("_previousMouse",new Microsoft.Xna.Framework.Input.MouseState());Set("_pointer",start);Call("DragStatusMap",Mouse(start),true);
   Set("_previousMouse",Mouse(start));Set("_pointer",new Point(start.X+dx,start.Y+dy));Call("DragStatusMap",Mouse(new(start.X+dx,start.Y+dy)),true);
   Call("DragStatusMap",new Microsoft.Xna.Framework.Input.MouseState(),true);
  }
  var orientationBefore=world.ManualOrientation;Drag(40,15);Check(world.ManualOrientation!=orientationBefore && world.GlobePan==Vector2.Zero,"Globe rotation changes orientation only");
  SelectDrag(1);Check((bool)Prop("MapDragSlide"),"Globe slide selected");
  var area=(Rectangle)Prop("StatusGlobeArea");var current=w.Nodes.Current(0);var cell=w.Cells[current.Cell];var p=w.Routes.Position(current.Cell,current.Center);
  world.ProjectVisible(p,cell.Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),area,out var oldAt);orientationBefore=world.ManualOrientation;
  Drag(45,20);world.ProjectVisible(p,cell.Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),area,out var movedAt);
  Check(world.ManualOrientation==orientationBefore && Vector2.Distance(movedAt-oldAt,new(45,20))<2,"Globe slide moves projection by mouse delta without rotating");
  Check((int)Call("HitGlobeNode",movedAt.ToPoint(),new[]{current.Id})==current.Id,"Slid globe node stays clickable");
  Set("_statusGlobe",false);Check((bool)Prop("MapDragSlide"),"Net defaults to slide independently");var panBefore=net.Pan;var angleBefore=net.ScreenAngle;Drag(35,15);Check(net.Pan!=panBefore && net.ScreenAngle==angleBefore,"Net slide changes pan only");
  SelectDrag(0);panBefore=net.Pan;Drag(40,0);Check(net.ScreenAngle!=angleBefore && net.Pan==panBefore,"Net rotation changes angle only");
  Set("_statusGlobe",true);Check((bool)Prop("MapDragSlide"),"Globe remembers slide across view switches");SelectDrag(0);Set("_statusGlobe",false);Check(!(bool)Prop("MapDragSlide"),"Net remembers rotation across view switches");SelectDrag(1);
  Set("_statusGlobe",true);Call("FocusStatusGlobe");Check(world.GlobePan==Vector2.Zero,"Current-location centering resets globe slide");net.ScreenAngle=0;net.ResetView();
  Console.WriteLine("PASS: independent right-drag defaults, all four drag modes, projected translation and clickable slid nodes.");
  foreach(bool globe in new[]{true,false}){
   Set("_statusGlobe",globe);net.ScreenAngle=.6f;
   var handle=(Rectangle)Prop("OrientationToyArea");var start=handle.Center;
   var pan=world.GlobePan;var rotation=world.ManualOrientation;var angle=net.ScreenAngle;
   var nodeAt=net.NodePosition(w,node,panel);
   Microsoft.Xna.Framework.Input.MouseState Mouse(Point at,bool held)=>new(at.X,at.Y,0,held?Microsoft.Xna.Framework.Input.ButtonState.Pressed:Microsoft.Xna.Framework.Input.ButtonState.Released,0,0,0,0);
   Set("_pointer",start);Check((bool)Call("DragOrientationRobot",Mouse(start,true),true,true),"Robot captures left press");
   var end=start+new Point(42,18);Set("_previousMouse",Mouse(start,true));Set("_pointer",end);
   Check((bool)Call("DragOrientationRobot",Mouse(end,true),true,false),"Robot captures motion");
   Check(globe?Vector2.Distance(world.GlobePan-pan,new Vector2(42f/panel.Width,18f/panel.Height))<.001f:Vector2.Distance(net.NodePosition(w,node,panel)-nodeAt,new(42,18))<.01f,"Robot slide follows screen delta in both views");
   Check(world.ManualOrientation==rotation && net.ScreenAngle==angle,"Robot drag preserves rotation");
   Check((Vector2)Get("_orientationDragOffset")!=Vector2.Zero,"Robot visibly follows drag");
   var globePan=world.GlobePan;var netPan=net.Pan;
   Call("DragOrientationRobot",Mouse(end,false),true,false);
   Check((Vector2)Get("_orientationDragOffset")==Vector2.Zero && !(bool)Get("_orientationDragging") && world.GlobePan==globePan && net.Pan==netPan,"Release returns only robot to center");
  }
  Set("_statusGlobe",true);Call("FocusStatusGlobe");net.ScreenAngle=0;net.ResetView();
  Console.WriteLine("PASS: direction robot left drag pans both views, preserves rotation and springs back without moving the map.");

 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");
  if(frame==0)Set("_mapDragListOpen",true);
  if(frame==1){Set("_statusGlobe",false);Set("_mapDragListOpen",true);}
  if(frame==2){Set("_statusGlobe",true);Call("StartMapTransition");}
  if(frame==9){Call("UpdateMapTransition",.1f);Check(!(bool)Get("_statusGlobe"),"Unfold completes");Call("StartMapTransition");}
  if(frame>=2 && frame<=8)Set("_mapTransitionAge",(frame-2)*.6f);
  if(frame>=9 && frame<=15)Set("_mapTransitionAge",(frame-9)*.6f);
  if(frame==16){Call("UpdateMapTransition",.1f);Check((bool)Get("_statusGlobe"),"Fold completes");Call("StartMapTransition");Set("_mapOrientation",Quaternion.CreateFromAxisAngle(Vector3.UnitX,MathHelper.Pi));Set("_mapTransitionAge",2.4f);}
  if(frame>=17){
   Set("_mapTransitionAge",-1f);Set("_screen",Enum.Parse(Get("_screen").GetType(),frame==17?"Rolling":frame==18?"CastReview":"WorldStatus"));
   if(frame>=19){Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",frame>=21);net.ShowMapSymbols=frame!=21;net.ShowEquator=frame is 19 or 21;}
   if(frame>=23){Set("_statusGlobe",frame>=25);net.ShowRobots=frame is 24 or 26;if(frame==24)net.ScreenAngle=MathHelper.Pi;if(frame==26){var other=(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{6})!;Call("RouteLayerClick",other.Center);}}
   world.GlobeRock=frame is 21 or 22?Game1.GlobeCradle((frame-21)*4):Matrix.Identity;
   base.Draw(t);
  }
  else{GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawStatusUi");batch.End();}
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"rotation-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame==16)Check(pixels.Count(c=>c==new Color(115,119,125))>100,"Back faces render in solid gray");
  if(frame>=19){
   Rectangle Layer(int i)=>(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!;
   Color Lamp(int i){var r=Layer(i);int x=(r.X+12)*pp.BackBufferWidth/1920,y=(r.Y+21)*pp.BackBufferHeight/1080;return pixels[y*pp.BackBufferWidth+x];}
   Check(Lamp(0)==(!(bool)Get("_statusGlobe")?new Color(105,108,113):net.ShowMapSymbols?new Color(114,255,143):new Color(38,65,48)),"Coordinates lamp distinguishes disabled/on/off");
   Check(Lamp(4)==(net.ShowEquator?new Color(114,255,143):new Color(38,65,48)),"Equator lamp independent");
   Check(pixels.Any(c=>c==CubeNet.EquatorColor)==net.ShowEquator,"Equator renders independently from coordinates");
  }
  if(frame>=23)Check(net.ShowRobots || net.RobotBadges.Count==0,"Hidden robots leave no hit areas in either view");
  if(frame is 24 or 26)foreach(var badge in net.RobotBadges){Check(badge.Detailed,"Both maps use the detailed robot display");int expected=net.RobotBadges.Last(b=>b.Bounds.Contains(badge.Bounds.Center)).Node;Check(net.HitRobotBadge(badge.Bounds.Center)==expected,"Upright overlapping robot badges select the topmost group in screen coordinates on both maps");}
  if(frame==26)foreach(var badge in net.RobotBadges)Check(w.Robots.Nodes[badge.Node].Robots.Any(r=>w.Relations.Controls(r.Owner)),"Other-country toggle filters robot badges");
  if(frame++>=26){Console.WriteLine("PASS: current-location centering, all six 30-degree globe controls, animation midpoint, full net circle, rotated hit tests, saved rotation, transition rendering, smooth automatic speed pulses and compact reset.");Exit();}
 }
}


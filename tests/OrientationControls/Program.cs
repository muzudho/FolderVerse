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
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  world=(WorldPreview)Get("_world");world.ShowSetup(w,true);net=(CubeNet)Get("_net");net.Home(w);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_statusGlobe",true);Call("FocusStatusGlobe");
  var panel=(Rectangle)Prop("NetPanel");var node=w.Nodes.Current(0);var position=w.Routes.Position(node.Cell,node.Center);
  Check(world.ProjectVisible(position,w.Cells[node.Cell].Normal,(float)Get("_yawAngle"),(float)Get("_pitchAngle"),panel,out var at),"Current location visible");
  Check(Vector2.Distance(at,new Vector2(panel.Center.X,panel.Center.Y))<.01f,"Current location centered");
  var toy=((ValueTuple<Rectangle,Rectangle,Rectangle>)typeof(Game1).GetMethod("OrientationAreas",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{panel,false})!).Item2;
  foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1}){
   var from=Quaternion.CreateFromRotationMatrix(world.ViewOrientation((float)Get("_yawAngle"),(float)Get("_pitchAngle")));
   var button=(Rectangle)Call("AxisButton",toy,axis,sign);Check((bool)Call("OrientationControlClick",button.Center),"Axis click consumed");
   Call("UpdateOrientationRotation",.2f);var half=world.ManualOrientation!.Value;Check(Math.Abs(Quaternion.Dot(from,half))<.999f,"Rotation animates");
   Call("UpdateOrientationRotation",.2f);var end=world.ManualOrientation!.Value;float angle=2*MathF.Acos(Math.Clamp(Math.Abs(Quaternion.Dot(from,end)),0,1));
   Check(Math.Abs(angle-MathHelper.ToRadians(30))<.001f,"Exactly 30 degrees on each axis and sign");
  }
  Set("_statusGlobe",false);
  for(int i=0;i<12;i++){
   Call("OrientationControlClick",((Rectangle)Call("AxisButton",toy,2,1)).Center);Call("UpdateOrientationRotation",.4f);
   var point=net.NodePosition(w,node,panel);Check(net.HitNode(w,panel,point.ToPoint())==node.Id,"Rotated node hit");
   Check(net.Hit(w,panel,point.ToPoint())==node.Cell,"Rotated cell hit");
   Check(net.MicroPoint(w,node.Cell,panel,point.ToPoint())==node.Center,"Rotated microcell hit");
  }
  Check(Math.Abs(net.ScreenAngle-MathHelper.TwoPi)<.00001f,"12 steps complete a circle");
  net.ScreenAngle=.5235988f;var saved=net.CaptureView();net.Home(w);net.RestoreView(saved);Check(net.ScreenAngle==saved.ScreenAngle,"Net angle restored");
  Set("_statusGlobe",true);Call("FocusStatusGlobe");
  foreach(int sign in new[]{-1,1}){
   Set("_screen",Enum.Parse(Get("_screen").GetType(),"Rolling"));Set("_yawAngle",0f);Set("_pitchAngle",0f);world.ManualOrientation=null;
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
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"Rolling"));Set("_yawAngle",0f);Set("_pitchAngle",0f);world.ManualOrientation=null;
  var queueToy=(Rectangle)Prop("OrientationToyArea");var queuedButton=(Rectangle)Call("AxisButton",queueToy,1,1);
  Call("OrientationControlClick",queuedButton.Center);Call("OrientationControlClick",queuedButton.Center);
  for(int i=0;i<70;i++)Call("AdvanceAutomaticOrientation",.1f);
  Check(Math.Abs(Quaternion.Dot(world.ManualOrientation!.Value,Quaternion.CreateFromAxisAngle(Vector3.UnitY,2.1f+MathHelper.ToRadians(60))))>.99999f,"Two clicks queue two complete 30-degree pulses");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var xPlus=(Rectangle)Call("AxisButton",(Rectangle)Prop("OrientationToyArea"),0,1);var xMinus=(Rectangle)Call("AxisButton",(Rectangle)Prop("OrientationToyArea"),0,-1);Check(xPlus.Y>xMinus.Y,"Only X button positions swapped");
  Rectangle Layer(int i)=>(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!;
  Call("RouteLayerClick",Layer(5).Center);Check(!net.ShowRobots,"Robot display off");Call("RouteLayerClick",Layer(5).Center);Check(net.ShowRobots,"Robot display on");
  Call("RouteLayerClick",Layer(6).Center);Check(!net.ShowOtherCountries && net.ControlledCountryColors && world.ControlledCountryColors,"Other country toggle controls both color renderers");
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

 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  var batch=(SpriteBatch)Get("_spriteBatch");
  if(frame==1)Set("_statusGlobe",false);
  if(frame==2){Set("_statusGlobe",true);Call("StartMapTransition");}
  if(frame==9){Call("UpdateMapTransition",.1f);Check(!(bool)Get("_statusGlobe"),"Unfold completes");Call("StartMapTransition");}
  if(frame>=2 && frame<=8)Set("_mapTransitionAge",(frame-2)*.6f);
  if(frame>=9 && frame<=15)Set("_mapTransitionAge",(frame-9)*.6f);
  if(frame==16){Call("UpdateMapTransition",.1f);Check((bool)Get("_statusGlobe"),"Fold completes");Call("StartMapTransition");Set("_mapOrientation",Quaternion.CreateFromAxisAngle(Vector3.UnitX,MathHelper.Pi));Set("_mapTransitionAge",2.4f);}
  if(frame>=17){
   Set("_mapTransitionAge",-1f);Set("_screen",Enum.Parse(Get("_screen").GetType(),frame==17?"Rolling":frame==18?"CastReview":"WorldStatus"));
   if(frame>=19){Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",frame>=21);net.ShowMapSymbols=frame!=21;net.ShowEquator=frame is 19 or 21;}
   if(frame>=23){Set("_statusGlobe",frame>=25);net.ShowRobots=frame is 24 or 26;if(frame==26){var other=(Rectangle)typeof(Game1).GetMethod("RouteLayerButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{6})!;Call("RouteLayerClick",other.Center);}}
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
  if(frame==26)foreach(var badge in net.RobotBadges)Check(w.Robots.Nodes[badge.Node].Robots.Any(r=>w.Relations.Controls(r.Owner)),"Other-country toggle filters robot badges");
  if(frame++>=26){Console.WriteLine("PASS: current-location centering, all six 30-degree globe controls, animation midpoint, full net circle, rotated hit tests, saved rotation, transition rendering, smooth automatic speed pulses and compact reset.");Exit();}
 }
}


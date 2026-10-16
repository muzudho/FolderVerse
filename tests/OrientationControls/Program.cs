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
  GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();Call("DrawStatusUi");batch.End();
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"rotation-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame==16)Check(pixels.Count(c=>c==new Color(115,119,125))>100,"Back faces render in solid gray");
  if(frame++>=16){Console.WriteLine("PASS: current-location centering, all six 30-degree globe controls, animation midpoint, full net circle, rotated hit tests, saved rotation, transition rendering.");Exit();}
 }
}


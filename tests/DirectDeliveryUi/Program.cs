using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new Checks();game.Run();
class Checks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;int a,b;long original;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static Rectangle Part(int side,int slot,int row)=>(Rectangle)typeof(Game1).GetMethod("DirectPart",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{side,slot,row})!;
 static Rectangle Cell(int side,int slot)=>(Rectangle)typeof(Game1).GetMethod("DirectCell",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{side,slot})!;
 static MouseState Mouse(Point p,ButtonState state)=>new(p.X,p.Y,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
 void Drag(int side,int slot,int row,int destinationSide,int destinationSlot){var from=Part(side,slot,row).Center;Set("_pointer",from);Call("DirectPairInput",Mouse(from,ButtonState.Pressed),true,false);var to=Cell(destinationSide,destinationSlot).Center;Set("_previousMouse",Mouse(from,ButtonState.Pressed));Set("_pointer",to);Call("DirectPairInput",Mouse(to,ButtonState.Released),false,false);Set("_previousMouse",new MouseState());}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  var node=w.Nodes.All.First(n=>!w.Routes.Land(n.Cell,n.Center) && w.Routes.Neighbors(n).Any(r=>!w.Routes.Land(w.Nodes.At(r.Target,r.Entry).Cell,w.Nodes.At(r.Target,r.Entry).Center)));var route=w.Routes.Neighbors(node).First(r=>!w.Routes.Land(w.Nodes.At(r.Target,r.Entry).Cell,w.Nodes.At(r.Target,r.Entry).Center));a=node.Id;b=w.Nodes.At(route.Target,route.Entry).Id;Check((bool)Call("DirectSeaRoute",a,b),"Sea connection uses ferry icon");foreach(var n in w.Nodes.All)n.Owner=1;w.Nodes.All[a].Owner=w.Nodes.All[b].Owner=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
  var robot=w.Robots.Create(0,RobotParts.Complete);original=robot.Id;w.Robots.Nodes[a].Add(robot);w.Robots.Nodes[b].Add(w.Robots.Create(0,RobotParts.Legs));((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  Set("_statusGlobe",false);((CubeNet)Get("_net")).FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1);Call("OpenDirectMap");
  Rectangle Footer(string name)=>(Rectangle)typeof(Game1).GetProperty(name,F)!.GetValue(this)!;
  var normal=Footer("PopulationTurnButton");
  Check(normal.Width==192 && normal.Height==144,"End turn uses 2:1.5 units");
  for(int i=0;i<6;i++){
   var rect=(Rectangle)Call("PrimaryActionBounds",i);
   Check(rect.Bottom==1070 && (i==1 || rect.Width==rect.Height),"Primary actions share a baseline with five squares");
   for(int j=0;j<i;j++)Check(!rect.Intersects((Rectangle)Call("PrimaryActionBounds",j)),"Primary hit bounds never overlap");
  }
  var delivery=(Rectangle)Call("PrimaryActionBounds",3);
  Check((bool)Call("MapOverlayClick",delivery.Center,false) && (bool)Get("_directMap"),"Active delivery action retains its mode until close");
  Call("UpdateMapFooter",.125f);var half=Footer("PopulationTurnButton");Check(half.Y>normal.Y && half.Top<1080,"Other actions slide down at the halfway point");
  Check((bool)Call("MapOverlayClick",normal.Center,false) && (bool)Get("_directMap"),"Original hidden turn-button location cannot end the turn");
  Check((bool)Call("MapOverlayClick",new Point(half.Center.X,half.Top+10),false) && (bool)Get("_directMap"),"Moving actions cannot activate during frame-out");
  Call("UpdateMapFooter",.125f);
  for(int i=0;i<6;i++)Check(i==3?((Rectangle)Call("PrimaryActionBounds",i))==delivery:((Rectangle)Call("PrimaryActionBounds",i)).Top>=1080,"Only delivery remains after frame-out");
  var closeMode=(Rectangle)typeof(Game1).GetMethod("MapModeCloseButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{3})!;
  Check((bool)Call("MapOverlayClick",closeMode.Center,false) && !(bool)Get("_directMap"),"Close beside delivery exits mode");
  Call("UpdateMapFooter",.125f);Check(Footer("PopulationTurnButton").Y==half.Y,"Other actions slide back in halfway");
  Check((bool)Call("MapOverlayClick",Footer("WarButton").Center,false) && !(bool)Get("_movementOpen"),"Returning buttons remain inactive until fully visible");
  Call("UpdateMapFooter",.125f);Check(Footer("PopulationTurnButton")==normal,"Actions return to exact original draw and hit bounds");
  Check((bool)Call("MapOverlayClick",delivery.Center,false) && (bool)Get("_directMap"),"Delivery action opens mode");
  Check((bool)Call("MapOverlayClick",Point.Zero,true) && !(bool)Get("_directMap"),"Escape also closes delivery mode");
  Call("OpenDirectMap");
  Console.WriteLine("PASS: action proportions, animated frame-out/in, hidden/moving input suppression, delivery close and Escape.");
  var viewNet=(CubeNet)Get("_net");var mainPanel=(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!;
  foreach(float angle in new[]{0f,MathHelper.PiOver2,MathHelper.Pi,MathHelper.Pi*1.5f}){
   viewNet.ScreenAngle=angle;var view=viewNet.CaptureView();Set("_directPairOpen",false);Call("OpenDirectPair",b,a);
   var ids=(int[])Get("_directNodes");var first=(Vector2)Call("DirectPairPosition",ids[0]);var second=(Vector2)Call("DirectPairPosition",ids[1]);
   Check(first.X<=second.X+.5f,"Panes follow map left/right at every net rotation");
   var copy=(CubeNet)Get("_directPairNet");Check(copy.ScreenAngle==angle && copy.Faces.SequenceEqual(view.Faces),"Pair map keeps unfolded faces and screen rotation");
   Check(viewNet.CaptureView()==view || viewNet.ScreenAngle==view.ScreenAngle && viewNet.Pan==view.Pan && viewNet.Zoom==view.Zoom && viewNet.Faces.SequenceEqual(view.Faces),"Opening pair leaves main map camera untouched");
   Call("FitDirectPairMap");Check(((int[])Get("_directNodes")).SequenceEqual(ids),"Fit does not reverse inspector assignment");
  }
  viewNet.ScreenAngle=0;
  if(viewNet.NodePosition(w,w.Nodes.All[a],mainPanel).X>viewNet.NodePosition(w,w.Nodes.All[b],mainPanel).X)viewNet.ScreenAngle=MathHelper.Pi;
  Set("_directPairOpen",false);Call("OpenDirectPair",a,b);
  Check((float)Get("_directPairSlide")==0,"Pair starts at the main camera without accepting hidden controls");
  var beforeOrders=w.Robots.PartPlanning.Orders.Count;Drag(0,0,0,1,0);Check(w.Robots.PartPlanning.Orders.Count==beforeOrders,"Sliding inspectors cannot receive part drags");
  Call("UpdateDirectPairTransition",.175f);var halfway=(Rectangle)typeof(Game1).GetProperty("DirectPairAnimatedPanel",F)!.GetValue(this)!;Check(halfway.Width>720 && halfway.Width<mainPanel.Width,"Map viewport interpolates between main and pair views");
  Call("UpdateDirectPairTransition",.175f);
  Check(((int[])Get("_directNodes"))[0]==a,"Transaction fixture follows visual order");
  var left=Cell(0,0);var right=Cell(1,0);
  Check(left.Right<600 && right.Left>1320,"Dedicated compact panes leave the center map open");
  var close=(Rectangle)typeof(Game1).GetField("DirectPairBack",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(close.Center.X==960 && close.Bottom<left.Top,"Shared close handle sits above the drag corridor");
  Check((Color)Call("DirectSideColor",0)==w.OwnerColor(w.PlayerSlot) && (Color)Call("DirectSideColor",1)==Color.White,"Left uses own country color and right uses white");
  var pairNet=(CubeNet)Get("_directPairNet");var map=(Rectangle)typeof(Game1).GetField("DirectPairMapBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(map.Contains(pairNet.NodePosition(w,w.Nodes.All[a],map).ToPoint()) && map.Contains(pairNet.NodePosition(w,w.Nodes.All[b],map).ToPoint()),"Dedicated map fits both endpoints");
  RobotWorld Preview()=>w.Robots.PartPlanning.Preview(w);
  void Undo(){var point=new Point(960,84);Set("_pointer",point);Call("DirectPairInput",Mouse(point,ButtonState.Pressed),true,false);Call("SyncDirectSlots");}
  Drag(1,0,2,0,5);Check(w.Robots.PartPlanning.Orders.Count==1 && w.Robots.PartPlanning.Orders.Any(o=>o.Source==b && o.Target==a),"Right-to-left drag creates a planning reservation");
  Check(Preview().Nodes[b].Count==0 && w.Robots.Nodes[b].Count==1,"Preview moves parts while physical stock stays unchanged");Undo();Check(w.Robots.PartPlanning.Orders.Count==0,"Real undo control cancels the last reservation");
  Drag(0,0,0,1,0);Check(w.Robots.PartPlanning.Orders.Count==1 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Cross-pane drag reserves without moving physical part");
  Drag(0,0,1,0,1);Check(Preview().Nodes[a].Count==2 && w.Robots.Nodes[a].Count==1 && w.Robots.PartPlanning.Orders.Count==2,"Local assembly is also deferred and appears in preview");
  var doc=AutoSaveStore.Capture(w);Check(AutoSaveStore.Restore<WorldSetup>(doc).Robots.PartPlanning.Orders.Count==2,"Planning reservations are saved");
  Undo();Undo();Check(w.Robots.PartPlanning.Orders.Count==0 && Preview().Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Undo restores the complete preview robot");
  void Whole(int fromSlot,int toSide,int toSlot){var from=Cell(0,fromSlot).Location+new Point(16,145);Set("_pointer",from);Call("DirectPairInput",Mouse(from,ButtonState.Pressed),true,false);var to=Cell(toSide,toSlot).Center;Set("_previousMouse",Mouse(from,ButtonState.Pressed));Set("_pointer",to);Call("DirectPairInput",Mouse(to,ButtonState.Released),false,false);}
  Whole(0,0,2);Check(((long?[][])Get("_directSlots"))[0][2]==original && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Cell drag within pane repositions complete robot");
  Set("_pointer",Part(0,2,0).Center);Check((RobotParts)Call("DirectHoverPart",0,2)==RobotParts.Head,"Part hover selects only head");
  Set("_pointer",Cell(0,2).Location+new Point(16,145));Check((RobotParts)Call("DirectHoverPart",0,2)==0,"Cell margin hover selects whole cell");
  Whole(2,1,0);Check(w.Robots.PartPlanning.Orders.Count==0 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Whole robot conflicting with destination leaves no partial reservations");
  Whole(2,1,1);Check(w.Robots.PartPlanning.Orders.Count==1 && w.Robots.PartPlanning.Orders[0].Parts==RobotParts.Complete,"Whole cell cross-pane drag reserves all parts atomically");
  Check(((System.Collections.IDictionary)Get("_workReservationEffects")).Contains(original),"Accepted delivery starts work animation without ending further input");
  Set("_directDrag",(0,original,RobotParts.Head));Set("_pointer",close.Center);
  Call("DirectPairInput",Mouse(close.Center,ButtonState.Pressed),true,false);Check((bool)Get("_directPairOpen"),"Close button cannot activate while carrying a part");Set("_directDrag",null!);
  var empty=Cell(0,11).Location+new Point(16,145);Set("_pointer",empty);
  Call("DirectPairInput",Mouse(empty,ButtonState.Pressed),true,false);
  var guidance=(string)Get("_directMessage");var font=(SpriteFont)Get("_font");
  Check(guidance.Contains("パーツ単位") && font.Characters.Contains('単') && font.MeasureString(guidance).X>0,"Empty-slot guidance has every glyph required for rendering");
 }
  NetFace[] edgeBefore=Array.Empty<NetFace>();int orderBefore;
 void Physical(Point point,double elapsed=0,bool pressed=false){var canvas=(Rectangle)Call("CanvasBounds");var p=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);Call("HandleInput",Mouse(p,pressed?ButtonState.Pressed:ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(elapsed)),true);}
 void ClickLetter(CubeNet net,Rectangle panel){
  foreach(var label in net.SeamLabels(panel)){var p=net.RotateScreen(label.Bounds.Center.ToVector2(),panel).ToPoint();if(!panel.Contains(p))continue;net.HoverEdge(panel,p);if(!net.CanClickEdge)continue;edgeBefore=net.Faces.ToArray();orderBefore=w.Robots.PartPlanning.Orders.Count;Physical(p);Physical(p,pressed:true);Physical(p);Check(net.IsAnimating,"Physical alphabet click starts face attachment");return;}
  throw new Exception("No clickable visible seam letter");
 }
 protected override void Update(GameTime t){}
 void CheckDeliveryNotes(){
  bool Marked(int first,int second)=>(bool)Call("DirectPairChecked",first,second);
  Call("OpenDirectPair",a,b);Call("UpdateDirectPairTransition",.35f);
  var button=(Rectangle)typeof(Game1).GetField("DirectPairCheckButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var before=w.Robots.PartPlanning.Orders.Select(o=>o.Number).ToArray();Set("_pointer",button.Center);
  Call("DirectPairInput",Mouse(button.Center,ButtonState.Pressed),true,false);
  Check(Marked(a,b) && Marked(b,a) && (bool)Get("_directPairOpen"),"Check button marks an undirected pair without closing it");
  Check(before.SequenceEqual(w.Robots.PartPlanning.Orders.Select(o=>o.Number)),"Marking does not alter delivery reservations");
  Call("OpenDirectPair",b,a);Call("UpdateDirectPairTransition",.35f);Check(Marked(a,b),"Reopening in reverse order retains the mark");
  w.HumanPlayers.Add(1);w.SelectTurnRuler(1);Check(!Marked(a,b),"Other human cannot see this delivery note");w.SelectTurnRuler(0);w.HumanPlayers.Remove(1);Check(Marked(a,b),"Original human retains note");
  Set("_pointer",button.Center);Call("DirectPairInput",Mouse(button.Center,ButtonState.Pressed),true,false);Check(!Marked(a,b),"Second click removes note");Call("DirectPairInput",Mouse(button.Center,ButtonState.Pressed),true,false);
  Call("ClearCurrentPlayerNodeChecks");Check(!Marked(a,b),"End-turn cleanup removes delivery checks");Call("ToggleDirectPairCheck");
 }
 protected override void Draw(GameTime t){
  if(frame==0)CheckDeliveryNotes();
  if(frame==1){Set("_directPairOpen",false);}
  if(frame==2){var hit=((System.Collections.IList)Get("_directTrucks"))[0]!;var bounds=(Rectangle)hit.GetType().GetField("Item1")!.GetValue(hit)!;Call("DirectMapClick",bounds.Center);Check((bool)Get("_directPairOpen"),"Clicking truck opens pair inspector");}
  if(frame==3){var arrival=Part(1,1,0);Set("_pointer",new Point(arrival.Right+32,arrival.Y+18));}
  if(frame==4){
   Call("CloseDirectDelivery");Call("OpenNodeInspector",a);Call("UpdateDomesticSlide",1f);
   ((CubeNet)Get("_net")).FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1);
  }
  if(frame==5){
   var trucks=(List<(Rectangle Bounds,int A,int B)>)Get("_directTrucks");
   Check(trucks.Count==0 && !(bool)Get("_directMap"),"Ordinary inspector leaves direct-delivery markers hidden");
   Call("OpenDirectMap");
  }
  if(frame==6){var trucks=(List<(Rectangle Bounds,int A,int B)>)Get("_directTrucks");Check(trucks.Count>0,"Selecting direct delivery restores truck markers");Call("DirectMapClick",trucks[0].Bounds.Center);Check((bool)Get("_directPairOpen"),"Selected overlay truck opens the pair");var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  if(frame==7){Call("DirectPairInput",new MouseState(),false,true);Check((bool)Get("_directPairClosing"),"Back starts the reverse transition");Call("UpdateDirectPairTransition",.35f);Check(!(bool)Get("_directPairOpen") && (bool)Get("_directMap"),"Back retains the selected direct-delivery overlay");}
  if(frame==8){
   Set("_statusGlobe",true);var globe=(WorldPreview)Get("_world");globe.ManualOrientation=null;
   var center=(w.Routes.Position(w.Nodes.All[a].Cell,w.Nodes.All[a].Center)+w.Routes.Position(w.Nodes.All[b].Cell,w.Nodes.All[b].Center))/2;
   Call("FocusGlobePosition",center);var orientation=globe.ViewOrientation((float)Get("_yawAngle"),(float)Get("_pitchAngle"));
   foreach(float angle in new[]{0f,MathHelper.PiOver2,MathHelper.Pi}){
    globe.ManualOrientation=Quaternion.CreateFromRotationMatrix(orientation*Matrix.CreateRotationZ(angle));var saved=globe.ManualOrientation;var pan=globe.GlobePan;float zoom=globe.GlobeZoom;
    Set("_directPairOpen",false);Call("OpenDirectPair",b,a);var ids=(int[])Get("_directNodes");var x=(Vector2)Call("DirectPairPosition",ids[0]);var y=(Vector2)Call("DirectPairPosition",ids[1]);
    Check(x.X<=y.X+.5f && (bool)Get("_directPairGlobe"),"Globe also keeps visual left/right");
    Check(((WorldPreview)Get("_directPairWorld")).ManualOrientation==saved && globe.ManualOrientation==saved && globe.GlobePan==pan && globe.GlobeZoom==zoom,"Globe direction is copied and main camera is unchanged");
   }
  }
  if(frame<9 && (bool)Get("_directPairOpen"))Set("_directPairSlide",1f);
  if(frame==8){foreach(var n in w.Nodes.All)n.Owner=0;var pairGlobe=(WorldPreview)Get("_directPairWorld");pairGlobe.GlobeZoom=.8f;pairGlobe.GlobeAnchor=null;}
  void ClickOtherTruck(){
   var trucks=(List<(Rectangle Bounds,int A,int B)>)Get("_directPairTrucks");var current=(int[])Get("_directNodes");
   var next=trucks.FirstOrDefault(t=>!current.Contains(t.A) || !current.Contains(t.B));Check(next.Bounds!=Rectangle.Empty,"Pair map exposes another reachable truck/ferry");
   var orders=w.Robots.PartPlanning.Orders.Select(o=>o.Number).ToArray();
   // Use physical 960x540 coordinates, then the same canvas conversion as production input.
   var canvas=(Rectangle)Call("CanvasBounds");var physical=new Point(canvas.X+next.Bounds.Center.X*canvas.Width/1920,canvas.Y+next.Bounds.Center.Y*canvas.Height/1080);
   var mouse=Mouse(physical,ButtonState.Pressed);Set("_pointer",(Point)Call("CanvasPoint",mouse));
   var snapshot=current.ToArray();Set("_directDrag",(0,original,RobotParts.Head));Call("DirectPairInput",mouse,true,false);Check(((int[])Get("_directNodes")).SequenceEqual(snapshot),"Carrying a part cannot switch to another route");Set("_directDrag",null!);
   Call("DirectPairInput",mouse,true,false);
   var selected=(int[])Get("_directNodes");Check(selected.Contains(next.A) && selected.Contains(next.B) && (bool)Get("_directPairOpen"),"Truck click switches pairs directly without returning to main map");
   Check(orders.SequenceEqual(w.Robots.PartPlanning.Orders.Select(o=>o.Number)),"Consecutive truck clicks preserve previous reservations");
   Check(!(bool)Get("_directPairFromMain"),"Consecutive pair switches keep main controls out of view");
   Check((float)Get("_directPairSlide")==0 && ((List<(Rectangle Bounds,int A,int B)>)Get("_directPairTrucks")).Count==0,"Switch clears stale truck hit targets during camera motion");
  }
  if(frame==9)ClickOtherTruck();
  if(frame==10)Call("UpdateDirectPairTransition",.35f);
  if(frame==11){Call("DirectPairInput",new MouseState(),false,true);Call("UpdateDirectPairTransition",.175f);}
  if(frame==12){Call("UpdateDirectPairTransition",.175f);Check(!(bool)Get("_directPairOpen"),"Reverse animation returns to the large map");Set("_statusGlobe",false);Call("OpenDirectPair",a,b);}
  if(frame==13)Call("UpdateDirectPairTransition",.175f);
  if(frame==14){Call("UpdateDirectPairTransition",.175f);((CubeNet)Get("_directPairNet")).FitAll((Rectangle)typeof(Game1).GetField("DirectPairMapBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!,.8f);}
  if(frame==15)ClickOtherTruck();
  if(frame==16)Call("UpdateDirectPairTransition",.175f);
  if(frame==17)Call("UpdateDirectPairTransition",.175f);
    if(frame==18){Call("CloseDirectDelivery");Call("OpenDirectMap");var net=(CubeNet)Get("_net");net.ScreenAngle=.3f;net.FitAll((Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!, .7f);ClickLetter(net,(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!);}
  if(frame==19){Physical(Point.Zero,.15);Check(((CubeNet)Get("_net")).IsAnimating,"Main attachment progresses across frames");}
  if(frame==20){Physical(Point.Zero,1);Check(!((CubeNet)Get("_net")).IsAnimating && !((CubeNet)Get("_net")).Faces.SequenceEqual(edgeBefore),"Main face attachment completes");Check((bool)Get("_directMap") && w.Robots.PartPlanning.Orders.Count==orderBefore,"Main attachment keeps delivery mode and reservations");Call("OpenDirectPair",a,b);Call("UpdateDirectPairTransition",.35f);var net=(CubeNet)Get("_directPairNet");net.FitAll(new(600,200,720,690),.65f);ClickLetter(net,new(600,200,720,690));}
  if(frame==21){Physical(Point.Zero,.15);Check(((CubeNet)Get("_directPairNet")).IsAnimating,"Pair attachment progresses across frames and draws");}
  if(frame==22){Physical(Point.Zero,1);var net=(CubeNet)Get("_directPairNet");Check(!net.IsAnimating && !net.Faces.SequenceEqual(edgeBefore),"Pair face attachment completes");Check((bool)Get("_directPairOpen") && w.Robots.PartPlanning.Orders.Count==orderBefore,"Pair attachment preserves inspectors and reservations");var ids=(int[])Get("_directNodes");Check(((Vector2)Call("DirectPairPosition",ids[0])).X<=((Vector2)Call("DirectPairPosition",ids[1])).X+.5f,"Inspectors realign after face attachment");}
  Call("UpdateMapFooter",.25f);base.Draw(t);
  if(frame==20)Check(((CubeNet)Get("_directPairNet")).IsAnimating,"Rendering does not cancel pair attachment");var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"direct-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame==1){var hits=(System.Collections.IList)Get("_directTrucks");Check(hits.Count>0,"Visible adjacent edge has a truck");}
  if(frame++==22){Console.WriteLine("PASS: physical rotated seam-letter attachment on main and pair delivery maps at 540p, animation rendering and preserved reservations.");Console.WriteLine("PASS: animated main/pair/return cameras, disabled moving controls, consecutive globe/net truck clicks at 540p and preserved reservations.");Console.WriteLine("PASS: edge trucks, hidden ordinary-inspector markers, selected overlay opening and return, two hangars, reservations and rendering.");Exit();}
 }
}

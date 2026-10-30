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
  Check((bool)Call("MapOverlayClick",delivery.Center,false) && !(bool)Get("_directMap"),"Delivery action toggles the overlay off in one click");
  Check((bool)Call("MapOverlayClick",delivery.Center,false) && (bool)Get("_directMap"),"Delivery action opens the overlay in one click");
  Check((bool)Call("MapOverlayClick",Point.Zero,true) && !(bool)Get("_directMap"),"Escape exits direct mode");
  Call("OpenDirectMap");
  Console.WriteLine("PASS: primary action proportions, separate hit bounds, direct overlay toggle and Escape.");
  var viewNet=(CubeNet)Get("_net");var mainPanel=(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!;
  foreach(float angle in new[]{0f,MathHelper.PiOver2,MathHelper.Pi,MathHelper.Pi*1.5f}){
   viewNet.ScreenAngle=angle;var view=viewNet.CaptureView();Call("OpenDirectPair",b,a);
   var ids=(int[])Get("_directNodes");var first=(Vector2)Call("DirectPairPosition",ids[0]);var second=(Vector2)Call("DirectPairPosition",ids[1]);
   Check(first.X<=second.X+.5f,"Panes follow map left/right at every net rotation");
   var copy=(CubeNet)Get("_directPairNet");Check(copy.ScreenAngle==angle && copy.Faces.SequenceEqual(view.Faces),"Pair map keeps unfolded faces and screen rotation");
   Check(viewNet.CaptureView()==view || viewNet.ScreenAngle==view.ScreenAngle && viewNet.Pan==view.Pan && viewNet.Zoom==view.Zoom && viewNet.Faces.SequenceEqual(view.Faces),"Opening pair leaves main map camera untouched");
   Call("FitDirectPairMap");Check(((int[])Get("_directNodes")).SequenceEqual(ids),"Fit does not reverse inspector assignment");
  }
  viewNet.ScreenAngle=0;
  if(viewNet.NodePosition(w,w.Nodes.All[a],mainPanel).X>viewNet.NodePosition(w,w.Nodes.All[b],mainPanel).X)viewNet.ScreenAngle=MathHelper.Pi;
  Call("OpenDirectPair",a,b);
  Check(((int[])Get("_directNodes"))[0]==a,"Transaction fixture follows visual order");
  var left=Cell(0,0);var right=Cell(1,0);
  Check(left.Right<600 && right.Left>1320,"Dedicated compact panes leave the center map open");
  var close=(Rectangle)typeof(Game1).GetField("DirectPairBack",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(close.Center.X==960 && close.Bottom<left.Top,"Shared close handle sits above the drag corridor");
  Check((Color)Call("DirectSideColor",0)==w.OwnerColor(w.PlayerSlot) && (Color)Call("DirectSideColor",1)==Color.White,"Left uses own country color and right uses white");
  var pairNet=(CubeNet)Get("_directPairNet");var map=(Rectangle)typeof(Game1).GetField("DirectPairMapBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(map.Contains(pairNet.NodePosition(w,w.Nodes.All[a],map).ToPoint()) && map.Contains(pairNet.NodePosition(w,w.Nodes.All[b],map).ToPoint()),"Dedicated map fits both endpoints");
  Drag(1,0,2,0,5);Check(w.Robots.DirectDelivery.Orders.Count==1 && w.Robots.DirectDelivery.Orders.Any(o=>o.Source==b && o.Target==a),"Right-to-left drag is equally supported");
  foreach(var order in w.Robots.DirectDelivery.Orders.ToArray())w.Robots.DirectDelivery.Cancel(order.Number);
  Drag(0,0,0,1,0);Check(w.Robots.DirectDelivery.Orders.Count==1 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Cross-pane drag reserves without moving physical part");
  Drag(0,0,1,0,1);Check(w.Robots.Nodes[a].Count==2 && w.Robots.DirectDelivery.Orders.Count==1,"Other unreserved body moves within source pane");
  var ghost=Part(1,0,0).Center;Set("_pointer",ghost);Call("DirectPairInput",Mouse(ghost,ButtonState.Pressed),true,false);Check(w.Robots.DirectDelivery.Orders.Count==0,"Click arrival ghost cancels");
  Drag(0,0,0,1,0);Drag(0,1,1,1,1);Check(w.Robots.DirectDelivery.Orders.Count==2,"Independent parts can use different destination cells");
  var doc=AutoSaveStore.Capture(w);Check(AutoSaveStore.Restore<WorldSetup>(doc).Robots.DirectDelivery.Orders.Count==2,"UI reservations are saved");
  foreach(var order in w.Robots.DirectDelivery.Orders.ToArray())w.Robots.DirectDelivery.Cancel(order.Number);
  var body=w.Robots.Nodes[a].Robots.Single(r=>(r.Parts&RobotParts.Body)!=0);w.Robots.MovePart(a,body.Id,RobotParts.Body,original,0);Call("OpenDirectPair",a,b);
  void Whole(int fromSlot,int toSide,int toSlot){var from=Cell(0,fromSlot).Location+new Point(16,145);Set("_pointer",from);Call("DirectPairInput",Mouse(from,ButtonState.Pressed),true,false);var to=Cell(toSide,toSlot).Center;Set("_previousMouse",Mouse(from,ButtonState.Pressed));Set("_pointer",to);Call("DirectPairInput",Mouse(to,ButtonState.Released),false,false);}
  Whole(0,0,2);Check(((long?[][])Get("_directSlots"))[0][2]==original && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Cell drag within pane repositions complete robot");
  Whole(2,1,0);Check(w.Robots.DirectDelivery.Orders.Count==0 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Whole robot conflicting with destination leaves no partial reservations");
  Whole(2,1,1);Check(w.Robots.DirectDelivery.Orders.Count==3 && w.Robots.DirectDelivery.LockedParts(a,original)==RobotParts.Complete,"Whole cell cross-pane drag reserves head body and legs together");
  Set("_pointer",Part(0,2,0).Center);Check((RobotParts)Call("DirectHoverPart",0,2)==RobotParts.Head,"Part hover selects only head");
  Set("_pointer",Cell(0,2).Location+new Point(16,145));Check((RobotParts)Call("DirectHoverPart",0,2)==0,"Cell margin hover selects whole cell");
  Set("_directDrag",(0,original,RobotParts.Head));Set("_pointer",close.Center);
  Call("DirectPairInput",Mouse(close.Center,ButtonState.Pressed),true,false);Check((bool)Get("_directPairOpen"),"Close button cannot activate while carrying a part");Set("_directDrag",null!);
  var empty=Cell(0,11).Location+new Point(16,145);Set("_pointer",empty);
  Call("DirectPairInput",Mouse(empty,ButtonState.Pressed),true,false);
  var guidance=(string)Get("_directMessage");var font=(SpriteFont)Get("_font");
  Check(guidance.Contains("パーツ単位") && font.Characters.Contains('単') && font.MeasureString(guidance).X>0,"Empty-slot guidance has every glyph required for rendering");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_directPairOpen",false);}
  if(frame==2){var hit=((System.Collections.IList)Get("_directTrucks"))[0]!;var bounds=(Rectangle)hit.GetType().GetField("Item1")!.GetValue(hit)!;Call("DirectMapClick",bounds.Center);Check((bool)Get("_directPairOpen"),"Clicking truck opens pair inspector");}
  if(frame==3)Set("_pointer",Cell(1,1).Location+new Point(16,145));
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
  if(frame==7){Call("DirectPairInput",new MouseState(),false,true);Check(!(bool)Get("_directPairOpen") && (bool)Get("_directMap"),"Back retains the selected direct-delivery overlay");}
  if(frame==8){
   Set("_statusGlobe",true);var globe=(WorldPreview)Get("_world");globe.ManualOrientation=null;
   var center=(w.Routes.Position(w.Nodes.All[a].Cell,w.Nodes.All[a].Center)+w.Routes.Position(w.Nodes.All[b].Cell,w.Nodes.All[b].Center))/2;
   Call("FocusGlobePosition",center);var orientation=globe.ViewOrientation((float)Get("_yawAngle"),(float)Get("_pitchAngle"));
   foreach(float angle in new[]{0f,MathHelper.PiOver2,MathHelper.Pi}){
    globe.ManualOrientation=Quaternion.CreateFromRotationMatrix(orientation*Matrix.CreateRotationZ(angle));var saved=globe.ManualOrientation;var pan=globe.GlobePan;float zoom=globe.GlobeZoom;
    Call("OpenDirectPair",b,a);var ids=(int[])Get("_directNodes");var x=(Vector2)Call("DirectPairPosition",ids[0]);var y=(Vector2)Call("DirectPairPosition",ids[1]);
    Check(x.X<=y.X+.5f && (bool)Get("_directPairGlobe"),"Globe also keeps visual left/right");
    Check(((WorldPreview)Get("_directPairWorld")).ManualOrientation==saved && globe.ManualOrientation==saved && globe.GlobePan==pan && globe.GlobeZoom==zoom,"Globe direction is copied and main camera is unchanged");
   }
  }
  if((bool)Get("_directPairOpen"))Set("_directPairSlide",1f);
  Call("UpdateMapFooter",.25f);base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var tex=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"direct-{frame}.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame==1){var hits=(System.Collections.IList)Get("_directTrucks");Check(hits.Count>0,"Visible adjacent edge has a truck");}
  if(frame++==8){Console.WriteLine("PASS: edge trucks, hidden ordinary-inspector markers, selected overlay opening and return, two hangars, reservations and rendering.");Exit();}
 }
}

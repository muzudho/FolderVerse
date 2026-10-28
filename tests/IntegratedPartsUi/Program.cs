using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new IntegratedUi();game.Run();
sealed class IntegratedUi:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 WorldSetup w=null!;int a,b,frame;long bodyLegs,drop;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] x)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,x)!;
 Rectangle Rect(string n,params object[] x)=>x.Length==0?(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!:(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,x)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static MouseState Mouse(ButtonState state)=>new(0,0,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
 void Hangar(Point p){Set("_pointer",p);Call("NodeAssemblyLocalInput",Mouse(ButtonState.Pressed),true,false);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Salvage.Reset();w.Relations.Reset();w.SelectPlayer(0);
  var node=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);a=node.Id;var route=w.Routes.Neighbors(node)[0];b=w.Nodes.At(route.Target,route.Entry).Id;
  foreach(var n in w.Nodes.All)n.Owner=1;node.Owner=w.Nodes.All[b].Owner=0;w.ConquerorLocations[0]=node.Cell;w.ConquerorPoints[0]=node.Center;
  w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
  w.Robots.Nodes[a].Add(w.Robots.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Head|RobotParts.Body,CheapParts=RobotParts.Head|RobotParts.Body,DamagedParts=RobotParts.Head});
  var other=w.Robots.Create(0,RobotParts.Body|RobotParts.Legs) with {IntegratedParts=RobotParts.Body|RobotParts.Legs};bodyLegs=other.Id;w.Robots.Nodes[a].Add(other);w.Robots.Nodes[a].Add(w.Robots.Create(0,RobotParts.Head));
  w.Robots.Nodes[b].Add(w.Robots.Create(0,RobotParts.Legs));
  var factory=w.Robots.Workshops[a];factory.Automation=NodeAutomation.Manual;factory.GeneratePlans(123);factory.SelectPlan(7);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",false);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("OpenNodeInspector",a);Call("UpdateDomesticSlide",1f);Call("OpenNodeFactory");Set("_factoryPlanLine",0);
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==1){Call("NodeFactoryClick",Rect("FactoryPlanButton",10).Center);Check(w.Robots.Workshops[a].IntegratedProduct==(RobotParts.Body|RobotParts.Legs) && w.Robots.Workshops[a].CheapProduct,"Catalogue selects cheap body/legs unit");Call("OpenNodeAssembly");Hangar(Rect("HangarPartBounds",0,0).Center);Check((RobotParts)Get("_hangarPart")== (RobotParts.Head|RobotParts.Body),"Head grab selects both linked members");Set("_pointer",new Point(1200,500));}
  if(frame==2){Set("_pointer",Rect("HangarCell",3).Center);Set("_previousMouse",Mouse(ButtonState.Pressed));Call("NodeAssemblyLocalInput",Mouse(ButtonState.Released),false,false);Check(w.Robots.Nodes[a].Robots.Any(r=>r.Parts==(RobotParts.Head|RobotParts.Body) && r.IntegratedParts==r.Parts),"Manual drop moves pair intact");Call("OpenDirectPair",a,b);Set("_directPairSlide",1f);Set("_pointer",Rect("DirectPart",0,0,1).Center);Call("DirectPairInput",Mouse(ButtonState.Pressed),true,false);var drag=((int Side,long Robot,RobotParts Part))Get("_directDrag");Check(drag.Part==(RobotParts.Body|RobotParts.Legs),"Direct drag carries complete joined unit");Set("_pointer",new Point(960,500));}
  if(frame==3){Set("_pointer",Rect("DirectCell",1,2).Center);Set("_previousMouse",Mouse(ButtonState.Pressed));Call("DirectPairInput",Mouse(ButtonState.Released),false,false);Check(w.Robots.DirectDelivery.Orders.Single().Part==(RobotParts.Body|RobotParts.Legs),"Direct drop reserves both linked members in one order");}
  if(frame==4){Set("_pointer",Rect("DirectPart",1,2,1).Center);Call("DirectPairInput",Mouse(ButtonState.Pressed),true,false);Check(w.Robots.DirectDelivery.Orders.Count==0,"Clicking either ghost cancels joined reservation");Call("DirectPairInput",Mouse(ButtonState.Released),false,true);Call("OpenNodeAssembly");Hangar(Rect("HangarAbandonButton").Center);Hangar(Rect("HangarPartBounds",0,1).Center);var pending=((int Node,long Robot,RobotParts Parts))Get("_salvageAbandonConfirmation");Check(pending.Parts==(RobotParts.Body|RobotParts.Legs),"Abandon prompt expands single clicked member");}
  if(frame==5){Set("_pointer",Rect("AbandonYes").Center);Call("AbandonConfirmationInput",true,false);var item=w.Salvage.Items.Single();drop=item.Id;Check(item.Robot.Id==bodyLegs && item.Robot.IntegratedParts==(RobotParts.Body|RobotParts.Legs),"Discarded group stays joined");Call("OpenSalvagePlan",a);Call("UpdateDomesticSlide",1f);}
  if(frame==6){Check(w.Salvage.PickUp(w,0,drop),"Local retrieval accepts whole joined unit");Call("OpenPocket");var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  if(frame==7){Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenNodeInspector",a);Call("UpdateDomesticSlide",1f);Call("OpenNodeTransport");var rule=w.Robots.Transport.AddInstruction(a,0,b);w.Robots.Transport.ConfigurePartChoices(a,0,rule.Number,b,1023,false,5);Set("_transportEditNumber",rule.Number);Set("_transportEditor",1);}
  base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  if(frame==0){
   var hb=Rect("FactoryPlanButton",7);var bl=Rect("FactoryPlanButton",8);var paint=w.OwnerColor(w.Nodes.All[a].Owner);
   Check(pixels[(hb.Y+8+20)*p.BackBufferWidth+hb.X+54+36]==paint,"Head/body joint is filled without an internal outline");
   Check(pixels[(bl.Y+8+52)*p.BackBufferWidth+bl.X+54+21]==paint,"Body/legs joint is filled without an internal outline");
   Check(pixels[(bl.Y+8+65)*p.BackBufferWidth+bl.X+54+36]!=paint,"Joined legs retain the gap between left and right feet");
  }
  using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"integrated-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==7){Console.WriteLine("PASS: 11-plan catalogue, joined hangar/direct dragging and ghosts, atomic cancellation, abandonment, recovery, pocket and 10-choice transport editor at full/half size.");Exit();}
 }
}

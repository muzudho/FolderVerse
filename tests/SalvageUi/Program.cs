using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new SalvageChecks();game.Run();
sealed class SalvageChecks:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 WorldSetup w=null!;int home,near,frame;long robotId,discard;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Rect(string n,params object[] a)=>a.Length==0?(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!:(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,a)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void HangarClick(Point p){Set("_pointer",p);Call("NodeAssemblyLocalInput",new MouseState(),true,false);}
 void PlanClick(Point p)=>Call("SalvageInput",p,true);
 void Confirm(bool yes){Set("_pointer",Rect(yes?"AbandonYes":"AbandonNo").Center);Call("AbandonConfirmationInput",true,false);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.Relations.Reset();w.SelectPlayer(0);w.Salvage.Reset();
  var node=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);home=node.Id;var route=w.Routes.Neighbors(node)[0];near=w.Nodes.At(route.Target,route.Entry).Id;
  foreach(var n in w.Nodes.All)n.Owner=1;node.Owner=w.Nodes.All[near].Owner=0;
  w.ConquerorLocations[0]=node.Cell;w.ConquerorPoints[0]=node.Center;
  w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
  var r=w.Robots.Create(0,RobotParts.Complete).Materialize() with {CheapParts=RobotParts.Body,DamagedParts=RobotParts.Head};robotId=r.Id;w.Robots.Nodes[home].Add(r);
  w.Salvage.EnsureAgents(w);w.Salvage.Items.Add(new(100,near,w.Robots.Create(1,RobotParts.Legs)));
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",false);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  Call("OpenNodeInspector",home);Call("UpdateDomesticSlide",1f);Call("OpenNodeAssembly");
  HangarClick(Rect("HangarAbandonButton").Center);HangarClick(Rect("HangarPartBounds",0,0).Center);
  Check(Get("_salvageAbandonConfirmation")!=null,"Abandon requires explicit in-game confirmation");Confirm(false);
  Check(w.Salvage.Items.Count==1 && w.Robots.Nodes[home].Robots.Single().Parts==RobotParts.Complete,"Cancel leaves stock untouched");
  HangarClick(Rect("HangarPartBounds",0,0).Center);Confirm(true);discard=w.Salvage.Items.Single(i=>i.AbandonedBy==0).Id;
  Check(w.Robots.Nodes[home].Robots.Single().Parts==(RobotParts.Body|RobotParts.Legs) && w.Salvage.Items.Single(i=>i.Id==discard).Robot.IsDamaged(RobotParts.Head),"Confirmed part abandonment updates physical stock and keeps damage");
  Call("ResetInspector");Call("DrawSalvageMap");Check(((System.Collections.ICollection)Get("_salvageHits")).Count==0,"Default map hides salvage markers and hit targets");
  Point Action(int i)=>((Rectangle)Call("PrimaryActionBounds",i)).Center;
  Call("MapOverlayClick",Action(3),false);Check((bool)Get("_directMap") && !(bool)Get("_salvagePlan"),"One click selects direct delivery");
  Call("MapOverlayClick",Point.Zero,true);Call("UpdateMapFooter",1f);Check(!(bool)Get("_directMap"),"Escape closes direct delivery");
  Call("MapOverlayClick",Action(5),false);Check((bool)Get("_salvagePlan") && !(bool)Get("_directMap"),"One click opens recovery plan exclusively");Call("UpdateDomesticSlide",1f);
  Set("_salvageSelected",discard);PlanClick(Rect("SalvageRequest").Center);
  Check(w.Salvage.Items.Single(i=>i.Id==discard).RequestedBy==0,"UI requests self-abandoned goods");
  var net=(CubeNet)Get("_net");Call("UpdateNodeFocusCircle");var color=new Color(200,160,120);
  Check(net.CountryFocus==0 && net.FocusCells.Count==0 && net.FocusCircleNodes.Contains(home),"Recovery spotlight includes agent, goods and accepting home");
  Check(net.FocusTerrain(color,new Vector2(-10000))==new Color(130,104,78),"Everything outside recovery circles is dimmed 35 percent");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){PlanClick(Rect("SalvagePickUp").Center);Check(w.Robots.Retinues[0].Robots.Count==1 && !w.Salvage.Items.Any(i=>i.Id==discard),"Local UI pickup places selected goods in pocket");PlanClick(Rect("SalvageToggle").Center);Check(w.Salvage.Agents.Single(a=>a.Owner==0).Paused,"Pause button stops agent");}
  if(frame==2){PlanClick(Rect("SalvageToggle").Center);for(int i=0;i<4;i++){PlanClick(Rect("SalvagePolicyButton",i).Center);Check(w.Salvage.Agents.Single(a=>a.Owner==0).Policy==(SalvagePolicy)i,"All four policy buttons apply");}Set("_salvageSelected",100L);PlanClick(Rect("SalvageRequest").Center);Set("_statusGlobe",true);Call("FocusStatusGlobe");}
  if(frame==3){w.Salvage.Advance(w,1);Set("_salvageFocusTurn",-1);var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  if(frame==4){Call("OpenNodeInspector",home);Call("UpdateDomesticSlide",1f);Call("OpenNodeAssembly");HangarClick(Rect("HangarAbandonButton").Center);HangarClick(Rect("HangarCell",0).Location+new Point(12,150));Check(Get("_salvageAbandonConfirmation")!=null,"Card margin selects all remaining parts for abandonment");}
  if(frame==5){Call("ResetInspector");Call("UpdateDomesticSlide",1f);}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"salvage-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame==4){Confirm(true);Check(w.Robots.Nodes[home].Count==0 && w.Salvage.Items.Any(i=>i.AbandonedBy==0 && i.Robot.Parts==(RobotParts.Body|RobotParts.Legs)),"Whole abandonment frees hangar slot");}
  if(frame++==5){Console.WriteLine("PASS: primary action entry and toggle, default hiding, recovery plan entry, four policies, pause/request/pickup, 35 percent dimming, part/whole abandonment confirmation, globe/net and full/half-size rendering.");Exit();}
 }
}

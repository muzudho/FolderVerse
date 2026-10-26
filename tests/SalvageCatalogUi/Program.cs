using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CatalogChecks();game.Run();
sealed class CatalogChecks:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 WorldSetup w=null!;int home,near,frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Rect(string n,params object[] a)=>a.Length==0?(Rectangle)typeof(Game1).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!:(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,a)!;
 void Click(string n,params object[] a){Set("_pointer",Rect(n,a).Center);Call("SalvageCatalogInput",true,false);}
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.Relations.Reset();w.SelectPlayer(0);w.Salvage.Reset();
  var node=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);home=node.Id;var route=w.Routes.Neighbors(node)[0];near=w.Nodes.At(route.Target,route.Entry).Id;
  foreach(var n in w.Nodes.All)n.Owner=1;node.Owner=w.Nodes.All[near].Owner=0;
  w.ConquerorLocations[0]=node.Cell;w.ConquerorPoints[0]=node.Center;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Salvage.EnsureAgents(w);
  for(int i=0;i<31;i++){
   var robot=w.Robots.Create(i%2,(RobotParts)(1+i%7)) with {IntegratedParts=i%7==2?(RobotParts)3:i%7==5?(RobotParts)6:0};
   robot=robot with {CheapParts=i%3==0?robot.Parts:0,DamagedParts=i%4==0?robot.Parts&RobotParts.Body:0};
   w.Salvage.Items.Add(new(i+1,i<27?home:near,robot));
  }
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_statusGlobe",false);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("OpenSalvagePlan",home);Call("UpdateDomesticSlide",1f);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   var hits=(List<(Rectangle Bounds,int Node)>)Get("_salvageHits");Check(hits.Count>0 && hits.Select(h=>h.Node).Distinct().Count()==hits.Count,"One basket hit per visible location despite many items");
   var hit=hits.First(h=>h.Node==home);Call("SalvageInput",hit.Bounds.Center,true);
   Check((bool)Get("_salvageCatalogOpen") && (int)Get("_salvageNodeFilter")==home,"Basket opens location-filtered catalogue");
   Check(((SalvageItem[])Call("VisibleSalvageItems")).Length==27,"Filtered catalogue includes every item at the location");
  }
  if(frame==2){Click("SalvageCatalogNext");Check((int)Get("_salvagePage")==1,"More than 24 items paginates");Click("SalvageCatalogCard",0);var id=(long)Get("_salvageSelected");Click("SalvageCatalogRequest");Check(w.Salvage.Items.Single(i=>i.Id==id).RequestedBy==0,"Selected last-page item receives request");}
  if(frame==3){Click("SalvageCatalogAll");Check((int)Get("_salvageNodeFilter")==-1 && ((SalvageItem[])Call("VisibleSalvageItems")).Length==31,"All-locations control removes filter");}
  if(frame==4){var selected=w.Salvage.Items.Single(i=>i.Id==(long)Get("_salvageSelected"));Click("SalvageCatalogMap");Check(!(bool)Get("_salvageCatalogOpen") && (bool)Get("_salvagePlan"),"Map action returns to recovery plan");Check((int)Get("_inspectorCenterNode")==selected.Node,"Selected location is centered");}
  if(frame==5){Call("SalvageInput",Rect("SalvageCatalogButton").Center,true);Check((bool)Get("_salvageCatalogOpen"),"Inspector button opens full catalogue");Call("OpenSalvageCatalog",home);Click("SalvageCatalogCard",0);int count=w.Salvage.Items.Count;var id=(long)Get("_salvageSelected");Click("SalvageCatalogPickUp");Check(w.Salvage.Items.Count==count-1 && !w.Salvage.Items.Any(i=>i.Id==id) && w.Robots.Retinues[0].Count==1,"Catalogue pickup preserves physical inventory");var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=960;g.PreferredBackBufferHeight=540;g.ApplyChanges();}
  if(frame==6){Call("SalvageCatalogInput",false,true);Check(!(bool)Get("_salvageCatalogOpen"),"Escape returns to map");w.Salvage.Items.Clear();Call("OpenSalvageCatalog",home);Click("SalvageCatalogMap");Click("SalvageCatalogRequest");Click("SalvageCatalogPickUp");Check(w.Robots.Retinues[0].Count==1,"Empty catalogue actions are inert");}
  base.Draw(t);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"catalog-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==6){Console.WriteLine("PASS: grouped basket markers, filtered/all catalogue, 24-item pages, selection/request/pickup, map centering, escape, empty inventory and full/half-size rendering.");Exit();}
 }
}

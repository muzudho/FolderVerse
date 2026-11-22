using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Collections;
using var game=new WarLayout(args[0]);game.Run();
sealed class WarLayout(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;CubeNet net=null!;WorldPreview globe=null!;SpriteBatch batch=null!;Node home=null!,target=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 static object Row(object row,string name)=>row.GetType().GetProperty(name)!.GetValue(row)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Directory.CreateDirectory(output);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");base.LoadContent();
  Set("_autoSaveEnabled",false);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_animationTime",10f);
  w=(WorldSetup)Get("_setup");w.SetParticipantCounts(2,2);w.SetWorld(2);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);w.HumanPlayers.Add(0);w.Relations.Reset();
  w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);home=w.Nodes.Current(0);home.Owner=0;var route=w.Routes.Neighbors(home)[0];target=w.Nodes.At(route.Target,route.Entry);target.Owner=1;
  var store=w.Robots.Nodes[home.Id];typeof(RobotStore).GetMethod("SetCapacity",F)!.Invoke(store,new object[]{200});for(int i=0;i<125;i++)store.Add(w.Robots.Create(0,RobotParts.Complete));
  for(int i=0;i<4;i++)w.Robots.Retinues[0].Add(w.Robots.Create(0,RobotParts.Complete));
  w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  net=(CubeNet)Get("_net");net.Home(w);net.ShowMapSymbols=false;net.ShowRobots=false;net.ShowBatteries=false;globe=(WorldPreview)Get("_world");globe.ShowSetup(w,true);batch=(SpriteBatch)Get("_spriteBatch");
  Call("OpenWarPlanning");Set("_domesticSlide",1f);Check((bool)Prop("CompactWarInspector"),"war uses compact inspector");
  var panel=(Rectangle)Prop("NodeInspectorBounds");var map=(Rectangle)Prop("MapViewBounds");
  Check(panel.Left>=0&&panel.Right<960&&panel.Bottom<=670,"war panel fits left side above both medallions");Check(map.Left<1000&&map.Width>900,"war map gains over 300 pixels of width");
  Check(!panel.Intersects(new Rectangle(20,764,312,304))&&!panel.Intersects(new Rectangle(221,706,98,98)),"war panel does not overlap player or subordinate medal");
  net.RestrictNodeActions=true;net.ActionTargetNodes.Clear();var color=new Color(80,170,220);Check(net.NodeFlagTint(home.Id,color)==color&&net.NodeTint(home.Id,color)!=color,"war flags stay enabled while unavailable buildings stay disabled");
  Call("SelectMoveNode",target.Id);CheckPages();
 }
 void CheckPages(){
  var sources=(IList)Get("_invasionSources");int source=-1;for(int i=0;i<sources.Count;i++)if((int)sources[i]!.GetType().GetField("Node")!.GetValue(sources[i])! == home.Id && !(bool)sources[i]!.GetType().GetField("Queen")!.GetValue(sources[i])!){source=i;break;}
  Check(source>=0,"large hangar is an eligible invasion source");var pages=(IList)Call("InvasionPages");var slots=new HashSet<int>();
  for(int page=0;page<pages.Count;page++)foreach(var row in (IEnumerable)pages[page]!){
   var r=(Rectangle)Row(row,"Bounds");Check(r.Bottom<=568,"source row stays inside shortened body");
   if((int)Row(row,"Index")!=source)continue;int first=(int)Row(row,"FirstSlot"),count=(int)Row(row,"SlotCount");for(int i=first;i<first+count;i++)if(!slots.Add(i))throw new Exception("Duplicate robot slot "+i);
  }
  Check(slots.SetEquals(Enumerable.Range(0,125)),"pagination retains every robot of a 125-unit source");
  int lastPage=(int)Call("InvasionPageForRobot",source,124);Set("_invasionPage",lastPage);var last=((IEnumerable)Call("InvasionVisibleRows")).Cast<object>().Single(row=>(int)Row(row,"Index")==source);
  var bounds=(Rectangle)Row(last,"Bounds");int firstSlot=(int)Row(last,"FirstSlot");var card=(Rectangle)typeof(Game1).GetMethod("InvasionCardAt",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{bounds,124-firstSlot})!;
  Check((bool)Call("InvasionClick",card.Center),"last robot card is clickable on its own page");
  var chosen=(HashSet<long>)sources[source]!.GetType().GetField("Robots")!.GetValue(sources[source])!;Check(chosen.Contains(w.Robots.Nodes[home.Id].Robots[124].Id),"last-page click reserves the intended robot");
  Set("_invasionPage",(int)Call("InvasionPageForSource",source));Call("FocusDemoNode",target);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(new Color(16,35,47));batch.Begin();Call("DrawStatusUi");batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,frame==0?"war-compact-globe.png":"war-compact-net.png")))image.SaveAsPng(file,1920,1080);
  var pixels=new Color[1920*1080];image.GetData(pixels);Check(!Enumerable.Range(676,78).Any(y=>Enumerable.Range(54,760).Any(x=>pixels[y*1920+x]==new Color(19,35,46))),"robot cards never spill below the compact panel");
  Check(net.KeepFlagsEnabled,"war keeps normal flag colors during rendering");
  if(frame++==0){Set("_statusGlobe",false);Call("FocusDemoNode",target);}else{Call("CloseMovement");Call("UpdateMapInteractionState");Check(!net.KeepFlagsEnabled,"leaving war restores ordinary flag palette");Call("OpenMovementWithFit",false);Set("_domesticSlide",1f);var peace=(Rectangle)Prop("NodeInspectorBounds");var peaceMap=(Rectangle)Prop("MapViewBounds");Check(peace.Bottom<=670&&peace.Right<960&&peaceMap.Width>900,"peace movement shares the short left inspector and wide map");var confirm=(Rectangle)typeof(Game1).GetField("ConfirmMoveButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check(confirm.Bottom<=670,"peace reservation button stays inside compact panel");Console.WriteLine("PASS compact war and peace inspectors, flag colors and paged robot selection");Exit();}
 }
}

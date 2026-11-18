using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
using var game=new DiplomacyProbe(args[0]);game.Run();
sealed class DiplomacyProbe(string output):Game1 {
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;SpriteBatch batch=null!;WorldSetup w=null!;TradeOffer offer=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool v,string s){if(!v)throw new Exception(s);Console.WriteLine("PASS "+s);}
 protected override void LoadContent(){
  w=new();w.SetParticipantCounts(2,18);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.AddRange(new[]{0,1});
  w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
  var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);a.Owner=0;b.Owner=1;
  var x=w.Robots.Create(0,RobotParts.Complete);var y=w.Robots.Create(1,RobotParts.Complete);w.Robots.Nodes[a.Id].Add(x);w.Robots.Nodes[b.Id].Add(y);
  offer=new(w.Population.Turn-1,new(0,a.Id,false),new(1,b.Id,false),new(x.Id,false),new(y.Id,false));w.Campaign.Trade.Offers.Add(offer);Check(w.Campaign.Trade.Possible(w,offer),"valid two-human exchange fixture");
  Set("_setup",w);Set("_world",new WorldPreview(GraphicsDevice));Set("_conquestOverviewTurn",w.Population.Turn);Set("_autoSaveEnabled",false);Set("_statisticsEnabled",false);
  batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(output,"font-content");var font=Fonts.LoadUi(Content);Set("_font",font);Set("_ui",Activator.CreateInstance(typeof(Game1).GetField("_ui",F)!.FieldType,batch,pixel,font)!);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_diplomacyPending",true);Set("_conquestOverview",true);Check(!(bool)Call("TryBeginPendingDiplomacy") && (bool)Get("_diplomacyPending"),"conquest overview defers diplomacy without consuming it");Set("_conquestOverview",false);Check((bool)Call("TryBeginPendingDiplomacy"),"diplomacy starts after overview before domestic");Call("UpdateDiplomacy",29.9);Check(w.Campaign.Trade.Offers.Count==1,"offer stays before 30-second deadline");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawDiplomacy");batch.End();GraphicsDevice.SetRenderTarget(null);using(var f=File.Create(Path.Combine(output,frame==0?"offer.png":"results.png")))target.SaveAsPng(f,1920,1080);
  if(frame++==0){
   Call("DiplomacyInput",true,false);Check((double)Get("_diplomacyIdleAge")==0,"interaction resets countdown without accepting");Call("UpdateDiplomacy",30d);
   Check(w.Campaign.Trade.Offers.Count==0,"timeout answers offer once");Check(w.Robots.Nodes[offer.From.Node].Robots.Single().Id==offer.Give.Id && w.Robots.Nodes[offer.To.Node].Robots.Single().Id==offer.Want.Id,"timeout declines without exchanging cargo");
   Call("UpdateDiplomacy",7.9);Check(Get("_screen").ToString()=="Diplomacy","summary stays before eight-second deadline");return;
  }
  Call("UpdateDiplomacy",.1);Check(Get("_screen").ToString()=="WorldStatus","summary timeout enters domestic screen");Call("UpdateDiplomacy",100d);Check(Get("_screen").ToString()=="WorldStatus","finished timer cannot advance twice");Check(!(bool)Get("_diplomacyPending"),"no duplicate diplomacy after restore");w.Campaign.Trade.Offers.Add(offer);Call("BeginDiplomacy");Set("_pointer",new Point(1500,980));Call("DiplomacyInput",true,false);Check(w.Robots.Nodes[offer.From.Node].Robots.Single().Id==offer.Want.Id && w.Robots.Nodes[offer.To.Node].Robots.Single().Id==offer.Give.Id,"explicit acceptance still exchanges cargo");Check((int)Get("_lastAutoSaveTurn")==-1,"trade decision invalidates pre-diplomacy autosave");Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_autoPlay",true);w.Campaign.Trade.Replies.Add(new(offer,"test result"));Check(!(bool)Call("BeginDiplomacy") && Get("_screen").ToString()=="WorldStatus","automatic mode retains domestic screen");Exit();
 }
}




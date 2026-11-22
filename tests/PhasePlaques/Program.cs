using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Reflection;
using var game=new PlaqueProbe(args[0]);game.Run();
sealed class PlaqueProbe(string output):Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 SpriteBatch batch=null!;WorldSetup w=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Screen(string name)=>Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,name));
 static void Check(bool b,string m){if(!b)throw new Exception(m);Console.WriteLine("PASS "+m);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Directory.CreateDirectory(output);
  AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");base.LoadContent();
  Set("_autoSaveEnabled",false);Set("_demoActive",false);Set("_autoPlay",false);Set("_conquestOverview",false);Set("_pointer",new Point(-10,-10));
  var fontContent=new ContentManager(Services,Environment.GetEnvironmentVariable("UI_FONT_ROOT")!);var font=fontContent.Load<SpriteFont>("UiFont");Set("_font",font);batch=(SpriteBatch)Get("_spriteBatch");Set("_ui",new UiPainter(batch,(Texture2D)Get("_pixel"),font));
  w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,1);w.SetWorld(42);w.SetCast(8);w.SetTerritoryRights(2);var planner=new ComputerLandPlanner(w);foreach(int r in w.LandOrder())w.ClaimLand(r,planner.Select(r,null).Cells);w.CompleteLand(3,25,25);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  foreach(string screen in new[]{"PlayerCount","ComputerPlayerCount","Rolling","Review","CastRolling","CastReview","PlacementRolling","PlacementReview","Ready","PlayerSelect","PlayerReady","LandConquest","Fortune","WorldStatus","Industry","Newspaper","Diplomacy","Transport","Battle","BattleResearch","LessonCatalog","OpeningBook","OpeningSpace","OpeningLottery","OpeningReview"}){
   Screen(screen);var value=typeof(Game1).GetProperty("CurrentPhasePlaque",F)!.GetValue(this);Check(value!=null,"phase mapped: "+screen);
  }
  Screen("Title");Check(typeof(Game1).GetProperty("CurrentPhasePlaque",F)!.GetValue(this)==null,"title keeps its own logo");
  var bounds=(Rectangle)typeof(Game1).GetField("PhasePlaqueBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check(bounds.Left>=640&&bounds.Bottom<=82&&bounds.Right<1510,"shared header stays in right pane above map controls and outside system menu");
 }
 void Capture(string name,Action draw){
  using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();draw();Call("DrawPhasePlaque");batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,name+".png"));target.SaveAsPng(file,1920,1080);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame++>0){Exit();return;}
  Screen("PlayerCount");Capture("01-player-count",()=>Call("DrawPlayerCount"));
  Screen("CastReview");Capture("02-cast",()=>Call("DrawCastUi"));
  Screen("WorldStatus");Set("_guideWorld",w);Set("_guideRuler",0);Set("_guideBriefing",GuideNarration.Build(w,0,true));Capture("03-guide",()=>Call("DrawGuideScene"));Set("_guideBriefing",null!);
  Screen("Industry");w.Industry.Queue(1);Call("BeginIndustry");Set("_industryAge",100d);Set("_manufacturingActive",false);Capture("04-industry",()=>Call("DrawIndustry"));
  Set("_manufacturingActive",true);Capture("05-manufacturing",()=>Call("DrawManufacturing"));Set("_manufacturingActive",false);
  Screen("Diplomacy");Capture("06-diplomacy",()=>Call("DrawDiplomacy"));
  Screen("BattleResearch");Capture("07-research",()=>Call("DrawBattleResearch"));
  Screen("WorldStatus");Capture("08-domestic-header",()=>{Call("DrawDomesticPhaseHeading");Call("DrawStatusMenuCore");Call("DrawMapNavigationButtons");Call("DrawRouteLayerButtons");});
  Screen("LandConquest");Capture("15-background-logo",()=>Call("DrawPhaseBackdropLogo"));
  Screen("LessonCatalog");Capture("12-lesson-catalog",()=>Call("DrawLessonCatalog"));
  Screen("Hierarchy");Capture("13-hierarchy",()=>Call("DrawHierarchyUi"));
  Screen("Pocket");Capture("14-pocket",()=>Call("DrawPocket"));
  Screen("Newspaper");Set("_turnPapers",TurnNewspaper.Build(w,0));Set("_turnPaperIndex",0);Set("_paperReadCountry",0);Call("BeginPaperScenes");Set("_paperAge",10d);Capture("10-newspaper",()=>Call("DrawTurnNewspaper"));
  Screen("WorldStatus");Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");Set("_conquestStage",4);Set("_conquestAge",0d);
  using(var broadcast=new RenderTarget2D(GraphicsDevice,1920,1080)){
   GraphicsDevice.SetRenderTarget(broadcast);Call("DrawConquestOverview",new Rectangle(0,0,1920,1080),Matrix.Identity,GraphicsDevice.Viewport);GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"11-conquest-broadcast.png"));broadcast.SaveAsPng(file,1920,1080);
  }
  Set("_conquestOverview",false);
  // Exercise every localized main/sub label and every atlas cell at both common display sizes.
  foreach(var size in new[]{new Point(1920,1080),new Point(1366,768)}){
   using var target=new RenderTarget2D(GraphicsDevice,size.X,size.Y);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin(transformMatrix:Matrix.CreateScale(size.X/1920f,size.Y/1080f,1));
   Call("DrawPhasePlaqueLabel","世界征服状況","朝の放送",4);batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"09-conquest-"+size.X+".png"));target.SaveAsPng(file,size.X,size.Y);
  }
  var icons=(Texture2D)Get("_phasePlaqueIcons");var pixels=new Color[icons.Width*icons.Height];icons.GetData(pixels);Check(pixels[0].A<=1&&pixels[0].R==0&&pixels[0].G==0&&pixels[0].B==0,"icon atlas transparent corners cannot leak colored RGB");
  Console.WriteLine("PASS phase plaques real scene rendering and screen mapping");Exit();
 }
}

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
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F|BindingFlags.Static)!.Invoke(this,a)!;
 void Screen(string name)=>Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,name));
 static void Check(bool b,string m){if(!b)throw new Exception(m);Console.WriteLine("PASS "+m);}
 protected override void LoadContent(){
  Window.Position=new(-3000,-3000);Directory.CreateDirectory(output);
  AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");base.LoadContent();
  Set("_autoSaveEnabled",false);Set("_demoActive",false);Set("_autoPlay",false);Set("_conquestOverview",false);Set("_pointer",new Point(-10,-10));
  var fontContent=new ContentManager(Services,Environment.GetEnvironmentVariable("UI_FONT_ROOT")!);var font=fontContent.Load<SpriteFont>("UiFont");Set("_font",font);batch=(SpriteBatch)Get("_spriteBatch");Set("_ui",new UiPainter(batch,(Texture2D)Get("_pixel"),font));
  w=(WorldSetup)Get("_setup");w.SetParticipantCounts(1,1);w.SetWorld(42);w.SetCast(8);w.SetTerritoryRights(2);var planner=new ComputerLandPlanner(w);foreach(int r in w.LandOrder())w.ClaimLand(r,planner.Select(r,null).Cells);w.CompleteLand(3,25,25);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  var wrapped=(string[])Call("WrapSpeechLines",new[]{new string('あ',100)},500);
  Check(wrapped.Length>1&&wrapped.All(l=>font.MeasureString(l).X*.48f<=500),"long speech wraps at fixed font size instead of shrinking");
  var balloon=(Rectangle)Call("SizeSpeechBalloon",new Rectangle(500,172,1000,144),new[]{"１行目","２行目","３行目"});
  Check(BalloonRenderer.TextArea(balloon).Height>=3*36,"broadcast grows to fit three common-size lines");
  var folder=(Rectangle)typeof(Game1).GetField("FolderTreeButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var directory=(Rectangle)typeof(Game1).GetField("ReservationDirectoryButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(folder.X==360&&directory.X>folder.Right&&directory.Right<680,"folder X stays fixed and reservation button clears title plaque");
  var controls=new[]{(Rectangle)typeof(Game1).GetField("SimpleDisplayButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!,(Rectangle)typeof(Game1).GetField("MapViewButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!,(Rectangle)Call("RouteLayerButton",6),(Rectangle)Call("RouteLayerButton",2),(Rectangle)Call("RouteLayerButton",3)};
  Check(controls.SelectMany((b,i)=>controls.Skip(i+1).Select(c=>!b.Intersects(c))).All(ok=>ok)&&controls.All(b=>b.Left>=1260&&b.Right<=1858),"map control rows have no overlaps and share right-side alignment");
  CheckOpeningDaylight();
  foreach(string screen in new[]{"PlayerCount","ComputerPlayerCount","Rolling","Review","CastRolling","CastReview","PlacementRolling","PlacementReview","Ready","PlayerSelect","PlayerReady","LandConquest","Fortune","WorldStatus","Industry","Newspaper","Diplomacy","Transport","Battle","BattleResearch","LessonCatalog","OpeningBook","OpeningSpace","OpeningLottery","OpeningReview"}){
   Screen(screen);var value=typeof(Game1).GetProperty("CurrentPhasePlaque",F)!.GetValue(this);Check(value!=null,"phase mapped: "+screen);
  }
  Screen("Title");Check(typeof(Game1).GetProperty("CurrentPhasePlaque",F)!.GetValue(this)==null,"title keeps its own logo");
  var title=(Rectangle)typeof(Game1).GetField("PhasePlaqueTitleBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var subtitle=(Rectangle)typeof(Game1).GetField("PhasePlaqueSubtitleBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var icon=(Rectangle)typeof(Game1).GetField("PhasePlaqueIconBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(subtitle.Width<=240&&subtitle.Height>=34&&icon.Width>=140&&icon.Bottom>128,"subtitle is shorter and taller; illustration protrudes beyond frame");
  var breaker=(Rectangle)typeof(Game1).GetField("SimpleDisplayButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(breaker.Left>=1240,"display controls clear the enlarged header");
  Check(boundsForIcon().Top>icon.Top&&boundsForIcon().Bottom>=icon.Bottom-12,"plate moves down around icon rather than hanging icon below it");
  Rectangle boundsForIcon()=>(Rectangle)typeof(Game1).GetField("PhasePlaqueBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(title.Center.X==subtitle.Center.X&&icon.Width==icon.Height,"titles share their center and icon uses a square without stretching");
  var bounds=(Rectangle)typeof(Game1).GetField("PhasePlaqueBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check(bounds.Center.X==960&&bounds.Top>=12&&bounds.Bottom<=150&&bounds.Right<1510,"header shares body center and has generous height with top margin");
 }
 void CheckOpeningDaylight(){
  using var preview=new WorldPreview(GraphicsDevice);
  object Read(string n)=>typeof(WorldPreview).GetField(n,F)!.GetValue(preview)!;
  foreach(int seed in new[]{1924187616,42,1}){
   var world=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};world.SetWorld(seed);
   typeof(WorldPreview).GetMethod("ShowOpeningPlanet",F)!.Invoke(preview,new object[]{world});
   if(seed==1924187616){
    var source=(VertexPositionColorNormal[])Read("_daySource");var extended=source.Concat(source.TakeLast(3)).ToArray();
    var oldBuffer=(VertexBuffer)Read("_vertices");var buffer=new VertexBuffer(GraphicsDevice,VertexPositionColorNormal.VertexDeclaration,extended.Length,BufferUsage.WriteOnly);buffer.SetData(extended);
    typeof(WorldPreview).GetField("_vertices",F)!.SetValue(preview,buffer);oldBuffer.Dispose();
    var reset=typeof(WorldPreview).GetMethod("ResetDaylightMesh",F);
    if(reset!=null)reset.Invoke(preview,new object[]{extended,(int)Read("_dayTerrainVertexCount")});
    else {typeof(WorldPreview).GetField("_daySource",F)!.SetValue(preview,extended);}
    Check(extended.Length%6==3,"regression includes an unpaired three-vertex model triangle");
   }
   preview.DaylightWorld=world;preview.DawnMeridian=.4f;
   // This is the original crash path: opening resource models append non-quad triangles.
   typeof(WorldPreview).GetMethod("ApplyDaylight",F)!.Invoke(preview,null);
   Validate("opening seed "+seed);
   world.SetParticipantCounts(1,1);world.SetCast(8);world.SetPlacement(1);
   preview.ShowLandConquest(world,previewMaples:true);preview.DaylightWorld=world;
   typeof(WorldPreview).GetMethod("ApplyDaylight",F)!.Invoke(preview,null);
   Validate("land preview seed "+seed);
  }
  void Validate(string label){
   var source=(VertexPositionColorNormal[])Read("_daySource");var working=(VertexPositionColorNormal[])Read("_dayWorking");var tiles=(MapTerrainTile[])Read("_dayTileMap");int terrain=(int)Read("_dayTerrainVertexCount");
   Check(source.Length==((VertexBuffer)Read("_vertices")).VertexCount&&source.Length==tiles.Length&&source.Length==working.Length,"daylight cache matches actual mesh: "+label);
   Check(terrain%6==0&&terrain<source.Length&&tiles.Skip(terrain).All(t=>t.Cell<0),"triangular resource models never enter six-vertex terrain mapping: "+label);
   var colors=working.Skip(terrain).Select(v=>v.Color).ToArray();preview.SelectedCell=0;preview.DawnMeridian=.41f;typeof(WorldPreview).GetMethod("ApplyDaylight",F)!.Invoke(preview,null);
   var phases=(float[])Read("_dayPhases");
   for(int i=terrain;i<source.Length;i++){float hour=WorldDayCycle.Wrap(preview.DawnMeridian-phases[i]+8/24f)*24;if(working[i].Color!=WorldDayCycle.Tint(source[i].Color,hour))throw new Exception("Terrain highlight recolored a resource model");}
   Check(true,"resource model keeps own palette under daylight: "+label);
  }
 }
 void Capture(string name,Action draw){
  using var target=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin();draw();Call("DrawPhasePlaque");batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,name+".png"));target.SaveAsPng(file,1920,1080);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame++>0){Exit();return;}
  Screen("OpeningBook");
  using(var opening=new RenderTarget2D(GraphicsDevice,1920,1080)){
   GraphicsDevice.SetRenderTarget(opening);Call("DrawOpeningFrame",new GameTime(TimeSpan.FromSeconds(4),TimeSpan.Zero));GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"00-opening-book.png"));opening.SaveAsPng(file,1920,1080);
  }
  var body=(Rectangle)typeof(Game1).GetField("OpeningIntroTextBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var plaque=(Rectangle)typeof(Game1).GetField("PhasePlaqueBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(body.Center.X==plaque.Center.X&&body.Top>=plaque.Bottom+24,"opening paragraph is centered below plaque with a clear gap");
  Screen("PlayerCount");Capture("01-player-count",()=>Call("DrawPlayerCount"));
  Screen("CastReview");Capture("02-cast",()=>Call("DrawCastUi"));
  foreach(int count in new[]{20,60}){
   var cast=new WorldSetup();cast.SetParticipantCounts(1,count-1);cast.SetWorld(42);cast.SetCast(8);cast.SetPlacement(1);Set("_setup",cast);Screen("PlacementReview");
   var boxes=Enumerable.Range(0,count).Select(i=>(Rectangle)Call("PortraitCell",i)).ToArray();
   Check(boxes.All(b=>b.Top>=180&&b.Bottom<=1010),"all cast portraits clear header and footer for "+count+" participants");
   Check(boxes.SelectMany((b,i)=>boxes.Skip(i+1).Select(c=>!b.Intersects(c))).All(ok=>ok),"cast portraits never overlap for "+count+" participants");
   Capture("02-placement-"+count,()=>{Call("DrawCastUi");Call("DrawCastPreviewInfo");});
   Call("BeginSelection");var selection=(CharacterTile[])Get("_selectionTiles");
   Check(selection.All(t=>t.Bounds.Top>=180&&t.Bounds.Bottom<=1012),"selection grid respects fixed plaque for "+count+" participants");
   var candidate=selection.First(t=>t.Slot>=0);Check((int)Call("HitSelection",candidate.Bounds.Center)==candidate.Slot,"selection hit test uses relocated portrait bounds for "+count+" participants");
   Capture("02-selection-"+count,()=>Call("DrawSelectionUi"));
  }
  Set("_setup",w);
  Screen("WorldStatus");Set("_guideWorld",w);Set("_guideRuler",0);Set("_guideBriefing",GuideNarration.Build(w,0,true));Capture("03-guide",()=>Call("DrawGuideScene"));Set("_guideBriefing",null!);
  Screen("Industry");w.Industry.Queue(1);Call("BeginIndustry");Set("_industryAge",100d);Set("_manufacturingActive",false);Capture("04-industry",()=>Call("DrawIndustry"));
  Set("_manufacturingActive",true);Capture("05-manufacturing",()=>Call("DrawManufacturing"));Set("_manufacturingActive",false);
  Screen("Diplomacy");Capture("06-diplomacy",()=>Call("DrawDiplomacy"));
  Screen("BattleResearch");Capture("07-research",()=>Call("DrawBattleResearch"));
  Screen("WorldStatus");Capture("08-domestic-header",()=>{Call("DrawDomesticPhaseHeading");Call("DrawStatusMenuCore");Call("DrawMapNavigationButtons");Call("DrawRouteLayerButtons");});
  Set("_movementOpen",true);Set("_warPlanning",false);Set("_domesticSlide",1f);Capture("16-peace-movement",()=>Call("DrawInspector",(Action)(()=>Call("DrawMovementPanel"))));Set("_movementOpen",false);Set("_domesticSlide",0f);
  ((int[])Get("_directNodes"))[0]=w.Nodes.Current(0).Id;((int[])Get("_directNodes"))[1]=w.Nodes.Current(0).Id;((CubeNet)Get("_directPairNet")).Home(w);Set("_directPairGlobe",false);Set("_directPairSlide",1f);Set("_directPairOpen",true);Capture("17-direct",()=>Call("DrawDirectPair"));Set("_longDeliveryMode",true);Capture("18-long-delivery",()=>Call("DrawLongPair"));Set("_longDeliveryMode",false);Set("_directPairOpen",false);
  Set("_transportOverview",true);Set("_domesticSlide",1f);Set("_populationNode",w.Nodes.Current(0).Id);for(int i=0;i<16;i++)w.Robots.Transport.AddInstruction(w.Nodes.Current(0).Id,w.Nodes.Current(0).Owner,(w.Nodes.Current(0).Id+1)%w.Nodes.All.Length);
  Check((int)typeof(Game1).GetProperty("TransportPageSize",F)!.GetValue(this)! ==2,"compact transport shows two instructions per page");
  Check(Enumerable.Range(0,15).All(i=>((Rectangle)Call("TransportVariantButton",i)).Bottom<612),"all fifteen transport choices fit above modal close button");
  Capture("19-transport-plan",()=>Call("DrawInspector",(Action)(()=>Call("DrawTransportOverviewPanel"))));Set("_transportEditNumber",w.Robots.Transport.Instructions(w.Nodes.Current(0).Id,w.Nodes.Current(0).Owner).First().Number);Set("_transportEditor",1);Capture("19-transport-parts",()=>Call("DrawInspector",(Action)(()=>Call("DrawTransportOverviewPanel"))));Set("_transportEditor",0);Set("_transportOverview",false);Set("_domesticSlide",0f);
  Set("_recoveryOpen",true);Set("_recoveryStage",4);Capture("20-recovery",()=>Call("DrawRecoveryPlanner"));Set("_recoveryOpen",false);
  Set("_tradePlannerOpen",true);Capture("21-trade",()=>Call("TradePlanner",true,false));Set("_tradePlannerOpen",false);
  Set("_stratagemCancelledNotice",new[]{"城壁への救援 / 地点の用途・所有国・予約が変わりました"});Capture("22-stratagem-cancelled",()=>Call("DrawStratagemInterface"));Set("_pointer",new Point(1140,677));Check((bool)Call("StratagemInput",new Microsoft.Xna.Framework.Input.MouseState(),true,false,true,0d),"cancellation confirmation consumes the click");Check(((string[])Get("_stratagemCancelledNotice")).Length==0,"confirmation clears the one-time notice");Set("_pointer",new Point(-10,-10));
  Screen("LandConquest");Capture("15-background-logo",()=>Call("DrawPhaseBackdropLogo"));
  Screen("LessonCatalog");Capture("12-lesson-catalog",()=>Call("DrawLessonCatalog"));
  Screen("Hierarchy");Capture("13-hierarchy",()=>Call("DrawHierarchyUi"));
  Screen("Pocket");Capture("14-pocket",()=>Call("DrawPocket"));
  Screen("Newspaper");Set("_turnPapers",TurnNewspaper.Build(w,0));Set("_turnPaperIndex",0);Set("_paperReadCountry",0);Call("BeginPaperScenes");Set("_paperAge",10d);Capture("10-newspaper",()=>Call("DrawTurnNewspaper"));
  Screen("WorldStatus");Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");Set("_conquestStage",4);Set("_conquestAge",0d);
  using(var broadcast=new RenderTarget2D(GraphicsDevice,1920,1080)){
   GraphicsDevice.SetRenderTarget(broadcast);Call("DrawConquestOverview",new Rectangle(0,0,1920,1080),Matrix.Identity,GraphicsDevice.Viewport);GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"11-conquest-broadcast.png"));broadcast.SaveAsPng(file,1920,1080);
  }
  Set("_conquestStage",3);
  using(var intro=new RenderTarget2D(GraphicsDevice,1920,1080)){
   GraphicsDevice.SetRenderTarget(intro);Call("DrawConquestOverview",new Rectangle(0,0,1920,1080),Matrix.Identity,GraphicsDevice.Viewport);GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"11-conquest-intro.png"));intro.SaveAsPng(file,1920,1080);
  }
  Set("_conquestOverview",false);
  // Exercise every localized main/sub label and every atlas cell at both common display sizes.
  foreach(var size in new[]{new Point(1920,1080),new Point(1366,768)}){
   using var target=new RenderTarget2D(GraphicsDevice,size.X,size.Y);GraphicsDevice.SetRenderTarget(target);GraphicsDevice.Clear(new Color(16,35,46));batch.Begin(transformMatrix:Matrix.CreateScale(size.X/1920f,size.Y/1080f,1));
   Call("DrawPhasePlaqueLabel","世界征服状況","朝の放送",4);batch.End();GraphicsDevice.SetRenderTarget(null);using var file=File.Create(Path.Combine(output,"09-conquest-"+size.X+".png"));target.SaveAsPng(file,size.X,size.Y);
  }
  var icons=(Texture2D)Get("_phasePlaqueIcons");var pixels=new Color[icons.Width*icons.Height];icons.GetData(pixels);Check(pixels[0].A<=1&&pixels[0].R==0&&pixels[0].G==0&&pixels[0].B==0,"icon atlas transparent corners cannot leak colored RGB");
  var source=(Rectangle)Get("_phasePlaqueFrameSource");var targetBox=(Rectangle)typeof(Game1).GetField("PhasePlaqueBounds",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  var segments=((Rectangle Source,Rectangle Target)[])typeof(Game1).GetMethod("PhasePlaqueSegments",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{source,targetBox})!;
  Check(segments.Sum(s=>s.Target.Width)==targetBox.Width&&segments[0].Target.Right==segments[1].Target.Left&&segments[1].Target.Right==segments[2].Target.Left,"cloud frame slices cover header without seams or overlaps");
  Check(Math.Abs(segments[0].Target.Width/(float)segments[0].Source.Width-targetBox.Height/(float)source.Height)<.002,"cloud lobes preserve uniform scale instead of vertical compression");
  Console.WriteLine("PASS phase plaques real scene rendering and screen mapping");Exit();
 }
}

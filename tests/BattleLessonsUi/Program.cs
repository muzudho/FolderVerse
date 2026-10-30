using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new LessonGame();game.Run();
sealed class LessonGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup world=null!;
 object Get(string name)=>typeof(Game1).GetField(name,F)!.GetValue(this)!;
 void Set(string name,object value)=>typeof(Game1).GetField(name,F)!.SetValue(this,value);
 object Call(string name,params object[] args)=>typeof(Game1).GetMethod(name,F)!.Invoke(this,args)!;
 Point CatalogPoint=>((Rectangle)typeof(Game1).GetField("LessonCatalogButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!).Center;
 void Screen(string name)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),name));
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Tutorial(int step)=>typeof(WorldSetup).GetProperty("TutorialStep")!.SetValue(world,step);
 void CheckAutomaticSummary(){
  var board=new BattleState(42){Finished=true};board.Armies.Add(new(){Owner=0});board.Capture(BattlePhase.Result);Set("_robotScenes",new[]{board});Set("_robotSummary",true);
  foreach(bool demo in new[]{true,false}){
   Screen("Battle");Set("_demoActive",demo);Set("_autoPlay",!demo);Set("_summaryReadAge",0d);Set("_summaryReadPapers",0);Set("_newspaperEdition",0);
   Call("UpdateAutomaticSummary",5d,demo);Check((int)Get("_summaryReadPapers")==0,"Automatic reader does not advance before six seconds");Call("UpdateAutomaticSummary",1d,demo);
   if(demo){Check((int)Get("_summaryReadPapers")==1 && Get("_screen").ToString()=="Battle","Demo reads first publisher for six seconds");Call("UpdateAutomaticSummary",6d,true);Check((int)Get("_summaryReadPapers")==2 && Get("_screen").ToString()=="Battle","Demo reads second publisher for six seconds");Call("UpdateAutomaticSummary",6d,true);}
   Check((int)Get("_summaryReadPapers")== (demo?3:1) && Get("_screen").ToString()!="Battle","Demo reads three papers, AUTO one, and both transition with a valid loaded world");
  }
  Set("_demoActive",false);Set("_autoPlay",false);Console.WriteLine("PASS: loaded DEMO three-paper and AUTO one-paper summary cadence and transition.");
 }
 void CheckPacing(){
  world.Lessons.Pending.AddRange(new[]{new LessonAward(0,BattleLesson.FlagCourtesy),new LessonAward(1,BattleLesson.FlagCourtesy),new LessonAward(2,BattleLesson.RetreatBackwards)});
  Call("BeginBattleResearch");Check(((bool[])Get("_lessonPickups")).SequenceEqual(new[]{true,false,true}),"One pickup per globally new type, including simultaneous country awards");
  Call("UpdateBattleResearch",6.99d,false);Check((int)Get("_lessonPage")==0,"First type retains reading time");Call("UpdateBattleResearch",.01d,false);Check((int)Get("_lessonPage")==1,"First pickup lasts seven seconds");
  Call("UpdateBattleResearch",.399d,false);Check((int)Get("_lessonPage")==1,"Repeated type remains until 0.4 seconds");Call("UpdateBattleResearch",.001d,false);Check((int)Get("_lessonPage")==2,"Repeated type advances at 0.4 seconds");
  Call("UpdateBattleResearch",.4d,false);Check((int)Get("_lessonPage")==2,"Second globally new type is also picked up");
  var saved=AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(world.Lessons));Check(saved.Owned[0]==BattleLesson.FlagCourtesy,"Global appearance history survives country-card saves");
  world.Lessons.Pending.Add(new(2,BattleLesson.FlagCourtesy));Call("BeginBattleResearch");Check(((bool[])Get("_lessonPickups")).All(p=>!p),"Later country gets no repeated pickup");Call("UpdateBattleResearch",.4d,false);Check(Get("_screen").ToString()!="BattleResearch","Later award completes at 0.4 seconds");
  Call("BeginBattleResearch");Check(Get("_screen").ToString()!="BattleResearch","Normal game omits empty research entirely");
  Tutorial(2);Call("BeginBattleResearch");Call("UpdateBattleResearch",1.99d,true);Check(Get("_screen").ToString()=="BattleResearch","Tutorial empty notice holds for two seconds even on input");Call("UpdateBattleResearch",.01d,false);Check(Get("_screen").ToString()!="BattleResearch","Tutorial empty notice finishes at exactly two seconds");Tutorial(0);
  world.Lessons.Reset();Console.WriteLine("PASS: global first-type pickups, seven-second reading, simultaneous duplicates, 0.4-second repeats, omitted normal empty results and two-second tutorial notices and persisted history.");
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);world.HumanPlayers.Add(0);
  Set("_conquestOverviewEnabled",false);Set("_cutInStarted",-10d);((CubeNet)Get("_net")).Home(world);((WorldPreview)Get("_world")).ShowSetup(world,true);
  world.PrepareTutorialStepTwo(123,456,789);world.SelectPlayer(0);world.Industry.Queue(1);world.Industry.Roll(world.PlacementSeed^world.FortuneSeed);int expected=world.Industry.Result,before=world.Robots.IndustryTurn;Call("BeginIndustry");Check(Get("_screen").ToString()!="Industry" && !world.Industry.Pending && world.Robots.IndustryTurn==before+expected,"Step two skips roulette while applying industry exactly once");Call("BeginIndustry");Check(world.Robots.IndustryTurn==before+expected,"Skipped industry is not applied twice");
  world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);world.HumanPlayers.Add(0);CheckAutomaticSummary();CheckPacing();
  var quiet=new BattleState(1);var busy=new BattleState(2);
  foreach(var board in new[]{quiet,busy}){board.Turn=1;board.Capture(BattlePhase.Choose);board.Capture(BattlePhase.Result);}
  busy.Attacks.Add(new(1,1,null,new(0,0),new(3,0),BattleWeapon.Rifle));
  Set("_robotScenes",new[]{quiet,busy});Set("_robotPlayback",new RobotBattlePlayback(new[]{quiet,busy}));Set("_robotRound",0);Set("_battleWave",0);
  Check(Math.Abs((double)typeof(Game1).GetProperty("RobotRoundEnd",F)!.GetValue(this)!-.4)<1e-8,"Displayed quiet battlefield takes 0.4 seconds even when another battlefield attacks");
  world.Lessons.Pending.AddRange(BattleLessons.All.Select(l=>new LessonAward(0,l)));
  var battle=new BattleState(42){Finished=true};battle.Armies.Add(new(){Owner=0});battle.Capture(BattlePhase.Result);Set("_robotScenes",new[]{battle});Set("_robotSummary",true);Screen("Battle");
  Set("_summaryReadAge",0d);Call("RobotBattleInput",5.99d,false,false);Check(Get("_screen").ToString()=="Battle","Newspaper remains before six idle seconds");
  Set("_pointer",new Point(100,950));Call("RobotBattleInput",0d,true,false);Check((double)Get("_summaryReadAge")==0,"Selecting a newspaper resets reading timer");
  Call("RobotBattleInput",5.99d,false,false);Check(Get("_screen").ToString()=="Battle","Newly selected newspaper retains six seconds");Call("RobotBattleInput",.01d,false,false);Check(Get("_screen").ToString()=="BattleResearch","Idle newspaper opens research before industry");Check(world.Lessons.Owned[0]==BattleLessons.All.Aggregate(BattleLesson.None,(a,b)=>a|b),"Research grants four cards");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame<4){Set("_lessonAge",3d);Set("_lastCutInPhase","戦闘研究フェーズ");Set("_cutInStarted",-10d);}
  if(frame==4){while(Get("_screen").ToString()=="BattleResearch"){Set("_lessonAge",3d);Call("UpdateBattleResearch",0d,true);}Check(Get("_screen").ToString()!="BattleResearch","Last card proceeds to industry/domestic");Screen("WorldStatus");Call("OpenStrategyInspector");Set("_domesticSlide",1f);Set("_lastCutInPhase","内政フェーズ");}
  if(frame==5){Check((bool)Call("DomesticViewClick",CatalogPoint),"World plan card button handles click");Check(Get("_screen").ToString()=="LessonCatalog","Button opens catalogue");}
  if(frame==6){Call("HandleInputCore",new Microsoft.Xna.Framework.Input.MouseState(),new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.Escape),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.FromSeconds(.02)),true);Check(Get("_screen").ToString()=="WorldStatus","Esc returns catalogue to world plan");Call("DomesticViewClick",CatalogPoint);world.SelectPlayer(1);world.Lessons.Owned[1]=BattleLesson.FireDiscipline;}
  if(frame==7){Tutorial(2);Screen("Battle");Set("_robotSummary",true);Call("RobotBattleInput",0d,false,true);Check(Get("_screen").ToString()=="BattleResearch" && ((LessonAward[])Get("_lessonAwards")).Length==0,"No duplicate awards next research");Set("_lessonAge",1d);Set("_lastCutInPhase","戦闘研究フェーズ");}
  if(frame==8){Call("UpdateBattleResearch",8d,false);Check(Get("_screen").ToString()!="BattleResearch","Research auto advances in demo/autoplay");Screen("WorldStatus");world.Relations.SetSuperior(1,0);Call("OpenStrategyInspector");Set("_domesticSlide",1f);Set("_lastCutInPhase","内政フェーズ");Call("FocusStatusGlobe");((WorldPreview)Get("_world")).GlobeZoom=2;}
  if(frame==9){world.Lessons.Pending.Add(new(2,BattleLesson.FlagCourtesy));Call("BeginBattleResearch");Set("_lessonAge",.2d);Check(((bool[])Get("_lessonPickups")).All(p=>!p),"Repeated card renders fully visible with no pickup animation");}
  if(frame==10){Call("UpdateBattleResearch",.2d,false);Check(Get("_screen").ToString()!="BattleResearch","Visible repeat completes at 0.4 seconds");Screen("WorldStatus");Call("ResetInspector");Set("_domesticSlide",0f);Set("_lastCutInPhase","内政フェーズ");Call("FocusStatusGlobe");}
  if(frame==11){Screen("Battle");Set("_robotSummary",true);Set("_summaryReadAge",3d);Set("_lastCutInPhase","戦闘結果");Set("_cutInStarted",-10d);}
  if(frame==12){Screen("LessonCatalog");Set("_lastCutInPhase","戦闘フェーズ");Set("_cutInStarted",-10d);world.Lessons.Owned[world.PlayerSlot]=BattleLessons.All.Aggregate(BattleLesson.None,(a,b)=>a|b);}
  if(frame==13){world.Lessons.Owned[0]&=~BattleLesson.CollisionTempo;world.Lessons.Pending.Add(new(0,BattleLesson.CollisionTempo));Call("BeginBattleResearch");Check(((LessonAward[])Get("_lessonAwards")).Any(a=>a.Lesson==BattleLesson.CollisionTempo),"New collision lesson has its research card");Set("_lessonAge",2d);}
  if(frame==14){world.Lessons.Owned[0]&=~BattleLesson.RelieveFront;world.Lessons.Pending.Add(new(0,BattleLesson.RelieveFront));Call("BeginBattleResearch");Check(((LessonAward[])Get("_lessonAwards")).Any(a=>a.Lesson==BattleLesson.RelieveFront),"Eighth lesson has its research card");Set("_lessonAge",2d);}
  if(frame==15){world.Lessons.Owned[0]&=~BattleLesson.WaitingDaggerSafety;world.Lessons.Pending.Add(new(0,BattleLesson.WaitingDaggerSafety));Call("BeginBattleResearch");Check(((LessonAward[])Get("_lessonAwards")).Any(a=>a.Lesson==BattleLesson.WaitingDaggerSafety),"Ninth lesson has a research card");Set("_lessonAge",2d);}
  if(frame==16){
   Screen("LessonCatalog");world.Lessons.Owned[world.PlayerSlot]=BattleLessons.All.Aggregate(BattleLesson.None,(a,b)=>a|b);Set("_lessonCatalogPage",0);
   Point Button(string name)=>((Rectangle)typeof(Game1).GetField(name,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!).Center;
   Set("_pointer",Button("LessonCatalogNext"));Call("LessonCatalogInput",true,false);Check((int)Get("_lessonCatalogPage")==1,"Catalogue next page");Call("LessonCatalogInput",true,false);Call("LessonCatalogInput",true,false);Check((int)Get("_lessonCatalogPage")==2,"Catalogue clamps at final page");
   Set("_pointer",Button("LessonCatalogPrevious"));Call("LessonCatalogInput",true,false);Check((int)Get("_lessonCatalogPage")==1,"Catalogue previous page");Set("_lessonCatalogPage",2);
  }
  base.Draw(time);
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"lesson-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame==11)Check(pixels[1003*p.BackBufferWidth+100]==new Color(248,194,58) && pixels[1003*p.BackBufferWidth+1600]==new Color(19,38,49),"Newspaper timer renders half-filled after three of six seconds");
  if(frame<4)Call("UpdateBattleResearch",0d,true);
  if(frame++==16){Console.WriteLine("PASS: newspaper/research/industry flow, four award reveals and newspaper timer, world-plan button, owned/unowned catalogue, empty research, subordinate plan, fast repeat and globe robots rendered.");Exit();}
 }
}

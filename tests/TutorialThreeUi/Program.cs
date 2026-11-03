using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
var routes=new Dictionary<RouteStep,int>{{new(1,new(0,1),new(1,0),new[]{new Point(1,1)}),11},{new(2,new(2,1),new(1,2),new[]{new Point(2,2)}),22}};
var routeCopy=AutoSaveStore.Restore<Dictionary<RouteStep,int>>(AutoSaveStore.Capture(routes));Check(routeCopy.Count==2 && routeCopy.All(p=>routeCopy.ContainsKey(p.Key)) && routeCopy.Keys.Select(k=>k.Target).Order().SequenceEqual(new[]{1,2}),"Rendered route cache record keys restore before hashing and remain searchable");
var world=new WorldSetup();world.PrepareTutorialStepThree(123,456,789);
Check(world.Width==3 && world.Height==1 && world.Depth==2 && world.Cells.Length==22 && world.PolarAxis==0,"3x1x2 slab with chopsticks/bowl poles");
Check(world.TutorialStep==3 && world.ActiveCount==7 && world.Looks.Take(7).Select(l=>l.BaseId).SequenceEqual(Enumerable.Range(0,7)),"Seven specific characters");
Check(!world.LandPending && world.Owners.All(o=>o>=0 && o<7) && world.TerritoryCounts.Sum()==22 && world.Robots.Nodes.Any(s=>s.Robots.Any(r=>r.Parts!=RobotParts.Complete)),"Normal random territory and mixed robot generation");
Check(world.Cells.Where(c=>c.Face==0).All(c=>WorldCoordinates.At(world,c).Y>0) && world.Cells.Where(c=>c.Face==1).All(c=>WorldCoordinates.At(world,c).Y<0),"X-axis latitude poles");
Check(WorldTerrain.ColorAt(.6f,Vector3.Right,0)==WorldTerrain.ColorAt(.6f,Vector3.Up),"Polar terrain colors follow X");
var net=new CubeNet();world.SelectPlayer(0);net.Home(world);net.SetCenter(world,4);Check(net.Faces[0].Up==Vector3.Right,"Net north follows bowl direction");
world.TutorialMapStage=2;world.TutorialMapRotations=7;world.TutorialMapNotices=7;var save=AutoSaveStore.Capture(world);var restored=AutoSaveStore.Restore<WorldSetup>(save);Check(restored.PolarAxis==0 && restored.TutorialMapStage==2 && restored.TutorialMapRotations==7,"Tutorial progress and poles survive saves");
foreach(var o in save.Objects)if(o.Fields!=null)foreach(var name in new[]{"PolarAxis","TutorialMapStage","TutorialMapRotations","TutorialMapNotices","TutorialMapEdgeChanged"})o.Fields.Remove(name);restored=AutoSaveStore.Restore<WorldSetup>(save);Check(restored.PolarAxis==1 && restored.TutorialMapStage==0,"Old saves default to Y poles and no map progress");
var second=new WorldSetup();second.PrepareTutorialStepThree(123,456,790);Check(!world.Owners.SequenceEqual(second.Owners),"Placement seed randomizes territories");
Console.WriteLine("PASS: slab dimensions, X poles/north/latitude/terrain, seven characters, normal random economy/robots, distinct placements and legacy/current saves.");
using var game=new MapLessonGame();game.Run();
sealed class MapLessonGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;CubeNet net=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Guide(int id){Call("UpdateTutorialStartGuide",100d);Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==id,"Expected map guide "+id);Set("_animationTime",(float)Get("_tutorialGuideStarted")+1);Call("UpdateTutorialChrome",.35f);}
 void Dismiss(){Set("_tutorialAttackGuide",false);Call("UpdateTutorialChrome",.35f);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);
  Check((bool)Get("_conquestOverviewEnabled"),"Overview remains enabled globally");
  foreach(string begin in new[]{"BeginTutorialStepOne","BeginTutorialStepTwo","BeginTutorialStepThree"}){
   Call(begin);var tutorial=(WorldSetup)Get("_setup");Check((bool)Prop("TutorialPrimaryActionsDisabled")== (tutorial.TutorialStep==3),"Primary actions are disabled only in step three");Check(!(bool)Get("_conquestOverview"),begin+" skips initial conquest overview");
   tutorial.Population.Advance(tutorial);Call("RestoreDomesticCamera");Check(!(bool)Get("_conquestOverview"),begin+" skips conquest overview on later turns");
  }
  typeof(WorldSetup).GetProperty("TutorialStep")!.SetValue(Get("_setup"),0);Set("_conquestOverviewTurn",-1);Call("BeginConquestOverview");Check((bool)Get("_conquestOverview"),"Normal game still starts overview after tutorials");Call("FinishConquestOverview");

  Set("_pointer",((Rectangle)typeof(Game1).GetMethod("TutorialPlanetCard",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!).Center);Call("TutorialMenuInput",true,false,false,new KeyboardState());
  Set("_pointer",new Point(1000,722));Call("TutorialMenuInput",true,false,false,new KeyboardState());w=(WorldSetup)Get("_setup");net=(CubeNet)Get("_net");
  Check(w.TutorialStep==3 && w.PlayerSlot==0 && w.HumanPlayers.SequenceEqual(new[]{0}) && (bool)Get("_statusGlobe"),"Menu starts step three with player one as No.1 on globe");
  Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Set("_tutorialAttackGuide",false);Set("_homePersuasionActive",false);Guide(8);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime time){
  if(frame==1){Dismiss();var button=(Rectangle)typeof(Game1).GetField("MapViewButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Check((bool)Call("RouteLayerClick",button.Center),"Actual unfold button handles click");Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==0,"Unfold animation must finish first");Call("UpdateMapTransition",10f);Guide(9);Check(!(bool)Get("_statusGlobe"),"Unfold yields net");}
  if(frame==2){Dismiss();var toy=(Rectangle)Prop("OrientationToyArea");var plus=(Rectangle)Call("AxisButton",toy,2,1);Call("OrientationControlClick",plus.Center);Call("UpdateOrientationRotation",.5f);Check(w.TutorialMapRotations==1 && Math.Abs(net.ScreenAngle-MathHelper.ToRadians(30))<.001,"Plus rotates net and records one distinct button");Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide") && w.TutorialMapStage==1,"One button cannot finish rotation lesson");var zero=(Rectangle)Prop("OrientationResetButton");Call("OrientationControlClick",zero.Center);Check(w.TutorialMapRotations==3 && net.ScreenAngle==0,"Zero resets rotation and records second button");var minus=(Rectangle)Call("AxisButton",toy,2,-1);Call("OrientationControlClick",minus.Center);Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==1,"Rotation must finish before next guide");Call("UpdateOrientationRotation",.5f);Guide(12);Check(w.TutorialMapRotations==7,"All three buttons required");}
  if(frame==3){
   string path=Path.Combine(AppContext.BaseDirectory,"tutorial-three-test-save.gz");Set("_autoSavePath",path);Set("_autoSaveEnabled",true);Set("_lastAutoSaveTurn",-1);Call("AutoSaveDomestic");Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Actual autosave resumes step three");Set("_autoSaveEnabled",false);File.Delete(path);w=(WorldSetup)Get("_setup");net=(CubeNet)Get("_net");Check(w.PolarAxis==0 && w.TutorialMapStage==2 && w.TutorialMapRotations==7,"Resume retains poles, stage and all three buttons");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Set("_conquestOverview",false);Guide(12);Dismiss();
   Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide"),"Dismissing slide guide does not finish the lesson");
   var toy=(Rectangle)Prop("OrientationToyArea");var start=new Point(toy.Center.X,toy.Center.Y-10);
   Set("_pointer",start);var held=new MouseState(start.X,start.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
   Call("DragOrientationRobot",held,true,true);Check(w.TutorialMapRotations==7,"Click alone does not finish slide lesson");
   Set("_previousMouse",held);Set("_pointer",new Point(start.X+40,start.Y+15));Call("DragOrientationRobot",held,true,false);
   Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide") && (w.TutorialMapRotations&8)==0,"Dragging keeps lesson incomplete until release");
   Call("DragOrientationRobot",new MouseState(),true,false);Guide(10);Dismiss();
   var slideCopy=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check((slideCopy.TutorialMapRotations&8)!=0,"Slide progress survives save");
   Call("ResetInspector");Set("_domesticSlide",0f);var panel=(Rectangle)Prop("NetPanel");net.FitAll(panel,.85f);net.ScreenAngle=0;bool clicked=false;
   foreach(var face in net.Faces.ToArray())foreach(int side in Enumerable.Range(0,4)){
    if(clicked)break;typeof(CubeNet).GetProperty("HoveredFace")!.SetValue(net,face.Face);typeof(CubeNet).GetProperty("HoveredSide")!.SetValue(net,side);if(!net.CanClickEdge)continue;
    var labels=net.SeamLabels(panel).Where(l=>l.Face==face.Face && l.Side==side).ToArray();if(labels.Length==0)continue;
    var point=net.RotateScreen(labels[0].Bounds.Center.ToVector2(),panel).ToPoint();net.HoverEdge(panel,point);if(!net.CanClickEdge)continue;
    Set("_previousMouse",new MouseState());Call("HandleInputCore",new MouseState(point.X,point.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);clicked=w.TutorialMapEdgeChanged;
   }
   Check(clicked && net.IsAnimating,"Actual alphabet click changes face attachment and records it");Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==2,"Attachment animation finishes before guide");net.UpdateAnimation(1f);Guide(11);
  }
  if(frame==4){Dismiss();var button=(Rectangle)typeof(Game1).GetField("MapViewButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Call("RouteLayerClick",button.Center);Call("UpdateMapTransition",10f);Guide(13);Check((bool)Get("_statusGlobe") && w.TutorialMapStage==4 && !w.TutorialCompletionShown,"Assembly starts globe rotation lesson before completion");}
  if(frame==5){
   Dismiss();var toy=(Rectangle)Prop("OrientationToyArea");
   for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1}){
    var button=(Rectangle)Call("AxisButton",toy,axis,sign);Check((bool)Call("OrientationControlClick",button.Center),"Globe axis button accepts click");
    Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==4,"Globe rotation animation finishes before advancing");Call("UpdateOrientationRotation",.5f);
   }
   Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==4 && !(bool)Get("_tutorialAttackGuide"),"Six axis buttons still require reset button");
   var zero=(Rectangle)Prop("OrientationResetButton");Call("OrientationControlClick",zero.Center);Guide(14);Check(w.TutorialMapStage==5 && !w.TutorialCompletionShown,"Seven distinct buttons unlock globe slide");
   var progress=new WorldSetup();progress.PrepareTutorialStepThree(123,456,789);progress.TutorialMapStage=w.TutorialMapStage;progress.TutorialMapRotations=w.TutorialMapRotations;progress.TutorialMapNotices=w.TutorialMapNotices;var copy=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(progress));Check(copy.TutorialMapRotations==w.TutorialMapRotations && copy.TutorialMapStage==5 && copy.TutorialMapNotices==w.TutorialMapNotices,"Globe rotation progress survives saves");
  }
  if(frame==6){
   Dismiss();var toy=(Rectangle)Prop("OrientationToyArea");var start=toy.Center;var held=new MouseState(start.X,start.Y,0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
   Set("_pointer",start);Call("DragOrientationRobot",held,true,true);Call("DragOrientationRobot",new MouseState(),true,false);Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==5,"Click without dragging does not finish globe slide");
   Set("_pointer",start);Call("DragOrientationRobot",held,true,true);Set("_previousMouse",held);Set("_pointer",new Point(start.X+40,start.Y+20));Call("DragOrientationRobot",held,true,false);
   Call("UpdateTutorialStartGuide",100d);Check(w.TutorialMapStage==5,"Slide waits for release");Call("DragOrientationRobot",new MouseState(),true,false);Guide(15);Check(w.TutorialMapStage==6 && w.TutorialCompletionShown,"Globe drag release unlocks final title-return guide");
  }
  if(frame==7){Dismiss();Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide"),"Completion guide is shown once");int turn=w.Population.Turn;var screen=Get("_screen");
   for(int i=0;i<6;i++){
    var r=(Rectangle)Call("PrimaryActionBounds",i);var canvas=(Rectangle)Call("CanvasBounds");var at=r.Center;
    Set("_previousMouse",new MouseState());Call("HandleInputCore",new MouseState(canvas.X+(int)Math.Round(at.X*canvas.Width/1920d),canvas.Y+(int)Math.Round(at.Y*canvas.Height/1080d),0,ButtonState.Pressed,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero),true);
    Check(w.Population.Turn==turn && Get("_screen").Equals(screen) && !(bool)Prop("InspectorVisible") && !(bool)Get("_directMap") && !(bool)Get("_salvagePlan"),"Disabled primary action "+i+" does not change game state");
    Check((bool)Call("MapOverlayClick",at,false) && (bool)Call("DomesticViewClick",at),"Both primary input routes consume disabled button "+i);
   }
   var review=(Rectangle)Prop("TutorialReviewButton");Set("_pointer",review.Center);Call("TutorialMessageInput",true,false);Check((bool)Get("_tutorialMessagesOpen") && (int)Get("_tutorialMessageSelection")==15,"Step three review highlights the latest completion guide");}
  if(frame==8){Call("TutorialMessageInput",false,true);Set("_pointer",((Rectangle)Call("PrimaryActionBounds",2)).Center);}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"tutorial-three-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==8){Console.WriteLine("PASS: menu/No.1, all eight mob guides, actual unfolding, plus/zero/minus, actual alphabet attachment, animation gating, assembly, all seven globe buttons, globe slide release, completion, save progress, all six disabled primary actions and review rendered.");Exit();}
 }
}

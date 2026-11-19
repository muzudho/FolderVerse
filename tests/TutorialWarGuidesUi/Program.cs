using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new WarGuideGame();game.Run();
sealed class WarGuideGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;int[] ownersBeforeBattle=null!;WorldSetup w=null!;string save="";
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Ready(){Set("_lastCutInPhase",Call("get_PlanningPhaseLabel"));Set("_cutInStarted",90d);Set("_homePersuasionActive",false);}
 void UpdateGuide(){Ready();Call("UpdateTutorialStartGuide",100d);}
 void Guide(int id){UpdateGuide();Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==id,"Expected guide "+id);Set("_animationTime",(float)Get("_tutorialGuideStarted")+1);}
 void Dismiss(){Set("_tutorialAttackGuide",false);Set("_tutorialGuideClosing",-1f);Set("_tutorialChromeSlide",0f);}
 protected override void LoadContent(){
  Content.RootDirectory=Environment.GetEnvironmentVariable("FOLDERVERSE_TEST_CONTENT") ?? Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();string medalPath=Path.Combine(AppContext.BaseDirectory,"war-guides-achievements.json");File.Delete(medalPath);Set("_tutorialAchievements",new TutorialAchievements(medalPath));Set("_autoSaveEnabled",false);
  var seedText=Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_WAR_SEED");
  if(int.TryParse(seedText,out int seed))Set("_random",new Random(seed));
  Call("BeginPetitstellaTutorial");w=(WorldSetup)Get("_setup");
  Console.WriteLine($"Tutorial seeds: random={seedText ?? "default"}, world={w.WorldSeed}, cast={w.CastSeed}, placement={w.PlacementSeed}");
  w.TutorialMapStage=6;Set("_tutorialIntroShown",true);
  save=Path.Combine(AppContext.BaseDirectory,"war-guides-save.gz");Set("_autoSavePath",save);Set("_autoSaveEnabled",true);
  var lines=(string[][])typeof(Game1).GetField("TutorialMessageLines",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(!lines[1].Any(l=>l.Contains("ターン終了")) && !lines[16].Any(l=>l.Contains("ターン終了")),"Initial reservation lessons do not mix in turn-end instructions");
  Check(lines[21].Any(line=>line.Contains("［この陣営のターン終了］")),"Turn-end guide names the button shown on the map");
  Call("ShowTutorialGuide",16);Set("_animationTime",1f+(float)Get("_tutorialGuideStarted"));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Check(w.Campaign.Phase==CampaignPhase.Domestic,"War planning starts during the domestic phase");
   Dismiss();Call("OpenWarPlanning");Call("UpdateDomesticSlide",1f);Guide(22);Check(w.TutorialWarTargetNoticeShown && !w.TutorialSortieNoticeShown && !w.TutorialTurnEndNoticeShown,"Opening war inspector explains rotation and target selection before sortie");
  }
  if(frame==2){
   Dismiss();Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide") && !w.TutorialTurnEndNoticeShown,"Closing without reservation gives no turn-end hint");
   Call("OpenWarPlanning");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Reopening without a target neither repeats target hint nor starts sortie hint");
   var net=(CubeNet)Get("_net");int target=net.MovementTargets.First(id=>w.Nodes.All[id].Owner>=0 && w.Nodes.All[id].Owner!=0);Call("SelectMoveNode",target);
   Guide(20);Check(w.TutorialSortieNoticeShown,"Selecting actual target unlocks troop strength and sortie instructions");
  }
  if(frame==3){
   Dismiss();
   var rows=(System.Collections.IList)Get("_invasionSources");int index=Enumerable.Range(0,rows.Count).First(i=>((Robot[])Call("InvasionRobots",rows[i]!)).Length>0);
   Call("OpenInvasionPicker",index);
   var available=(Robot[])Call("InvasionRobots",rows[index]!);
   var bounds=typeof(Game1).GetMethod("InvasionRobotCard",BindingFlags.Static|BindingFlags.NonPublic)!;
   var done=(Rectangle)typeof(Game1).GetField("InvasionPickerDone",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
   var canvas=(Rectangle)Call("CanvasBounds");
   void Pick(Point point){
    int x=canvas.X+(int)Math.Round(point.X*canvas.Width/1920d);
    int y=canvas.Y+(int)Math.Round(point.Y*canvas.Height/1080d);
    var time=new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero);
    foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
     Call("HandleInput",new MouseState(x,y,0,state,0,0,0,0),new KeyboardState(),time,true);
   }
   int count=Math.Min(available.Length,18);
   for(int slot=0;slot<count;slot++)
    Pick(((Rectangle)bounds.Invoke(null,new object[]{slot})!).Center);
   Pick(done.Center);
   Check((int)Get("_invasionPicker")==-1,"Confirming through the usual input closes the sortie picker");
   Console.WriteLine($"Sortie selection: {count} robots, confirmed with normal input");
   Check((bool)Call("get_TutorialHasAttackReservation"),"Actual sortie confirmation creates valid attack reservation");UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide") && !w.TutorialTurnEndNoticeShown,"Reservation while inspector open waits for close");
   Call("CloseNodeInspector");Call("UpdateDomesticSlide",.1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Closing animation must finish before hint");Call("UpdateDomesticSlide",1f);Guide(21);Check(w.TutorialTurnEndNoticeShown,"Closed reserved inspector explains turn end");
  }
  if(frame==4){
   Dismiss();Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Reservation and guide progress resume");w=(WorldSetup)Get("_setup");Check(w.TutorialWarTargetNoticeShown && w.TutorialSortieNoticeShown && w.TutorialTurnEndNoticeShown && (int)Get("_tutorialLastMessage")==21,"Both notices and latest review selection persist");UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Resume does not repeat guide");
   Call("OpenWarPlanning");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Sortie guide stays one-time after reload");Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Turn-end guide stays one-time after reload");
  }
  if(frame==5){
   ownersBeforeBattle=w.Nodes.All.Select(node=>node.Owner).ToArray();
   var button=(Rectangle)Call("get_PopulationTurnButton");
   var canvas=(Rectangle)Call("CanvasBounds");
   int x=canvas.X+(int)Math.Round(button.Center.X*canvas.Width/1920d);
   int y=canvas.Y+(int)Math.Round(button.Center.Y*canvas.Height/1080d);
   var inputTime=new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero);
   foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
    Call("HandleInput",new MouseState(x,y,0,state,0,0,0,0),new KeyboardState(),inputTime,true);
   Check(Get("_screen").ToString()=="Transport" && w.Campaign.Phase==CampaignPhase.Delivery,"Clicking the named turn-end button starts delivery");
  }
  if(frame==6){
   string lastScreen=Get("_screen").ToString()!;
   for(int tick=1;tick<=240 && Get("_screen").ToString()!="Battle";tick++){
    var time=new GameTime(TimeSpan.FromSeconds(100+tick*.25),TimeSpan.FromSeconds(.25));
    Call("HandleInput",new MouseState(),new KeyboardState(),time,true);
    string screen=Get("_screen").ToString()!;
    if(screen!=lastScreen || tick%20==0){
     Console.WriteLine($"After turn end tick={tick} screen={screen} phase={w.Campaign.Phase}");
     base.Draw(time);
    }
    lastScreen=screen;
   }
   Check(Get("_screen").ToString()=="Battle","The reserved sortie reaches the battle screen after delivery and deployment");
   Check(w.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==0),"The player reservation executed as a march");
  }
  if(frame==7){
   string lastScreen=Get("_screen").ToString()!;
   bool sawNews=false;
   for(int tick=1;tick<=480;tick++){
    var time=new GameTime(TimeSpan.FromSeconds(160+tick*.25),TimeSpan.FromSeconds(.25));
    if(lastScreen=="Disposition" && Get("_dispositionScene")==null && w.Relations.Pending.Count>0){
     base.Draw(time);
     var button=typeof(Game1).GetMethod("DispositionButton",BindingFlags.Static|BindingFlags.NonPublic)!;
     var point=((Rectangle)button.Invoke(null,new object[]{(int)DefeatedChoice.Recruit})!).Center;
     var canvas=(Rectangle)Call("CanvasBounds");
     int x=canvas.X+(int)Math.Round(point.X*canvas.Width/1920d);
     int y=canvas.Y+(int)Math.Round(point.Y*canvas.Height/1080d);
     foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})
      Call("HandleInput",new MouseState(x,y,0,state,0,0,0,0),new KeyboardState(),time,true);
     Check(Get("_dispositionScene")!=null,"Clicking Recruit starts the disposition scene");
     Console.WriteLine($"Disposition Recruit accepted at {point}");
    }else{
     Call("HandleInput",new MouseState(),tick%2==0 && lastScreen!="WorldStatus"?new KeyboardState(Keys.Enter):new KeyboardState(),time,true);
    }
    Call("UpdateDispositionScene",time.ElapsedGameTime.TotalSeconds);
    string screen=Get("_screen").ToString()!;
    sawNews|=screen=="Battle" && (bool)Get("_robotSummary");
    if(screen!=lastScreen || tick%20==0){
     Console.WriteLine($"After battle tick={tick} screen={screen} phase={w.Campaign.Phase} attack={w.TutorialAttackCompleted}");
     base.Draw(time);
    }
    lastScreen=screen;
    if(screen=="WorldStatus" && (bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==4)break;
   }
   var captured=w.Nodes.All.Where(node=>node.Owner==0 && ownersBeforeBattle[node.Id]!=0).Select(node=>node.Id);
   Console.WriteLine($"Post-battle ownership: gained=[{string.Join(",",captured)}] totals={w.Nodes.All.Count(node=>node.Owner==0)}/{w.Nodes.All.Length} ending={w.WorldConquestEndingShown}");
   var ranking=w.Relations.ProvisionalRanking(w);
   var playerRank=ranking.Single(entry=>entry.Ruler==0);
   Console.WriteLine($"Conquest progress: cells={playerRank.Cells}/{w.Cells.Length}, eligible={w.Relations.CanWinConquest(0)}, ending={w.WorldConquestEndingShown}");
   Check(sawNews,"Battle playback reaches the result newspaper");
   Check(lastScreen=="WorldStatus" && w.TutorialAttackCompleted && w.TutorialPocketIntroShown && (bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==4,
     "Battle, result and return to the map unlock the pocket and movement guide");
  }
  if(frame==8){
   double now=300;
   int wheelPosition=0;
   void Send(Point point,ButtonState button=ButtonState.Released,KeyboardState keys=default){
    var canvas=(Rectangle)Call("CanvasBounds");
    int x=canvas.X+(int)Math.Round(point.X*canvas.Width/1920d);
    int y=canvas.Y+(int)Math.Round(point.Y*canvas.Height/1080d);
    var time=new GameTime(TimeSpan.FromSeconds(now+=.25),TimeSpan.FromSeconds(.25));
    Call("HandleInput",new MouseState(x,y,wheelPosition,button,0,0,0,0),keys,time,true);
   }
   void Click(Point point){
    Send(point);Send(point,ButtonState.Pressed);Send(point);
   }
   Send(default,keys:new KeyboardState(Keys.Enter));
   for(int step=0;step<4;step++)Send(default);
   Click(((Rectangle)Call("PrimaryActionBounds",0)).Center);
   Check((bool)Get("_reignView"),"The country button opens its inspector");
   for(int step=0;step<4;step++)Send(default);
   var pocket=(Rectangle)typeof(Game1).GetField("PocketButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
   Click(pocket.Center+new Point((int)Call("get_NodeInspectorOffset"),0));
   Check(Get("_screen").ToString()=="Pocket","The pocket button opens the actual exchange screen");
   var stock=w.Robots.Nodes[w.Nodes.Current(0).Id];
   var slots=(long?[][])Get("_pocketSlots");
   var from=((IEnumerable<int>)Call("PocketVisible",1)).First(i=>slots[1][i] is long id &&
     stock.Robots.Any(robot=>robot.Id==id && (bool)Call("PocketRobotSelectable",robot,true)));
   var to=((IEnumerable<int>)Call("PocketVisible",0)).First(i=>slots[0][i]==null);
   long robotId=slots[1][from]!.Value;
   var source=((Rectangle)Call("PocketViewCard",1,from)).Center;
   var destination=((Rectangle)Call("PocketViewCard",0,to)).Center;
   Send(source);Send(source,ButtonState.Pressed);Send(destination,ButtonState.Pressed);Send(destination);
   Check(w.TutorialPocketed && w.Robots.Retinues[0].Robots.Any(robot=>robot.Id==robotId),
    "The usual drag puts a robot in the pocket");
   Send(default,keys:new KeyboardState(Keys.Escape));Send(default);
   Check(Get("_screen").ToString()=="WorldStatus" && w.TutorialPocketNoticeShown && (int)Get("_tutorialGuideMessage")==5,
    "Returning from the pocket shows the completion notice");
   Console.WriteLine($"Pocket drag source={source} destination={destination} robot={robotId}");
   Send(default,keys:new KeyboardState(Keys.Enter));
   for(int step=0;step<4;step++)Send(default);
   if(!(bool)Get("_reignView")){
    Click(((Rectangle)Call("PrimaryActionBounds",0)).Center);
    for(int step=0;step<4;step++)Send(default);
   }
   Rectangle Button(string name)=>(Rectangle)typeof(Game1).GetField(name,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
   Click(Button("MovementButton").Center+new Point((int)Call("get_NodeInspectorOffset"),0));
   Check((bool)Get("_movementOpen"),"The movement reservation button opens the normal map selector");
   for(int step=0;step<4;step++)Send(default);
   var net=(CubeNet)Get("_net");
   var candidate=-1;
   var mapPoint=Point.Zero;
   void FindDestination(){
    base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
   bool war=(bool)Get("_warPlanning");
   IEnumerable<int> targets=war?net.MovementTargets.OrderDescending():net.MovementTargets.Order();
   if(war && Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_PRIORITIZE_CELL")=="1"){
    bool IsInHumanFaction(int owner){
     for(int ruler=owner;ruler>=0;ruler=w.Relations.Superiors[ruler])if(ruler==0)return true;
     return false;
    }
    var remainingByNode=w.Cells.Select(cell=>w.Nodes.InCell(cell.Id).ToArray())
     .SelectMany(nodes=>nodes.Select(node=>(node.Id,Remaining:nodes.Count(other=>!IsInHumanFaction(other.Owner)))))
     .ToDictionary(item=>item.Id,item=>item.Remaining);
    targets=targets.OrderBy(id=>remainingByNode[id]);
    Console.WriteLine($"Attack candidates by remaining sites: {string.Join(",",targets.Where(net.AttackHighlightTargets.Contains).Select(id=>$"{id}/{remainingByNode[id]}"))}");
   }
   if(war && Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_WEAK_TARGET")=="1")
    targets=targets.OrderBy(id=>w.Robots.Nodes[id].Robots.Count(r=>r.Owner!=0 && r.Parts==RobotParts.Complete));
   foreach(int id in targets){
    if((bool)Get("_warPlanning") && (!net.AttackHighlightTargets.Contains(id) || w.Nodes.All[id].Owner==0))continue;
    var args=new object[]{id,Vector2.Zero};
    if(!(bool)Call("OperationPosition",args)){Console.WriteLine($"Rejected id={id} point={args[1]} globe={Get("_statusGlobe")} bounds={Call("get_MapViewBounds")}");continue;}
    var point=((Vector2)args[1]).ToPoint();
    if(Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_PROJECTION_LOG")=="1")Console.WriteLine($"Candidate projection: id={id} point={point} bounds={Call("get_MapViewBounds")} globe={Get("_statusGlobe")}");
    if(!((Rectangle)Call("get_MapViewBounds")).Contains(point))continue;
    int hit=(bool)Get("_statusGlobe")?(int)Call("HitGlobeNode",point,net.MovementTargets):
     net.HitNode(w,(Rectangle)Call("get_NetPanel"),point,net.MovementTargets);
    if(hit!=id)continue;
    if((bool)Call("get_OrientationControlsVisible")){
     var toy=(Rectangle)Call("get_OrientationToyArea");
     bool covered=toy.Contains(point) || ((Rectangle)Call("get_OrientationResetButton")).Contains(point);
     for(int axis=(bool)Call("get_OrientationGlobe")?0:2;axis<3;axis++)foreach(int sign in new[]{-1,1})covered|=((Rectangle)Call("AxisButton",toy,axis,sign)).Contains(point);
     if(covered){Console.WriteLine($"Skip covered candidate={id} point={point}");continue;}
    }
    candidate=id;mapPoint=point;break;
   }
   }
   FindDestination();
   if(candidate<0 && (bool)Get("_statusGlobe")){
    Click(Button("MapViewButton").Center);
    for(int step=0;step<20;step++)Send(default);
    FindDestination();
   }
   Check(candidate>=0,"A valid movement destination is visible");
   Click(mapPoint);
   Check((int)Get("_selectedMoveNode")==candidate,"Clicking the map selects the intended destination");
   Click(Button("ConfirmMoveButton").Center+new Point((int)Call("get_NodeInspectorOffset"),0));
   Check(w.Campaign.PlannedOrders.Count(order=>order.Peaceful && order.Ruler==0 && order.TargetNode==candidate)==1,
    "Confirming movement creates one reservation at the selected destination");
   Console.WriteLine($"Movement reservation target={candidate} point={mapPoint}");
   if((bool)Call("get_InspectorVisible")){
    Click(((Rectangle)Call("get_InspectorClose")).Center);
    for(int step=0;step<4;step++)Send(default);
   }
   for(int tick=0;tick<240 && (bool)Get("_homePersuasionActive");tick++){
    Send(default);
    if(tick%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
   }
   for(int tick=0;tick<4;tick++)Send(default);
   Console.WriteLine($"Before movement turn end: screen={Get("_screen")} guide={Get("_tutorialAttackGuide")} persuasion={Get("_homePersuasionActive")} inspector={Call("get_InspectorVisible")}");
   Click(((Rectangle)Call("get_PopulationTurnButton")).Center);
   string movementScreen=Get("_screen").ToString()!;
   bool subordinate=Get("_subordinateNodes")!=null;
   Console.WriteLine($"After movement turn end: screen={movementScreen} phase={w.Campaign.Phase} player={w.PlayerSlot} subordinate={subordinate}");
   Check(movementScreen=="Transport" || subordinate,"Ending the turn begins delivery or subordinate domestic operations");
   for(int step=0;step<480;step++){
    Send(default,keys:step%2==1 && movementScreen!="WorldStatus"?new KeyboardState(Keys.Enter):default);
    var screen=Get("_screen").ToString()!;
    if(screen!=movementScreen || step%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
    movementScreen=screen;
    if(screen=="WorldStatus" && w.TutorialMoveNoticeShown)break;
   }
   Check(w.TutorialMoved && w.Nodes.Current(0).Id==candidate &&
    w.Robots.Retinues[0].Robots.Any(robot=>robot.Id==robotId),
    "The reserved move reaches its destination with the robot in the pocket");
   Console.WriteLine($"Movement end screen={movementScreen} phase={w.Campaign.Phase} lastCutIn={Get("_lastCutInPhase")} cutInStarted={Get("_cutInStarted")} now={now} guide={Get("_tutorialAttackGuide")} guideId={Get("_tutorialGuideMessage")} messages={Get("_tutorialMessagesOpen")} auto={Get("_autoPlay")} demo={Get("_demoActive")} overview={Get("_conquestOverview")} moved={w.TutorialMoved} notice={w.TutorialMoveNoticeShown}");
   Check(movementScreen=="WorldStatus" && w.TutorialMoveNoticeShown,
    "The map shows the movement completion notice");
   Check(((TutorialAchievements)Get("_tutorialAchievements")).Has(TutorialAchievements.PetitstellaLessons),
    "The completed lessons award the Petitstella medal");
   Console.WriteLine($"Movement arrived target={candidate} robot={robotId} lessons medal earned");
   foreach(int guide in new[]{17,18,19}){
    Send(default,keys:new KeyboardState(Keys.Enter));
    for(int tick=0;tick<5;tick++)Send(default);
    Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==guide,
     $"Movement completion leads to guide {guide}");
   }
   Send(default,keys:new KeyboardState(Keys.Enter));
   for(int tick=0;tick<5;tick++)Send(default);
   base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
   Console.WriteLine($"Before ending screen={Get("_screen")} guide={Get("_tutorialAttackGuide")} chrome={Get("_tutorialChromeSlide")} retiring={Call("get_StatusMenuRetiring")} rate={Get("_rateTarget")} persuasion={Get("_homePersuasionActive")} overview={Get("_conquestOverview")}");
   for(int tick=0;tick<240 && (bool)Get("_homePersuasionActive");tick++){
    Send(default);
    if(tick%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
   }
   Check(!(bool)Get("_homePersuasionActive"),"The home persuasion animation finishes");
   for(int tick=0;tick<4;tick++)Send(default);
   int extraTurns=Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_EXTRA_TURN")=="1"?1:0;
   if(extraTurns==1 && Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_REPEAT_TURN")=="1")extraTurns++;
   if(int.TryParse(Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_EXTRA_TURNS"),out int requestedTurns))extraTurns=Math.Clamp(requestedTurns,0,24);
   for(int extraTurn=0;extraTurn<extraTurns;extraTurn++){
    // The domestic phase can return to the map before its cut-in and persuasion start.
    double cutInSeconds=Convert.ToDouble(typeof(Game1).GetField("PhaseCutInSeconds",BindingFlags.Static|BindingFlags.NonPublic)!.GetRawConstantValue());
    base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
    for(int tick=0;tick<240 && now-(double)Get("_cutInStarted")<cutInSeconds;tick++)Send(default);
    Send(default);
    for(int tick=0;tick<240 && ((bool)Get("_homePersuasionActive") || (bool)Get("_nodeInspectorClosing"));tick++){
     Send(default);
     if(tick%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
    }
    Check(!(bool)Get("_homePersuasionActive") && !(bool)Get("_nodeInspectorClosing"),"The next turn settles before opening war planning");
    March[] secondOrders=Array.Empty<March>();
    if(Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_EXTRA_WAR")=="1"){
     Click(((Rectangle)Call("PrimaryActionBounds",2)).Center);
     for(int tick=0;tick<8;tick++)Send(default);
     candidate=-1;
     FindDestination();
     if(candidate<0 && (bool)Get("_statusGlobe")){
      Click(Button("MapViewButton").Center);
      for(int tick=0;tick<20;tick++)Send(default);
      FindDestination();
     }
     for(int zoom=0;candidate<0 && zoom<8 && !(bool)Get("_statusGlobe");zoom++){
      wheelPosition-=120;
      Send(((Rectangle)Call("get_MapViewBounds")).Center);
      FindDestination();
      Console.WriteLine($"Camera zoom-out step={zoom+1} candidate={candidate}");
     }
     Check(candidate>=0,"A second war target is visible");
     Console.WriteLine($"Second sortie target before combat: target={candidate} owner={w.Nodes.All[candidate].Owner}");
     void TraceSelection(string label){
      var toy=(Rectangle)Call("get_OrientationToyArea");
      Console.WriteLine($"Orientation {label}: visible={Call("get_OrientationControlsVisible")} toy={toy} toyHit={toy.Contains(mapPoint)} reset={Call("get_OrientationResetButton")} rotating={Call("get_OrientationRotating")} transitioning={Call("get_MapTransitionActive")}");
      Console.WriteLine($"Primary gate {label}: blocked={Call("TutorialPrimaryActionClick",mapPoint)} handle={Call("get_InspectorHandle")} handleHit={((Rectangle)Call("get_InspectorHandle")).Contains(mapPoint)}");
      for(int action=0;action<8;action++){var rect=(Rectangle)Call("PrimaryActionBounds",action);if(rect.Contains(mapPoint))Console.WriteLine($"Primary overlap action={action} rect={rect} tutorial={Call("TutorialPrimaryActionAllowed",action)} phase={Call("PhasePrimaryActionAllowed",action)}");}
      Console.WriteLine($"Dispatch {label}: pointer={Get("_pointer")} outcome={Get("_inputOutcome")} transport={Get("_transportOverview")} nodeTransport={Get("_nodeTransportOpen")} mapList={Get("_mapLayerListOpen")} trafficList={Get("_trafficLayerListOpen")} previousMouse={Get("_previousMouse")}");
      int hit=(bool)Get("_statusGlobe")?(int)Call("HitGlobeNode",mapPoint,net.MovementTargets):net.HitNode(w,(Rectangle)Call("get_NetPanel"),mapPoint,net.MovementTargets);
      Console.WriteLine($"Input geometry {label}: hit={hit} canvas={Call("CanvasBounds")} screen={Get("_screen")} withdrawal={Get("_withdrawalEncounter")}");
      foreach(var encounter in w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished)){
       var badge=(Rectangle)Call("ContinuationBadge",encounter);
       var armies=(Array)Call("WithdrawalArmies",encounter);
       if(badge.Contains(mapPoint))Console.WriteLine($"Continuation overlap: id={encounter.Id} target={encounter.Target} armies={armies.Length} bounds={badge}");
      }
      Console.WriteLine($"Selection {label}: target={candidate} point={mapPoint} actual={Get("_selectedMoveNode")} war={Get("_warPlanning")} movement={Get("_movementOpen")} globe={Get("_statusGlobe")} guide={Get("_tutorialAttackGuide")} chrome={Get("_tutorialChromeSlide")} domestic={Get("_domesticSlide")} closing={Get("_nodeInspectorClosing")} persuasion={Get("_homePersuasionActive")} picker={Get("_invasionPicker")} bounds={Call("get_MapViewBounds")} animating={net.IsAnimating}");
     }
     TraceSelection("before");
     Click(mapPoint);
     TraceSelection("after");
     Check((int)Get("_selectedMoveNode")==candidate,"The map selects the second war target");
     for(int tick=0;tick<8;tick++)Send(default);
     base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
     var sources=(System.Collections.IList)Get("_invasionSources");
     var rows=(IEnumerable<(int Index,int Row,Rectangle Bounds)>)Call("InvasionVisibleRows");
     Console.WriteLine($"Sortie picker inspection: target={candidate}, sources={sources.Count}, rows={rows.Count()}, inspectorSlide={Get("_domesticSlide")}");
     var card=typeof(Game1).GetMethod("InvasionCardAt",BindingFlags.Static|BindingFlags.NonPublic)!;
     int selectedCount=0;
     foreach(var row in rows.ToArray()){
      var available=(Robot[])Call("InvasionRobots",sources[row.Index]!);
      var preview=(Robot[])Call("InvasionPreviewRobots",sources[row.Index]!);
      Console.WriteLine($"Sortie row: index={row.Index}, available={available.Length}, previews={preview.Length}, bounds={row.Bounds}");
      foreach(int slot in Enumerable.Range(0,preview.Length).Where(slot=>available.Any(item=>item.Id==preview[slot].Id))){
       var bounds=(Rectangle)card.Invoke(null,new object[]{row.Bounds,slot})!;
       var point=bounds.Center+new Point((int)Call("get_NodeInspectorOffset"),0);
       string State()=>$"screen={Get("_screen")}, withdrawal={Call("get_SelectedWithdrawal")!=null}, war={Get("_warPlanning")}, movement={Get("_movementOpen")}, selected={Get("_selectedMoveNode")}, guide={Get("_tutorialAttackGuide")}, closing={Get("_nodeInspectorClosing")}, persuasion={Get("_homePersuasionActive")}, map={((Rectangle)Call("get_MapViewBounds")).Contains(point)}";
       var before=State();
       Click(point);
       Console.WriteLine($"Sortie click state: before [{before}] after [{State()}]");
       Console.WriteLine($"Sortie card: robot={preview[slot].Id}, click={point}, orders={w.Campaign.PlannedOrders.Count}");
       selectedCount++;
      }
     }
     secondOrders=w.Campaign.PlannedOrders.Where(order=>!order.Peaceful && order.TargetNode==candidate).ToArray();
     Check(selectedCount>0 && secondOrders.Sum(order=>order.RobotIds?.Length ?? 0)==selectedCount,"The second attack reserves all visible selected cards");
     Console.WriteLine($"Second sortie sources={secondOrders.Length}, selected robots={selectedCount}");
     Console.WriteLine($"Second sortie local garrison: {string.Join(",",w.Robots.Nodes[candidate].Robots.Select(robot=>$"{robot.Id}/{robot.Owner}"))}");
     Console.WriteLine($"Second sortie reserved: target={candidate}, point={mapPoint}");
     Click(((Rectangle)Call("get_InspectorClose")).Center);
     for(int tick=0;tick<8;tick++)Send(default);
    }
    string CellOwnership()=>string.Join(" ",w.Cells.Select(cell=>$"{cell.Id}:[{string.Join(",",w.Nodes.InCell(cell.Id).Select(node=>$"{node.Id}/{node.Owner}"))}]"));
    Console.WriteLine($"Cell ownership before: {CellOwnership()} superiors={string.Join(",",w.Relations.Superiors.Take(w.ActiveCount))}");
    int turn=w.Population.Turn;
    Click(((Rectangle)Call("get_PopulationTurnButton")).Center);
    string screen=Get("_screen").ToString()!;
    for(int tick=0;tick<480;tick++){
     if(screen=="Disposition" && Get("_dispositionScene")==null && Get("_mercyResult")!=null){
      var confirm=(Rectangle)typeof(Game1).GetField("MercyConfirm",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
      Click(confirm.Center);
      screen=Get("_screen").ToString()!;
      if(screen=="WorldStatus")break;
     }
     if(screen=="Disposition" && Get("_dispositionScene")==null && w.Relations.Pending.Count>0){
      var button=typeof(Game1).GetMethod("DispositionButton",BindingFlags.Static|BindingFlags.NonPublic)!;
      var choice=Environment.GetEnvironmentVariable("FOLDERVERSE_TUTORIAL_RECRUIT")=="1" && w.Relations.Accepts(w,w.Relations.Pending[0].Ruler)?DefeatedChoice.Recruit:DefeatedChoice.RemoveBattery;
      var bounds=(Rectangle)button.Invoke(null,new object[]{(int)choice})!;
      int pending=w.Relations.Pending.Count;
      Console.WriteLine($"Disposition candidate ruler={w.Relations.Pending[0].Ruler} victor={w.Relations.Pending[0].Victor} accepts={w.Relations.Accepts(w,w.Relations.Pending[0].Ruler)} choice={choice}");
      base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
      if((bool)Get("_mercyInput") || Get("_mercyResult")!=null){
       var confirm=(Rectangle)typeof(Game1).GetField("MercyConfirm",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
       Console.WriteLine($"Disposition mercy modal: input={Get("_mercyInput")} result={Get("_mercyResult")!=null} pending={pending}");
       Click(confirm.Center);
       continue;
      }
      Click(bounds.Center);
      Check(w.Relations.Pending.Count<pending,"The disposition button accepts the selected outcome");
      Console.WriteLine($"Additional turn disposition: choice={choice} click={bounds.Center} pending={pending}->{w.Relations.Pending.Count}");
     }
     Send(default,keys:tick%2==1 && screen is "Transport" or "Battle" or "BattleResearch"?new KeyboardState(Keys.Enter):default);
     Call("UpdateDispositionScene",.25d);
     screen=Get("_screen").ToString()!;
     if(tick%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
     if(screen=="Ending" || screen=="WorldStatus" && w.Population.Turn>turn && w.PlayerSlot==0)break;
    }
    foreach(var order in secondOrders){
     var marches=w.Campaign.RobotBattles.LastMarches.Count(march=>march.Owner==order.Ruler && march.Source==order.SourceNode && march.Target==order.TargetNode);
     var participants=w.Campaign.RobotBattles.Scenes.Where(battle=>battle.TargetNode==order.TargetNode)
      .SelectMany(battle=>battle.Frames).SelectMany(battleFrame=>battleFrame.Units)
      .Where(unit=>unit.Owner==order.Ruler && unit.Robot!=null).Select(unit=>unit.Robot.Id).Distinct().ToArray();
     var roles=w.Campaign.RobotBattles.Scenes.Where(battle=>battle.TargetNode==order.TargetNode)
      .SelectMany(battle=>battle.Frames).SelectMany(battleFrame=>battleFrame.Units)
      .Where(unit=>unit.Robot!=null).Select(unit=>$"{unit.Robot.Id}/{unit.Owner}/{unit.Role}").Distinct();
     Console.WriteLine($"Second sortie roles: {string.Join(",",roles)}");
     foreach(long id in order.RobotIds ?? Array.Empty<long>()){
      var scenes=w.Campaign.RobotBattles.Scenes.Where(b=>b.Frames.Any(f=>f.Units.Any(u=>u.Robot?.Id==id))).Select(b=>b.TargetNode);
      var stores=w.Nodes.All.Where(n=>w.Robots.Nodes[n.Id].Robots.Any(r=>r.Id==id)).Select(n=>n.Id);
      Console.WriteLine($"Reserved robot trace: id={id} scenes={string.Join(",",scenes)} stores={string.Join(",",stores)}");
     }
     var cancellations=w.Campaign.LastCancelledWars.Where(item=>item.Ruler==order.Ruler && item.Target==order.TargetNode);
     Console.WriteLine($"Second sortie result: ruler={order.Ruler} source={order.SourceNode} target={order.TargetNode} reserved={string.Join(",",order.RobotIds ?? Array.Empty<long>())} marches={marches} participants={string.Join(",",participants)} cancelled={string.Join(",",cancellations.Select(item=>item.Reason))} owner={w.Nodes.All[order.TargetNode].Owner}");
    }
    Console.WriteLine($"Cell ownership after: {CellOwnership()} superiors={string.Join(",",w.Relations.Superiors.Take(w.ActiveCount))}");
    Console.WriteLine($"Additional turn: {turn}->{w.Population.Turn}, screen={screen}, phase={w.Campaign.Phase}, cells={w.Relations.ProvisionalRanking(w).Single(entry=>entry.Ruler==0).Cells}/{w.Cells.Length}");
    if(screen=="Ending"){Check(w.Relations.ProvisionalRanking(w).Any(r=>r.Ruler==0 && r.Cells==w.Cells.Length),"Automatic ending follows actual player conquest");break;}
     Check(w.Population.Turn>turn && screen=="WorldStatus","The next turn returns to the map after the completed lessons");
    for(int tick=0;tick<12;tick++)Send(default);
    bool waitedForPersuasion=(bool)Get("_homePersuasionActive");
    for(int tick=0;tick<240 && (bool)Get("_homePersuasionActive");tick++){
     Send(default);
     if(tick%20==0)base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
    }
    for(int tick=0;tick<4;tick++)Send(default);
    Console.WriteLine($"After additional turn: waitedForPersuasion={waitedForPersuasion}, persuasion={Get("_homePersuasionActive")}, guide={Get("_tutorialAttackGuide")}, chrome={Get("_tutorialChromeSlide")}, retiring={Call("get_StatusMenuRetiring")}");
   }
   bool conquestEnding=Get("_screen").ToString()=="Ending";
    if(!conquestEnding){
    Click(Button("StatusMenuButton").Center);
   Check((bool)Get("_statusMenuOpen"),"System menu opens after the final advice");
   Click(Button("EndingMenuItem").Center);
    }
    Console.WriteLine($"Ending verification: automatic={conquestEnding} winner={Get("_endingWinner")} cells={Get("_endingCells")} medal={((TutorialAchievements)Get("_tutorialAchievements")).Has(TutorialAchievements.PetitstellaConquest)}");
   Check(Get("_screen").ToString()=="Ending","The system menu opens the ending");
   base.Draw(new GameTime(TimeSpan.FromSeconds(now),TimeSpan.Zero));
   var endingSize=GraphicsDevice.PresentationParameters;var endingPixels=new Color[endingSize.BackBufferWidth*endingSize.BackBufferHeight];GraphicsDevice.GetBackBufferData(endingPixels);
    using(var endingImage=new Texture2D(GraphicsDevice,endingSize.BackBufferWidth,endingSize.BackBufferHeight))using(var endingFile=File.Create(Path.Combine(AppContext.BaseDirectory,"war-guide-ending.png"))){endingImage.SetData(endingPixels);endingImage.SaveAsPng(endingFile,endingImage.Width,endingImage.Height);}
    Click(Button("EndingReturn").Center);
   Check(Get("_screen").ToString()=="Title","The ending button returns to the title");
   Click(Button("StartButton").Center);
   Click(new Point(960,417));
   Check((bool)Get("_tutorialMenuOpen"),"How to Play opens again from the title");
   var medalPath=Path.Combine(AppContext.BaseDirectory,"war-guides-achievements.json");
   var medals=new TutorialAchievements(medalPath);
   Check(medals.Has(TutorialAchievements.PetitstellaLessons) && medals.Has(TutorialAchievements.PetitstellaConquest)==conquestEnding,
    "The earned lessons medal persists without awarding a conquest medal for a manual ending");
   Console.WriteLine("Post-move advice, manual ending, title return and saved lessons medal confirmed");
  }
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"war-guide-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==8){File.Delete(save);Set("_autoSaveEnabled",false);Console.WriteLine("PASS: rotation/target then sortie then turn-end instructions, actual sortie confirmation, close-without-order exclusion, closing animation, one-time guides and save/resume.");Exit();}
 }
}

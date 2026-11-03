using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new JourneyGame();game.Run();
sealed class JourneyGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;
 int frame;WorldSetup w=null!;Node home=null!;int originalNodeCount;long pocketRobot;string save="",medalsPath="";TutorialAchievements medals=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Bounds(string n,params object[] args)=>(Rectangle)typeof(Game1).GetMethod(n,S)!.Invoke(null,args)!;
 Rectangle Prop(string n)=>(Rectangle)typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Ready(){Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Set("_homePersuasionActive",false);}
 void Guide(int id){Ready();Call("UpdateTutorialStartGuide",100d);Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==id,"Expected integrated guide "+id);Set("_animationTime",(float)Get("_tutorialGuideStarted")+1);}
 void Dismiss(){Set("_tutorialAttackGuide",false);Set("_tutorialGuideClosing",-1f);Set("_tutorialChromeSlide",0f);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Call("BeginPetitstellaTutorial");w=(WorldSetup)Get("_setup");w.TutorialMapStage=6;Set("_tutorialIntroShown",true);originalNodeCount=w.Nodes.All.Length;
  Check(w.Nodes.All.All(n=>w.Robots.Nodes[n.Id].Count==6 && w.Robots.Nodes[n.Id].Robots.Count(r=>r.Owner==0)==(n.Owner==0?6:2)),"Petitstella gives two extra player robots to every own and foreign node");
  Check(w.Robots.Nodes.SelectMany(s=>s.Robots).All(r=>r.Parts==RobotParts.Complete && r.NativeParts(r.Owner)==3 && r.Role==RobotRole.Soldier),"Extra troops are complete undamaged national soldiers");w.Robots.Check();
  save=Path.Combine(AppContext.BaseDirectory,"journey-save.gz");medalsPath=Path.Combine(AppContext.BaseDirectory,"journey-medals.json");if(File.Exists(medalsPath))File.Delete(medalsPath);
  medals=new(medalsPath);medals.Award(TutorialAchievements.FirstAttack);string previousMedals=File.ReadAllText(medalsPath);Set("_tutorialAchievements",medals);Set("_autoSavePath",save);
  Check(!(bool)Call("TutorialPrimaryActionAllowed",0),"Country stays locked before current-run attack despite existing medal");
  home=w.Nodes.Current(0);var attackHome=w.Nodes.All.First(n=>n.Owner==0 && w.Routes.Neighbors(n).Any(r=>w.Nodes.At(r.Target,r.Entry).Owner!=0));var route=w.Routes.Neighbors(attackHome).First(r=>w.Nodes.At(r.Target,r.Entry).Owner!=0);var target=w.Nodes.At(route.Target,route.Entry);
  w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};var fighter=w.Robots.Nodes[attackHome.Id].Robots.First(r=>r.CanFight);
  var order=new March(0,target.Id,1,attackHome.Id,false,false,new[]{fighter.Id});w.Campaign.Resolve(w,new[]{order});Check(TutorialAchievements.Attacked(w),"Real resolved attack qualifies");
  Set("_autoSaveEnabled",true);Call("OpenRobotBattle");Check((bool)Get("_tutorialAttackPending"),"Existing medal cannot suppress current-run attack progress");
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"Battle"));Set("_battleIntroAge",6.6d);Call("RobotBattleInput",0d,false,false);
  Check(w.TutorialAttackCompleted && (bool)Get("_tutorialAttackAchieved") && (double)Get("_tutorialAchievementAge")==0,"Attack is awarded even after intro has already finished");
  Check(File.ReadAllText(medalsPath)==previousMedals,"Existing global medal remains idempotent");Call("QueueTutorialAttackAchievement");Check(!(bool)Get("_tutorialAttackPending"),"Current-run achievement is announced once");Set("_tutorialAchievementAge",1d);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Set("_tutorialAchievementAge",-1d);Call("BeginIndustry");Check(ReferenceEquals(w,Get("_setup")) && w.Nodes.All.Length==originalNodeCount,"Pocket lesson continues on same world after industry");
   Guide(4);Check((bool)Call("TutorialPrimaryActionAllowed",0) && (bool)Call("TutorialPrimaryActionAllowed",1) && (bool)Call("TutorialPrimaryActionAllowed",2),"Country, end turn and war are enabled after achievement");for(int i=3;i<6;i++)Check(!(bool)Call("TutorialPrimaryActionAllowed",i),"Unrelated primary actions remain locked");
   Check(w.TutorialPocketCourse && w.TutorialPocketIntroShown && !w.TutorialCompletionPending,"Integrated pocket course replaces old step-one ending");
  }
  if(frame==2){
   Dismiss();Call("MapOverlayClick",((Rectangle)Call("PrimaryActionBounds",0)).Center,false);Check((bool)Get("_reignView"),"Enabled country button opens actual inspector");Call("UpdateDomesticSlide",1f);Call("OpenPocket");
   int before=w.Robots.Retinues[0].Count;var store=w.Robots.Nodes[w.Nodes.Current(0).Id];int index=Enumerable.Range(0,store.Count).FirstOrDefault(i=>store.Robots[i].CanFight,-1);Check(index>=0,"Pocket fixture retains a robot at home");pocketRobot=store.Robots[index].Id;
   Set("_pointer",Bounds("PocketCard",index,true).Center);Call("PocketInput",true,false);Set("_pointer",Bounds("PocketTransferButton",true).Center);Call("PocketInput",true,false);
   Check(w.TutorialPocketed && w.Robots.Retinues[0].Count==before+1 && w.Robots.Retinues[0].Robots.Any(r=>r.Id==pocketRobot),"Actual pocket transfer records integrated lesson");Call("PocketInput",false,true);Guide(5);
  }
  if(frame==3){
   Dismiss();Call("ResetInspector");Call("OpenMovement");Call("UpdateNodeFocusCircle");var net=(CubeNet)Get("_net");int current=w.Nodes.Current(0).Id;
   net.HoveredNode=current;Check(!net.EmphasizedNode(current) && !net.MovementTargets.Contains(current),"Movement origin is neither hover-emphasized nor offered as destination");
   int candidate=net.MovementTargets.First();net.HoveredNode=candidate;Check(net.EmphasizedNode(candidate),"Movement destination still responds to hover");
   var target=w.Routes.NodeChoices(0).Select(r=>w.Nodes.At(r.Target,r.Entry)).First(n=>n.Id!=w.Nodes.Current(0).Id && w.Campaign.RobotBattles.CanMarch(w,0,n.Id));int source=w.Nodes.Current(0).Id;
   w.Campaign.Resolve(w,new[]{new March(0,target.Id,0,source,true,true,Peaceful:true)});Check(w.TutorialMoved && w.Nodes.Current(0).Id==target.Id && w.Robots.Retinues[0].Robots.Any(r=>r.Id==pocketRobot),"Actual peaceful move carries pocket and records lesson");Call("CloseMovement");Call("UpdateNodeFocusCircle");net.HoveredNode=w.Nodes.Current(0).Id;Check(net.HoverSuppressedNode==-1 && net.EmphasizedNode(net.HoveredNode),"Closing movement restores ordinary current-node hover");Call("BeginIndustry");Guide(6);
  }
  if(frame==4){Dismiss();Guide(17);Check(w.TutorialCompletionShown && w.TutorialPocketNoticeShown && w.TutorialMoveNoticeShown && w.TutorialStep==1,"Both pocket objectives complete inside integrated step-one world");}
  if(frame==5){Dismiss();Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Mid-advice save resumes");w=(WorldSetup)Get("_setup");Check(w.TutorialPostMoveStage==1,"Advice progress persists after first message");Guide(18);}
  if(frame==6){Dismiss();Guide(19);Check(w.TutorialPostMoveStage==3,"All three post-movement advice messages are tracked");}
  if(frame==7){Dismiss();Set("_pointer",Prop("TutorialReviewButton").Center);Call("TutorialMessageInput",true,false);Check((int)Get("_tutorialMessageSelection")==19,"Review highlights latest save-slot advice");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"journey-progress-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==7){
   Dismiss();Set("_lastAutoSaveTurn",-1);Call("AutoSaveDomestic");Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Integrated pocket progress resumes");w=(WorldSetup)Get("_setup");Check(w.TutorialAttackCompleted && w.TutorialPocketed && w.TutorialMoved && w.TutorialPocketIntroShown && w.TutorialCompletionShown && w.TutorialPostMoveStage==3 && (bool)Call("TutorialPrimaryActionAllowed",0),"Attack, pocket, move and country unlock persist together");Ready();Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide"),"Resume does not repeat completed guides");
   Check((int)Get("_tutorialLastMessage")==19,"Resume remembers the latest advice for review");
   w.TutorialPostMoveStage=0;w.TutorialCompletionShown=false;w.TutorialPocketed=w.TutorialPocketNoticeShown=false;Guide(17);Check(!w.TutorialCompletionShown,"Post-move advice does not require pocket achievement");Dismiss();
   File.Delete(save);File.Delete(medalsPath);Set("_autoSaveEnabled",false);Console.WriteLine("PASS: pre-existing medal, real attack, post-intro achievement detection, country unlock, same-world pocket/move, completion, extra troops on all nodes, post-move advice, review and save/resume.");Exit();
  }
 }
}

using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new PlanGame();game.Run();
sealed class PlanGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 static void Check(bool value,string text){if(!value)throw new Exception(text);}
 void Guide(int index){Call("UpdateTutorialStartGuide",100d);Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==index,"Expected guide "+index);Set("_animationTime",(float)Get("_tutorialGuideStarted")+1f);}
 void MoveFirst()
 {
  w.PrepareTutorialStepTwo(123,456,789);w.SelectPlayer(0);w.HumanPlayers.Add(0);Screen("WorldStatus");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Set("_tutorialIntroShown",true);
  int home=w.Nodes.Current(0).Id;int next=w.Routes.NodeChoices(0).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=home);
  w.Campaign.Resolve(w,new[]{new March(0,next,0,home,true,true,Peaceful:true)});Guide(6);Check(!w.TutorialCompletionShown,"Move alone does not complete step two");Set("_tutorialAttackGuide",false);
  Call("OpenPocket");int index=w.Robots.Nodes[next].Robots.ToList().FindIndex(r=>r.Owner==0);var card=(Rectangle)typeof(Game1).GetMethod("PocketCard",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{index,true})!;Set("_pointer",card.Center);Call("PocketInput",true,false);Set("_pointer",new Point(1100,630));Call("PocketInput",true,false);
  Screen("WorldStatus");Guide(5);Set("_tutorialAttackGuide",false);Guide(7);Check(w.TutorialCompletionShown,"Move-first order completes after actual pocket transfer");Set("_tutorialAttackGuide",false);Set("_tutorialIntroShown",false);
 }
 protected override void LoadContent()
 {
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");MoveFirst();w.PrepareTutorialStepTwo(123,456,789);w.SelectPlayer(0);w.HumanPlayers.Add(0);
  Set("_conquestOverviewEnabled",false);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("FocusStatusGlobe");
  Screen("WorldStatus");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Guide(4);Set("_tutorialAttackGuide",false);
  Call("OpenMovementWithFit",false);var targets=((CubeNet)Get("_net")).MovementTargets;Check(targets.Any(n=>w.Nodes.All[n].Owner!=0),"Foreign movement targets selectable");Call("CloseMovement");
  Set("_statusGlobe",true);Call("OpenMovement");Check((bool)Get("_statusGlobe"),"Opening movement preserves globe view");
  targets.UnionWith(w.Nodes.All.Select(n=>n.Id));Call("FitDestinationView");
  Check(targets.Any(id=>!(bool)Call("OperationPosition",new object[]{id,Vector2.Zero})),"Fixture includes a destination on the hidden globe surface");
  Check((bool)Get("_statusGlobe"),"Hidden destinations do not automatically unfold peaceful movement");Call("CloseMovement");
  Set("_statusGlobe",false);Call("OpenMovement");Check(!(bool)Get("_statusGlobe"),"Opening movement also preserves an already selected net");Call("CloseMovement");
  Check(!(bool)typeof(Game1).GetProperty("TransportOverviewAllowed",F)!.GetValue(this)!,"Step two hides the large-map transport overview button");
  Check(!(bool)Call("TransportOverviewClick",new Point(850,1035)) && !(bool)Get("_transportOverview"),"Hidden transport button cannot open its panel");
  // Exercise actual pocket input, rather than setting its achievement flag.
  Call("OpenPocket");var card=(Rectangle)typeof(Game1).GetMethod("PocketCard",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{3,true})!;
  Check(w.Robots.Nodes[w.Nodes.Current(0).Id].Robots[3].Owner==0,"Own robot on foreign node");Set("_pointer",card.Center);Call("PocketInput",true,false);Set("_pointer",new Point(1100,630));Call("PocketInput",true,false);
  Check(w.TutorialPocketed && w.Robots.Retinues[0].Count==1,"Actual pocket transfer records achievement");Screen("WorldStatus");Guide(5);
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t)
 {
  if(frame==1){Set("_tutorialAttackGuide",false);int home=w.Nodes.Current(0).Id;int next=w.Routes.NodeChoices(0).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=home);Call("OpenMovementWithFit",false);Set("_domesticSlide",1f);Call("SelectMoveNode",next);Call("MovementClick",new Point(700,940),new Microsoft.Xna.Framework.Input.KeyboardState());Check(w.Campaign.PlannedOrders.Single().Peaceful,"Movement UI reserves a peaceful visit");Check((int)typeof(Game1).GetProperty("WarReservationCount",F)!.GetValue(this)! == 0,"Foreign visits are not labelled war reservations");w.Campaign.Advance(w,-1,0,false);Guide(6);}
  if(frame==2){Set("_tutorialAttackGuide",false);Guide(7);Check(w.TutorialCompletionShown,"Both achievements complete tutorial");}
  if(frame==3){Set("_tutorialAttackGuide",false);Call("UpdateTutorialStartGuide",101d);Check(!(bool)Get("_tutorialAttackGuide"),"Guides only once");Call("OpenStrategyInspector");Set("_domesticSlide",1f);Check(!(bool)Call("TrainingInput",new Point(700,1040),true,false) && !(bool)Get("_trainingOpen"),"Step two training button is unavailable");}
  if(frame==4){Call("WandererPolicyClick",new Point(300,920));Check((bool)Get("_wandererPolicyOpen"),"World plan policy button opens four-choice dialog");}
  if(frame==5){Set("_pointer",new Point(700,570));Call("WandererPolicyInput",true,false);Check(w.WandererPolicyFor(0)==WandererPolicy.Persuade,"Policy selection works");Set("_tutorialMenuOpen",true);Screen("Title");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"plan-{frame}.png"));image.SaveAsPng(output,image.Width,image.Height);
  if(frame++==5){Console.WriteLine("PASS: actual pocket transfer, foreign movement targets, tutorial intro/success/completion, one-time notices and four-choice policy UI.");Exit();}
 }
}

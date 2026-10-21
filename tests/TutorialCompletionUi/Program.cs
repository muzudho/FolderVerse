using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CompletionGame();game.Run();
sealed class CompletionGame:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.PrepareTutorialStepOne(123,456,789);w.CompleteLand(90);w.SelectPlayer(0);w.HumanPlayers.Add(0);
  Set("_conquestOverviewEnabled",false);((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Call("FocusStatusGlobe");
  int source=w.Nodes.Current(0).Id,target=w.Nodes.Current(1).Id;var b=new BattleState(1){Kind=BattleKind.Node,SourceNode=source,TargetNode=target,Defender=1};b.Armies.Add(new(){Owner=0});b.Armies.Add(new(){Owner=1});w.Campaign.RobotBattles.Scenes.Add(b);w.Campaign.RobotBattles.LastMarches.Add((0,source,target));
  Screen("Battle");Call("CheckTutorialAttackAchievement");Check(w.TutorialCompletionPending,"Achievement schedules next domestic guide");Call("UpdateTutorialStartGuide",100d);Check(!(bool)Get("_tutorialAttackGuide"),"Completion message does not interrupt the battle");
  Screen("WorldStatus");Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",90d);Call("UpdateTutorialStartGuide",90.1d);Check(!(bool)Get("_tutorialAttackGuide"),"Guide waits until domestic title card has ended");
  Call("UpdateTutorialStartGuide",100d);Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==3 && w.TutorialCompletionShown && !w.TutorialCompletionPending,"Next domestic phase announces completion exactly once");Set("_animationTime",1f);
  var review=(Rectangle)typeof(Game1).GetProperty("TutorialReviewButton",F)!.GetValue(this)!;var system=(Rectangle)typeof(Game1).GetField("StatusMenuButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(!review.Intersects(system) && !review.Intersects(new Rectangle(1050,20,40,60)),"Review, medal and system menu do not overlap");
  var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.TutorialCompletionShown && !restored.TutorialCompletionPending,"Completion state survives saves");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Set("_tutorialAttackGuide",false);Call("UpdateTutorialStartGuide",101d);Check(!(bool)Get("_tutorialAttackGuide"),"Completion guide is not repeated on later frames");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100+frame),TimeSpan.Zero));var pp=GraphicsDevice.PresentationParameters;var colors=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(colors);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(colors);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"completion-{frame}.png"));image.SaveAsPng(output,image.Width,image.Height);
  if(frame++==1){Console.WriteLine("PASS: next-domestic completion dialogue, no battle interruption, one-time/save behavior and non-overlapping header controls.");Exit();}
 }
}

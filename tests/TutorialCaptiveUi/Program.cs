using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new CaptiveLessonGame();game.Run();
sealed class CaptiveLessonGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;Captive captive=null!;TutorialAchievements medals=null!;string path="",normalPath="",beforeDuplicate="";
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 bool Needs(DefeatedChoice choice)=>(bool)Call("TutorialCaptiveChoiceNeedsExplanation",choice);
 void Capture(){w=(WorldSetup)Get("_setup");int ruler=1;w.Relations.Pending.Clear();w.Relations.SetSuperior(ruler,-1);w.Relations.Powered[ruler]=true;captive=new(ruler,0,w.Nodes.Current(ruler).Id);w.Relations.Capture(w,ruler,0,captive.Node);Call("OpenDisposition");Check(w.Relations.Pending.Count==1,"Capture opens actual disposition choice");}
 void Choose(int i){Set("_pointer",((Rectangle)typeof(Game1).GetMethod("DispositionButton",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{i})!).Center);Call("DispositionInput",true,false);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Call("BeginPetitstellaTutorial");
  path=Path.Combine(AppContext.BaseDirectory,"captive-medals.json");normalPath=Path.Combine(AppContext.BaseDirectory,"normal-medals.json");if(File.Exists(path))File.Delete(path);if(File.Exists(normalPath))File.Delete(normalPath);medals=new(path);Set("_tutorialAchievements",medals);Capture();
  w.Looks[captive.Ruler]=Enumerable.Range(0,60).Select(i=>new ConquerorLook(i,0)).First(l=>l.Keywords.Personality is 3 or 9 or 11);
  Check(Enum.GetValues<DefeatedChoice>().All(Needs),"First capture explains all three actions");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Choose(0);Check(medals.Unlocked.Count==0 && w.Relations.Pending.Count==1 && Needs(DefeatedChoice.Recruit),"Refused recruitment earns nothing and keeps explanation");
   w.Looks[captive.Ruler]=Enumerable.Range(0,60).Select(i=>new ConquerorLook(i,0)).First(l=>l.Keywords.Personality is not (3 or 9 or 11));Choose(0);Call("UpdateDispositionScene",2d);
   Check(medals.Has(TutorialAchievements.CaptiveRecruited) && w.Relations.Superiors[captive.Ruler]==0 && !Needs(DefeatedChoice.Recruit) && Needs(DefeatedChoice.Release) && Needs(DefeatedChoice.RemoveBattery),"Successful recruitment unlocks only its own common achievement");
  }
  if(frame==2){Call("FinishDispositionScene");Call("BeginTutorialStepTwo");Capture();Check(!Needs(DefeatedChoice.Recruit) && Needs(DefeatedChoice.Release) && Needs(DefeatedChoice.RemoveBattery),"Recruit explanation stays hidden on different tutorial world");}
  if(frame==3){Choose(1);Check(medals.Has(TutorialAchievements.CaptiveReleased) && w.Relations.Released[captive.Ruler] && w.Relations.Powered[captive.Ruler] && !Needs(DefeatedChoice.Release),"Release records actual choice and suppresses its next explanation");}
  if(frame==4){Call("BeginTutorialStepThree");Capture();Check(!Needs(DefeatedChoice.Recruit) && !Needs(DefeatedChoice.Release) && Needs(DefeatedChoice.RemoveBattery),"Only battery explanation remains on third world");}
  if(frame==5){int ruler=captive.Ruler;Check(w.Nodes.All.Any(n=>n.Owner==ruler),"Battery test starts with owned nodes");Choose(2);Call("UpdateDispositionScene",2d);Check(medals.Has(TutorialAchievements.CaptiveBatteryRemoved) && !w.Relations.Powered[ruler] && w.Nodes.All.All(n=>n.Owner!=ruler),"Battery achievement follows actual removal and unclaimed territory");}
  if(frame==6){Call("FinishDispositionScene");medals=new TutorialAchievements(path);Set("_tutorialAchievements",medals);Call("BeginPetitstellaTutorial");Capture();Check(medals.Unlocked.Count==3 && Enum.GetValues<DefeatedChoice>().All(c=>!Needs(c)),"All three common achievements survive reload and suppress hints on first world");beforeDuplicate=File.ReadAllText(path);Set("_tutorialCaptiveToastSeconds",0d);}
  if(frame==7){Choose(1);Check(File.ReadAllText(path)==beforeDuplicate && (double)Get("_tutorialCaptiveToastSeconds")==0,"Repeated choice does not rewrite achievement timestamp or announce again");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"tutorial-captive-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==7){
   w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);w.HumanPlayers.Add(0);var normal=new TutorialAchievements(normalPath);Set("_tutorialAchievements",normal);Capture();Choose(1);Check(normal.Unlocked.Count==0,"Ordinary games do not unlock tutorial-only achievements");
   File.Delete(path);File.Delete(normalPath);Console.WriteLine("PASS: first-capture explanations, refusal exclusion, three independent achievements, shared across tutorial worlds, persisted reload, no repeated hints/toasts, ordinary-game exclusion.");Exit();
  }
 }
}

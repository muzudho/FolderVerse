using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using var game=new PlanetMedalGame();game.Run();
sealed class PlanetMedalGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic,S=BindingFlags.Static|BindingFlags.NonPublic;int frame;WorldSetup w=null!;TutorialAchievements medals=null!;string path="",save="";
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Menu(){Call("ReturnToTitle");Call("OpenTitleStartMenu");Set("_pointer",((Rectangle)typeof(Game1).GetMethod("TitleMenuItem",S)!.Invoke(null,new object[]{0})!).Center);Call("TitleStartMenuInput",true,false,false,new KeyboardState(),true);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Call("BeginPetitstellaTutorial");w=(WorldSetup)Get("_setup");
  path=Path.Combine(AppContext.BaseDirectory,"planet-medals.json");save=Path.Combine(AppContext.BaseDirectory,"planet-save.gz");if(File.Exists(path))File.Delete(path);medals=new(path);Set("_tutorialAchievements",medals);Set("_autoSavePath",save);Set("_autoSaveEnabled",true);
  foreach(string id in new[]{TutorialAchievements.FirstAttack,TutorialAchievements.CaptiveRecruited,TutorialAchievements.CaptiveReleased,TutorialAchievements.CaptiveBatteryRemoved,"tutorial-planet-other-lessons"})medals.Award(id);
  Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaLessons) && !medals.Has(TutorialAchievements.PetitstellaConquest),"Shared and other-planet achievements do not earn Petitstella medals");
  w.TutorialMapStage=6;w.TutorialPocketed=w.TutorialMoved=true;Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaLessons),"A shared first-attack achievement cannot replace current-planet attack progress");w.TutorialAttackCompleted=true;w.TutorialMoved=false;Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaLessons),"All required planet actions must complete");w.TutorialMoved=true;
  // Backfill a completed saved journey when Continue loads it.
  Set("_lastAutoSaveTurn",-1);Call("AutoSaveDomestic");Call("CheckAutoSave");Check((bool)Call("ResumeAutoSave"),"Completed save resumes");w=(WorldSetup)Get("_setup");Check(medals.Has(TutorialAchievements.PetitstellaLessons) && !medals.Has(TutorialAchievements.PetitstellaConquest),"Resume backfills only actually completed planet medal");
  Menu();
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   Call("BeginPetitstellaTutorial");w=(WorldSetup)Get("_setup");foreach(var n in w.Nodes.All)n.Owner=1;Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaConquest),"Enemy conquest does not award player's medal");
   foreach(var n in w.Nodes.All)n.Owner=0;w.TutorialMapBeforeWar=false;Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaConquest),"Ordinary or separate old step cannot award planet conquest");w.TutorialMapBeforeWar=true;
   Set("_demoActive",true);Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaConquest),"Demo never awards medals");Set("_demoActive",false);
   Call("StartEnding");Check(medals.Has(TutorialAchievements.PetitstellaConquest),"World conquest records before ending deletes save");Call("UpdateEnding",0d,true);Menu();
   medals=new(path);Set("_tutorialAchievements",medals);Check(medals.Has(TutorialAchievements.PetitstellaLessons) && medals.Has(TutorialAchievements.PetitstellaConquest),"Both planet medals survive ending, title and store reload");
  }
  if(frame==2){
   string before=File.ReadAllText(path);Call("RecordPetitstellaMedals");Check(File.ReadAllText(path)==before,"Repeated checks leave achievement timestamps unchanged");
   var r=(Rectangle)typeof(Game1).GetProperty("TutorialClearMedals",S)!.GetValue(null)!;Set("_pointer",r.Center);Call("TutorialMenuInput",true,false,false,new KeyboardState());Check(!medals.Has(TutorialAchievements.PetitstellaLessons) && !medals.Has(TutorialAchievements.PetitstellaConquest),"Existing clear-medals action resets both planet medals");Call("RecordPetitstellaMedals");Check(!medals.Has(TutorialAchievements.PetitstellaLessons) && !medals.Has(TutorialAchievements.PetitstellaConquest),"Stale title world cannot immediately re-award cleared medals");
  }
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"planet-medals-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==2){File.Delete(path);File.Delete(save);Set("_autoSaveEnabled",false);Console.WriteLine("PASS: shared/planet separation, all required actions, completed-save backfill, independent player conquest, demo exclusion, existing one/two medal icons, persistence and clearing.");Exit();}
 }
}

using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new WarGuideGame();game.Run();
sealed class WarGuideGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;WorldSetup w=null!;string save="";
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Ready(){Set("_lastCutInPhase",w.Campaign.Phase==CampaignPhase.Military?"軍事フェーズ":"内政フェーズ");Set("_cutInStarted",90d);Set("_homePersuasionActive",false);}
 void UpdateGuide(){Ready();Call("UpdateTutorialStartGuide",100d);}
 void Guide(int id){UpdateGuide();Check((bool)Get("_tutorialAttackGuide") && (int)Get("_tutorialGuideMessage")==id,"Expected guide "+id);Set("_animationTime",(float)Get("_tutorialGuideStarted")+1);}
 void Dismiss(){Set("_tutorialAttackGuide",false);Set("_tutorialGuideClosing",-1f);Set("_tutorialChromeSlide",0f);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);Call("BeginPetitstellaTutorial");w=(WorldSetup)Get("_setup");w.TutorialMapStage=6;Set("_tutorialIntroShown",true);
  save=Path.Combine(AppContext.BaseDirectory,"war-guides-save.gz");Set("_autoSavePath",save);Set("_autoSaveEnabled",true);
  var lines=(string[][])typeof(Game1).GetField("TutorialMessageLines",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
  Check(!lines[1].Any(l=>l.Contains("ターン終了")) && !lines[16].Any(l=>l.Contains("ターン終了")),"Initial reservation lessons do not mix in turn-end instructions");
  Call("ShowTutorialGuide",16);Set("_animationTime",1f+(float)Get("_tutorialGuideStarted"));
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){
   w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();
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
   Call("OpenInvasionPicker",index);Set("_pointer",new Point(400,350));Call("InvasionPickerInput",true,false);Set("_pointer",new Point(1400,820));Call("InvasionPickerInput",true,false);
   Check((bool)Call("get_TutorialHasAttackReservation"),"Actual sortie confirmation creates valid attack reservation");UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide") && !w.TutorialTurnEndNoticeShown,"Reservation while inspector open waits for close");
   Call("CloseNodeInspector");Call("UpdateDomesticSlide",.1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Closing animation must finish before hint");Call("UpdateDomesticSlide",1f);Guide(21);Check(w.TutorialTurnEndNoticeShown,"Closed reserved inspector explains turn end");
  }
  if(frame==4){
   Dismiss();Call("ReturnToTitle");Check((bool)Call("ResumeAutoSave"),"Reservation and guide progress resume");w=(WorldSetup)Get("_setup");Check(w.TutorialWarTargetNoticeShown && w.TutorialSortieNoticeShown && w.TutorialTurnEndNoticeShown && (int)Get("_tutorialLastMessage")==21,"Both notices and latest review selection persist");UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Resume does not repeat guide");
   Call("OpenWarPlanning");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Sortie guide stays one-time after reload");Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);UpdateGuide();Check(!(bool)Get("_tutorialAttackGuide"),"Turn-end guide stays one-time after reload");
  }
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"war-guide-{frame}.png")))image.SaveAsPng(file,image.Width,image.Height);
  if(frame++==4){File.Delete(save);Set("_autoSaveEnabled",false);Console.WriteLine("PASS: rotation/target then sortie then turn-end instructions, actual sortie confirmation, close-without-order exclusion, closing animation, one-time guides and save/resume.");Exit();}
 }
}

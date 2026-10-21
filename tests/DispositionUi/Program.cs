using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
void Check(bool ok,string text){if(!ok)throw new Exception(text);}
var b=new BattleState(42,new(){NoWillTurns=2,SegmentTurns=3,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1};
b.Armies.Add(new(){Owner=0,Side=0});b.Armies.Add(new(){Owner=1,Side=2});b.Units.Add(new(){Id=1,Owner=0,Position=new(0,0),Daggers=0,Rifles=0,Shields=1});
BattleTurnResolver.Finish(b);Check(!b.Finished,"Defender elimination alone does not win a standing flag");
for(int i=0;i<3;i++)BattleTurnResolver.Advance(b);
Check(!b.Finished && b.FlagStanding && !b.Army(0).Withdrawing,"Flag march may span a segment and retains morale while advancing");
b=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));b.Allied=(a,c)=>a==c;
for(int i=0;i<20 && !b.Finished;i++)BattleTurnResolver.Advance(b);
Check(b.Finished && b.Winner==0 && !b.FlagStanding && b.Units[0].Position==b.Flag,"Survivor reaches the flag, knocks it down and wins immediately without ten-turn wait");
var race=new BattleState(4,new(){PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1};for(int owner=0;owner<3;owner++)race.Armies.Add(new(){Owner=owner,Side=owner});race.Units.Add(new(){Id=1,Owner=0,Position=new(5,4)});race.Units.Add(new(){Id=2,Owner=2,Position=new(9,9)});BattleTurnResolver.Advance(race);Check(race.Finished && race.Winner==0 && !race.FlagStanding,"With no defenders, flag taker wins even in a race between attacking countries");
var edge=new BattleState(3){Kind=BattleKind.Edge};edge.Armies.Add(new(){Owner=0});edge.Units.Add(new(){Id=1,Owner=0,Position=new(0,0)});BattleTurnResolver.Finish(edge);Check(edge.Finished,"Edge battle still wins by elimination");
Console.WriteLine("PASS: mandatory flag march, low-morale progress, continuation/save, immediate victory on flag fall and edge exclusion.");
using var game=new DispositionGame();game.Run();
sealed class DispositionGame:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;int frame,ruler;WorldSetup w=null!;Captive captive=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F|BindingFlags.Static)!.Invoke(this,a)!;
 Rectangle StaticRect(string n)=>(Rectangle)typeof(Game1).GetField(n,BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);w.HumanPlayers.Add(0);
  Set("_conquestOverviewEnabled",false);ruler=Enumerable.Range(1,w.ActiveCount-1).First(r=>w.Relations.Accepts(w,r));captive=new(ruler,0,w.Nodes.Current(ruler).Id);
  w.Relations.Pending.Add(captive);Screen("Disposition");Set("_pointer",((Rectangle)Call("DispositionButton",0)).Center);Call("DispositionInput",true,false);Call("UpdateDispositionScene",2d);
  Check(w.Relations.Superiors[ruler]==0 && Get("_dispositionScene")!=null,"Recruitment applies once and holds dialogue instead of leaving immediately");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==1){Call("FinishDispositionScene");w.Relations.Pending.Add(captive);Screen("Disposition");Set("_pointer",((Rectangle)Call("DispositionButton",2)).Center);Call("DispositionInput",true,false);Call("UpdateDispositionScene",2d);Check(!w.Relations.Powered[ruler],"Battery removal applies while animation is displayed");}
  if(frame==2){Call("FinishDispositionScene");Check(w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==ruler).Cells==0,"Removed conqueror has zero cells");Call("StatusMenuClick",StaticRect("StatusMenuButton").Center);Call("StatusMenuClick",StaticRect("InactiveMenuItem").Center);Check(Get("_screen").ToString()=="InactiveList","System restore-battery menu opens list");}
  if(frame==3){Set("_pointer",((Rectangle)Call("InsertBatteryButton",0)).Center);Call("DispositionInput",true,false);Check(w.Relations.Powered[ruler] && w.Relations.CanAct(ruler) && w.Relations.Superiors[ruler]==-1 && !w.Relations.Pending.Any(c=>c.Ruler==ruler),"System revival restores independent active play without a second disposition");Check(w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==ruler).Cells==0,"Revival does not restore territory");Call("DispositionInput",false,true);((CubeNet)Get("_net")).Home(w);((WorldPreview)Get("_world")).ShowSetup(w,true);Call("FocusStatusGlobe");Set("_statusGlobe",true);Call("StatusMenuClick",StaticRect("StatusMenuButton").Center);}
  if(frame>=4){Screen("Battle");Set("_robotSummary",true);var board=new BattleState(4){TargetNode=captive.Node,SourceNode=captive.Node,Finished=true,Winner=0};board.Armies.Add(new(){Owner=0});board.Armies.Add(new(){Owner=ruler});board.Capture(BattlePhase.Result);Set("_robotScenes",new[]{board});Set("_newspaperMusicMood",(NewspaperMusicMood)(frame-4));}
  if(frame==4){Set("_pointer",StaticRect("NewspaperMusicButton").Center);Call("RobotBattleInput",0d,true,false);Check((NewspaperMusicMood)Get("_newspaperMusicMood")==NewspaperMusicMood.CalmDay,"Newspaper BGM button cycles the atmosphere");Set("_newspaperMusicMood",NewspaperMusicMood.BusyMorning);}
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var output=File.Create(Path.Combine(AppContext.BaseDirectory,$"disposition-{frame}.png"));texture.SaveAsPng(output,texture.Width,texture.Height);
  if(frame++==7){Console.WriteLine("PASS: recruitment/removal dialogue, system menu, revival without territory, globe 3D and four newspaper music controls rendered.");Exit();}
 }
}

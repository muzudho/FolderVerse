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
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);world.HumanPlayers.Add(0);
  Set("_conquestOverviewEnabled",false);Set("_cutInStarted",-10d);((CubeNet)Get("_net")).Home(world);((WorldPreview)Get("_world")).ShowSetup(world,true);
  world.Lessons.Pending.AddRange(BattleLessons.All.Select(l=>new LessonAward(0,l)));
  var battle=new BattleState(42){Finished=true};battle.Armies.Add(new(){Owner=0});battle.Capture(BattlePhase.Result);Set("_robotScenes",new[]{battle});Set("_robotSummary",true);Screen("Battle");
  Call("RobotBattleInput",0d,false,true);Check(Get("_screen").ToString()=="BattleResearch","Newspaper continuation opens research before industry");Check(world.Lessons.Owned[0]==(BattleLesson)7,"Research grants three cards");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame<3){Set("_lessonAge",3d);Set("_lastCutInPhase","戦闘研究フェーズ");Set("_cutInStarted",-10d);}
  if(frame==3){Call("UpdateBattleResearch",0d,true);Check(Get("_screen").ToString()!="BattleResearch","Last card proceeds to industry/domestic");Screen("WorldStatus");Call("OpenStrategyInspector");Set("_domesticSlide",1f);Set("_lastCutInPhase","内政フェーズ");}
  if(frame==4){Check((bool)Call("DomesticViewClick",CatalogPoint),"World plan card button handles click");Check(Get("_screen").ToString()=="LessonCatalog","Button opens catalogue");}
  if(frame==5){Call("HandleInputCore",new Microsoft.Xna.Framework.Input.MouseState(),new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.Escape),new GameTime(TimeSpan.FromSeconds(20),TimeSpan.FromSeconds(.02)),true);Check(Get("_screen").ToString()=="WorldStatus","Esc returns catalogue to world plan");Call("DomesticViewClick",CatalogPoint);world.SelectPlayer(1);world.Lessons.Owned[1]=BattleLesson.FireDiscipline;}
  if(frame==6){Screen("Battle");Set("_robotSummary",true);Call("RobotBattleInput",0d,false,true);Check(Get("_screen").ToString()=="BattleResearch" && ((LessonAward[])Get("_lessonAwards")).Length==0,"No duplicate awards next research");Set("_lessonAge",3d);Set("_lastCutInPhase","戦闘研究フェーズ");}
  if(frame==7){Call("UpdateBattleResearch",8d,false);Check(Get("_screen").ToString()!="BattleResearch","Research auto advances in demo/autoplay");Screen("WorldStatus");world.Relations.SetSuperior(1,0);Call("OpenStrategyInspector");Set("_domesticSlide",1f);Set("_lastCutInPhase","内政フェーズ");}
  base.Draw(time);
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"lesson-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame<2)Call("UpdateBattleResearch",0d,true);
  if(frame++==7){Console.WriteLine("PASS: newspaper/research/industry flow, three award reveals, world-plan button, owned/unowned catalogue, empty research, subordinate plan and automatic continuation rendered.");Exit();}
 }
}

using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using System.Collections;
using System.Text.Json;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var original=new WorldSetup();original.SetWorld(123);original.SetCast(456);original.SetPlacement(789);original.SelectPlayer(0);
original.Population.Cells[0].Migration[2,3]=.123456m;original.Relations.SetSuperior(1,0);original.HumanPlayers.Add(0);
int node=original.Nodes.Current(0).Id;var firstStore=original.Robots.Nodes[node];
var rule=original.Robots.Transport.AddInstruction(node,0,(node+1)%original.Nodes.All.Length);original.Robots.Transport.ConfigurePartChoices(node,0,rule.Number,rule.Target,33,true,7);original.Robots.Transport.SetInstructionRate(node,0,rule.Number,137);
var battle=new BattleState(42,new(){NoWillTurns=1000,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=1,FlagStanding=false,LastFlagOwner=0};battle.Armies.Add(new(){Owner=0,Side=0});battle.Armies.Add(new(){Owner=1,Side=2});battle.Units.Add(new(){Id=991,Owner=0,Position=new(2,2),Rifles=2,Shields=1});battle.Units.Add(new(){Id=992,Owner=1,Position=new(8,8)});battle.Army(0).Waiting.Enqueue(new(){Id=993,Owner=0});
var encounter=new RobotEncounter{Id=123,Kind=BattleKind.Node,Target=node};encounter.Battles.Add(battle);original.Campaign.RobotBattles.Encounters.Add(encounter);original.Campaign.RobotBattles.Scenes.Add(battle);
var homes=(IDictionary)typeof(RobotCampaign).GetField("_homes",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(original.Campaign.RobotBattles)!;homes.Add(991L,(firstStore,node));
battle.Capture(BattlePhase.Result);var document=AutoSaveStore.Capture(original);Check(battle.Frames.Count==1 && original.Campaign.RobotBattles.Scenes.Count==1,"Saving does not mutate live battle playback");var copy=AutoSaveStore.Restore<WorldSetup>(JsonSerializer.Deserialize<AutoSaveStore.Document>(JsonSerializer.Serialize(document))!);
Check(copy.Nodes.Current(0).Id==node && copy.Routes.Neighbors(copy.Nodes.All[node]).Count()==original.Routes.Neighbors(original.Nodes.All[node]).Count(),"World back-references and terrain remain operational");
Check(copy.Population.Cells[0].Migration[2,3]==.123456m && copy.Relations.Superiors[1]==0 && copy.HumanPlayers.SequenceEqual(original.HumanPlayers),"Population decimal matrix and player relations restored");
var restoredRule=copy.Robots.Transport.Instructions(node,0).Single();Check(restoredRule.PartChoices==33 && restoredRule.Degrees==137 && restoredRule.Limit==7 && restoredRule.Disposal && copy.Robots.Transport.RestDegrees(node,0)==223,"Mixed quality transport plan, quota and rest restored");
var restoredBattle=copy.Campaign.RobotBattles.Encounters.Single().Battles.Single();Check(copy.Campaign.RobotBattles.Scenes.Count==0 && restoredBattle.Frames.Count==0,"Finished playback history is omitted from turn-boundary saves");Check(restoredBattle.Random.Next()==battle.Random.Next() && restoredBattle.Army(0).Waiting.Peek().Id==993 && !restoredBattle.FlagStanding,"PRNG, reinforcement queue and fallen flag restored");
var copiedHomes=(IDictionary)typeof(RobotCampaign).GetField("_homes",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(copy.Campaign.RobotBattles)!;Check(ReferenceEquals(((ValueTuple<RobotStore,int>)copiedHomes[991L]!).Item1,copy.Robots.Nodes[node]),"Battle return reservation retains shared store identity");
Check(copy.Robots.Create(0,RobotParts.Head).Id==original.Robots.Create(0,RobotParts.Head).Id,"Robot ID generator restored");
battle.Allied=original.Relations.Allied;restoredBattle.Allied=copy.Relations.Allied;BattleTurnResolver.Advance(battle);BattleTurnResolver.Advance(restoredBattle);Check(battle.Units.Select(u=>(u.Id,u.Owner,u.Position,u.Rifles,u.Shields)).SequenceEqual(restoredBattle.Units.Select(u=>(u.Id,u.Owner,u.Position,u.Rifles,u.Shields))) && battle.Random.Next()==restoredBattle.Random.Next(),"Continued battle evolves identically after restore");
original.Robots.AdvanceIndustry(n=>original.Nodes.All[n].Owner);copy.Robots.AdvanceIndustry(n=>copy.Nodes.All[n].Owner);Check(original.Robots.IndustryTurn==copy.Robots.IndustryTurn && original.Robots.Nodes.SelectMany(n=>n.Robots).SequenceEqual(copy.Robots.Nodes.SelectMany(n=>n.Robots)),"Factory/assembly continuation evolves identically after restore");
Console.WriteLine("PASS: full domain graph, population matrix, terrain references, mixed transport, PRNG, queue and battle store identity.");
using var game=new SaveGame();game.Run();
sealed class SaveGame:Game1{
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 object Prop(string n)=>typeof(Game1).GetProperty(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string m){if(!ok)throw new Exception(m);}
 string path=Path.Combine(AppContext.BaseDirectory,"autosave-test","autosave.json.gz");
 void Input(int x,int y,ButtonState b=ButtonState.Released,double dt=0){Call("HandleInput",new MouseState(x,y,0,b,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.Zero,TimeSpan.FromSeconds(dt)),true);}
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSavePath",path);Set("_autoSaveEnabled",true);if(File.Exists(path))File.Delete(path);
  var w=(WorldSetup)Get("_setup");w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);var net=(CubeNet)Get("_net");var world=(WorldPreview)Get("_world");world.ShowSetup(w,true);net.Home(w);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  int source=w.Nodes.Current(0).Id;var factory=w.Robots.Workshops[source];for(int seed=0;factory.ProductionLines.Length==0;seed++)factory.GenerateLines(seed);
  Call("OpenNodeInspector",source);Call("OpenNodeFactory");Set("_factoryDropDown",0);Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);Check(!(bool)Get("_nodeFactoryOpen") && (int)Get("_factoryDropDown")==-1,"Closing clears stale factory modal");
  Set("_nodeFactoryOpen",true);Set("_factoryDropDown",0);Set("_populationNode",-1);Set("_populationCell",-1);Call("NodeDialogClick",new Point(200,500),new KeyboardState());Check(!(bool)Call("NodeFactoryClick",new Point(200,500)),"Invalid node cannot index factory arrays");Call("ResetInspector");
  net.ShowBatteries=false;net.ShowOtherCountries=false;world.GlobeZoom=2.5f;world.ManualOrientation=Quaternion.CreateFromYawPitchRoll(.1f,.2f,.3f);
  Call("AutoSaveDomestic");Check(File.Exists(path) && (bool)Get("_hasAutoSave"),"Initial domestic boundary saves");var bytes=File.ReadAllBytes(path);Call("AutoSaveDomestic");Check(bytes.SequenceEqual(File.ReadAllBytes(path)),"Same global turn does not rewrite autosave");
  Call("ReturnToTitle");Check(File.Exists(path) && (bool)Get("_hasAutoSave"),"Ordinary quit retains save and Resume availability");
  Input(960,930);Input(960,930,ButtonState.Pressed);Input(960,930);Check((bool)Get("_titleMenuOpen") && Get("_screen").ToString()=="Title","Start opens the paper menu without resuming immediately");Input(960,517);Input(960,517,ButtonState.Pressed);Input(960,517);Check(Get("_screen").ToString()=="WorldStatus" && !net.ShowBatteries && !net.ShowOtherCountries && world.GlobeZoom==2.5f,"Title Resume restores campaign and per-player view");w=(WorldSetup)Get("_setup");Check(w.Nodes.Current(0).Id==source,"Resume reconstructs current node");
  w.Population.Advance(w);Call("RestoreDomesticCamera");Check((int)Get("_lastAutoSaveTurn")==1 && !bytes.SequenceEqual(File.ReadAllBytes(path)) && Directory.GetFiles(Path.GetDirectoryName(path)!).Length==1,"Next global turn replaces one save slot atomically");
  var good=File.ReadAllBytes(path);File.WriteAllText(path,"broken");Call("ReturnToTitle");Check(!(bool)Call("ResumeAutoSave") && Get("_screen").ToString()=="Title" && File.ReadAllText(path)=="broken","Corrupt save fails safely without deleting it");File.WriteAllBytes(path,good);Check((bool)Call("ResumeAutoSave"),"Good save still resumes");
  Call("StartEnding");Check(File.Exists(path),"Save remains while ending is on screen");Call("UpdateEnding",10d,false);Check(!File.Exists(path) && !(bool)Get("_hasAutoSave") && Get("_screen").ToString()=="Title","Completing ending removes autosave and restores fresh start");
  File.WriteAllBytes(path,good);Call("CheckAutoSave");Call("OpenTitleStartMenu");
  Console.WriteLine("PASS: factory crash regression, atomic single-slot turn save, title resume, camera/preferences, corrupt-file recovery and ending deletion.");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);tex.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"title-resume.png")))tex.SaveAsPng(file,tex.Width,tex.Height);
  Call("DeleteCompletedAutoSave");Exit();
 }
}

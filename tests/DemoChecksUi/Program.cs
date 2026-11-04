using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new DemoMarkGame();game.Run();
sealed class DemoMarkGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame,home,target;WorldSetup w=null!;
 object Get(string n){var f=typeof(Game1).GetField(n,F);return f!=null?f.GetValue(this)!:typeof(Game1).GetProperty(n,F)!.GetValue(this)!;}
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 void StepDemo(){Call("UpdateDemoDomestic",1.8d);}
 int Stage=>(int)Get("_demoDomesticStage");
 Point Click=>((Vector2)Get("_demoClickPoint")).ToPoint();
 protected override void LoadContent(){Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();Set("_autoSaveEnabled",false);}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  int scenario=frame/9,step=frame%9;
  if(step==0){
   Call("ResetInspector");Call("ClearAllNodeChecks");w=(WorldSetup)Get("_setup");w.PrepareCampaignDuel(123,456,789,12,0,1);w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};home=w.Nodes.Current(0).Id;
   var store=w.Robots.Nodes[home];if(!store.HasSpace)store.Remove(store.Robots[0].Id);store.Add(w.Robots.Create(0,RobotParts.Complete));
   var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=scenario>=2?960:1920;graphics.PreferredBackBufferHeight=scenario>=2?540:1080;graphics.ApplyChanges();
   ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);Set("_statusGlobe",scenario%2==1);Set("_screen",Enum.Parse(typeof(Game1).GetField("_screen",F)!.FieldType,"WorldStatus"));Set("_demoActive",true);Set("_autoPlay",false);Set("_demoIntro",false);Set("_demoSwing",0f);Set("_demoMapVisits",0);Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
   Call("OpenNodeInspector",home);Call("UpdateDomesticSlide",1f);Set("_demoDomesticStage",10);Set("_demoDomesticAge",0d);StepDemo();Check(Stage==20,"Finishing node work enters green-check step");StepDemo();
   Check((bool)Call("NodeChecked",home) && Stage==11 && ((Rectangle)Get("NodeCheckButton")).Contains(Click),"Demo cursor clicks the actual green handle check after work");
  }
  if(step==1){StepDemo();Call("UpdateDomesticSlide",1f);Check(!(bool)Get("InspectorVisible") && Stage==19,"Node close finishes before domestic end");StepDemo();Check(w.Campaign.Phase==CampaignPhase.Delivery && !(bool)Call("NodeChecked",home),"Demo completes domestic work and expires green notes before delivery");Call("OpenMilitaryPhase");Set("_demoMapVisits",0);Set("_demoDomesticAge",0d);StepDemo();Call("UpdateDomesticSlide",1f);Check(Stage==31 && (bool)Get("_warPlanning") && ((Rectangle)Get("WarButton")).Contains(Click),"Demo opens actual war primary action in military phase");}
  if(step==2){for(int attempt=0;attempt<8 && Stage==31;attempt++)StepDemo();target=(int)Get("_selectedMoveNode");Check(Stage==32 && target>=0,"Demo finds a real visible attack target");var net=(CubeNet)Get("_net");int hit=(bool)Get("_statusGlobe")?(int)Call("HitGlobeNode",Click,net.MovementTargets):net.HitNode(w,(Rectangle)Get("NetPanel"),Click,net.MovementTargets);Check(hit==target,"War cursor matches actual projected target");}
  if(step==3){StepDemo();Check(Stage==33 && (int)Get("_invasionPicker")>=0,"Demo opens actual sortie selection modal");}
  if(step==4){StepDemo();Check(Stage==34 && w.Campaign.PlannedOrders.Count==0,"Draft selection is not prematurely reserved");}
  if(step==5){StepDemo();Check(Stage==35 && (int)Get("_invasionPicker")==-1 && w.Campaign.PlannedOrders.Any(o=>o.TargetNode==target && !o.Peaceful),"Demo confirms actual sortie reservation without removed final button");}
  if(step==6){StepDemo();Call("PrepareDestinationMap");Call("UpdateNodeFocusCircle");var checks=(Dictionary<int,HashSet<int>>)Get("_warChecksByPlayer");Check(Stage==36 && checks[w.TurnPlayer].Contains(target) && ((Rectangle)Get("NodeCheckButton")).Contains(Click),"Demo clicks actual red handle check after reservation");Check(!((CubeNet)Get("_net")).AttackHighlightTargets.Contains(target) && !(bool)Call("NodeChecked",home),"Red check removes spotlight after domestic green note expired");}
  if(step==7){StepDemo();Call("UpdateDomesticSlide",1f);Check(Stage==19 && !(bool)Get("InspectorVisible") && w.Campaign.PlannedOrders.Any(o=>o.TargetNode==target),"Demo closes reserved inspector before independent end turn");}
  if(step==8){StepDemo();Check(!((Dictionary<int,HashSet<int>>)Get("_warChecksByPlayer")).ContainsKey(0) && !((Dictionary<int,HashSet<int>>)Get("_nodeChecksByPlayer")).ContainsKey(0),"Demo end turn clears both notes");Check(w.Campaign.RobotBattles.LastMarches.Any(m=>m.Owner==0 && m.Target==target),"Demo's actual reservation executes on end turn");Console.WriteLine($"PASS: check/war sequence scenario={scenario} target={target} size={GraphicsDevice.Viewport.Width}");}
  base.Draw(new GameTime(TimeSpan.FromSeconds(100),TimeSpan.Zero));
  if(step is 0 or 6){var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var image=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);image.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"demo-checks-{scenario}-{step}.png"));image.SaveAsPng(file,image.Width,image.Height);}
  if(++frame==36){Console.WriteLine("PASS: actual green/red clicks, selection-confirm-reserve-mark-close-turn order, net/globe at 1080p/540p, separate notes and turn expiry; no player save writes.");Exit();}
 }
}

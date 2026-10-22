using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using var game=new UsabilityGame();game.Run();
sealed class UsabilityGame:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup world=null!;CubeNet net=null!;int frame,node;Robot own=null!,foreign=null!;long transferred;
 Color[] ownHover=null!,foreignBaseline=null!;int target;Rectangle flatBattery;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 Rectangle Rect(string n,params object[] a)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,a)!;
 void Screen(string n)=>Set("_screen",Enum.Parse(Get("_screen").GetType(),n));
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 void Pointer(Point p){Set("_pointer",p);}
 void Click(Point p){Pointer(p);Call("PocketInput",true,false);}
 void Persuasion(bool manual){
  Screen("WorldStatus");Set("_homePersuasion",new List<DomesticPersuasion>{new(0,node,foreign,foreign,false)});Set("_homePersuasionActive",false);Set("_homePersuasionAge",0d);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",0d);
  Check((bool)Call("UpdateHomePersuasion",.1d,false,10d) && (bool)Get("_nodeAssemblyOpen"),"Persuasion opens the hangar first");Set("_domesticSlide",1f);
  Call("UpdateHomePersuasion",manual?5d:8d,manual,20d);
  Check(!(bool)Get("_homePersuasionActive") && (bool)Get("_nodeInspectorClosing") && (float)Get("_domesticSlide")==1,"Persuasion starts the same close animation as the handle");
  Call("UpdateDomesticSlide",.2f);Check(Math.Abs((float)Get("_domesticSlide")-.5f)<.01,"Close animation reaches halfway at 0.2 seconds");Call("UpdateDomesticSlide",.2f);
  Check((int)Get("_populationCell")==-1 && !(bool)Get("_nodeAssemblyOpen") && (float)Get("_domesticSlide")==0 && Get("_screen").ToString()=="WorldStatus","Manual/automatic persuasion completes the 0.4-second close into map");
 }
 void QuietPlayback(){
  var a=new BattleState(1);a.Armies.Add(new(){Owner=0});a.Turn=1;a.Capture(BattlePhase.Choose);a.Capture(BattlePhase.Result);
  var b=new BattleState(2);b.Armies.Add(new(){Owner=1});for(int i=1;i<=2;i++){b.Turn=i;b.Capture(BattlePhase.Choose);b.Capture(BattlePhase.Result);}
  Set("_robotScenes",new[]{a,b});Set("_robotPlayback",new RobotBattlePlayback(new[]{a,b}));Set("_robotRound",0);Set("_battleWave",0);Set("_battleAge",0d);Set("_robotSummary",false);Set("_robotPlaying",true);Set("_robotPendingFocus",-1);Screen("Battle");
  Call("RobotBattleInput",.399d,false,false);Check((int)Get("_robotRound")==0,"Quiet turn holds until 0.4 seconds");Call("RobotBattleInput",.001d,false,false);
  Check((int)Get("_robotRound")==1 && (int)Get("_battleWave")==1 && (int)Get("_robotPendingFocus")==-1,"Quiet turn advances at 0.4 seconds across board focus with no five-second hold");
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.PrepareTutorialStepTwo(123,456,789);world.SelectPlayer(0);world.HumanPlayers.Add(0);node=world.Nodes.Current(0).Id;
  Check(Math.Abs(WorldPreview.BatteryTilt(2,0)-MathHelper.PiOver2)<.0001 && Math.Abs(WorldPreview.BatteryTilt(6,0))<.0001 && Math.Abs(WorldPreview.BatteryTilt(0,0)-WorldPreview.BatteryTilt(8,0))<.0001,"Battery completes a slow eight-second standing/lying cycle");
  foreach(var source in new[]{new Rectangle(0,0,256,192),new Rectangle(40,70,166,170)}){var crop=PortraitRenderer.FaceSource(source);Check(source.Contains(crop) && crop.Width==crop.Height,"Face crop remains square and inside each portrait/atlas tile");}
  foreach(var r in world.Robots.Nodes[node].Robots.ToArray())world.Robots.Nodes[node].Remove(r.Id);
  own=world.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=1,LegsOrigin=1};foreign=world.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=0,BodyOrigin=0,LegsOrigin=0};world.Robots.Nodes[node].Add(own);world.Robots.Nodes[node].Add(foreign);
  world.Robots.Retinues[0].Add(world.Robots.Create(0,RobotParts.Head|RobotParts.Body));Set("_conquestOverviewEnabled",false);Set("_tutorialAttackGuide",false);Set("_tutorialIntroShown",true);Persuasion(true);Persuasion(false);
  QuietPlayback();net=(CubeNet)Get("_net");net.Home(world);((WorldPreview)Get("_world")).ShowSetup(world,true);net.ShowBatteries=false;net.ShowOtherCountries=false;
  Call("OpenPocket");Check((bool)Call("PocketRobotSelectable",own,true) && !(bool)Call("PocketRobotSelectable",foreign,true),"Selection uses owner rather than manufacture colors");
  Check(!(bool)Call("PocketTransferEnabled",true) && !(bool)Call("PocketTransferEnabled",false),"Transfer is disabled until selection");
  Click(Rect("PocketCard",1,true).Center);Check(Get("_pocketRobot")==null,"Foreign card cannot be selected even with own-country paint");
  Click(Rect("PocketCard",0,true).Center);Check((long)Get("_pocketRobot")==own.Id && (bool)Call("PocketTransferEnabled",true) && !(bool)Call("PocketTransferEnabled",false),"Only the selected source direction enables transfer");
  world.Robots.PendingEdits.Add(new(node,new[]{own.Id},null));Check(!(bool)Call("PocketRobotSelectable",own,true) && !(bool)Call("PocketTransferEnabled",true),"Reserved robot is not highlighted as movable");world.Robots.PendingEdits.Clear();
  transferred=own.Id;Click(Rect("PocketTransferButton",true).Center);Check(world.Robots.Retinues[0].Robots.Any(r=>r.Id==transferred && r.Owner==0 && r.Origin(RobotParts.Head)==1),"Pocket transfer preserves owner, identity and original paint");
  Call("OpenPocket");
  Console.WriteLine("PASS: persuasion closes both ways, owner/origin mismatch, foreign refusal, selection/directional enabling, reserved robot refusal and identity-preserving transfer.");
 }
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
  if(frame==0)Pointer(Rect("PocketCard",0,false).Center);
  if(frame==1){Click(Rect("PocketCard",1,false).Center);Pointer(Rect("PocketTransferButton",false).Center);}
  if(frame==2)Pointer(Rect("PocketCard",0,true).Center);
  if(frame==3){Click(Rect("PocketTransferButton",false).Center);Check(world.Robots.Nodes[node].Robots.Any(r=>r.Id==transferred),"Deployment from the pocket returns to current foreign node");Pointer(Rect("PocketCard",1,true).Center);}
  if(frame==4 || frame==5){Call("PocketInput",false,true);Screen("WorldStatus");Call("ResetInspector");Set("_domesticSlide",0f);Set("_statusGlobe",frame==4);if(frame==4)Call("FocusStatusGlobe");else{net.Home(world);net.FitNodesAtSource(world,world.Nodes.All[node],(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1f);}Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);net.ShowBatteries=false;net.ShowOtherCountries=false;}
  if(frame==6){Set("_statusGlobe",true);Call("FocusStatusGlobe");Set("_yawAngle",(float)Get("_yawAngle")+MathHelper.Pi);}
  if(frame==7){Set("_statusGlobe",false);net.ShowOtherCountries=true;net.Home(world);Call("OpenMovementWithFit",false);Set("_domesticSlide",1f);Set("_domesticMapLeft",535f);Call("FitDestinationView");Check(!(bool)Call("HasMoveReservation"),"Lamp is off before a confirmed move");target=net.MovementTargets.First();Call("SelectMoveNode",target);Check(((int[])Call("MoveReservationTargets")).Contains(target),"Selected destination immediately gets a bus");}
  if(frame==8){Call("MovementClick",new Point(700,940),new Microsoft.Xna.Framework.Input.KeyboardState());Check(world.Campaign.PlannedOrders.Any(o=>o.Peaceful && o.TargetNode==target) && ((int[])Call("MoveReservationTargets")).Contains(target),"Confirmed peaceful reservation retains the bus");Check((bool)Call("HasMoveReservation"),"Confirmed peaceful move lights the lamp");world.Campaign.Plan(new(0,target,1,node,false,false,new[]{transferred}));Set("_domesticSlide",0f);Set("_domesticMapLeft",55f);net.Home(world);net.FitNodesAtSource(world,world.Nodes.All[target],(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1f);}
  if(frame==9 || frame==10){Set("_statusGlobe",true);Call("FocusStatusGlobe");net.ShowBatteries=true;net.ShowOtherCountries=false;Set("_animationTime",frame==9?2f:6f);}
  if(frame==11 || frame==12){
   if(frame==11){world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);var harbor=world.Nodes.All.First(n=>n.IsHarbor);harbor.Owner=0;world.ConquerorLocations[0]=harbor.Cell;world.ConquerorPoints[0]=harbor.Center;world.Nodes.Recount();Check(world.TerritoryCounts[0]>0 && world.Nodes.Current(0).IsHarbor,"Fixture is a landed harbor with territory");((WorldPreview)Get("_world")).ShowSetup(world,true);Call("ResetInspector");}
   Set("_statusGlobe",frame==11);if(frame==11)Call("FocusStatusGlobe");else{net.Home(world);net.FitNodesAtSource(world,world.Nodes.Current(0),(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!,1f);}
   Check((bool)Call("WandererLocationVisible",new object[]{Vector2.Zero}),"Owned harbor retains portrait chip on globe and net");
  }
  base.Draw(t);
  if(frame>=4 && frame<=6){var args=new object[]{Vector2.Zero};Check((bool)Call("WandererLocationVisible",args)==(frame<6),"Wanderer marker respects globe visibility and works with battery/other countries hidden");world.Relations.Powered[0]=false;Check(!(bool)Call("WandererLocationVisible",new object[]{Vector2.Zero}),"No marker for battery removed conqueror");world.Relations.Powered[0]=true;}
  if(frame==7 || frame==8){var args=new object[]{target,Vector2.Zero};Check((bool)Call("OperationPosition",args),"Bus target is visible");var at=(Vector2)args[1];var map=(Rectangle)typeof(Game1).GetProperty("MapViewBounds",F)!.GetValue(this)!;
   foreach(var point in new[]{at,new Vector2(map.Left+2,map.Top+2),new Vector2(map.Right-2,map.Top+2),new Vector2(map.Left+2,map.Bottom-2),new Vector2(map.Right-2,map.Bottom-2)}){
    var bus=(Rectangle)Call("VisibleMoveBusBounds",point);Check(map.Contains(bus) && !bus.Intersects(new Rectangle((int)point.X-57,(int)point.Y-65,114,61)),"Bus stays on screen and apart from sword even at map corners");
   }
  }
  if(frame==9 || frame==10){var badges=(List<(int Ruler,Rectangle Bounds)>)typeof(CubeNet).GetField("_batteries",F)!.GetValue(net)!;var box=badges.Single(b=>b.Ruler==0).Bounds;if(frame==9)flatBattery=box;else Check(box!=flatBattery,"Battery bounds and hit target follow standing/lying animation");}
  var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"usability-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  Color[] CardPixels(Rectangle box)=>Enumerable.Range(box.Top,box.Height).SelectMany(y=>Enumerable.Range(box.Left,box.Width).Select(x=>pixels[y*p.BackBufferWidth+x])).ToArray();
  if(frame==0)ownHover=CardPixels(Rect("PocketCard",0,false));
  if(frame==1){Check(!ownHover.SequenceEqual(CardPixels(Rect("PocketCard",0,false))),"Own card changes visibly under hover");foreignBaseline=CardPixels(Rect("PocketCard",0,true));}
  if(frame==2)Check(foreignBaseline.SequenceEqual(CardPixels(Rect("PocketCard",0,true))),"Foreign card does not falsely highlight as clickable");
  if(frame++==12){
   var graphics=(GraphicsDeviceManager)Get("_graphics");var before=Window.ClientBounds.Size;Set("_invasionPicker",0);Set("_previousKeyboard",new Microsoft.Xna.Framework.Input.KeyboardState());
   var f11=new Microsoft.Xna.Framework.Input.KeyboardState(Microsoft.Xna.Framework.Input.Keys.F11);
   void Input(Microsoft.Xna.Framework.Input.KeyboardState keys,bool active)=>Call("HandleInput",new Microsoft.Xna.Framework.Input.MouseState(),keys,new GameTime(TimeSpan.FromSeconds(30),TimeSpan.Zero),active);
   Input(f11,false);Check(!graphics.IsFullScreen,"Inactive F11 does not toggle");Input(new(),true);Input(f11,true);Check(graphics.IsFullScreen && !graphics.HardwareModeSwitch,"F11 enters desktop fullscreen even with a modal open");Input(f11,true);Check(graphics.IsFullScreen,"Holding F11 does not toggle repeatedly");Input(new(),true);Input(f11,true);Check(!graphics.IsFullScreen && Window.ClientBounds.Size==before,"F11 restores the former window dimensions");Set("_invasionPicker",-1);
   Console.WriteLine("PASS: F11 fullscreen/windowed, restored dimensions, held key and inactive window.");Console.WriteLine("PASS: hover, animated persuasion closure, quiet-turn playback, portrait chip, offscreen hint, selected/persistent buses, sword separation and animated battery hit bounds rendered.");Exit();}
 }
}

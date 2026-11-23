using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using var game=new DemoInspectorChecks();game.Run();
sealed class DemoInspectorChecks:Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;int node;WorldSetup world=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 protected override void LoadContent(){
  Content.RootDirectory=Environment.GetEnvironmentVariable("FOLDERVERSE_TEST_CONTENT") ?? Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();world=(WorldSetup)Get("_setup");world.SetWorld(123);world.SetCast(456);world.SetPlacement(789);world.SelectPlayer(0);
  ((WorldPreview)Get("_world")).ShowSetup(world,true);((CubeNet)Get("_net")).Home(world);
  Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Set("_conquestOverview",false);Set("_conquestOverviewEnabled",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);
  Call("ResetInspector");Check((int)Get("_inspectedNode")==-1,"Fixture starts with no inspected supervisor");
  if(!PendingAudit && Environment.GetEnvironmentVariable("FOLDERVERSE_FACTORY_MODAL_INPUT")!="1")Set("_demoActive",true);
  Set("_demoMapVisits",1);Set("_demoDomesticStage",0);Set("_demoDomesticAge",0d);
  for(int attempt=0;attempt<4 && (int)Get("_populationNode")<0;attempt++)Call("UpdateDemoDomestic",1.8d);
  Call("UpdateDomesticSlide",1f);
  node=(int)Get("_populationNode");Check(node>=0 && (int)Get("_inspectedNode")==node && (bool)Get("_nodeOfficeOpen"),"Demo opens the office with synchronized node and supervisor IDs");
  var font=(SpriteFont)Get("_font");Check("伏太峡渓渡谷".All(font.Characters.Contains),"Compiled font includes every previously missing static glyph");Check(font.DefaultCharacter=='?' && !font.Characters.Contains('\uE000'),"Unknown external text has an explicit fallback");Check(font.MeasureString("external \uE000").X>0,"Unknown dynamic glyph cannot throw during measurement");
  if(Environment.GetEnvironmentVariable("FOLDERVERSE_DEMO_FACTORY_ONLY")=="1")frame=11;
 }
 protected override void Update(GameTime t){}
 bool PendingAudit=>Environment.GetEnvironmentVariable("FOLDERVERSE_PENDING_RENDER")=="1";
 bool PendingCanvas=>Environment.GetEnvironmentVariable("FOLDERVERSE_PENDING_CANVAS")=="1";
 void PendingClick(Point point){
  var canvas=(Rectangle)Call("CanvasBounds");int x=canvas.X+(int)Math.Round(point.X*canvas.Width/1920d),y=canvas.Y+(int)Math.Round(point.Y*canvas.Height/1080d);
  Console.WriteLine($"Canvas before logical={point} canvas={canvas} physical={x},{y} screen={Get("_screen")} factory={Get("_nodeFactoryOpen")} plan={Get("_factoryPlanLine")} slide={Get("_domesticSlide")} persuasion={Get("_homePersuasionActive")} chrome={Get("_tutorialChromeSlide")} phase={Call("get_PlanningPhaseLabel")} cutin={Get("_lastCutInPhase")}/{Get("_cutInStarted")}");
  foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released}){
   Call("HandleInput",new MouseState(x,y,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(TimeSpan.FromSeconds(100),TimeSpan.FromSeconds(.1)),true);
   var args=new object[]{Call("PlanningWorkshop",node),null!};var lines=(RobotWorkshop[])typeof(Game1).GetMethod("FactoryScreenLines",F)!.Invoke(this,args)!;
   Console.WriteLine($"event={state} screen={Get("_screen")} factory={Get("_nodeFactoryOpen")} population={Get("_populationNode")}/{Get("_populationCell")} pendingCount={world.Robots.ProductionIntakes.Count} sources={string.Join(",",world.Robots.ProductionIntakes.Select(i=>i.Source))} office={Get("_nodeOfficeOpen")} assembly={Get("_nodeAssemblyOpen")} transport={Get("_nodeTransportOpen")} disposal={Get("_nodeDisposalOpen")} closing={Get("_nodeInspectorClosing")} moving={Get("_movementOpen")} album={typeof(Game1).GetProperty("FactoryAlbumOpen",F)!.GetValue(this)} planLine={Get("_factoryPlanLine")} manage={Call("CanManageNodeFactory",world.Nodes.All[node].Owner)} demo={Get("_demoActive")} harvest={Get("_factoryHarvestPage")} owner={world.Nodes.All[node].Owner} player={world.PlayerSlot} turnPlayer={world.TurnPlayer} basePlans={string.Join(",",world.Robots.Workshops[node].ProductionLines.Select(l=>l.SelectedPlan))} screenPlans={string.Join(",",lines.Select(l=>l.SelectedPlan))}");
  }
  Console.WriteLine($"Canvas after pointer={Get("_pointer")} screen={Get("_screen")} pending={Get("_pendingIntakesOpen")} factory={Get("_nodeFactoryOpen")} slide={Get("_domesticSlide")} persuasion={Get("_homePersuasionActive")}");
 }
 void DrawPendingAudit(GameTime t){
  if(frame==0){
   if(PendingCanvas){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=int.TryParse(Environment.GetEnvironmentVariable("FOLDERVERSE_PENDING_WIDTH"),out int width)?width:960;g.PreferredBackBufferHeight=g.PreferredBackBufferWidth*9/16;g.ApplyChanges();world.HumanPlayers.Clear();world.HumanPlayers.Add(world.PlayerSlot);world.Relations.Leader=world.PlayerSlot;node=world.Nodes.Current(world.PlayerSlot).Id;world.Robots.Workshops[node].Automation=NodeAutomation.Manual;Call("ResetInspector");Call("OpenNodeInspector",node);Set("_paperReadTurn",world.Population.Turn);Set("_paperReadCountry",world.PlayerSlot);}
   world.Robots.ProductionIntakes.Clear();
   for(int i=0;i<10;i++)world.Robots.ProductionIntakes.Add(new ProductionIntake(node,null,new Robot(920000+i,world.PlayerSlot,RobotParts.Head),new RobotWorkshop()));
   Call("OpenNodeFactory");Set("_intakeNode",node);Set("_intakePage",0);Set("_pendingIntakesOpen",true);
   if(PendingCanvas){Set("_pendingIntakesOpen",false);Call("UpdateDomesticSlide",1f);var footer=(Rectangle)typeof(Game1).GetField("FactoryPendingIntakesButton",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;base.Draw(t);var pp0=GraphicsDevice.PresentationParameters;var pixels0=new Color[pp0.BackBufferWidth*pp0.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels0);using(var tex0=new Texture2D(GraphicsDevice,pp0.BackBufferWidth,pp0.BackBufferHeight)){tex0.SetData(pixels0);using var initial=File.Create(Path.Combine(AppContext.BaseDirectory,"pending-initial.png"));tex0.SaveAsPng(initial,tex0.Width,tex0.Height);}Console.WriteLine($"After initial draw factory={Get("_nodeFactoryOpen")} count={world.Robots.ProductionIntakes.Count} bounds={typeof(Game1).GetProperty("NodeInspectorBounds",F)!.GetValue(this)}");PendingClick(footer.Center);bool opened=(bool)Get("_pendingIntakesOpen");if(!opened){Set("_nodeFactoryOpen",true);Console.WriteLine($"Direct footer comparison result={Call("NodeFactoryClick",new Point(364,944))} pending={Get("_pendingIntakesOpen")} factory={Get("_nodeFactoryOpen")}");}Check(opened,"Canvas footer click failed");}
  }
  if(frame==1){
   var next=(Rectangle)typeof(Game1).GetField("IntakeNext",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
   if(PendingCanvas)PendingClick(next.Center);else{Set("_pointer",next.Center);Call("ProductionIntakeInput",true,false);}Check((int)Get("_intakePage")==1,"Pending next page did not advance");
  }
  if(frame==2){
   var row=(Rectangle)typeof(Game1).GetMethod("IntakeRow",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{1})!;
   if(PendingCanvas)PendingClick(row.Center);else{Set("_pointer",row.Center);Call("ProductionIntakeInput",true,false);}
   Check(!(bool)Get("_pendingIntakesOpen") && ReferenceEquals(Get("_intakeLine"),world.Robots.ProductionIntakes[9].Line),"Rendered second page selected wrong product");
  }
  if(frame==3){if(PendingCanvas)Call("HandleInput",new MouseState(),new KeyboardState(Keys.Escape),new GameTime(TimeSpan.FromSeconds(101),TimeSpan.FromSeconds(.1)),true);else Call("ProductionIntakeInput",false,true);Check(Get("_intakeLine")==null,"Target picker Escape did not close");Call("ResetInspector");Check(!(bool)Get("_pendingIntakesOpen"),"Reset kept pending list");}
  base.Draw(t);var size=GraphicsDevice.PresentationParameters;var pixels=new Color[size.BackBufferWidth*size.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);
  using var tex=new Texture2D(GraphicsDevice,size.BackBufferWidth,size.BackBufferHeight);tex.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"pending-render-{frame}.png"));tex.SaveAsPng(file,tex.Width,tex.Height);
  if(frame++==3){Console.WriteLine("PASS pending rendered pages, second-page product identity, target picker and Escape/reset; synthetic pending fixture.");Exit();}
 }
 protected override void Draw(GameTime t){
  if(Environment.GetEnvironmentVariable("FOLDERVERSE_FACTORY_MODAL_INPUT")=="1"){
   if(frame==0){world.HumanPlayers.Clear();world.HumanPlayers.Add(world.PlayerSlot);world.Relations.Leader=world.PlayerSlot;Set("_factoryHarvestPage",false);node=world.Nodes.Current(world.PlayerSlot).Id;world.Robots.Workshops[node].Automation=NodeAutomation.Manual;Call("ResetInspector");Call("OpenNodeInspector",node);Call("UpdateDomesticSlide",1f);Set("_paperReadTurn",world.Population.Turn);Set("_paperReadCountry",world.PlayerSlot);world.EquipmentMode=RobotEquipmentMode.Materials;Call("OpenNodeFactory");Set("_factoryHarvestPage",false);Set("_factoryPlanLine",0);var point=((Rectangle)typeof(Game1).GetMethod("FactoryAlbumCard",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{0})!).Center;PendingClick(point);Check((int)Get("_factoryPlanLine")==-1,"Human album click did not close plan picker");Check(world.Robots.Workshops[node].ProductionLines[0].SelectedPlan==0,"Human album plan not applied");Set("_factoryPlanLine",0);Call("HandleInput",new MouseState(),new KeyboardState(Keys.Escape),new GameTime(TimeSpan.FromSeconds(101),TimeSpan.FromSeconds(.1)),true);Check((int)Get("_factoryPlanLine")==-1,"Human album Escape did not close");}
   base.Draw(t);if(frame++==1){Console.WriteLine("PASS normal human factory album click selection and Escape via HandleInput");Exit();}return;
  }
  if(PendingAudit){DrawPendingAudit(t);return;}
  if(frame==1){Set("_inspectedNode",-1);} // Office must render its explicit node, independent of region supervisor state.
  if(frame==2){
   Set("_inspectedNode",node);Set("_demoDomesticStage",11);Set("_demoDomesticAge",0d);
   var close=(Rectangle)typeof(Game1).GetProperty("InspectorClose",F)!.GetValue(this)!;
   Call("UpdateDemoDomestic",1.8d);Check(close.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Demo misses inspector close");
   Check((bool)Get("_nodeInspectorClosing"),"Demo did not use the animated close action");Call("UpdateDomesticSlide",1f);
   Check((int)Get("_populationNode")==-1 && (int)Get("_inspectedNode")==-1 && !(bool)Get("_nodeOfficeOpen"),"Demo closes inspectors through the shared reset path");
  }
  if(frame==3){Call("OpenNodeInspector",node);Call("OpenNodeAssembly");Call("UpdateDomesticSlide",1f);Set("_hangarMessage","external \uE000");}
  if(frame==4){Set("_demoDomesticStage",11);Set("_demoDomesticAge",0d);Call("UpdateDemoDomestic",1.8d);Call("UpdateDomesticSlide",1f);}
  if(frame==5){
   Check((int)Get("_demoDomesticStage")==17,"Domestic inspector close leads to my country before movement");
   // Exercise the transport controls independently of the domestic end sequence.
   Set("_demoDomesticStage",12);
   var button=(Rectangle)typeof(Game1).GetProperty("TransportOverviewButton",F)!.GetValue(this)!;
   Call("UpdateDemoDomestic",1.8d);Check(button.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Transport click misses primary action");
   Check((bool)Get("_transportOverview") && !(bool)Get("_nodeOfficeOpen") && !(bool)Get("_nodeTransportOpen"),"Transport primary did not replace the closed office");Call("UpdateDomesticSlide",1f);
  }
  if(frame==6){Call("UpdateDemoDomestic",1.8d);Check((bool)Get("_nodeInspectorClosing"),"Transport close did not animate");Call("UpdateDomesticSlide",1f);}
  if(frame==7){Call("UpdateDemoDomestic",1.8d);Check((bool)Get("_reignView"),"Demo did not open my country before movement");Call("UpdateDomesticSlide",1f);}
  if(frame==8){
   var button=(Rectangle)typeof(Game1).GetField("MovementButton",F|BindingFlags.Static)!.GetValue(null)!;
   var visibleMovement=(Rectangle)Call("InspectorScreenBounds",button);
   Call("UpdateDemoDomestic",1.8d);Check((bool)Get("_movementOpen") && !(bool)Get("_reignView"),"Demo did not open movement through the country inspector");
   Check(visibleMovement.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Movement click misses visible button");
  }
  if(frame==9){
   Console.WriteLine($"Demo movement follow-up: stage={Get("_demoDomesticStage")}, overview={Get("_transportOverview")}, movement={Get("_movementOpen")}");
   var destination=((CubeNet)Get("_net")).MovementTargets.First();
   Call("SelectMoveNode",destination);Set("_demoDomesticStage",15);Set("_demoDomesticAge",0d);
   int orders=world.Campaign.PlannedOrders.Count;
   var confirm=(Rectangle)typeof(Game1).GetField("ConfirmMoveButton",F|BindingFlags.Static)!.GetValue(null)!;
   var visibleConfirm=(Rectangle)Call("InspectorScreenBounds",confirm);
   Call("UpdateDemoDomestic",1.8d);
   Check(visibleConfirm.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Demo movement confirmation misses visible button");
   Check(!(bool)Get("_movementOpen") && world.Campaign.PlannedOrders.Count==orders+1 && world.Campaign.PlannedOrders.Any(o=>o.Peaceful && o.TargetNode==destination),"Demo movement confirmation creates exactly one peaceful reservation");
   Call("ResetInspector");Set("_screen",Enum.Parse(Get("_screen").GetType(),"PlayerCount"));
   Set("_demoActive",true);Set("_demoIntro",false);Set("_demoStepAge",0d);Call("UpdateDemo",2.99d,true,false,false,false,100d);
  }
  if(frame==10){
   Call("UpdateDemo",.02d,true,false,false,false,100.02d);
   Check(Get("_screen").ToString()=="ComputerPlayerCount","Human count bypassed computer count");
   var confirm=(Rectangle)typeof(Game1).GetField("PlayerCountConfirm",F|BindingFlags.Static)!.GetValue(null)!;
   Check(confirm.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Count click misses rendered confirm");
  }
  if(frame>=11){
   int step=(frame-11)/2;int[] plans={3,0,1,2,4,10};
   if(frame is 11 or 23){var g=(GraphicsDeviceManager)Get("_graphics");g.PreferredBackBufferWidth=frame==11?1920:960;g.PreferredBackBufferHeight=frame==11?1080:540;g.ApplyChanges();}
   if((frame-11)%2==0){
    Call("ResetInspector");Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("OpenNodeInspector",node);Call("OpenNodeFactory");Call("UpdateDomesticSlide",1f);
    var line=world.Robots.Workshops[node].ProductionLines[0];line.SelectPlan(plans[step%plans.Length]);
    var expected=(Rectangle)Call("InspectorScreenBounds",Call("FactoryProductionButton",0));Set("_demoDomesticStage",9);Set("_demoDomesticAge",0d);Call("UpdateDemoDomestic",1.8d);
    Check(expected.Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Demo factory click follows actual period and disabled production");Check((int)Get("_factoryPlanLine")==0,"Demo factory click opens plan menu through normal input");
    Console.WriteLine($"factory-plan-before={line.SelectedPlan} period={line.ProductionPeriod} click={Get("_demoClickPoint")} bounds={expected}");
   }else{
    var expected=(Rectangle)typeof(Game1).GetMethod("FactoryPlanButton",F|BindingFlags.Static)!.Invoke(null,new object[]{0})!;Set("_demoDomesticAge",0d);Call("UpdateDemoDomestic",1.8d);
    Check(((Rectangle)Call("InspectorScreenBounds",expected)).Contains(((Vector2)Get("_demoClickPoint")).ToPoint()),"Demo candidate click matches drawn rectangle");
    var line=world.Robots.Workshops[node].ProductionLines[0];Check((int)Get("_factoryPlanLine")==-1 && line.SelectedPlan==0 && line.Manufacturing,"Demo really selects plan zero and resumes production");
    Console.WriteLine($"factory-plan-after={line.SelectedPlan} manufacturing={line.Manufacturing}");
   }
  }
  base.Draw(t);var pp=GraphicsDevice.PresentationParameters;var pixels=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(pixels);using var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"demo-office-{frame}.png"));texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame++==34){Console.WriteLine("PASS: Demo factory current-rectangle opening and real plan selection across six plans at1080p/540p; Demo office, animated close, transport primary, my-country movement, human/computer count, click bounds and actual rendering.");Exit();}
 }
}

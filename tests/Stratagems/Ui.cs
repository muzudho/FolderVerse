using FolderVerse;using System.Reflection;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using Microsoft.Xna.Framework.Input;
using var preview=new Preview();preview.Run();
sealed class Preview:Game1{
 readonly bool fullFlow=Environment.GetEnvironmentVariable("STRATAGEM_FULL_FLOW")=="1";
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame;SpriteBatch batch=null!;UiPainter ui=null!;WorldSetup world=null!;CubeNet net=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);object Call(string n,params object[] values)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,values)!;
 void Check(bool yes,string text){if(!yes)throw new Exception(text);Console.WriteLine("PASS UI "+text);}
 void Click(Point at){Set("_pointer",at);ui.Pointer=at;Check((bool)Call("StratagemInput",new MouseState(),true,false,true,.01d),"click consumed "+at);}
 protected override void LoadContent(){
  if(fullFlow){
   AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("STRATAGEM_FONT"));
   Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("STRATAGEM_FONT")!,"Content");base.LoadContent();Set("_autoSaveEnabled",false);
   world=AutoSaveStore.Read<WorldSetup>(Environment.GetEnvironmentVariable("STRATAGEM_FIXTURE")!);Set("_setup",world);ui=(UiPainter)Get("_ui");net=(CubeNet)Get("_net");net.Home(world);
   ((WorldPreview)Get("_world")).ShowSetup(world,true);Set("_statusGlobe",false);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));Call("RefreshStratagems");return;
  }
  AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("STRATAGEM_ART_BASE"));
  world=AutoSaveStore.Read<WorldSetup>(Environment.GetEnvironmentVariable("STRATAGEM_FIXTURE")!);Set("_setup",world);
  Content.RootDirectory=Environment.GetEnvironmentVariable("STRATAGEM_FONT")!;var font=Content.Load<SpriteFont>("Content/UiFont");font.DefaultCharacter='?';batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});ui=new(batch,pixel,font);Set("_spriteBatch",batch);Set("_pixel",pixel);Set("_font",font);Set("_ui",ui);Set("_statusGlobe",false);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));net=(CubeNet)Get("_net");net.Home(world);Call("RefreshStratagems");
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(fullFlow){DrawFullFlow(time);return;}
  if(frame==0){Set("_pointer",new Point(320,900));ui.Pointer=new(320,900);}
  if(frame==1)Click(new(320,900));
  if(frame==2)Click(new(200,340));
  if(frame==3){Click(new(200,340));for(int step=0;step<20;step++)Call("UpdateDomesticSlide",.05f);Check((bool)Call("StratagemInput",new MouseState(),true,false,true,.01d),"panel input blocked");}
  if(frame==4){Click(new(700,780));Check(world.Campaign.Stratagems.Pending.Count==1&&!world.Campaign.PlannedOrders.Any(),"reserve remains deferred");}
  if(frame==5){Click(new(200,250));Check(world.Campaign.Stratagems.Pending.Count==0,"cancel bundle");}
  if(frame==6){Click(new(150,900));for(int step=0;step<20;step++)Call("UpdateDomesticSlide",.05f);Check((bool)typeof(Game1).GetProperty("StratagemManualActions",F)!.GetValue(this)!,"manual button exposes existing actions");}
  if(frame==7){foreach(var store in world.Robots.Nodes)typeof(RobotStore).GetMethod("Replace",F)!.Invoke(store,new object[]{Array.Empty<Robot>()});Call("RefreshStratagems");Check(((Array)Get("_stratagemMatches")).Length==0 && (bool)typeof(Game1).GetProperty("StratagemManualActions",F)!.GetValue(this)!,"no-match fallback exposes manual actions");}
  if(frame==8){var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=1280;graphics.PreferredBackBufferHeight=720;graphics.ApplyChanges();}
  if(frame==9){world=AutoSaveStore.Read<WorldSetup>(Environment.GetEnvironmentVariable("STRATAGEM_FIXTURE")!);Set("_setup",world);net.Home(world);Call("RefreshStratagems");Click(new(320,900));Click(new(200,340));Click(new(200,340));for(int step=0;step<20;step++)Call("UpdateDomesticSlide",.05f);}
  if(frame==10){foreach(var node in world.Nodes.All.Where(n=>n.Owner==0 && n.Id%17==0).Take(8))node.Owner=1;foreach(var node in world.Nodes.All.Where(n=>n.Owner==0))while(world.Robots.Nodes[node.Id].Count<3)world.Robots.Nodes[node.Id].Add(world.Robots.Create(0,RobotParts.Complete));Call("RefreshStratagems");Click(new(320,900));var groups=(int[])typeof(Game1).GetProperty("StratagemGroups",F)!.GetValue(this)!;Check(groups.Length>6,"two-level list contains multiple target pages");Click(new(800,725));Check((int)Get("_stratagemPage")==1,"next candidate page");}
  if(frame==11){Click(new(200,340));Check((int)Get("_stratagemGroup")>=0 && (int)Get("_stratagemPage")==0,"page selection opens exact target combinations");}
  if(frame==12){var plans=world.Campaign.Stratagems;var first=plans.Find(world,0).First(m=>m.Kind==StratagemKind.WallSortie);Check(plans.Reserve(world,0,first,out _),"first UI pending bundle");var next=plans.Find(world,0).First(m=>m.Kind==first.Kind);Check(plans.Reserve(world,0,next,out _),"second UI pending bundle");long keep=plans.Pending[1].Id;Call("RefreshStratagems");Click(new(200,250));Check((bool)Get("_stratagemReservationsOpen"),"open multiple reservation list");Click(new(200,340));Check(plans.Pending.Single().Id==keep,"cancel chosen first bundle, preserve second");
   var close=(Rectangle)typeof(Game1).GetField("StratagemClose",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Click(close.Center);
   Check((int)Get("_stratagemSelected")<0 && plans.Pending.Single().Id==keep,"close clears selection and preserves reservation");
   Click(new(320,900));Check((int)Get("_stratagemSelected")>=0,"card reopens after close");
   Check((bool)Call("StratagemInput",new MouseState(),false,true,true,.01d),"Escape consumed by card panel");
   Check((int)Get("_stratagemSelected")<0 && plans.Pending.Single().Id==keep,"Escape closes panel and preserves reservation");
   var pending=plans.Pending.Single();var changed=pending.Match.Troops.First(t=>t.Reservation==HangarReservation.None);
   Check(world.Robots.ToggleReservation(changed.Node,changed.Before.Id,HangarReservation.Defense),"manual conflict prepared after reservation");
   var end=(Rectangle)typeof(Game1).GetField("StratagemEnd",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Click(end.Center);
   Check((int)Get("_stratagemSelected")== (int)pending.Match.Kind && ((string)Get("_stratagemMessage")).Length>0,"failed end turn reopens offending card with reason");
   Check(plans.Pending.Single().Id==keep && !world.Campaign.PlannedOrders.Any() && world.Campaign.Phase==CampaignPhase.Domestic,"failed end turn preserves pending bundle without partial orders or phase advance");}
  GraphicsDevice.Clear(new Color(16,34,45));var bounds=(Rectangle)Call("CanvasBounds");var transform=Matrix.CreateScale(bounds.Width/1920f,bounds.Height/1080f,1)*Matrix.CreateTranslation(bounds.X,bounds.Y,0);batch.Begin(transformMatrix:transform);
  ui.Text("内政フェーズ / 計略操作の隔離検証",new(54,60),.75f,new Color(255,237,197));
  var panel=(Rectangle)typeof(Game1).GetProperty("NetPanel",F)!.GetValue(this)!;net.Draw(ui,world,panel,-1,0,false);Call("DrawPrimaryActionButtonsCore");Call("DrawStratagemInterface");batch.End();
  var pp=GraphicsDevice.PresentationParameters;var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(data);using(var file=File.Create(Path.Combine(Environment.GetEnvironmentVariable("STRATAGEM_OUTPUT")!,$"ui-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame++==12){Console.WriteLine("PASS actual card UI, grouped selection, preview, reserve/cancel, manual/fallback and 1280x720 draw");Exit();}
 }
 void DrawFullFlow(GameTime time){
  if(frame==0)Click(new(320,900));
  if(frame==1)Click(new(200,340));
  if(frame==2){Click(new(200,340));Call("UpdateDomesticSlide",1f);}
  if(frame==3){Click(new(700,780));Check(world.Campaign.Stratagems.Pending.Count==1 && !world.Campaign.PlannedOrders.Any(),"full load reserve remains deferred");}
  if(frame==4){var end=(Rectangle)typeof(Game1).GetField("StratagemEnd",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;Click(end.Center);
   Check(!world.Campaign.Stratagems.Pending.Any(),"end button commits pending bundle");
   Check(world.Campaign.PlannedOrders.Any(),"end button creates march orders");
   Console.WriteLine($"FULL FLOW phase={world.Campaign.Phase} screen={Get("_screen")} subordinate={Get("_subordinateNodes")!=null} cpu={Get("_computerDomesticNodes")!=null}");
  }
  if(frame==5 && world.Campaign.Phase==CampaignPhase.Delivery){Call("FinishTransport");Check(world.Campaign.RobotBattles.LastMarches.Any(),"committed sortie executes after delivery");Console.WriteLine($"FULL FLOW after delivery phase={world.Campaign.Phase} screen={Get("_screen")}");}
  if(frame==6){Check(((BattleState[])Get("_robotScenes")).Length>0,"committed card produces replay scenes");Call("FinishTransport");Check(Get("_screen").ToString()=="Battle","second transport enters battle replay");}
  if(frame==7){for(int tick=0;tick<2000 && !(bool)Get("_robotSummary");tick++)Call("RobotBattleInput",100d,false,true);
   Check((bool)Get("_robotSummary"),"battle replay reaches result summary through input");}
  base.Draw(time);
  if(frame==7){var pp=GraphicsDevice.PresentationParameters;var data=new Color[pp.BackBufferWidth*pp.BackBufferHeight];GraphicsDevice.GetBackBufferData(data);using var texture=new Texture2D(GraphicsDevice,pp.BackBufferWidth,pp.BackBufferHeight);texture.SetData(data);using var file=File.Create(Path.Combine(Environment.GetEnvironmentVariable("STRATAGEM_OUTPUT")!,"full-flow-summary.png"));texture.SaveAsPng(file,texture.Width,texture.Height);}
  if(frame++==7){Console.WriteLine("PASS full LoadContent card reservation/end turn/delivery/battle summary route");Exit();}
 }

}

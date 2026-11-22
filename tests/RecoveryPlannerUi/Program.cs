using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
using var game=new RecoveryUi(args[0]);game.Run();
sealed class RecoveryUi(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;SpriteBatch batch=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Click(int x,int y){Set("_pointer",new Point(x,y));Call("RecoveryPlannerInput",true,false);}
 void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Salvage.Reset();w.Salvage.EnableExpeditions(w);foreach(var n in w.Nodes.All)n.Owner=0;int h=w.Nodes.All.First(n=>n.Functional).Id;w.ConquerorLocations[0]=w.Nodes.All[h].Cell;w.ConquerorPoints[0]=w.Nodes.All[h].Center;
 foreach(var material in Enum.GetValues<RobotMaterial>()){var robot=w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=(RobotMaterial)(((int)material+1)%9),LegsMaterial=material};w.Robots.Nodes[h].Add(robot);var good=w.Robots.Create(1,RobotParts.Complete) with{HeadMaterial=material,BodyMaterial=material,LegsMaterial=material,IntegratedParts=(int)material%2==0?RobotParts.Head|RobotParts.Body:0};w.Salvage.Items.Add(new(1000+(int)material,h,good));}
 Set("_setup",w);batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");Set("_portraitRenderer",new PortraitRenderer(Content));var fonts=new Microsoft.Xna.Framework.Content.ContentManager(Services,Path.Combine(Environment.GetEnvironmentVariable("PROBE_FONT")!,"Content"));var font=fonts.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",Activator.CreateInstance(typeof(Game1).GetField("_ui",F)!.FieldType,batch,pixel,font)!);Call("OpenSalvagePlan",-1);Check((bool)Get("_recoveryOpen")&&(int)Get("_recoveryStage")==0,"primary salvage opens worker-first full planner");Click(800,250);Check((long)Get("_recoveryWorker")>0,"choose real complete worker");}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
 using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Black);batch.Begin();Call(frame==7?"DrawPocket":"DrawRecoveryPlanner");batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"recovery-"+frame+".png")))image.SaveAsPng(file,1920,1080);
 switch(frame){case 0:Click(470,980);Check((int)Get("_recoveryStage")==1,"next opens filters");Click(470,375);break;
 case 1:Click(470,980);Check((int)Get("_recoveryStage")==2,"filters open catalog");Click(800,250);Check(((System.Collections.Generic.HashSet<RecoveryTarget>)Get("_recoveryTargets")).Count==1,"catalog selects target");break;
 case 2:Click(1100,50);Check((bool)Get("_recoveryMap"),"catalog map toggle");break;
 case 3:Click(1100,50);Click(470,980);Check((int)Get("_recoveryStage")==3,"target selection opens return and loan confirmation");break;
 case 4:Click(470,980);Check((int)Get("_recoveryStage")==4&&w.Salvage.Missions.Count==1&&w.Salvage.LoanCount(0)==2,"dispatch click creates cancellable reservation with two loan slots");break;
 case 5:Click(850,250);Check((int)Get("_recoveryStage")==5,"status opens full cargo targets and history");break;
 case 6:Set("_recoveryOpen",false);Call("SyncPocket");Check((bool)Call("RecoveryLoanSlot",0,11),"pocket loan slot is decorated and blocked");break;
 case 7:Check(w.Salvage.Missions.Single().Pending,"viewing screens does not dispatch before turn end");Console.WriteLine("PASS 8 UI screenshots, navigation and pocket loan markers");Exit();return;}
 frame++;
 }
}
using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
using var game=new RecoveryUi(args[0]);game.Run();
sealed class RecoveryUi(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;SpriteBatch batch=null!;int frame;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Click(int x,int y){Set("_pointer",new Point(x,y));Call("RecoveryPlannerInput",true,false);}
 void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Salvage.Reset();w.Salvage.EnableExpeditions(w);foreach(var n in w.Nodes.All)n.Owner=0;int h=w.Nodes.All.First(n=>n.Functional).Id;w.ConquerorLocations[0]=w.Nodes.All[h].Cell;w.ConquerorPoints[0]=w.Nodes.All[h].Center;
 foreach(var material in Enum.GetValues<RobotMaterial>()){var robot=w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=(RobotMaterial)(((int)material+1)%9),LegsMaterial=material};w.Robots.Nodes[h].Add(robot);var good=w.Robots.Create(1,RobotParts.Complete) with{HeadMaterial=material,BodyMaterial=material,LegsMaterial=material,IntegratedParts=(int)material%2==0?RobotParts.Head|RobotParts.Body:0};w.Salvage.Items.Add(new(1000+(int)material,h,good));}
 Set("_setup",w);batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");Set("_portraitRenderer",new PortraitRenderer(Content));var fonts=new Microsoft.Xna.Framework.Content.ContentManager(Services,Path.Combine(Environment.GetEnvironmentVariable("PROBE_FONT")!,"Content"));var font=fonts.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",Activator.CreateInstance(typeof(Game1).GetField("_ui",F)!.FieldType,batch,pixel,font)!);Call("OpenSalvagePlan",-1);Check((bool)Get("_recoveryOpen")&&(int)Get("_recoveryStage")==4,"primary salvage opens recovery status first");}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
 using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawRecoveryPlanner");batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"recovery-"+frame+".png")))image.SaveAsPng(file,1920,1080);
 switch(frame){
 case 0:Click(1700,1020);Check((int)Get("_recoveryStage")==0&&!(bool)Get("_recoveryLocation"),"new team opens source map");break;
 case 1:var net=(CubeNet)Get("_recoveryNet");var at=net.AnimatedNodePosition(w,w.Nodes.Current(0)!,new(670,350,1230,570));Click((int)at.X,(int)at.Y);Check((bool)Get("_recoveryLocation"),"source map click opens local hangar");break;
 case 2:Click(800,400);Check((long)Get("_recoveryWorker")>0,"hangar selects eligible worker");Click(1700,1020);Check((int)Get("_recoveryStage")==1,"confirm opens questions");break;
 case 3:Click(900,230);Check((int)Get("_recoveryQuestion")==1,"specific request opens shape checks");break;
 case 4:Click(1700,1020);Check((int)Get("_recoveryQuestion")==2,"shape proceeds to material checks");break;
 case 5:Click(1700,1020);Check((int)Get("_recoveryQuestion")==3,"material proceeds to reachability question");break;
 case 6:Click(900,230);Check((int)Get("_recoveryQuestion")==4,"reachability proceeds to damage question");break;
 case 7:Click(900,230);Check((int)Get("_recoveryStage")==2&&(bool)Get("_recoveryMap"),"questions open target map first");break;
 case 8:var targetNet=(CubeNet)Get("_recoveryNet");var targetAt=targetNet.AnimatedNodePosition(w,w.Nodes.Current(0)!,new(670,350,1230,570));Click((int)targetAt.X,(int)targetAt.Y);Check(!(bool)Get("_recoveryMap"),"target map click opens local catalog");break;
 case 9:Click(800,400);Check(((System.Collections.Generic.HashSet<RecoveryTarget>)Get("_recoveryTargets")).Count==1,"catalog selects target");Click(1700,1020);Check((int)Get("_recoveryStage")==3,"target opens confirmation");break;
 case 10:Click(1700,1020);Check((int)Get("_recoveryStage")==4&&w.Salvage.Missions.Count==1&&w.Salvage.LoanCount(0)==2,"dispatch reserves worker and cargo slots");break;
 case 11:Click(850,420);Check((int)Get("_recoveryStage")==5,"status opens mission detail");break;
 case 12:
 Check(w.Salvage.Missions.Single().Pending,"viewing screens does not dispatch before turn end");
 Click(800,50);Click(1700,1020);Call("SelectRecoveryLocation",w.Nodes.Current(0)!.Id,false);
 Click(800,400);Check((long)Get("_recoveryWorker")==-1,"reserved worker cannot be selected again");
 for(int i=1;i<=5;i++)Click(700+i%4*295,365+i/4*185);
 Check(((List<long>)Get("_recoveryWorkers")).Count==5,"five additional workers selectable within six active teams");
 Click(700+6%4*295,365+6/4*185);Check(((List<long>)Get("_recoveryWorkers")).Count==5,"six active team limit blocks seventh worker");
 var targets=(HashSet<RecoveryTarget>)Get("_recoveryTargets");foreach(var item in w.Salvage.Items.Skip(1).Take(5))targets.Add(new(item.Id,item.Robot.Parts));
 Set("_recoveryStage",3);Click(1700,1020);Check(w.Salvage.Missions.Count(m=>!m.Finished)==6&&w.Salvage.Missions.SelectMany(m=>m.Targets).Distinct().Count()==6,"batch dispatch creates distinct targets across all six teams");
 Console.WriteLine("PASS recovery right pane flow, source and target map clicks, six worker cap");Exit();return;}
 frame++;
 }
}
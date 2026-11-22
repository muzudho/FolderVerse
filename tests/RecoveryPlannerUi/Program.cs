using FolderVerse;using Microsoft.Xna.Framework;using Microsoft.Xna.Framework.Graphics;using System.Reflection;
using var game=new RecoveryUi(args[0]);game.Run();
sealed class RecoveryUi(string output):Game1{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;WorldSetup w=null!;SpriteBatch batch=null!;int frame,home,goal;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;void Set(string n,object v)=>typeof(Game1).GetField(n,F)!.SetValue(this,v);object Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,a)!;
 void Click(int x,int y){Set("_pointer",new Point(x,y));Call("RecoveryPlannerInput",true,false);}
 void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
 protected override void LoadContent(){Window.Position=new(-3000,-3000);AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));w=new(){EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Salvage.Reset();w.Salvage.EnableExpeditions(w);foreach(var n in w.Nodes.All)n.Owner=0;int h=w.Nodes.All.First(n=>n.Functional).Id;home=h;goal=w.Nodes.All.Last(n=>n.Functional&&n.Id!=h&&w.Cells[n.Cell].Face==w.Cells[w.Nodes.All[h].Cell].Face).Id;w.ConquerorLocations[0]=w.Nodes.All[h].Cell;w.ConquerorPoints[0]=w.Nodes.All[h].Center;
 foreach(var material in Enum.GetValues<RobotMaterial>()){var robot=w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=(RobotMaterial)(((int)material+1)%9),LegsMaterial=material};w.Robots.Nodes[h].Add(robot);var good=w.Robots.Create(1,RobotParts.Complete) with{HeadMaterial=material,BodyMaterial=material,LegsMaterial=material,IntegratedParts=(int)material%2==0?RobotParts.Head|RobotParts.Body:0};w.Salvage.Items.Add(new(1000+(int)material,h,good));}
 Set("_setup",w);batch=new(GraphicsDevice);var pixel=new Texture2D(GraphicsDevice,1,1);pixel.SetData(new[]{Color.White});Set("_spriteBatch",batch);Set("_pixel",pixel);Content.RootDirectory=Path.Combine(Environment.GetEnvironmentVariable("OLD_RUNTIME")!,"Content");Set("_portraitRenderer",new PortraitRenderer(Content));var fonts=new Microsoft.Xna.Framework.Content.ContentManager(Services,Path.Combine(Environment.GetEnvironmentVariable("PROBE_FONT")!,"Content"));var font=fonts.Load<SpriteFont>("UiFont");Set("_font",font);Set("_ui",Activator.CreateInstance(typeof(Game1).GetField("_ui",F)!.FieldType,batch,pixel,font)!);Call("OpenSalvagePlan",-1);Check((bool)Get("_recoveryOpen")&&(int)Get("_recoveryStage")==4,"primary salvage opens recovery status first");}
 Point MapPoint(int id){object[] args={id,Vector2.Zero};Check((bool)Call("RecoveryMapPosition",args),"map point is front-facing "+id);return ((Vector2)args[1]).ToPoint();}
 protected override void Update(GameTime t){}
 protected override void Draw(GameTime t){
 using var image=new RenderTarget2D(GraphicsDevice,1920,1080);GraphicsDevice.SetRenderTarget(image);GraphicsDevice.Clear(Color.Black);batch.Begin();Call("DrawRecoveryPlanner");batch.End();GraphicsDevice.SetRenderTarget(null);using(var file=File.Create(Path.Combine(output,"recovery-"+frame+".png")))image.SaveAsPng(file,1920,1080);
 switch(frame){
 case 0:Click(1700,1020);Check((int)Get("_recoveryStage")==6&&!(bool)Get("_recoveryLocation"),"first question chooses receiving hangar");break;
 case 1:Click(1400,50);Check((bool)Get("_recoveryGlobe"),"receiving map can switch to globe");break;
 case 2:var receiving=MapPoint(goal);Click(receiving.X,receiving.Y);Check((int)Get("_recoveryDestination")==goal&&(bool)Get("_recoveryLocation"),"globe click opens receiving hangar");break;
 case 3:Click(800,400);Check(((List<long>)Get("_recoveryWorkers")).Count==0&&(int)Get("_recoverySource")==-1,"receiving hangar is inspection only");Click(1700,1020);Check((int)Get("_recoveryStage")==1,"receiving hangar proceeds to desired goods");break;
 case 4:Click(900,230);Check((int)Get("_recoveryQuestion")==1,"specific goods opens shape selection");break;
 case 5:Click(1700,1020);Check((int)Get("_recoveryQuestion")==2,"shapes proceed to materials");break;
 case 6:Click(1700,1020);Check((int)Get("_recoveryQuestion")==4,"materials proceed to condition before choosing source");break;
 case 7:Click(900,230);Check((int)Get("_recoveryStage")==0&&!(bool)Get("_recoveryLocation"),"goods conditions precede worker source question");break;
 case 8:Click(1400,50);Check(!(bool)Get("_recoveryGlobe"),"worker map can switch to net");break;
 case 9:var net=(CubeNet)Get("_recoveryNet");var saved=net.CaptureView();var point=MapPoint(home);float small=(float)Call("RecoverySpotlightRadius",home,point.ToVector2());net.ZoomAt(new(670,350,1230,570),point,2400);float large=(float)Call("RecoverySpotlightRadius",home,point.ToVector2());Check(large>small&&small>=32,"spotlight grows with map zoom rather than fixed clipped radius");net.RestoreView(saved);var source=MapPoint(home);Click(source.X,source.Y);Check((int)Get("_recoverySource")==home&&home!=goal,"source can differ from receiving destination");break;
 case 10:Click(800,400);Click(1700,1020);Check((int)Get("_recoveryStage")==1&&(int)Get("_recoveryQuestion")==3&&(int)Get("_recoveryDestination")==goal&&(int)Get("_recoverySource")==home,"reachability question waits until both endpoints and worker are chosen");break;
 case 11:Click(900,230);Check((int)Get("_recoveryStage")==2&&(bool)Get("_recoveryMap"),"reachable-only selection opens target map");break;
 case 12:Click(1400,50);Check((bool)Get("_recoveryGlobe"),"target map supports globe too");break;
 case 13:var target=MapPoint(home);Click(target.X,target.Y);Check(!(bool)Get("_recoveryMap"),"globe target opens catalog");break;
 case 14:Click(800,400);Click(1700,1020);Check((int)Get("_recoveryStage")==3,"chosen goods proceed to confirmation");break;
 case 15:Click(1700,1020);Check((int)Get("_recoveryStage")==4&&w.Salvage.Missions.Count==1&&w.Salvage.Missions[0].DeliveryNode==goal&&w.Salvage.Missions[0].Source==home,"dispatch reserves independent source and receiving hangar");break;
 case 16:Click(850,420);Check((int)Get("_recoveryStage")==5,"status opens mission detail");break;
 case 17:
 Check(w.Salvage.Missions.Single().Pending,"screens never dispatch before turn end");
 int loans=w.Salvage.LoanCount(0);foreach(var q in new[]{(6,0),(1,0),(1,1),(1,2),(1,4),(0,0),(1,3),(2,0),(3,0)}){
 Set("_recoveryStage",q.Item1);Set("_recoveryQuestion",q.Item2);((List<long>)Get("_recoveryWorkers")).Add(999);((HashSet<RecoveryTarget>)Get("_recoveryTargets")).Add(new(999,RobotParts.Head));Click(800,1020);
 Check((int)Get("_recoveryStage")==4&&((List<long>)Get("_recoveryWorkers")).Count==0&&((HashSet<RecoveryTarget>)Get("_recoveryTargets")).Count==0&&w.Salvage.LoanCount(0)==loans&&w.Salvage.Missions.Single().Pending,"cancel discards only draft stage "+q.Item1+" question "+q.Item2);
 }
 Set("_recoveryStage",1);Call("RecoveryPlannerInput",false,true);Check((bool)Get("_recoveryOpen")&&(int)Get("_recoveryStage")==4,"escape also cancels draft to status");
 Call("SelectRecoveryDestination",goal);Set("_recoveryStage",0);Call("SelectRecoveryLocation",home,false);
 Click(800,400);Check((long)Get("_recoveryWorker")==-1,"reserved worker cannot be chosen again");
 for(int i=1;i<=5;i++)Click(700+i%4*295,365+i/4*185);
 Check(((List<long>)Get("_recoveryWorkers")).Count==5,"five additional workers selectable under six-team limit");
 Click(700+6%4*295,365+6/4*185);Check(((List<long>)Get("_recoveryWorkers")).Count==5,"seventh active worker blocked");
 var targets=(HashSet<RecoveryTarget>)Get("_recoveryTargets");foreach(var item in w.Salvage.Items.Skip(1).Take(5))targets.Add(new(item.Id,item.Robot.Parts));
 Set("_recoveryStage",3);Click(1700,1020);Check(w.Salvage.Missions.Count(m=>!m.Finished)==6&&w.Salvage.Missions.All(m=>m.DeliveryNode==goal)&&w.Salvage.Missions.SelectMany(m=>m.Targets).Distinct().Count()==6,"six independent missions retain receiving hangar and distinct goods");
 Console.WriteLine("PASS recipient-first flow, delayed reachability, both maps, draft cancellation and six teams");Exit();return;}
 frame++;
 }
}
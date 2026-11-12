using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
sealed class WorkerPlansUi:Game1
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame,node,choice;long workerId;WorldSetup w=null!;RobotWorkshop line=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 Rectangle Rect(string n,params object[] args)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
 void Click(Point point){
  var canvas=(Rectangle)Call("CanvasBounds");var screen=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);
  foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(screen.X,screen.Y,0,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
 }
 protected override void LoadContent(){
  Content.RootDirectory=Path.Combine(AppContext.BaseDirectory,"Content");base.LoadContent();
  choice=-1;
  for(int seed=0;seed<40 && choice<0;seed++){
   (w,node)=Fixture.Create(123+37*seed);var r=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1,BodyOrigin=2,CheapParts=RobotParts.Head};workerId=r.Id;line=Fixture.Appoint(w,node,r);
   choice=Fixture.Options.FirstOrDefault(i=>{var preview=line.PlanAt(i).Preview(0);return (preview.Parts&(RobotParts.Head|RobotParts.Body))==(RobotParts.Head|RobotParts.Body) && preview.IsCheap(RobotParts.Head) && !preview.IsCheap(RobotParts.Body) && (preview.HeadOrigin==1 || preview.BodyOrigin==2);},-1);
  }
  Check(choice>=0,"Fixture includes visible foreign colors and mixed quality");
  Set("_setup",w);Set("_conquestOverviewEnabled",false);Set("_conquestOverview",false);Set("_lastCutInPhase","内政フェーズ");Set("_cutInStarted",-10d);Set("_screen",Enum.Parse(Get("_screen").GetType(),"WorldStatus"));
  ((WorldPreview)Get("_world")).ShowSetup(w,true);((CubeNet)Get("_net")).Home(w);
  Call("OpenNodeInspector",node);Call("OpenNodeFactory");Call("UpdateDomesticSlide",1f);line.SelectPlan(3);
 }
 protected override void Update(GameTime time){}
 protected override void Draw(GameTime time){
  if(frame==1){Click(Rect("FactoryRobotButton",1,1).Center);Check((int)Get("_factoryPlanLine")==1,"Real factory click opens the additional worker lane catalogue");Set("_pointer",Rect("FactoryPlanButton",choice).Center);}
  if(frame==2){Click(Rect("FactoryPlanButton",choice).Center);Check(line.SelectedPlan==choice && line.CurrentProductionPlan==line.PlanAt(choice),"Candidate click installs the exact per-part recipe");var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  if(frame==3){line.SelectPlan(3);Click(Rect("FactoryRobotButton",1,1).Center);Check((int)Get("_factoryPlanLine")==1,"Half-size real click opens the worker catalogue");Set("_pointer",Rect("FactoryPlanButton",choice).Center);}
  if(frame==4){
   Click(Rect("FactoryPlanButton",choice).Center);Check(line.SelectedPlan==choice,"Half-size real click selects the expected mixed plan");var recipe=line.PlanAt(choice);
   // This fixture advances only industry; use immediate storage instead of the domestic intake queue.
   w.Robots.ProductionIntakeEnabled=false;
   for(int turn=0;turn<recipe.Turns;turn++)w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
   Fixture.Same(recipe,w.Robots.Nodes[node].Robots.Single(r=>r.Id!=workerId));Call("OpenNodeAssembly");
  }
  if(frame==5){
   w.EquipmentMode=RobotEquipmentMode.Materials;
   typeof(RobotWorkshop).GetMethod("ConfigureMaterialCatalogue",F)!.Invoke(line,new object[]{0,w.EquipmentMode});
   Check(line.PlanCount==11,"Material album must include all eleven choices");
   Call("OpenNodeFactory");Click(Rect("FactoryRobotButton",1,line.ProductionPeriod).Center);
   Check((int)Get("_factoryPlanLine")==1,"Worker lane opens material album at 540p");
   Check(Rect("FactoryAlbumCard",5).Y==Rect("FactoryAlbumCard",0).Y &&
         Rect("FactoryAlbumCard",6).X==Rect("FactoryAlbumCard",0).X,"Album uses six columns");
  }
  if(frame==6){
   Click(Rect("FactoryAlbumCard",10).Center);
   Check(line.SelectedPlan==10 && (int)Get("_factoryPlanLine")==-1,"Right-side album click selects the exact plan");
   Click(Rect("FactoryRobotButton",1,line.ProductionPeriod).Center);
   Click(Rect("FactoryAlbumCard",3).Center);
   Check(!line.Manufacturing && (int)Get("_factoryPlanLine")==-1,"Album can turn production off");
  }
  base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"worker-plans-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame++==6){Console.WriteLine("PASS: legacy catalogue and 540p material album clicks, exact worker recipes, and stopping production.");Exit();}
 }
}

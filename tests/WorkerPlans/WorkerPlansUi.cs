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
   typeof(RobotWorld).GetMethod("InitializeMapleSupply",F)!.Invoke(w.Robots,new object[]{w});
   typeof(RobotWorkshop).GetMethod("ConfigureMaterialCatalogue",F)!.Invoke(line,new object[]{0,w.EquipmentMode});
   var recipes=(ProductionPlan[])typeof(RobotWorkshop).GetField("_workerPlans",F)!.GetValue(line)!;
   recipes[choice]=recipes[choice] with {HeadMaterial=RobotMaterial.Foam,BodyMaterial=RobotMaterial.Maple};
   var recipe=AutoSaveStore.Restore<ProductionPlan>(AutoSaveStore.Capture(line.PlanAt(choice)));
   Check(recipe.Preview(0).HeadMaterial==RobotMaterial.Foam && recipe.Preview(0).BodyMaterial==RobotMaterial.Maple,
     "Mixed material recipe survives save and previews each part");
   Check(line.PlanCount==11,"Material album must include all eleven choices");
   Call("OpenNodeFactory");Click(Rect("FactoryRobotButton",1,line.ProductionPeriod).Center);
   Check((int)Get("_factoryPlanLine")==1,"Worker lane opens material album at 540p");
   Check(Rect("FactoryAlbumCard",5).Y==Rect("FactoryAlbumCard",0).Y &&
         Rect("FactoryAlbumCard",6).X==Rect("FactoryAlbumCard",0).X,"Album uses six columns");
   Set("_pointer",Rect("FactoryRecipeCard",choice).Center);
  }
  if(frame==6){
   var upperRight=Rect("FactoryAlbumCard",5);
   Click(new Point(upperRight.Right-8,upperRight.Bottom-8));
   Check(line.SelectedPlan==6 && (int)Get("_factoryPlanLine")==-1,"Upper-right card selects from its lower corner");
   Click(Rect("FactoryRobotButton",1,line.ProductionPeriod).Center);
   Check((int)Get("_factoryPlanLine")==1,"Reopen after selecting the upper-right card");
   Click(Rect("FactoryRecipeCard",10).Center);
   Check(line.SelectedPlan==10 && (int)Get("_factoryPlanLine")==-1,"Right-side album click selects the exact plan");
   Click(Rect("FactoryRobotButton",1,line.ProductionPeriod).Center);
   Click(Rect("FactoryAlbumAction",0).Center);
   Check((bool)Get("_factoryHarvestPage") && (int)Get("_factoryPlanLine")==1,"Harvest button opens the harvest catalogue");
   Click(Rect("FactoryAlbumAction",0).Center);
   Check(!(bool)Get("_factoryHarvestPage") && (int)Get("_factoryPlanLine")==1,"Harvest catalogue returns to the manufacturing cards");
   Click(Rect("FactoryAlbumAction",1).Center);
   Check(!line.Manufacturing && (int)Get("_factoryPlanLine")==-1,"Album can turn production off");
  }
  if(frame==7){
   var basic=w.Robots.Workshops[node].ProductionLines[0];
   typeof(RobotWorkshop).GetMethod("ConfigureMaterialCatalogue",F)!.Invoke(basic,new object[]{0,w.EquipmentMode});
   int split=Array.FindIndex(basic.Plans,plan=>plan.Product==RobotParts.Complete && plan.IntegratedParts==0);
   Check(split>=0,"The base lane contains a split complete recipe");
   basic.Plans[split]=basic.Plans[split] with {HeadMaterial=RobotMaterial.Foam,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Silicone};
   basic.SelectPlan(3);
   Click(Rect("FactoryRobotButton",0,1).Center);
   var mixed=basic.PlanAt(split).Preview(0);
   Check((int)Get("_factoryPlanLine")==0 && mixed.Parts==RobotParts.Complete && mixed.IntegratedParts==0 &&
    new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Select(mixed.Material).Distinct().Count()==3,
    "The base lane shows three separate parts with distinct materials");
   Set("_pointer",Rect("FactoryRecipeCard",split).Center);
  }
  if(frame==8){
   var basic=w.Robots.Workshops[node].ProductionLines[0];
   int split=Array.FindIndex(basic.Plans,plan=>plan.Product==RobotParts.Complete && plan.IntegratedParts==0);
   basic.Plans[split]=basic.Plans[split] with {
    Turns=65,HeadMaterial=RobotMaterial.Polypropylene,LegsMaterial=RobotMaterial.Polypropylene,
    HeadOrigin=1,BodyOrigin=2,LegsOrigin=0};
   var mixed=basic.PlanAt(split).Preview(0);
   Check(mixed.HeadOrigin==1 && mixed.BodyOrigin==2 && mixed.LegsOrigin==0,
    "Narrow album fixture previews three different part origins");
   Check(basic.PlanAt(split).Turns>=30 && basic.PlanAt(split).Turns%3!=0,"Narrow album fixture has a two-digit mixed-number duration");
   var graphics=(GraphicsDeviceManager)Get("_graphics");
   graphics.PreferredBackBufferWidth=800;graphics.PreferredBackBufferHeight=450;graphics.ApplyChanges();
   Set("_pointer",Rect("FactoryRecipeCard",split).Center);
  }
  if(frame==9){
   var robot=w.Robots.Create(0,RobotParts.Complete) with {
    HeadOrigin=1,BodyOrigin=2,LegsOrigin=0,
    HeadMaterial=RobotMaterial.Foam,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Silicone};
   w.Robots.Nodes[node].Add(robot);
   w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
   Check(w.Robots.Nodes[node].Robots.Single(r=>r.Id==robot.Id)==robot,"Reload preserves the three-origin hangar robot");
   Set("_setup",w);Set("_factoryPlanLine",-1);
   Call("OpenNodeInspector",node);Call("OpenNodeAssembly");Call("UpdateDomesticSlide",1f);
   Set("_hangarIntroStart",float.NegativeInfinity);
   var slots=(long?[])Get("_hangarSlots");
   int slot=Array.IndexOf(slots,(long?)robot.Id);
   Check(slot>=0,"Restored mixed robot has a visible hangar slot");
   Set("_pointer",((Rectangle)Call("AssemblyPartBounds",slot,0)).Center);
  }
  base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"worker-plans-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame++==9){Console.WriteLine("PASS: material album, split base recipes, narrow album and restored mixed hangar rendering.");Exit();}
 }
}

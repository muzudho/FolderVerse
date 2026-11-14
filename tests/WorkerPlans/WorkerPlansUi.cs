using FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
sealed class WorkerPlansUi:Game1
{
 Color[] workerFlagBaseline=null!;
 Color[] workerLowerBaseline=null!;
 Color[] workerBodyBaseline=null!;
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;int frame,node,choice;long workerId;WorldSetup w=null!;RobotWorkshop line=null!;
 object Get(string n)=>typeof(Game1).GetField(n,F)!.GetValue(this)!;
 void Set(string n,object value)=>typeof(Game1).GetField(n,F)!.SetValue(this,value);
 object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,F)!.Invoke(this,args)!;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 Rectangle Rect(string n,params object[] args)=>(Rectangle)typeof(Game1).GetMethod(n,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args)!;
 void Click(Point point){
  var canvas=(Rectangle)Call("CanvasBounds");var screen=new Point(canvas.X+point.X*canvas.Width/1920,canvas.Y+point.Y*canvas.Height/1080);
  int wheel=((MouseState)Get("_previousMouse")).ScrollWheelValue;
  foreach(var state in new[]{ButtonState.Released,ButtonState.Pressed,ButtonState.Released})Call("HandleInput",new MouseState(screen.X,screen.Y,wheel,state,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
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
  if(frame==1){Click(((Rectangle)Call("FactoryProductionButton",1)).Center);Check((int)Get("_factoryPlanLine")==1,"Real factory click opens the additional worker lane catalogue");Set("_pointer",Rect("FactoryPlanButton",choice).Center);}
  if(frame==2){Click(Rect("FactoryPlanButton",choice).Center);Check(line.SelectedPlan==choice && line.CurrentProductionPlan==line.PlanAt(choice),"Candidate click installs the exact per-part recipe");var graphics=(GraphicsDeviceManager)Get("_graphics");graphics.PreferredBackBufferWidth=960;graphics.PreferredBackBufferHeight=540;graphics.ApplyChanges();}
  if(frame==3){line.SelectPlan(3);Click(((Rectangle)Call("FactoryProductionButton",1)).Center);Check((int)Get("_factoryPlanLine")==1,"Half-size real click opens the worker catalogue");Set("_pointer",Rect("FactoryPlanButton",choice).Center);}
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
   Call("OpenNodeFactory");Click(((Rectangle)Call("FactoryProductionButton",1)).Center);
   Check((int)Get("_factoryPlanLine")==1,"Worker lane opens material album at 540p");
   Check(Rect("FactoryAlbumCard",5).Y==Rect("FactoryAlbumCard",0).Y &&
         Rect("FactoryAlbumCard",6).X==Rect("FactoryAlbumCard",0).X,"Album uses six columns");
   Set("_pointer",Rect("FactoryRecipeCard",choice).Center);
  }
  if(frame==6){
   var upperRight=Rect("FactoryAlbumCard",5);
   Click(new Point(upperRight.Right-8,upperRight.Bottom-8));
   Check(line.SelectedPlan==6 && (int)Get("_factoryPlanLine")==-1,"Upper-right card selects from its lower corner");
   Click(((Rectangle)Call("FactoryProductionButton",1)).Center);
   Check((int)Get("_factoryPlanLine")==1,"Reopen after selecting the upper-right card");
   Click(Rect("FactoryRecipeCard",10).Center);
   Check(line.SelectedPlan==10 && (int)Get("_factoryPlanLine")==-1,"Right-side album click selects the exact plan");
   Click(((Rectangle)Call("FactoryProductionButton",1)).Center);
   Click(Rect("FactoryAlbumAction",0).Center);
   Check(!(bool)Get("_factoryHarvestPage") && (int)Get("_factoryPlanLine")==1 && line.SelectedPlan==10,
    "The former harvest button area does not change the catalogue or recipe");
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
   Click(((Rectangle)Call("FactoryProductionButton",0)).Center);
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
  if(frame==10){
   var deposits=(int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(w.Robots)!;
   deposits[node]=1;
   var raw=(int[][])typeof(RobotWorld).GetField("_rawDeposits",F)!.GetValue(w.Robots)!;
   raw[(int)RawMaterial.Vinyl-1][node]=0;
   Set("_nodeFactoryOpen",true);Set("_nodeAssemblyOpen",false);
   Set("_factoryPlanLine",0);Set("_factoryHarvestPage",true);
   Click(Rect("HarvestAlbumCard",2).Center);
   Check((bool)Get("_factoryHarvestPage") && (int)Get("_factoryPlanLine")==0 && w.Robots.Workshops[node].ProductionLines[0].MapleRemaining==0,
    "The harvest warning card stays open and an over-limit click does not schedule collection");
   var cards=Enumerable.Range(0,5).Select(i=>Rect("HarvestAlbumCard",i)).ToArray();
   Check(cards.All(card=>card.Y==cards[0].Y && !card.Intersects(Rect("HarvestAlbumAction",1))) &&
     cards.Zip(cards.Skip(1)).All(pair=>pair.First.Right<pair.Second.Left),"Five columns and separate controls");
   Click(new Point(1700,75));
   Check((int)Get("_factoryPlanLine")==-1 && !(bool)Get("_nodeAssemblyOpen"),"Outside click closes the modal without changing inspector tabs");
   Set("_factoryPlanLine",0);
  }
  if(frame==11){
   var factory=w.Robots.Workshops[node];
   Set("_factoryPlanLine",-1);
   var holidayCanvas=(Rectangle)Call("CanvasBounds");
   var holiday=Rect("FactoryHolidayTrack",0);
   void DragHoliday(int x,ButtonState button)
   {
    var screen=new Point(holidayCanvas.X+x*holidayCanvas.Width/1920,holidayCanvas.Y+holiday.Center.Y*holidayCanvas.Height/1080);
    Call("HandleInput",new MouseState(screen.X,screen.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
   }
   DragHoliday(holiday.X+60,ButtonState.Pressed);
   DragHoliday(holiday.X+150,ButtonState.Pressed);
   DragHoliday(holiday.X+150,ButtonState.Released);
   Check(factory.ProductionLines[0].RestTurns==5,"Holiday handle snaps to whole industrial turns");
   var extra=w.Robots.Create(0,RobotParts.Complete);
   Fixture.Appoint(w,node,extra).SelectPlan(3);
   Set("_factoryPlanLine",-1);
   Call("OpenNodeFactory");
   Check((bool)typeof(Game1).GetProperty("FactoryOverviewActive",F)!.GetValue(this)!,"Opening starts with all three lanes visible");
   Set("_factoryIntroStart",(float)Get("_animationTime")-2f);
   Check(!(bool)typeof(Game1).GetProperty("FactoryOverviewActive",F)!.GetValue(this)!,"Opening animation ends at the two-lane view");
   var canvas=(Rectangle)Call("CanvasBounds");
   var wheelPoint=new Point(canvas.X+canvas.Width/6,canvas.Y+canvas.Height/3);
   void Pan(int y,ButtonState right)=>Call("HandleInput",new MouseState(wheelPoint.X,y,0,ButtonState.Released,ButtonState.Released,right,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
   Pan(wheelPoint.Y,ButtonState.Pressed);Pan(wheelPoint.Y-45,ButtonState.Pressed);Pan(wheelPoint.Y-45,ButtonState.Released);
   Check((int)Get("_factoryPage")==1,"Right drag scrolls the detailed viewport");
   Click(new Point(691,941));
   Call("HandleInput",new MouseState(wheelPoint.X,wheelPoint.Y,-120,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released),new KeyboardState(),new GameTime(),true);
   Check((int)Get("_factoryZoom")==1,"Wheel zooms out to three lanes");
   Click(((Rectangle)Call("FactoryOverviewBounds",2,3)).Center);
   Check((int)Get("_factoryPage")==1 && (int)Get("_factoryZoom")==0,"Selecting lane three in the overview zooms to its detail");
   Click(Rect("FactoryConveyorArrow",((Rectangle)Call("FactoryProductionButton",2))).Center);
   Check((int)Get("_factoryPlanLine")==2,"The second visible row opens lane three");
   Set("_factoryPlanLine",-1);
   Click(new Point(691,941));
   Check((int)Get("_factoryPage")==0,"Up arrow scrolls back to the first two lanes");
   w.Robots.Nodes[node].Remove(extra.Id);
   w.Robots.SyncWorkers(node);
   Check(w.Robots.PartPlanning.ReserveRole(w,node,workerId,HangarReservation.Harvest,0),
    "Reserve harvest for the existing manufacturing worker");
   var arguments=new object[]{factory,null};
   var lines=(RobotWorkshop[])typeof(Game1).GetMethod("FactoryScreenLines",F)!.Invoke(this,arguments)!;
   Check(lines.Length==factory.ProductionLines.Length && lines[1].HarvestOnly && !factory.ProductionLines[1].HarvestOnly,
    "Pending role switch previews harvest at the same lane index");
   Set("_factoryPlanLine",-1);
   Click(Rect("FactoryRobotButton",1,3).Center);
   Check((int)Get("_factoryPlanLine")==1 && (bool)Get("_factoryHarvestPage"),"Reassigned lane opens the harvest catalogue");
   ((int[])typeof(RobotWorld).GetField("_mapleDeposits",F)!.GetValue(w.Robots)!)[node]=3;
   Click(Rect("HarvestAlbumCard",0).Center);
   Click(Rect("HarvestLoadButton",1,2).Center);
   lines=(RobotWorkshop[])typeof(Game1).GetMethod("FactoryScreenLines",F)!.Invoke(this,new object[]{factory,null})!;
   Check(lines[1].MapleRemaining==2,"The harvest lane sets the amount independently of the catalogue");
   w.Campaign.FinishDomestic(w);
   Check(factory.ProductionLines[1].HarvestOnly && factory.ProductionLines[1].MapleRemaining==2,
    "The harvest recipe survives confirmation of the role switch");
  }
  if(frame==12){
   (w,node)=Fixture.Create();
   w.EquipmentMode=RobotEquipmentMode.Materials;
   w.Robots.ConfigureFactories(w);
   var worker=w.Robots.Create(0,RobotParts.Complete) with {
    HeadOrigin=1,BodyOrigin=0,LegsOrigin=2,
    HeadMaterial=RobotMaterial.Foam,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Tin};
   line=Fixture.Appoint(w,node,worker);
   var snapshot=line.WorkerSnapshot!;
   foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
    Check(snapshot.Origin(part)==worker.Origin(part) && snapshot.Material(part)==worker.Material(part),
     "Factory worker drawing retains each part's origin and material");
   Set("_setup",w);
   ((WorldPreview)Get("_world")).ShowSetup(w,true);
   Call("OpenNodeInspector",node);Call("OpenNodeFactory");Call("UpdateDomesticSlide",1f);
  }
  if(frame==13){
   int origin=line.WorkerSnapshot!.Origin(RobotParts.Head);
   w.SetCountryAppearance(origin,w.ColorIndices[origin],w.CountrySecondaryColor(origin),(w.CountryPattern(origin)+1)%5);
  }
  if(frame==14){
   int origin=line.WorkerSnapshot!.Origin(RobotParts.Legs);
   w.SetCountryAppearance(origin,(w.ColorIndices[origin]+1)%WorldSetup.Colors.Length,
    w.CountrySecondaryColor(origin),(w.CountryPattern(origin)+1)%5);
  }
  if(frame==15){
   int origin=line.WorkerSnapshot!.Origin(RobotParts.Body);
   w.SetCountryAppearance(origin,(w.ColorIndices[origin]+3)%WorldSetup.Colors.Length,
    w.CountrySecondaryColor(origin),(w.CountryPattern(origin)+1)%5);
  }
  base.Draw(time);var p=GraphicsDevice.PresentationParameters;var pixels=new Color[p.BackBufferWidth*p.BackBufferHeight];GraphicsDevice.GetBackBufferData(pixels);using var texture=new Texture2D(GraphicsDevice,p.BackBufferWidth,p.BackBufferHeight);texture.SetData(pixels);using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,$"worker-plans-{frame}.png")))texture.SaveAsPng(file,texture.Width,texture.Height);
  if(frame>=12){
   var canvas=(Rectangle)Call("CanvasBounds");
   int x=canvas.X+166*canvas.Width/1920,y=canvas.Y+628*canvas.Height/1080;
   int width=87*canvas.Width/1920,height=48*canvas.Height/1080;
   var head=Enumerable.Range(y,height).SelectMany(row=>pixels.Skip(row*p.BackBufferWidth+x).Take(width)).ToArray();
   int lowerTop=canvas.Y+704*canvas.Height/1080;
   int lowerBottom=canvas.Y+755*canvas.Height/1080;
   int lowerRight=canvas.X+265*canvas.Width/1920;
   var lower=Enumerable.Range(lowerTop,lowerBottom-lowerTop)
    .SelectMany(row=>pixels.Skip(row*p.BackBufferWidth+x).Take(lowerRight-x)).ToArray();
   if(frame==12){workerFlagBaseline=head;workerLowerBaseline=lower;}
   else if(frame==13){
    Check(!head.SequenceEqual(workerFlagBaseline!),"Changing only the head's country pattern changes the factory worker's head pixels");
    Check(lower.SequenceEqual(workerLowerBaseline),"Changing the head flag preserves lower body, legs and adjacent equipment pixels");
    workerFlagBaseline=head;
   }else if(frame==14){
    Check(head.SequenceEqual(workerFlagBaseline),"Changing the leg flag preserves head pixels");
    Check(!lower.SequenceEqual(workerLowerBaseline),"Changing the leg flag updates lower worker pixels");
   }
   if(frame>=14){
    // The lower edge of the head sprite overlaps the torso. Compare its upper portion separately.
    var headTop=head.Take(head.Length/2).ToArray();
    var torso=Enumerable.Range(canvas.Y+675*canvas.Height/1080,28*canvas.Height/1080)
     .SelectMany(row=>pixels.Skip(row*p.BackBufferWidth+x).Take(width)).ToArray();
    if(frame==14)workerBodyBaseline=headTop.Concat(torso).ToArray();
    else{
     Check(headTop.SequenceEqual(workerBodyBaseline.Take(headTop.Length)),"Changing the torso flag preserves the upper head");
     Check(!torso.SequenceEqual(workerBodyBaseline.Skip(headTop.Length)),"Changing the torso flag updates the torso pixels");
    }
   }
  }
  if(frame++==15){Console.WriteLine("PASS: material album, hangar, harvest warnings, reassignment and mixed factory worker flag rendering.");Exit();}
 }
}

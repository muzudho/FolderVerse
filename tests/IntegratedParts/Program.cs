using FolderVerse;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static void Reject(Action action){try{action();}catch(InvalidOperationException){return;}catch(ArgumentException){return;}throw new Exception("Invalid change succeeded");}
static RobotWorld Bare(){var r=new RobotWorld();r.Initialize(2,2);return r;}
static (WorldSetup W,int A,int B) Fixture(){
 var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Salvage.Reset();w.Relations.Reset();w.SelectPlayer(0);
 var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);
 foreach(var n in w.Nodes.All)n.Owner=1;a.Owner=b.Owner=0;w.ConquerorLocations[0]=a.Cell;w.ConquerorPoints[0]=a.Center;
 w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var s in w.Robots.Workshops)s.AutoAssemblyDisabled=true;
 w.Salvage.Agents.Add(new(){Owner=0,Node=a.Id,PreviousNode=a.Id,Policy=SalvagePolicy.Early});return(w,a.Id,b.Id);
}
foreach(var integrated in new[]{RobotParts.Head|RobotParts.Body,RobotParts.Body|RobotParts.Legs}){
 var world=Bare();var complete=world.Create(0,RobotParts.Complete) with {IntegratedParts=integrated};world.Nodes[0].Add(complete);
 var hit=(integrated&RobotParts.Head)!=0?RobotParts.Head:RobotParts.Legs;
 var damaged=complete with {DamagedParts=hit};
 Check(damaged.DamagedParts==hit && damaged.EffectiveDamagedParts==integrated && damaged.NativeParts(0)==1,"Actual damage remains per body part while both joined parts are ineffective");
 Check((damaged.PersuadableParts(1)&integrated)==0 && (damaged with {DamagedParts=0}).NativeParts(0)==3,"Ineffective joined parts cannot be persuaded, and repairing actual damage restores both");
 Reject(()=>world.QueueSplit(0,complete.Id,hit));Reject(()=>world.Split(0,complete.Id,hit));
 Check(world.Nodes[0].Count==1 && world.PendingEdits.Count==0,"Rejected split is atomic");
 var separate=RobotParts.Complete&~integrated;world.Split(0,complete.Id,separate);
 Check(world.Nodes[0].Robots.Single(r=>r.Parts==integrated).IntegratedParts==integrated,"Normal remaining part can be detached from joined pair");
 var assembled=world.Assemble(0,world.Nodes[0].Robots.Select(r=>r.Id).ToArray());
 Check(assembled.Parts==RobotParts.Complete && assembled.IntegratedParts==integrated,"Reassembly preserves connection without joining another pair");
 long moved=world.MovePart(0,assembled.Id,hit,null);
 Check(world.Nodes[0].Robots.Single(r=>r.Id==moved).Parts==integrated && world.Nodes[0].Robots.Single(r=>r.Id==assembled.Id).Parts==separate,"Dragging either member moves complete joined unit");
}
Check(!Robot.ValidIntegratedParts(RobotParts.Head|RobotParts.Legs) && !Robot.ValidIntegratedParts(RobotParts.Complete),"No head/legs or three-part integration exists");
var bare=Bare();Reject(()=>bare.Nodes[0].Add(bare.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Head|RobotParts.Legs}));
var pair=bare.Create(0,RobotParts.Head|RobotParts.Body) with {IntegratedParts=RobotParts.Head|RobotParts.Body,CheapParts=RobotParts.Head|RobotParts.Body};var body=bare.Create(0,RobotParts.Body);bare.Nodes[0].Add(pair);bare.Nodes[0].Add(body);
Reject(()=>bare.MovePart(0,pair.Id,RobotParts.Head,body.Id));Reject(()=>bare.MovePart(0,body.Id,RobotParts.Body,pair.Id));
Check(bare.Nodes[0].Count==2 && bare.Nodes[0].Robots.Single(r=>r.Id==pair.Id).IntegratedParts==pair.IntegratedParts,"Incompatible overlaps do not partially swap or cut joined target");

for(int index=7;index<=10;index++){
 var world=Bare();var factory=world.Workshops[0];factory.GeneratePlans(123);var plan=factory.PlanAt(index);int baseline=factory.Plans.FirstOrDefault(p=>p.Product==plan.Product)?.Turns??factory.IntegratedPlans[(index-7)%2].Turns;
 Check(plan.Turns==Math.Max(1,(index>=9?baseline/2:baseline)-1) && plan.IntegratedParts==plan.Product && plan.IntegratedParts is (RobotParts)3 or (RobotParts)6,"Regular and cheap joined plans save one production turn, minimum one");
 factory.SelectPlan(index);for(int t=0;t<plan.Turns;t++)world.AdvanceIndustry(_=>0);
 var result=world.Nodes[0].Robots.Single();Check(result.IntegratedParts==plan.IntegratedParts && result.CheapParts==(plan.Cheap?result.Parts:0),"Factory manufactures joined metadata and correct quality");
 factory.Configure(RobotParts.Head,4);Check(factory.IntegratedProduct==0 && factory.ProductionPeriod==4,"Selecting ordinary manufacturing clears connection and discount");
}

bare=Bare();pair=bare.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Head|RobotParts.Body};bare.Nodes[0].Add(pair);
Reject(()=>bare.Transport.Resolve(bare,new[]{new RobotShipment(0,1,pair.Id,RobotParts.Head)},(_,_)=>true));
bare.Transport.Resolve(bare,new[]{new RobotShipment(0,1,pair.Id,RobotParts.Head|RobotParts.Body)},(_,_)=>true);
Check(bare.Nodes[0].Robots.Single().Parts==RobotParts.Legs && bare.Nodes[1].Robots.Single().IntegratedParts==(RobotParts.Head|RobotParts.Body),"Transport moves complete joined group and preserves normal remainder");
bare=Bare();pair=bare.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Head|RobotParts.Body};bare.Nodes[0].Add(pair);
var rule=bare.Transport.AddInstruction(0,0,1);bare.Transport.ConfigurePartChoices(0,0,rule.Number,1,1<<6,false,5);bare.Transport.SetInstructionRate(0,0,rule.Number,360);bare.Transport.ResolvePlans(bare,(_,_)=>true,3);
Check(bare.Nodes[1].Robots.Single().IntegratedParts==(RobotParts.Head|RobotParts.Body),"Player-facing transport instructions can select joined units");

var (w,a,b)=Fixture();var robot=w.Robots.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Head|RobotParts.Body,CheapParts=RobotParts.Head|RobotParts.Body,DamagedParts=RobotParts.Head};w.Robots.Nodes[a].Add(robot);
var order=w.Robots.DirectDelivery.Reserve(w,a,b,robot.Id,RobotParts.Head,null,0);
Check(order.Part==(RobotParts.Head|RobotParts.Body) && w.Robots.PartLocked(a,robot,RobotParts.Body),"Direct reservation expands and locks whole joined group");
Check(w.Robots.DirectDelivery.Cancel(order.Number),"Whole joined reservation can be cancelled");
w.Robots.DirectDelivery.ReserveParts(w,a,b,robot.Id,RobotParts.Complete,null,0);
Check(w.Robots.DirectDelivery.Orders.Count==2,"Whole robot reserves joined pair once and remaining part once");
w.Robots.Advance((_,_)=>true,n=>w.Nodes.All[n].Owner,1,industry:false);
var arrived=w.Robots.Nodes[b].Robots.Single();Check(w.Robots.Nodes[a].Count==0 && arrived.Parts==RobotParts.Complete && arrived.IntegratedParts==robot.IntegratedParts && arrived.DamagedParts==RobotParts.Head && arrived.IsDamaged(RobotParts.Body) && arrived.CheapParts==robot.CheapParts,"Direct arrival preserves integration, actual/effective damage and quality");

(w,a,b)=Fixture();robot=w.Robots.Create(0,RobotParts.Complete) with {IntegratedParts=RobotParts.Body|RobotParts.Legs,DamagedParts=RobotParts.Legs};w.Robots.Nodes[a].Add(robot);
var dropped=w.Salvage.Abandon(w,0,a,robot.Id,RobotParts.Body);
Check(dropped.Robot.Parts==(RobotParts.Body|RobotParts.Legs) && dropped.Robot.IntegratedParts==dropped.Robot.Parts && dropped.Robot.DamagedParts==RobotParts.Legs && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Head,"Abandonment cannot detach half of pair");
Check(w.Salvage.Request(w,0,dropped.Id),"Joined scrap can be requested");w.Salvage.Advance(w,1);
Check(w.Salvage.Agents.First(c=>c.Owner==0).Cargo.Single().IntegratedParts==dropped.Robot.IntegratedParts,"Recovery carries connection unchanged");
var doc=AutoSaveStore.Capture(w);var loaded=AutoSaveStore.Restore<WorldSetup>(doc);
Check(loaded.Salvage.Agents.First(c=>c.Owner==0).Cargo.Single().IntegratedParts==dropped.Robot.IntegratedParts,"Connection survives save while in transit");
w.Salvage.Advance(w,2);Check(w.Robots.Nodes[a].Robots.Any(r=>r.IntegratedParts==dropped.Robot.IntegratedParts),"Recovered group remains joined in accepting hangar");
foreach(var e in doc.Objects)if(e.Fields!=null){e.Fields.Remove("<IntegratedParts>k__BackingField");e.Fields.Remove("<IntegratedProduct>k__BackingField");e.Fields.Remove("<IntegratedPlans>k__BackingField");}
loaded=AutoSaveStore.Restore<WorldSetup>(doc);Check(loaded.Robots.Nodes.SelectMany(s=>s.Robots).All(r=>r.IntegratedParts==0) && loaded.Robots.Workshops[0].PlanAt(7).IntegratedParts==(RobotParts.Head|RobotParts.Body),"Old saves stay separable and gain new manufacturing catalogue");
Console.WriteLine("PASS: asymmetric integration, actual/effective damage and repair, atomic split/swap, manufacturing discount, transport/direct delivery, salvage and current/legacy saves.");

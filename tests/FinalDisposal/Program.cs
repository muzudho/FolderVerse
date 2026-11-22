using FolderVerse;
using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));
const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
int checks=0;
void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,F)!.Invoke(target,args)!;
void Prop(object target,string name,object value)=>target.GetType().GetProperty(name,F)!.SetValue(target,value);
void Raw(RobotStore s,params MaplePackage[] p)=>Call(s,"SetMaplePackages",(object)p);
WorldSetup Fixture(){
 var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(8);w.SetPlacement(3);
 foreach(var n in w.Nodes.All)n.Owner=0;w.SelectPlayer(0);Prop(w.Robots,"ScrapRulesEnabled",true);
 return w;
}
(int Node,long Worker) Setup(WorldSetup w){
 int n=w.Nodes.All.First(n=>n.Functional && !n.IsHarbor).Id;
 var r=w.Robots;var s=r.Nodes[n];Raw(s);Call(s,"Replace",(object)Array.Empty<Robot>());Call(s,"SetWarehouseLimit",60);
 s.Add(r.Create(0,RobotParts.Complete) with {WarehouseKeeper=true});
 var worker=r.Create(0,RobotParts.Complete);s.Add(worker);
 r.Workshops[n]=new RobotWorkshop();Call(r.Workshops[n],"RemoveProductionLines");r.ConfigureFactories(w);
 Check(r.ToggleReservation(n,worker.Id,HangarReservation.Dismantler,0),"appoint dismantler");
 r.PrepareDomestic(w.Population.Turn,(a,b)=>false,_=>0,_=>true);
 return(n,worker.Id);
}
foreach(var parts in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs,RobotParts.Complete}){
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;var s=r.Nodes[n];
 var cargo=r.Create(0,parts,disposal:true);s.Add(cargo);
 int ticks=RobotWorkshop.DisposalTurns(parts);
 for(int i=0;i<ticks-1;i++)r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(s.Robots.Any(x=>x.Id==cargo.Id && x.IsDismantling),"physical WIP retained and marked");
 Check(!s.Robots.Single(x=>x.Id==cargo.Id).CanFight,"dismantling cannot fight");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;s=r.Nodes[n];r.ConfigureFactories(w);
 r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(!s.Robots.Any(x=>x.Id==cargo.Id),"converted once");
 Check(s.MaplePackages.Single().Material==RawMaterial.Scrap && s.MaplePackages.Single().Units==SalvagePlan.PartCount(parts),"exact one-third compression");
 r.AdvanceIndustry(_=>0,id=>id!=n);Check(s.MaplePackages.Sum(p=>p.Units)==SalvagePlan.PartCount(parts),"no duplicate scrap");
}
{
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;var s=r.Nodes[n];
 var head=r.Create(0,RobotParts.Head,disposal:true);s.Add(head);s.Add(r.Create(0,RobotParts.Complete));s.Add(r.Create(0,RobotParts.Complete));
 Raw(s,new MaplePackage(900001,32));Check(s.Count==5,"fixture packs raw around partial robot");
 for(int i=0;i<4;i++)r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(s.MaplePackages.All(p=>p.Material!=RawMaterial.Scrap),"no illegal mixed bay");
 Check(r.Workshops[n].ProductionLines.Single().DisposalAge==3,"output wait retains completed dismantling");
 var keeper=s.Robots.Single(x=>x.WarehouseKeeper);
 Check(!r.CanSetReservation(n,keeper,HangarReservation.Warehouse,0),"capacity locks keeper release");
 Check(r.ToggleReservation(n,worker,HangarReservation.Dismantler,0),"worker release keeps cargo in original reserved space");
 Check(s.Robots.Single(x=>x.Id==head.Id).DismantlingAge==3,"release preserves progress");
 Check(r.ToggleReservation(n,worker,HangarReservation.Dismantler,0),"reappoint");
 s.Remove(s.Robots.First(x=>!x.Stationed && x.Id!=head.Id).Id);
 r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(s.MaplePackages.Any(p=>p.Material==RawMaterial.Scrap),"space freed then completion resumes");
}
{
 var w=Fixture();var (n,_)=Setup(w);var s=w.Robots.Nodes[n];w.Nodes.All[n].HasFinalDisposal=true;
 Raw(s,new MaplePackage(900001,2){Material=RawMaterial.Scrap,ScrapLots=new[]{new ScrapLot(1,n,0),new ScrapLot(1,n,1)}});
 Prop(w.Population,"Turn",1);w.Robots.ProcessFinalDisposal(w);
 Check(s.MaplePackages.Single().Units==1,"only old cohort disposed");
 w.Robots.ProcessFinalDisposal(w);Check(s.MaplePackages.Single().Units==1,"same-turn processing idempotent");
 var p=s.MaplePackages.Single();var split=ScrapCargo.Portion(ScrapCargo.Join(p,p),1,1);
 Check(split.Units==1 && split.ScrapLots.Single().ArrivedTurn==1,"split preserves age exactly");
 var moved=ScrapCargo.Arrive(p,n+1,9);Check(moved.ScrapLots.Single().ArrivedTurn==9,"transport resets residence");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Prop(w.Population,"Turn",2);w.Robots.ProcessFinalDisposal(w);
 Check(w.Robots.Nodes[n].MaplePackages.Count==0,"aged scrap disposed after restore");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;var s=r.Nodes[n];Raw(s,new MaplePackage(900001,3){Material=RawMaterial.Scrap});
 var robot=r.Create(0,RobotParts.Body);s.Add(robot);r.Discarding.Reserve(w,n,0,robot.Id,RobotParts.Body);r.Discarding.Reserve(w,n,0,900001);
 Check(s.Robots.Any(x=>x.Id==robot.Id) && s.MaplePackages.Count==1,"reservation does not remove cargo");
 Check(!r.CanShip(n,robot),"discard reservation locks shipment");
 Check(!r.CanSetReservation(n,robot,HangarReservation.Disposal,0),"discard reservation locks conflicting role");
 var extra=r.Create(0,RobotParts.Complete);s.Add(extra);bool refused=false;
 try{r.Discarding.Reserve(w,n,0,extra.Id);}catch(InvalidOperationException){refused=true;}
 Check(refused,"quota combines robot parts and scrap by volume");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;r.Discarding.Resolve(w);
 Check(!r.Nodes[n].Robots.Any(x=>x.Id==robot.Id) && r.Nodes[n].MaplePackages.Count==0,"discard resolves");
 var basket=w.Salvage.Items.Single(x=>x.Robot.RecoveredRaw!=null);
 Check(basket.Robot.RecoveredRaw.Units==3,"basket retains raw quantity");
 Check(!w.Salvage.PickUp(w,0,basket.Id),"scrap must return through recovery team");
 Check(r.Discarding.Orders.Count==0,"orders cleared");
 bool directRefused=false;try{w.Salvage.Abandon(w,0,n,extra.Id,extra.Parts);}catch(InvalidOperationException){directRefused=true;}
 Check(directRefused && r.Nodes[n].Robots.Any(x=>x.Id==extra.Id),"direct abandonment cannot bypass per-turn disposal budget");
}
{
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;
 int to=w.Routes.Neighbors(w.Nodes.All[n]).Select(e=>w.Nodes.At(e.Target,e.Entry).Id).First(id=>id!=n);
 var target=r.Nodes[to];Raw(target);Call(target,"Replace",(object)Array.Empty<Robot>());Call(target,"SetWarehouseLimit",30);target.Add(r.Create(0,RobotParts.Complete) with {WarehouseKeeper=true});
 r.Workshops[to]=new RobotWorkshop();Call(r.Workshops[to],"RemoveProductionLines");r.ConfigureFactories(w);
 var cargo=r.Create(0,RobotParts.Head,disposal:true);r.Nodes[n].Add(cargo);r.AdvanceIndustry(_=>0,id=>id!=n);
 w.Campaign.FunctionTransfers.Reserve(w,n,to,true);Call(w.Campaign.FunctionTransfers,"Resolve",w);
 Check(r.Nodes[to].Robots.Any(x=>x.Id==worker) && r.Nodes[to].Robots.Any(x=>x.Id==cargo.Id && x.DismantlingAge==1),"factory move carries worker and locked workpiece");
 r.AdvanceIndustry(_=>0,id=>id!=to);r.AdvanceIndustry(_=>0,id=>id!=to);
 Check(r.Nodes[to].MaplePackages.Any(p=>p.Material==RawMaterial.Scrap),"moved dismantler continues from saved work");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;
 w.Nodes.All[n].HasFinalDisposal=true;w.Nodes.All[n].Purpose=NodePurpose.FinalDisposal;
 int to=w.Routes.Neighbors(w.Nodes.All[n]).Select(e=>w.Nodes.At(e.Target,e.Entry).Id).First(id=>id!=n);
 Raw(r.Nodes[to]);Call(r.Nodes[to],"Replace",(object)Array.Empty<Robot>());Call(r.Nodes[to],"SetWarehouseLimit",30);r.Nodes[to].Add(r.Create(0,RobotParts.Complete) with {WarehouseKeeper=true});
 r.Workshops[to]=new RobotWorkshop();Call(r.Workshops[to],"RemoveProductionLines");r.ConfigureFactories(w);
 w.Campaign.FunctionTransfers.Reserve(w,n,to);Call(w.Campaign.FunctionTransfers,"Resolve",w);
 Check(w.Nodes.All[n].Functional && w.Nodes.All[n].HasFinalDisposal && w.Nodes.All[n].Purpose==NodePurpose.FinalDisposal,"landmark persists after all movable facilities leave");
 Check(r.Nodes[n].TotalCapacity==1 && r.Nodes[n].WarehouseLimit==1,"one hangar retained");
 Check(r.Nodes[n].Count==0 && r.Nodes[to].Robots.Count(x=>x.FactoryWorker)==1,"movable staff moved");
 var cargo=r.Create(0,RobotParts.Head,disposal:true);r.Nodes[to].Add(cargo);r.AdvanceIndustry(_=>0,id=>id!=to);
 var movedWorker=r.Nodes[to].Robots.Single(x=>x.FactoryWorker);
 Check(r.ToggleReservation(to,movedWorker.Id,HangarReservation.Dismantler,0),"release transferred dismantler");
 for(int i=0;i<4;i++)r.AdvanceIndustry(_=>0,id=>id!=to);
 Check(r.Nodes[to].Robots.Single(x=>x.Id==cargo.Id).DismantlingAge==1,"no ghost lane after transfer and release");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;var s=r.Nodes[n];
 Raw(s,new MaplePackage(900001,1){Material=RawMaterial.Scrap});r.Discarding.Reserve(w,n,0,900001);r.Discarding.Resolve(w);
 var item=w.Salvage.Items.Single(i=>i.Robot.RecoveredRaw!=null);
 w.Salvage.Agents.Clear();var agent=new SalvageAgent{Owner=0,Node=n,PreviousNode=n,Policy=SalvagePolicy.Early};w.Salvage.Agents.Add(agent);
 Check(w.Salvage.Request(w,0,item.Id),"recovery request accepts own discarded scrap");
 w.Salvage.Advance(w,1);Check(!w.Salvage.Items.Any(i=>i.Id==item.Id) && agent.Cargo.Any(c=>c.RecoveredRaw!=null),"team collects scrap");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));w.Salvage.Advance(w,2);
 Check(w.Robots.Nodes[n].MaplePackages.Any(p=>p.Material==RawMaterial.Scrap && p.Units==1),"team unloads scrap as raw after save");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;var s=r.Nodes[n];
 Raw(s,new MaplePackage(900001,1){Material=RawMaterial.Scrap,ScrapLots=new[]{new ScrapLot(1,n,0)}},new MaplePackage(900002,1){Material=RawMaterial.Scrap,ScrapLots=new[]{new ScrapLot(1,n,1)}});
 Check((bool)Call(r,"RearrangeMaple",w,n,900001L,(long?)900002L,false),"actual scrap merge");
 Check(s.MaplePackages.Single().Units==2 && s.MaplePackages.Single().ScrapLots.Length==2,"merge preserves cohorts and total");
 Check((bool)Call(r,"RearrangeMaple",w,n,900002L,null,true),"actual scrap split");
 Check(s.MaplePackages.Sum(p=>p.Units)==2 && s.MaplePackages.SelectMany(p=>p.ScrapLots).Count()==2,"split conserves exact age and quantity");
 w.Nodes.All[n].HasFinalDisposal=true;Prop(w.Population,"Turn",1);r.ProcessFinalDisposal(w);
 Check(s.MaplePackages.Sum(p=>p.Units)==1,"merge then split cannot age new scrap early");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;
 int to=FunctionalDeliveryRoutes.Targets(w,n).First();Raw(r.Nodes[to]);Call(r.Nodes[to],"Replace",(object)Array.Empty<Robot>());
 Raw(r.Nodes[n],new MaplePackage(900001,1){Material=RawMaterial.Scrap,ScrapLots=new[]{new ScrapLot(1,n,0)}});
 Prop(w.Population,"Turn",5);r.PrepareDomestic(5,(a,b)=>false,_=>0,_=>true);w.Nodes.All[to].HasFinalDisposal=true;
 Check(r.ReserveMapleShipment(w,n,to,900001),"scrap real shipping reservation");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;Call(r,"ResolveMapleShipments",w);
 Check(r.Nodes[n].MaplePackages.Count==0 && r.Nodes[to].MaplePackages.Single().ScrapLots.Single()==new ScrapLot(1,to,5),"shipping arrival resets age after save");
 r.ProcessFinalDisposal(w);Check(r.Nodes[to].MaplePackages.Count==1,"new arrival not disposed immediately");
 Prop(w.Population,"Turn",6);r.ProcessFinalDisposal(w);Check(r.Nodes[to].MaplePackages.Count==0,"one full turn residence expires");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;
 for(int i=0;i<3;i++){var worker=r.Create(0,RobotParts.Complete);r.Nodes[n].Add(worker);Check(r.ToggleReservation(n,worker.Id,HangarReservation.Dismantler,0),"additional dismantler beyond old three-lane limit");}
 Check(r.Workshops[n].ProductionLines.Count(l=>l.DismantleOnly)==4,"no dismantling cap at three");
}
foreach(int seed in new[]{1,42,104729,628374}){
 var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(0,8);w.SetWorld(seed);w.SetCast(8);w.SetTerritoryRights(492471852);
 var planner=new ComputerLandPlanner(w);foreach(int owner in w.LandOrder())w.ClaimLand(owner,planner.Select(owner,null).Cells);
 w.CompleteLand(1544938375,25,25);
 Check(w.Robots.ScrapRulesEnabled && w.Nodes.All.Any(n=>n.HasFinalDisposal),"new world guarantees landmark");
 Check(w.Nodes.All.Where(n=>n.HasFinalDisposal).All(n=>!n.IsHarbor && !w.Routes.NearWhirlpool(n) && n.Purpose==NodePurpose.FinalDisposal),"permanent sites separate from ports and whirlpools");
 var seen=new HashSet<int>{0};var q=new Queue<int>();q.Enqueue(0);
 while(q.TryDequeue(out int at))foreach(var e in w.Routes.Neighbors(w.Nodes.All[at])){int next=w.Nodes.At(e.Target,e.Entry).Id;if(seen.Add(next))q.Enqueue(next);}
 Check(seen.Count==w.Nodes.All.Length,"world traffic network connected");
}
{
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;
 int to=FunctionalDeliveryRoutes.Targets(w,n).First();Raw(r.Nodes[to]);Call(r.Nodes[to],"Replace",(object)Array.Empty<Robot>());
 var cargo=r.Create(0,RobotParts.Head|RobotParts.Body,disposal:true);r.Nodes[n].Add(cargo);r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(!r.CanMoveDismantling(n,r.Nodes[n].Robots.Single(x=>x.Id==cargo.Id),cargo.Parts),"active disassembly cannot depart");
 Check(r.ToggleReservation(n,worker,HangarReservation.Dismantler,0),"release before shipping");
 r.DirectDelivery.ReserveParts(w,n,to,cargo.Id,cargo.Parts,null,0);
 Check(r.DirectDelivery.Orders.Count==1 && r.DirectDelivery.Orders.Single().Part==cargo.Parts,"whole disassembly ships as one unit");
 Check(r.ToggleReservation(n,worker,HangarReservation.Dismantler,0),"reappoint while shipment is reserved");
 r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(r.Workshops[n].ProductionLines.Single().DisposalTarget==null && r.Nodes[n].Robots.Single(x=>x.Id==cargo.Id).DismantlingAge==1,"shipping reservation prevents a second dismantler taking the cargo");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;
 Call(r.DirectDelivery,"Resolve",r,(Func<int,int,bool>)((a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b)),(Func<int,bool>)(_=>false),(Func<int,int,bool>)((owner,node)=>true));
 Check(r.Nodes[to].Robots.Single(x=>x.Id==cargo.Id).DismantlingAge==1,"direct delivery preserves dismantling progress");
 Check(!r.Nodes[n].Robots.Any(x=>x.Id==cargo.Id),"no duplicate disassembly cargo");
}
{
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;
 int to=RobotLongDelivery.Neighbors(w,n,0).First();Raw(r.Nodes[to]);Call(r.Nodes[to],"Replace",(object)Array.Empty<Robot>());
 var cargo=r.Create(0,RobotParts.Complete,disposal:true);r.Nodes[n].Add(cargo);r.AdvanceIndustry(_=>0,id=>id!=n);
 Check(r.ToggleReservation(n,worker,HangarReservation.Dismantler,0),"release disassembly before long delivery");
 r.LongDelivery.Reserve(w,n,to,cargo.Id,cargo.Parts,null,0);
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;Call(r.LongDelivery,"Advance",w);
 var arrived=r.Nodes[to].Robots.Single(x=>x.Id==cargo.Id);
 Check(arrived.IsDismantling && arrived.DismantlingAge==1 && !arrived.CanFight,"long delivery preserves irreversible dismantling");
 Check(r.LongDelivery.Orders.Count==0,"long delivery completes");
}
{
 var w=Fixture();var (n,worker)=Setup(w);var r=w.Robots;var cargo=r.Create(0,RobotParts.Head,disposal:true);r.Nodes[n].Add(cargo);
 r.AdvanceIndustry(_=>0,id=>id!=n);r.ToggleReservation(n,worker,HangarReservation.Dismantler,0);
 Check(!r.CanEditReservedPart(n,r.Nodes[n].Robots.Single(x=>x.Id==cargo.Id),0),"disassembly cannot be reassembled after worker release");
 r.Discarding.Reserve(w,n,0,cargo.Id);r.Discarding.Resolve(w);
 Check(!r.Nodes[n].Robots.Any(x=>x.Id==cargo.Id) && w.Salvage.Items.Any(i=>i.Robot.Id==cargo.Id && i.Robot.DismantlingAge==1),"disassembly can be discarded whole without losing progress");
}
{
 var w=Fixture();var (n,_)=Setup(w);var r=w.Robots;
 Raw(r.Nodes[n],new MaplePackage(900001,2){Material=RawMaterial.Scrap});
 var method=typeof(WorldSetup).Assembly.GetType("FolderVerse.ComputerTrade")!.GetMethod("Value",BindingFlags.Static|BindingFlags.NonPublic)!;
 var endpoint=new TradeEndpoint(0,n,false);
 Check((int)method.Invoke(null,new object[]{w,endpoint,endpoint,new TradeCargo(900001,true)})! ==0,"computer does not value useless scrap as production material");
 var line=r.Workshops[n].ProductionLines.Single();typeof(RobotWorkshop).GetField("_rawConsumed",F)!.SetValue(line,new[]{0,0,0,0,0});
 Check(line.RawConsumed(RawMaterial.Scrap)==0,"new raw kind cannot index past existing material ledgers");
}
Console.WriteLine($"PASS {checks} final-disposal assertions");

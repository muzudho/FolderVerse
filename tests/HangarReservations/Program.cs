using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static void Locked(Action action){try{action();}catch(InvalidOperationException){return;}catch(ArgumentException){return;}throw new Exception("Reserved operation succeeded");}
var robots=new RobotWorld();robots.Initialize(3,2);var shop=robots.Workshops[0];
for(int seed=0;shop.ProductionLines.Length!=1;seed++)shop.GenerateLines(seed);
var a=robots.Create(0,RobotParts.Complete);var b=robots.Create(0,RobotParts.Complete);var c=robots.Create(0,RobotParts.Complete);
foreach(var r in new[]{a,b,c})robots.Nodes[0].Add(r);
Check(robots.ToggleReservation(0,a.Id,HangarReservation.Defense),"Defense reservation");
a=robots.Nodes[0].Robots.Single(r=>r.Id==a.Id);
Check(a.CanFight && robots.AssemblyLocked(0,a) && !robots.CanShip(0,a),"Defense can fight but stays locked at home");
Locked(()=>robots.Split(0,a.Id,RobotParts.Head));Locked(()=>robots.MovePart(0,a.Id,RobotParts.Head,null));Locked(()=>robots.Transport.Resolve(robots,new[]{new RobotShipment(0,1,a.Id,a.Parts)},(_,_)=>true));
shop.ConfigureDisposal();Locked(()=>shop.Dispose(robots.Nodes[0],a.Id));Check(robots.ToggleReservation(0,a.Id,HangarReservation.Disposal),"Defense reservation is overwritten by disposal");Check(robots.ToggleReservation(0,a.Id,HangarReservation.Defense),"Disposal is overwritten by defense after checking clean eligibility");
Check(robots.ToggleReservation(0,b.Id,HangarReservation.Worker) && shop.ProductionLines.Length==2 && shop.BaseLineCount==1 && shop.WorkerAt(1)==b.Id,"Worker opens one distinguishable lane");
Check(robots.ToggleReservation(0,c.Id,HangarReservation.Worker) && shop.ProductionLines.Length==3,"Second worker opens third lane");
var d=robots.Create(0,RobotParts.Complete);robots.Nodes[0].Add(d);Check(!robots.ToggleReservation(0,d.Id,HangarReservation.Worker),"Three lane cap");
var retained=shop.ProductionLines[2];Check(robots.ToggleReservation(0,b.Id,HangarReservation.Worker) && shop.ProductionLines.Length==2 && ReferenceEquals(retained,shop.ProductionLines[1]),"Removing one worker closes only its lane");
var before=shop.ProductionLines[0];var restored=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(robots));Check(restored.Nodes[0].Robots.Single(r=>r.Id==a.Id).DefenseOnly && restored.Nodes[0].Robots.Single(r=>r.Id==c.Id).FactoryWorker && restored.Workshops[0].WorkerAt(1)==c.Id,"Reservations and worker lanes persist");
Check(robots.ToggleReservation(0,a.Id,HangarReservation.Defense),"Click again unlocks defender");
Check(robots.ToggleReservation(0,a.Id,HangarReservation.Disposal),"Disposal reservation");Locked(()=>robots.QueueSplit(0,a.Id,RobotParts.Head));Check(robots.ToggleReservation(0,a.Id,HangarReservation.Disposal),"Disposal cancellation");
a=robots.Nodes[0].Robots.Single(r=>r.Id==a.Id);var rule=robots.Transport.AddInstruction(0,0,1);robots.Transport.ConfigurePartChoices(0,0,rule.Number,1,1,false,1);robots.Transport.SetInstructionRate(0,0,rule.Number,360);
Check(!robots.AssemblyLocked(0,a) && robots.ToggleReservation(0,a.Id,HangarReservation.Defense),"Transport plan does not lock assembly or roles");Check(robots.ToggleReservation(0,a.Id,HangarReservation.Defense),"Cancel defender while keeping transport plan");robots.Split(0,a.Id,RobotParts.Head);var rest=robots.Nodes[0].Robots.Single(r=>r.Parts==(RobotParts.Body|RobotParts.Legs));robots.Assemble(0,new[]{a.Id,rest.Id},0);Check(robots.Transport.Instructions(0,0).Count==1,"Assembly and split preserve transport plan");robots.Transport.DeleteInstruction(0,0,rule.Number);
robots.Reservations=new ConquestCampaign();robots.Reservations.Plan(new(0,1,1,0,false,RobotIds:new[]{a.Id}));Check(robots.AssemblyLocked(0,a) && !robots.CanShip(0,a),"Sortie reservation locks assembly and transport");robots.Reservations.CancelSource(0,0);Check(!robots.AssemblyLocked(0,a),"Sortie cancellation unlocks");
shop.LimitLines(1);robots.SyncWorkers(0);Check(shop.ProductionLines.Length==1 && !robots.Nodes[0].Robots.Any(r=>r.FactoryWorker) && !robots.ToggleReservation(0,a.Id,HangarReservation.Worker),"Sea cap releases surplus workers and disallows second lane");
var empty=robots.Workshops[1];empty.LimitLines(1);for(int seed=0;empty.ProductionLines.Length!=0;seed++)empty.GenerateLines(seed);var seaWorker=robots.Create(0,RobotParts.Complete);robots.Nodes[1].Add(seaWorker);Check(robots.ToggleReservation(1,seaWorker.Id,HangarReservation.Worker) && empty.ProductionLines.Length==1 && empty.BaseLineCount==0,"Sea with zero permanent lanes can staff one lane");
var incomplete=robots.Create(0,RobotParts.Head);robots.Nodes[2].Add(incomplete);Check(!robots.ToggleReservation(2,incomplete.Id,HangarReservation.Worker) && !robots.ToggleReservation(2,incomplete.Id,HangarReservation.Defense),"Incomplete robots cannot staff or defend");
var world=new WorldSetup();world.SetWorld(0);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
Check(world.Nodes.All.Where(n=>n.Terrain==TravelTerrain.Sea).All(n=>world.Robots.Workshops[n.Id].ProductionLines.Length<=1 && world.Robots.Workshops[n.Id].LineLimit==1),"All generated sea factories capped");
var source=world.Nodes.All.First(n=>world.Routes.Neighbors(n).Any());var target=world.Nodes.At(world.Routes.Neighbors(source)[0].Target,world.Routes.Neighbors(source)[0].Entry);source.Owner=0;target.Owner=1;world.Relations.Reset();
world.Robots.Initialize(world.Nodes.All.Length,world.ActiveCount);world.Robots.ConfigureFactories(world);
var attacker=world.Robots.Create(0,RobotParts.Complete);var defender=world.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true};world.Robots.Nodes[source.Id].Add(attacker);world.Robots.Nodes[target.Id].Add(defender);
Check(world.Campaign.RobotBattles.AvailableFrom(world,1,target.Id)==0,"Defender cannot attack");world.Campaign.RobotBattles.Rules=new(){SegmentTurns=1};
world.Campaign.RobotBattles.Advance(world,new[]{new March(0,target.Id,1,source.Id,false,RobotIds:new[]{attacker.Id})});
Check(world.Campaign.RobotBattles.Scenes.Any(s=>s.Units.Any(u=>u.Robot?.Id==defender.Id)),"Defense-only robot enters battle when its node is attacked");
var legacy=AutoSaveStore.Capture(robots);foreach(var entry in legacy.Objects)if(entry.Fields!=null)foreach(string field in new[]{"<DefenseOnly>k__BackingField","<FactoryWorker>k__BackingField","_baseLineCount","_lineLimit","_workerLines","<Reservations>k__BackingField"})entry.Fields.Remove(field);
var old=AutoSaveStore.Restore<RobotWorld>(legacy);Check(old.Nodes.All(s=>s.Robots.All(r=>!r.Stationed)),"Old saves default to no new reservations");
var lane=empty.ProductionLines[0];lane.Configure(RobotParts.Head,1);robots.AdvanceIndustry(_=>0);Check(robots.Nodes[1].Robots.Any(r=>r.Parts==RobotParts.Head),"Staffed lane actually manufactures");
Console.WriteLine("PASS: reservation locks/cancellation, defense deployment, transport/sortie conflicts, worker lanes and preserved identities, sea limits, eligibility and saves.");
var shipping=new RobotWorld();shipping.Initialize(2,1);var head=shipping.Create(0,RobotParts.Head);var body=shipping.Create(0,RobotParts.Body|RobotParts.Legs);shipping.Nodes[0].Add(head);shipping.Nodes[0].Add(body);shipping.Transport.SetPlan(0,0,RobotParts.Complete,new[]{new TransportWeight(1,360)});
shipping.Advance((x,y)=>x!=y,_=>0,1,industry:false);Check(shipping.Nodes[0].Count==0 && shipping.Nodes[1].Robots.Single().CanFight,"Automatically assemble before selecting complete-robot transport");
Console.WriteLine("PASS: transport plans remain intact, do not lock parts, and ship from assembled stock.");
// Validate first, then replace exactly one role and its old sortie membership.
var roles=new RobotWorld();roles.Initialize(1,1);var factory=roles.Workshops[0];for(int seed=0;factory.ProductionLines.Length!=1;seed++)factory.GenerateLines(seed);
var roleRobot=roles.Create(0,RobotParts.Complete);var partner=roles.Create(0,RobotParts.Complete);roles.Nodes[0].Add(roleRobot);roles.Nodes[0].Add(partner);
foreach(var from in new[]{HangarReservation.Disposal,HangarReservation.Defense,HangarReservation.Worker})foreach(var to in new[]{HangarReservation.Disposal,HangarReservation.Defense,HangarReservation.Worker}){
 roles.Nodes[0].Remove(roleRobot.Id);roles.Nodes[0].Add(roleRobot);roles.SyncWorkers(0);
 Check(roles.ToggleReservation(0,roleRobot.Id,from) && roles.ToggleReservation(0,roleRobot.Id,to),"Role overwrite accepted");var current=roles.Nodes[0].Robots.Single(r=>r.Id==roleRobot.Id);
 Check((current.Disposal?1:0)+(current.DefenseOnly?1:0)+(current.FactoryWorker?1:0)==(from==to?0:1),"Only the chosen role remains; same role cancels");
}
roles.Nodes[0].Remove(roleRobot.Id);roles.Nodes[0].Add(roleRobot with {DefenseOnly=true});roles.SyncWorkers(0);factory.LimitLines(1);
Check(!roles.ToggleReservation(0,roleRobot.Id,HangarReservation.Worker) && roles.Nodes[0].Robots.Single(r=>r.Id==roleRobot.Id).DefenseOnly,"Full factory rejects overwrite and retains defender");factory.LimitLines(3);
roles.Nodes[0].Remove(roleRobot.Id);roles.Nodes[0].Add(roleRobot with {Parts=RobotParts.Head,Disposal=true});Check(!roles.ToggleReservation(0,roleRobot.Id,HangarReservation.Defense) && roles.Nodes[0].Robots.Single(r=>r.Id==roleRobot.Id).Disposal,"Ineligible overwrite retains disposal reservation");
roles.Nodes[0].Remove(roleRobot.Id);roles.Nodes[0].Add(roleRobot);roles.Reservations=new ConquestCampaign();roles.Reservations.Plan(new(0,1,2,0,false,RobotIds:new[]{roleRobot.Id,partner.Id}));
Check(roles.ToggleReservation(0,roleRobot.Id,HangarReservation.Worker) && roles.Reservations.PlannedOrders.Single().RobotIds!.SequenceEqual(new[]{partner.Id}) && roles.Reservations.PlannedOrders.Single().Fighters==1,"Worker overwrite removes only this robot from sortie plan");
Check(roles.PrepareSortieReservation(0,new[]{roleRobot.Id}) && !roles.Nodes[0].Robots.Single(r=>r.Id==roleRobot.Id).Stationed && factory.ProductionLines.Length==1,"Sortie selection closes assigned worker lane");
roles.Reservations.CancelSource(0,0);roles.Reservations.Plan(new(0,1,2,0,false));Check(roles.ToggleReservation(0,roleRobot.Id,HangarReservation.Defense) && roles.Reservations.PlannedOrders.Single().RobotIds!.SequenceEqual(new[]{partner.Id}),"Legacy count-only sortie materializes other robots before overwrite");
roles.Reservations.CancelSource(0,0);roles.Reservations.Plan(new(0,1,1,0,true,RobotIds:new[]{partner.Id}));Check(roles.ToggleReservation(0,partner.Id,HangarReservation.Disposal) && roles.Reservations.PlannedOrders.Single().Queen && roles.Reservations.PlannedOrders.Single().Fighters==0,"Keep queen participation when last selected robot changes roles");
factory.ConfigureDisposal();factory.Dispose(roles.Nodes[0],partner.Id);Check(!roles.ToggleReservation(0,partner.Id,HangarReservation.Defense) && roles.Nodes[0].Robots.Single(r=>r.Id==partner.Id).Disposal,"Active disposal cannot be overwritten");
roles.Reservations.RobotBattles.Encounters.Add(new(){Kind=BattleKind.Node,Target=0});Check(!roles.ToggleReservation(0,roleRobot.Id,HangarReservation.Disposal),"Ongoing node battle blocks overwrite");
Console.WriteLine("PASS: all role replacements, same-role cancellation, failed replacement preservation, sortie membership/queen preservation and worker closure, active-work and battle locks.");

{
 var flagWorld=new WorldSetup();flagWorld.SetWorld(0);flagWorld.SetCast(123);flagWorld.SetPlacement(456);flagWorld.SelectPlayer(0);flagWorld.Relations.SetSuperior(1,0);
 flagWorld.Robots.Initialize(flagWorld.Nodes.All.Length,flagWorld.ActiveCount);var jobs=flagWorld.Robots;flagWorld.Nodes.All[0].Owner=0;flagWorld.Nodes.All[1].Owner=1;jobs.ConfigureFactories(flagWorld);jobs.Training[1].DeploymentPolicy=1;
 var mixed=jobs.Create(0,RobotParts.Complete) with {HeadOrigin=0,BodyOrigin=1,LegsOrigin=1};jobs.Nodes[0].Add(mixed);
 Check(mixed.ShoeOwner<0,"Assembled complete robot is barefoot before reservation");
 Check(jobs.ToggleReservation(0,mixed.Id,HangarReservation.Defense,1),"Present part country can reserve mixed defender");
 var working=jobs.Nodes[0].Robots.Single();
 Check(working.Owner==1 && working.ShoeOwner==1 && working.DefenseFlag==new DefenseFlagReservation(1,0,0),"Commander and host flag are recorded separately");
 Check(working.HeadOrigin==0 && working.BodyOrigin==1 && working.LegsOrigin==1,"Flag changes shoes without recoloring parts");
 Check(!jobs.ToggleReservation(0,mixed.Id,HangarReservation.Defense,0) && jobs.Nodes[0].Robots.Single()==working,"Another country cannot steal or cancel the first defense reservation");
 Check(!jobs.ToggleReservation(0,mixed.Id,HangarReservation.Worker,0),"Other roles cannot bypass reservation commander");
 var resumed=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(jobs)).Nodes[0].Robots.Single();Check(resumed.DefenseFlag==working.DefenseFlag && resumed.ShoeOwner==1,"Flag and shoes survive save");
 var legacyFlagSave=AutoSaveStore.Capture(jobs);foreach(var entry in legacyFlagSave.Objects.Where(e=>e.Type.StartsWith("FolderVerse.Robot,")))entry.Fields.Remove("<DefenseFlag>k__BackingField");
 Check(AutoSaveStore.Restore<RobotWorld>(legacyFlagSave).Nodes[0].Robots.Single().DefenseOnly,"Old saves without flag metadata remain readable");
 Check(jobs.ToggleReservation(0,mixed.Id,HangarReservation.Defense,1) && jobs.Nodes[0].Robots.Single().ShoeOwner<0,"Reserving country cancels and removes shoes");
 var partialRobot=jobs.Create(1,RobotParts.Legs);jobs.Nodes[0].Add(partialRobot);Check(!jobs.ToggleReservation(0,partialRobot.Id,HangarReservation.Defense,1) && partialRobot.ShoeOwner<0,"Incomplete robot cannot wear work shoes");
 var foreign=jobs.Create(0,RobotParts.Complete);jobs.Nodes[0].Add(foreign);Check(!jobs.ToggleReservation(0,foreign.Id,HangarReservation.Defense,1),"No affiliated part means no defense reservation");
 Check(jobs.ToggleReservation(0,mixed.Id,HangarReservation.Defense,1),"Can reserve again after cancellation");
 var local=jobs.Create(1,RobotParts.Complete);jobs.Nodes[1].Add(local);jobs.ToggleReservation(1,local.Id,HangarReservation.Defense,1);
 jobs.ReleaseDefenseFlag(0,0);Check(jobs.Nodes[0].Robots.Single(r=>r.Id==mixed.Id).ShoeOwner<0 && jobs.Nodes[1].Robots.Single().ShoeOwner==1,"Only reservations tied to the lost flag are released");
 Check(RobotWorld.ValidateDefenseFlag(working,_=>2).ShoeOwner<0,"Defender returning after flag loss also loses reservation");
 Console.WriteLine("PASS: barefoot assembly, mixed-country first claim, own-country cancellation, no-part/partial rejection, save compatibility and flag-specific release.");
}

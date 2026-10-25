using FolderVerse;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static (WorldSetup W,int Home,int Far) Fixture(){
 var w=new WorldSetup();w.SetWorld(123);w.SetCast(456);w.SetPlacement(789);w.Relations.Reset();w.SelectPlayer(0);
 w.Salvage.Reset();w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
 foreach(var n in w.Nodes.All)n.Owner=-1;
 int home=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0).Id;w.Nodes.All[home].Owner=0;
 w.ConquerorLocations[0]=w.Nodes.All[home].Cell;w.ConquerorPoints[0]=w.Nodes.All[home].Center;
 w.Salvage.EnsureAgents(w);
 int far=w.Nodes.All.Select(n=>(n.Id,Path:w.Salvage.Path(w,0,home,n.Id))).Where(p=>p.Path.Length>=3).OrderBy(p=>p.Path.Length).First().Id;
 return(w,home,far);
}
var (w,home,far)=Fixture();var plan=w.Salvage;var agent=plan.Agents.Single();agent.Policy=SalvagePolicy.Early;
var manufactured=w.Robots.Create(0,RobotParts.Complete).Materialize() with {CheapParts=RobotParts.Head,DamagedParts=RobotParts.Body};w.Robots.Nodes[home].Add(manufactured);
var abandoned=plan.Abandon(w,0,home,manufactured.Id,RobotParts.Head);
Check(w.Robots.Nodes[home].Robots.Single().Parts==(RobotParts.Body|RobotParts.Legs) && abandoned.AbandonedBy==0 && abandoned.Robot.IsCheap(RobotParts.Head),"Partial abandonment preserves remaining robot and provenance/quality");
Check(abandoned.Robot.Id!=manufactured.Id && abandoned.Robot.Origin(RobotParts.Head)==manufactured.Origin(RobotParts.Head),"Split cargo has unique identity and original manufacture country");
plan.Advance(w,1);Check(plan.Items.Count==1 && agent.Cargo.Count==0,"Own abandonment is not automatically collected");
Check(plan.Request(w,0,abandoned.Id),"Own abandonment can be explicitly requested");plan.Advance(w,2);Check(plan.Items.Count==0 && agent.Cargo.Single().IsCheap(RobotParts.Head),"Request permits collection of own discarded goods");
plan.Advance(w,3);Check(agent.Cargo.Count==0 && w.Robots.Nodes[home].Robots.Any(r=>r.Id==abandoned.Robot.Id),"Recovery delivers cargo into nearest owned hangar");
var free=new SalvageItem(100,home,w.Robots.Create(1,RobotParts.Body).Materialize() with {DamagedParts=RobotParts.Body});plan.Items.Add(free);
Check(plan.PickUp(w,0,free.Id) && w.Robots.Retinues[0].Robots.Single().Owner==0 && w.Robots.Retinues[0].Robots.Single().Origin(RobotParts.Body)==1 && w.Robots.Retinues[0].Robots.Single().IsDamaged(RobotParts.Body),"Conqueror picks up locally without losing damage or origin");
var defended=w.Robots.Create(0,RobotParts.Complete) with {DefenseOnly=true};w.Robots.Nodes[home].Add(defended);
try{plan.Abandon(w,0,home,defended.Id,defended.Parts);throw new Exception("Reserved abandonment accepted");}catch(InvalidOperationException){}

foreach(var policy in Enum.GetValues<SalvagePolicy>()){
 (w,home,far)=Fixture();plan=w.Salvage;agent=plan.Agents.Single();agent.Policy=policy;
 plan.Items.Add(new(101,far,w.Robots.Create(1,RobotParts.Head)));
 int distance=plan.Path(w,0,home,far).Length-1;
 plan.Advance(w,1);Check(agent.Node!=home && plan.Path(w,0,home,agent.Node).Length==2,"Agent advances exactly one connected edge each global turn");
 int at=agent.Node;plan.Advance(w,1);Check(agent.Node==at,"Same global turn cannot advance agent twice");
 for(int turn=2;turn<distance*2+12;turn++)plan.Advance(w,turn);
 Check(plan.Items.Count==0 && agent.Cargo.Count==0 && w.Robots.Nodes[home].Count==1,"All recovery policies eventually deliver finite stock");
}
(w,home,far)=Fixture();plan=w.Salvage;agent=plan.Agents.Single();agent.Policy=SalvagePolicy.Early;
plan.Items.Add(new(102,home,w.Robots.Create(1,RobotParts.Head)));agent.Paused=true;plan.Advance(w,1);Check(plan.Items.Count==1,"Paused recovery does not take goods");agent.Paused=false;
for(int i=0;i<12;i++)w.Robots.Nodes[home].Add(w.Robots.Create(0,RobotParts.Legs));plan.Advance(w,2);Check(plan.Items.Count==1 && agent.Cargo.Count==0,"No accepting hangar defers pickup without deleting goods");
w.Robots.Nodes[home].Remove(w.Robots.Nodes[home].Robots[0].Id);plan.Advance(w,3);Check(agent.Cargo.Count==1,"Freed space permits recovery");
for(int i=0;w.Robots.Nodes[home].HasSpace;i++)w.Robots.Nodes[home].Add(w.Robots.Create(0,RobotParts.Legs));plan.Advance(w,4);Check(agent.Cargo.Count==1,"Full destination preserves undelivered cargo");
var saved=AutoSaveStore.Capture(w);var restored=AutoSaveStore.Restore<WorldSetup>(saved);
Check(restored.Salvage.Agents.Single().Cargo.Count==1 && restored.Salvage.LastTurn==4,"Cargo, agent and global-turn guard survive save");
foreach(var e in saved.Objects)if(e.Type.StartsWith("FolderVerse.WorldSetup,") && e.Fields!=null)e.Fields.Remove("<Salvage>k__BackingField");
restored=AutoSaveStore.Restore<WorldSetup>(saved);Check(restored.Salvage!=null && restored.Salvage.Items.Count==0,"Legacy saves gain empty recovery state");
Check(restored.Salvage.LegacyPlacementPending,"Legacy recovery placement is scheduled once on resume");restored.Salvage.Initialize(restored);Check(!restored.Salvage.LegacyPlacementPending && restored.Salvage.Items.Count>0,"Legacy placement creates finite initial goods without recurring spawns");

(w,home,far)=Fixture();plan=w.Salvage;agent=plan.Agents.Single();w.Nodes.All[far].Owner=1;
var foreignDrop=new SalvageItem(103,far,w.Robots.Create(1,RobotParts.Head));plan.Items.Add(foreignDrop);
Check(!plan.CanRecover(w,0,foreignDrop) && !plan.Request(w,0,foreignDrop.Id),"Recovery cannot trespass into enemy territory");
var created=new WorldSetup();created.SetWorld(123);created.SetCast(456);created.SetPlacement(789);
Check(created.Salvage.Items.Count>0 && created.Salvage.Items.All(i=>i.AbandonedBy==-1) && created.Salvage.Agents.Count==created.ActiveCount,"Initial goods are unowned and every landed country has one agent");
Console.WriteLine("PASS: abandonment, provenance/quality/damage, pickup, recovery policies and bounded movement, pauses/capacity, enemy borders and current/legacy saves.");

(w,home,far)=Fixture();plan=w.Salvage;agent=plan.Agents.Single();agent.Policy=SalvagePolicy.Bulk;
for(int i=0;i<3;i++)plan.Items.Add(new(200+i,home,w.Robots.Create(1,RobotParts.Complete)));
plan.Advance(w,1);Check(agent.Load==6 && agent.Cargo.Count==2 && plan.Items.Count==1,"Agent respects six physical-part capacity");
plan.Advance(w,2);Check(agent.Load==0 && w.Robots.Nodes[home].Count==2 && plan.Items.Count==1,"Bulk trip unloads without duplicating or deleting overflow goods");

(w,home,far)=Fixture();plan=w.Salvage;agent=plan.Agents.Single();
plan.Items.Add(new(300,home,w.Robots.Create(1,RobotParts.Head)));
plan.Advance(w,1,n=>n==home);Check(plan.Items.Count==1 && agent.Cargo.Count==0,"Incoming combat blocks recovery before battle state is created");
plan.Advance(w,2);Check(plan.Items.Count==0 && agent.Cargo.Count==1,"Recovery resumes after combat restriction is removed");

(w,home,far)=Fixture();var neighbor=w.Routes.Neighbors(w.Nodes.All[home])[0];int sender=w.Nodes.At(neighbor.Target,neighbor.Entry).Id;w.Nodes.All[sender].Owner=0;
var anchor=w.Robots.Create(0,RobotParts.Legs);var head=w.Robots.Create(0,RobotParts.Head);w.Robots.Nodes[home].Add(anchor);w.Robots.Nodes[sender].Add(head);
w.Robots.DirectDelivery.Reserve(w,sender,home,head.Id,RobotParts.Head,anchor.Id,0);
try{w.Salvage.Abandon(w,0,home,anchor.Id,anchor.Parts);throw new Exception("Arrival anchor abandonment accepted");}catch(InvalidOperationException){}
Check(w.Robots.Nodes[home].Robots.Single().Id==anchor.Id && w.Salvage.Items.Count==0,"Abandonment preserves arrival anchor and pending delivery");
Console.WriteLine("PASS: recovery load capacity, incoming battle deferral and direct-delivery anchor protection.");

var tutorial=new WorldSetup();tutorial.PrepareTutorialStepTwo(123,456,789);tutorial.SelectPlayer(0);int tutorialHome=tutorial.Nodes.Current(0).Id;
tutorial.Nodes.All[tutorialHome].Owner=0;
var tutorialPart=tutorial.Robots.Create(0,RobotParts.Head);tutorial.Robots.Nodes[tutorialHome].Add(tutorialPart);
var tutorialDrop=tutorial.Salvage.Abandon(tutorial,0,tutorialHome,tutorialPart.Id,RobotParts.Head);
Check(tutorial.Salvage.Request(tutorial,0,tutorialDrop.Id),"Explicit recovery can be requested in tutorial");
tutorial.Salvage.Advance(tutorial,1);tutorial.Salvage.Advance(tutorial,2);
Check(tutorial.Robots.Nodes[tutorialHome].Robots.Any(r=>r.Id==tutorialPart.Id),"Explicit tutorial abandonment can be recovered without free initial loot");

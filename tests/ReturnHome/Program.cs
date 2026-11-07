using FolderVerse;
using System.Reflection;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
static void Advance(WorldSetup w)=>typeof(RobotWorld).GetMethod("AdvanceReturnHome",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots,new object[]{w});
static int[] Neighbors(WorldSetup w,int node)=>w.Routes.Neighbors(w.Nodes.All[node]).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray();
static WorldSetup Fixture(int placement=3){
    var w=new WorldSetup();w.SetParticipantCounts(1,3);w.SetWorld(1);w.SetCast(2);w.SetPlacement(placement);w.SelectPlayer(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);
    foreach(var n in w.Nodes.All)n.Owner=3;
    w.Nodes.All[0].Owner=0;return w;
}
static Robot Foreign(WorldSetup w,int head=1,int body=1,int legs=2)=>w.Robots.Create(1,RobotParts.Complete) with {HeadOrigin=head,BodyOrigin=body,LegsOrigin=legs};
{
    var w=Fixture();var dist=new Dictionary<int,int>{{0,0}};var queue=new Queue<int>();queue.Enqueue(0);
    while(queue.TryDequeue(out int at))foreach(int next in Neighbors(w,at))if(dist.TryAdd(next,dist[at]+1))queue.Enqueue(next);
    int target=dist.OrderByDescending(p=>p.Value).First().Key;w.Nodes.All[target].Owner=1;w.Nodes.All[Neighbors(w,0)[0]].Owner=2;
    var robot=Foreign(w);w.Robots.Nodes[0].Add(robot);
    Check(w.Robots.ToggleReturnHome(w,robot.Id,0,out _),"A complete foreign robot may be reserved");
    var reserved=w.Robots.Nodes[0].Robots.Single();Check(reserved.Stationed && !reserved.CanFight && w.Robots.AssemblyLocked(0,reserved) && !w.Robots.CanShip(0,reserved),"Pending return cannot fight, split or ship");
    Check(w.Robots.ToggleReturnHome(w,robot.Id,0,out _) && w.Robots.Nodes[0].Robots.Single().ReturnHomeCommander==null,"Reservation can be cancelled before departure");
    w.Robots.ToggleReturnHome(w,robot.Id,0,out _);Advance(w);
    Check(w.Robots.Nodes[0].Count==0 && w.Robots.Nodes[0].ReservedReturns==0,"Departure frees the original hangar without reserving a return slot");
    Check(w.Robots.ReturningRobots.Single().Country==1,"Majority origin wins over a nearer minority country");
    Check(w.Robots.Nodes.Skip(1).All(s=>s.Robots.All(r=>r.Id!=robot.Id)),"Foreign transit consumes no intermediate hangar slot");
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    for(int i=1;i<dist[target];i++)Advance(w);
    var home=w.Robots.Nodes[target].Robots.Single(r=>r.Id==robot.Id);
    Check(w.Robots.ReturningRobots.Count==0 && home.Owner==1 && home.ReturnHomeCommander==null && home.HeadOrigin==1 && home.LegsOrigin==2,"One-hop-per-delivery journey survives save and preserves all parts on arrival");w.Robots.Check();
}
{
    var winners=new HashSet<int>();
    for(int seed=0;seed<50;seed++){
        var w=Fixture(seed);foreach(var node in w.Nodes.All)if(node.Id!=0)node.Owner=-1;var adjacent=Neighbors(w,0);Check(adjacent.Length>=2,"Fixture has two entrances");
        w.Nodes.All[adjacent[0]].Owner=1;w.Nodes.All[adjacent[1]].Owner=2;
        var robot=Foreign(w,1,2,3);w.Robots.Nodes[0].Add(robot);
        w.Robots.ToggleReturnHome(w,robot.Id,0,out _);Advance(w);
        int winner=w.Robots.Nodes.SelectMany(s=>s.Robots).Single(r=>r.Id==robot.Id).Owner;
        Check(winner is 1 or 2,"Equal origins choose a nearest country");winners.Add(winner);
    }
    Check(winners.Count==2,"Equal-distance countries are randomly selected rather than always taking the first");
}
{
    var w=Fixture();int target=Neighbors(w,0)[0];w.Nodes.All[target].Owner=1;
    var store=w.Robots.Nodes[target];for(int i=0;i<store.TotalCapacity;i++)store.Add(w.Robots.Create(1,RobotParts.Head));
    var robot=Foreign(w);w.Robots.Nodes[0].Add(robot);w.Robots.ToggleReturnHome(w,robot.Id,0,out _);Advance(w);
    Check(w.Robots.ReturningRobots.Single().Node==target && w.Robots.ReturningRobots.Single().Status.Contains("満杯"),"Full destination waits in transit without overflowing its hangar");
    store.Remove(store.Robots.First().Id);Advance(w);
    Check(w.Robots.ReturningRobots.Count==0 && store.Count==store.TotalCapacity && store.Robots.Any(r=>r.Id==robot.Id),"Waiting cargo enters exactly once when capacity becomes available");
}
{
    var w=Fixture();var robot=Foreign(w);w.Robots.Nodes[0].Add(robot);
    Check(w.Robots.ToggleReturnHome(w,robot.Id,0,out string message) && message==RobotWorld.NoHomeCountry,"Missing origin countries show the required reason");Advance(w);
    Check(w.Robots.Nodes[0].Robots.Single().ReturnHomeStatus=="帰る国がもうありません。" && w.Robots.ReturningRobots.Count==0,"No-home reservation misfires without deleting the robot");
    w.Robots.ToggleReturnHome(w,robot.Id,0,out _);
    var native=robot with {HeadOrigin=0};w.Robots.Nodes[0].Remove(robot.Id);w.Robots.Nodes[0].Add(native);
    Check(!w.Robots.ToggleReturnHome(w,native.Id,0,out _),"Even one domestic part prevents repatriation");
    var legacy=AutoSaveStore.Capture(w);foreach(var entry in legacy.Objects)if(entry.Fields!=null){entry.Fields.Remove("<ReturningRobots>k__BackingField");entry.Fields.Remove("<ReturnHomeCommander>k__BackingField");entry.Fields.Remove("<ReturnHomeStatus>k__BackingField");}
    var restored=AutoSaveStore.Restore<WorldSetup>(legacy);Check(restored.Robots.ReturningRobots.Count==0 && restored.Robots.Nodes[0].Robots.Single().ReturnHomeCommander==null,"Old saves default to no return reservations");
}
{
    var w=Fixture();var distances=new Dictionary<int,int>{{0,0}};var queue=new Queue<int>();queue.Enqueue(0);
    while(queue.TryDequeue(out int at))foreach(int next in Neighbors(w,at))if(distances.TryAdd(next,distances[at]+1))queue.Enqueue(next);
    int target=distances.OrderByDescending(p=>p.Value).First().Key;w.Nodes.All[target].Owner=1;
    var robot=Foreign(w,1,1,1);w.Robots.Nodes[0].Add(robot);w.Robots.ToggleReturnHome(w,robot.Id,0,out _);Advance(w);
    Check(w.Robots.ReturningRobots.Count==1,"Fixture departs before country disappears");
    int merged=Neighbors(w,0).First(n=>n!=target);w.Nodes.All[merged].Owner=0;
    w.Campaign.FunctionTransfers.Reserve(w,0,merged);
    typeof(NodeFunctionTransfers).GetMethod("Resolve",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Campaign.FunctionTransfers,new object[]{w});
    Check(w.Robots.ReturningRobots.Single().Source==merged,"Function transfer carries the origin inspector's in-transit report to its new host");
    w.Relations.Powered[1]=false;w.Nodes.All[target].Owner=-1;Advance(w);
    Check(w.Robots.ReturningRobots.Single().Status==RobotWorld.NoHomeCountry,"Country lost during transit retains the cargo with the required failure reason");
    w.Relations.Powered[1]=true;w.Nodes.All[target].Owner=1;
    for(int i=0;i<distances[target]+1 && w.Robots.ReturningRobots.Count>0;i++)Advance(w);
    Check(w.Robots.Nodes[target].Robots.Any(r=>r.Id==robot.Id),"A revived home country permits a waiting journey to continue");
}
Console.WriteLine("PASS: foreign-only eligibility, cancellation, majority/nearest/random destinations, one-hop foreign transit, save/resume, full hangar waiting, lost/revived countries, no-country reason and legacy saves.");

using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static (WorldSetup W,int Source,int Target,int Enemy,int Alternate) Fixture(){
    var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    foreach(var n in w.Nodes.All)n.Owner=1;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    var battle=w.Campaign.RobotBattles;
    var chain=(from b in w.Nodes.All
        from c in battle.MarchTargets(w,b.Id)
        from a in battle.MarchTargets(w,b.Id)
        where a!=c && !battle.MarchTargets(w,a).Contains(c) && FunctionalDeliveryRoutes.Connected(w,a,b.Id)
        from d in battle.MarchTargets(w,c)
        where d!=a && d!=b.Id && battle.CanAttackFrom(w,1,d,c)
        select (A:a,B:b.Id,C:c,D:d)).First();
    w.Nodes.All[chain.C].Owner=2;
    w.Robots.Nodes[chain.A].Add(w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true});
    w.Robots.Nodes[chain.A].Add(w.Robots.Create(1,RobotParts.Complete));
    while(w.Robots.Nodes[chain.A].HasSpace)w.Robots.Nodes[chain.A].Add(w.Robots.Create(1,RobotParts.Head));
    while(w.Robots.Nodes[chain.B].HasSpace)w.Robots.Nodes[chain.B].Add(w.Robots.Create(1,RobotParts.Head));
    w.Robots.Nodes[chain.C].Add(w.Robots.Create(2,RobotParts.Complete) with {DefenseOnly=true});
    return(w,chain.A,chain.B,chain.C,chain.D);
}
var (w,source,target,enemy,alternate)=Fixture();
long ready=w.Robots.Nodes[source].Robots.Single(r=>r.CanFight && !r.DefenseOnly).Id;
int leader=w.Relations.Leader;
ComputerLogistics.Plan(w,true);
Check(w.Robots.LongDelivery.Orders.Any(o=>o.RobotId==ready),"Ordinary front shipment is reserved first");
Check(!w.Robots.DirectDelivery.Orders.Any(o=>o.RobotId==ready),"Recovery exchange does not steal normally shipped soldier");
Check(w.Relations.Leader==leader,"Shipping priority leaves display leader unchanged");
(w,source,target,enemy,alternate)=Fixture();
foreach(var n in w.Nodes.All.Where(n=>n.Owner==1 && w.Campaign.RobotBattles.MarchTargets(w,n.Id).Contains(enemy)))
    while(w.Robots.Nodes[n.Id].HasSpace)w.Robots.Nodes[n.Id].Add(w.Robots.Create(1,RobotParts.Head));
ComputerLogistics.Plan(w,true);
Check(w.Robots.DirectDelivery.Orders.Count==4,"Recovery swap remains possible when ordinary front slots are full");
(w,source,target,enemy,alternate)=Fixture();w.SetParticipantCounts(1,2);w.SelectPlayer(1);w.HumanPlayers.Add(1);
ComputerLogistics.Plan(w);
Check(!w.Robots.DirectDelivery.Orders.Any(o=>o.Owner==1) && !w.Robots.LongDelivery.Orders.Any(o=>o.Commander==1),"Manual country is protected");
Console.WriteLine("PASS normal shipment priority, no double reservation, full-front fallback, unchanged leader and manual protection");
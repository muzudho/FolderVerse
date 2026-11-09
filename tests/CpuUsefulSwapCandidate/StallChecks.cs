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
    w.Robots.Nodes[chain.B].Add(w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true});
    w.Robots.Nodes[chain.B].Add(w.Robots.Create(1,RobotParts.Complete));
    while(w.Robots.Nodes[chain.B].HasSpace)w.Robots.Nodes[chain.B].Add(w.Robots.Create(1,RobotParts.Head));
    w.Robots.Nodes[chain.C].Add(w.Robots.Create(2,RobotParts.Complete) with {DefenseOnly=true});
    return(w,chain.A,chain.B,chain.C,chain.D);
}
var (w,source,target,enemy,alternate)=Fixture();int leader=w.Relations.Leader;
ComputerLogistics.Plan(w,true);
Check(w.Robots.DirectDelivery.Orders.Count==4,"Stalled army uses a two-way exchange at a full front");
Check(w.Relations.Leader==leader,"Fallback does not change UI leader");
(w,source,target,enemy,alternate)=Fixture();
for(int i=0;i<3;i++)w.Robots.Nodes[alternate].Add(w.Robots.Create(1,RobotParts.Complete));
ComputerLogistics.Plan(w,true);
Check(w.Robots.DirectDelivery.Orders.Count==0,"A currently viable assault prevents the recovery exchange");
(w,source,target,enemy,alternate)=Fixture();w.SetParticipantCounts(1,2);w.SelectPlayer(1);w.HumanPlayers.Add(1);
ComputerLogistics.Plan(w);
Check(w.Robots.DirectDelivery.Orders.Count==0,"Manual rulers are excluded from recovery exchange");
Console.WriteLine("PASS stalled-only exchange, active-assault protection, unchanged leader and manual protection");

(w,source,target,enemy,alternate)=Fixture();
w.Nodes.All[alternate].Owner=0;
for(int i=0;i<3;i++)w.Robots.Nodes[alternate].Add(w.Robots.Create(0,RobotParts.Complete));
ComputerLogistics.Plan(w,true);
Check(w.Robots.DirectDelivery.Orders.Any(o=>o.Source==source && o.Target==target),"An independent faction's viable attack must not block this stalled faction's exchange");
Console.WriteLine("PASS independent-faction attack does not suppress local recovery exchange");
(w,source,target,enemy,alternate)=Fixture();
foreach(var r in w.Robots.Nodes[target].Robots.Where(r=>r.Parts==RobotParts.Complete).ToArray())w.Robots.Nodes[target].Remove(r.Id);
while(w.Robots.Nodes[target].HasSpace)w.Robots.Nodes[target].Add(w.Robots.Create(1,RobotParts.Head));
ComputerLogistics.Plan(w,true);
Check(w.Robots.DirectDelivery.Orders.Count==0,"Do not exchange when one arrival still cannot unlock an assault");
Console.WriteLine("PASS useful-exchange requirement rejects insufficient one-unit recovery");
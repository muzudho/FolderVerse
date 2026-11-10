using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static (WorldSetup W,int A,int B,Robot Weak,Robot Premium) Fixture(bool fill=true){
    var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    foreach(var n in w.Nodes.All)n.Owner=1;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);
    w.ConquerorLocations[1]=a.Cell;w.ConquerorPoints[1]=a.Center;
    w.Robots.Nodes[a.Id].Add(w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true});
    var weak=w.Robots.Create(1,RobotParts.Complete) with {BodyOrigin=2,LegsOrigin=2};
    var premium=w.Robots.Create(1,RobotParts.Complete) with {PremiumParts=RobotParts.Head};
    if(fill){w.Robots.Nodes[a.Id].Add(weak);for(int i=0;i<2;i++)w.Robots.Nodes[a.Id].Add(w.Robots.Create(1,RobotParts.Complete));w.Robots.Nodes[a.Id].Add(premium);}
    return(w,a.Id,b.Id,weak,premium);
}
var (w,a,b,weak,premium)=Fixture();ComputerLogistics.Plan(w,true);w.Robots.Check();
Check(w.Robots.Retinues[1].Count==4 && w.Robots.Retinues[1].Robots[0].Id==premium.Id,"Stalled queen loads strong pocket escorts through ordinary transfer");
Check(w.Robots.Nodes[a].Count==1 && w.Robots.Nodes[a].Robots[0].DefenseOnly,"Home guard is preserved");
(w,a,b,weak,premium)=Fixture();w.Robots.Training[1].DeploymentPolicy=2;ComputerLogistics.Plan(w,true);
Check(w.Robots.Retinues[1].Robots[0].Id==weak.Id,"Explicit deployment preference is preserved");
(w,a,b,weak,premium)=Fixture();w.SetParticipantCounts(1,2);w.SelectPlayer(1);w.HumanPlayers.Add(1);ComputerLogistics.Plan(w);
Check(w.Robots.Retinues[1].Count==0,"Manual pocket is not changed");
(w,a,b,weak,premium)=Fixture();w.Relations.Leader=1;w.Robots.DirectDelivery.Reserve(w,a,b,weak.Id,RobotParts.Head,null,0);w.Relations.Leader=0;ComputerLogistics.Plan(w,true);
Check(w.Robots.Retinues[1].Count==0 && w.Robots.DirectDelivery.Orders.Count==1,"Existing work reservation is preserved");
(w,a,b,weak,premium)=Fixture(false);w.Robots.Nodes[b].Add(w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true});
for(int i=0;i<2;i++)w.Robots.Nodes[b].Add(w.Robots.Create(1,RobotParts.Complete));var orders=new List<March>();ComputerJointAttacks.Plan(w,orders,true);
Check(orders.Any(o=>o.Ruler==1 && o.Queen && o.Peaceful && o.TargetNode==b),"Idle queen can seek an own-country escort depot");
Console.WriteLine("PASS pocket supply, equipment and policy ordering, guard, manual and reservation protection, depot recovery route");

using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);a.Owner=b.Owner=1;
w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
var robot=w.Robots.Create(1,RobotParts.Complete);w.Robots.Nodes[a.Id].Add(robot);
bool rejected=false;try{w.Robots.DirectDelivery.ReserveParts(w,a.Id,b.Id,robot.Id,RobotParts.Complete,null,0);}catch(InvalidOperationException){rejected=true;}
Check(rejected && w.Robots.DirectDelivery.Orders.Count==0,"Public API still rejects a different country");
w.Relations.SetSuperior(1,0);w.Robots.DirectDelivery.ReserveParts(w,a.Id,b.Id,robot.Id,RobotParts.Complete,null,0);
Check(w.Robots.DirectDelivery.Orders.Count==3 && w.Relations.Leader==0,"Public subordinate authority still works without changing leader");
w.Robots.Advance((from,to)=>FunctionalDeliveryRoutes.Connected(w,from,to),n=>w.Nodes.All[n].Owner,1,industry:false);w.Robots.Check();
Check(w.Robots.Nodes[a.Id].Count==0 && w.Robots.Nodes[b.Id].Robots.Single().Parts==RobotParts.Complete,"Public whole robot delivery still resolves");
Console.WriteLine("PASS public authority rejection, subordinate control and normal delivery");

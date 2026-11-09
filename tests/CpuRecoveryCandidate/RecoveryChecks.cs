using FolderVerse;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static void Recovery(WorldSetup w,List<March> orders,bool all=true)=>typeof(WorldSetup).Assembly.GetType("FolderVerse.ComputerRecoveryMoves")!
    .GetMethod("Plan",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{w,orders,all});
static (WorldSetup W,int A,int B) Fixture(){
    var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    foreach(var n in w.Nodes.All)n.Owner=1;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);
    w.ConquerorLocations[1]=a.Cell;w.ConquerorPoints[1]=a.Center;
    for(int i=0;i<2;i++)w.Robots.Nodes[b.Id].Add(w.Robots.Create(2,RobotParts.Head));
    return(w,a.Id,b.Id);
}
var (w,a,b)=Fixture();int leader=w.Relations.Leader;var orders=new List<March>();Recovery(w,orders);
Check(orders.Single().Ruler==1 && orders[0].TargetNode==b && orders[0].Peaceful && orders[0].Fighters==0,"Idle queen follows a legal friendly route to captured parts");
Check(w.Relations.Leader==leader,"Recovery does not change displayed leader");
w.Robots.Nodes[a].Add(w.Robots.Create(2,RobotParts.Head));orders.Clear();Recovery(w,orders);
Check(orders.Count==0,"Queen stays to finish existing local persuasion");
(w,a,b)=Fixture();orders=new(){new(1,b,0,a,Queen:true,PocketOnly:true,Peaceful:true)};var original=orders[0];Recovery(w,orders);
Check(orders.Count==1 && ReferenceEquals(orders[0],original),"Existing queen order is preserved");
(w,a,b)=Fixture();int enemy=w.Campaign.RobotBattles.MarchTargets(w,b).First(n=>n!=a && w.Campaign.RobotBattles.MarchTargets(w,n).Contains(b));
w.Nodes.All[enemy].Owner=2;var attacker=w.Robots.Create(2,RobotParts.Complete);w.Robots.Nodes[enemy].Add(attacker);
orders=new(){new(2,b,1,enemy,Queen:false,RobotIds:new[]{attacker.Id})};Recovery(w,orders);
Check(orders.Count==1,"Existing assault suppresses the recovery plan");
(w,a,b)=Fixture();w.SetParticipantCounts(1,2);w.SelectPlayer(1);w.HumanPlayers.Add(1);orders.Clear();Recovery(w,orders,false);
Check(orders.Count==0,"Manual queen is not moved");
(w,a,b)=Fixture();foreach(var node in w.Nodes.All)node.Owner=-1;w.Nodes.All[a].Owner=1;
int far=w.Nodes.All.First(n=>RobotLongDelivery.Path(w,a,n.Id,1).Length>=3).Id;w.Nodes.All[far].Owner=1;
for(int i=0;i<2;i++)w.Robots.Nodes[far].Add(w.Robots.Create(2,RobotParts.Head));orders.Clear();Recovery(w,orders);
Check(orders.Count==0,"Recovery does not route through unowned territory");
Console.WriteLine("PASS legal recovery route, stay for persuasion, existing orders, assault priority, manual control and friendly-only path");

using FolderVerse;
using System.Reflection;
static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
static void Exchange(WorldSetup w,int a,int b,long ready,long partial)=>typeof(RobotDirectDelivery).GetMethod("ReserveComputerExchange",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.DirectDelivery,new object[]{w,a,b,ready,partial,1});
static void Reject(Action action){try{action();}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){return;}throw new Exception("Invalid exchange succeeded");}
static (WorldSetup W,int A,int B,Robot Ready,Robot Head,Robot Guard) Fixture(bool foreign=false){
    var w=new WorldSetup();w.SetParticipantCounts(0,3);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);a.Owner=b.Owner=1;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var shop in w.Robots.Workshops)shop.AutoAssemblyDisabled=true;
    var ready=w.Robots.Create(1,RobotParts.Complete);var guard=w.Robots.Create(1,RobotParts.Complete) with {DefenseOnly=true};
    var head=w.Robots.Create(foreign?2:1,RobotParts.Head);w.Robots.Nodes[a.Id].Add(ready);w.Robots.Nodes[a.Id].Add(guard);w.Robots.Nodes[b.Id].Add(head);
    while(w.Robots.Nodes[a.Id].HasSpace)w.Robots.Nodes[a.Id].Add(w.Robots.Create(1,RobotParts.Head));
    while(w.Robots.Nodes[b.Id].HasSpace)w.Robots.Nodes[b.Id].Add(w.Robots.Create(1,RobotParts.Head));
    return(w,a.Id,b.Id,ready,head,guard);
}
static void Resolve(WorldSetup w)=>w.Robots.Advance((a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,1,industry:false);
var (w,a,b,ready,head,guard)=Fixture();int leader=w.Relations.Leader;
Reject(()=>Exchange(w,a,b,guard.Id,head.Id));Check(w.Robots.DirectDelivery.Orders.Count==0,"Guard cannot be exchanged");
Exchange(w,a,b,ready.Id,head.Id);Check(w.Relations.Leader==leader,"Computer reservation does not modify displayed leader");
Check(w.Robots.DirectDelivery.Orders.Count==4,"Both directions are reserved together");
Reject(()=>Exchange(w,a,b,ready.Id,head.Id));Check(w.Robots.DirectDelivery.Orders.Count==4,"Duplicate reservation leaves previous exchange intact");
Check(w.Robots.DirectDelivery.Preview(w.Robots,a).Length==12 && w.Robots.DirectDelivery.Preview(w.Robots,b).Length==12,"Full hangar previews preserve occupancy");
AutoSaveStore.Write(args[0],w);var restored=AutoSaveStore.Read<WorldSetup>(args[0]);Resolve(restored);restored.Robots.Check();
Check(restored.Robots.Nodes[a].Count==12 && restored.Robots.Nodes[b].Count==12,"Saved exchange resolves without extra capacity");
Check(restored.Robots.Nodes[b].Robots.Count(r=>r.CanFight)==1 && restored.Robots.Nodes[a].Robots.Any(r=>r.Id==guard.Id && r.DefenseOnly),"Soldier reaches destination and guard stays home");
foreach(var order in w.Robots.DirectDelivery.Orders.ToArray())w.Robots.DirectDelivery.Cancel(order.Number);
Resolve(w);Check(w.Robots.Nodes[a].Robots.Any(r=>r.Id==ready.Id) && w.Robots.Nodes[b].Robots.Any(r=>r.Id==head.Id),"Cancelling both directions preserves original robots");
(w,a,b,ready,head,guard)=Fixture(true);Reject(()=>Exchange(w,a,b,ready.Id,head.Id));
Check(w.Robots.DirectDelivery.Orders.Count==0,"Late permission failure rolls back first direction");
w.Robots.Nodes[b].Remove(head.Id);w.Robots.Nodes[b].Add(head with {Owner=1});Exchange(w,a,b,ready.Id,head.Id);
Check(w.Robots.DirectDelivery.Orders[0].Number==1,"Rollback also restores reservation numbering");
Console.WriteLine("PASS atomic exchange, permission rollback, duplicate protection, capacity, guard, cancellation and save/resume");

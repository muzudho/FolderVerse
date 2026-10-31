using FolderVerse;
using System.Text.Json;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static RobotWorld World(){var w=new RobotWorld();w.Initialize(2,2);w.Reservations=new ConquestCampaign();return w;}
foreach(var kind in new[]{HangarReservation.Defense,HangarReservation.Intercept,HangarReservation.Worker}){
 var w=World();var r=w.Create(0,RobotParts.Complete) with {DamagedParts=RobotParts.Complete,DefenseOnly=kind==HangarReservation.Defense,DefenseFlag=kind==HangarReservation.Defense?new(0,0,0):null,InterceptOnly=kind==HangarReservation.Intercept,FactoryWorker=kind==HangarReservation.Worker};
 w.Nodes[0].Add(r);w.RevalidateWorkReservations();var next=w.Nodes[0].Robots.Single();
 Check(!next.Stationed && next.ShoeOwner<0 && next.DamagedParts==r.DamagedParts,"All-damaged robots immediately lose their work and shoes without losing parts");
}
{
 var w=World();var wreck=w.Create(1,RobotParts.Complete) with {HeadOrigin=0,BodyOrigin=0,LegsOrigin=0,DamagedParts=RobotParts.Complete,DefenseOnly=true,DefenseFlag=new(1,0,1)};w.Nodes[0].Add(wreck);
 w.RevalidateWorkReservations();var freed=w.Nodes[0].Robots.Single();
 Check(w.CanTransferPocket(0,0,freed),"Manufacturing origins permit collection even when all native parts are damaged and the old owner differs");
 w.TransferPocket(0,0,freed.Id,true);var pocket=w.Retinues[0].Robots.Single();
 Check(pocket.Owner==0 && pocket.DamagedParts==RobotParts.Complete && !pocket.Stationed && w.Nodes[0].Count==0,"The rescued wreck reaches the pocket unchanged and barefoot");
}
{
 var w=World();var bad=w.Create(0,RobotParts.Complete) with {DamagedParts=RobotParts.Complete};var good=w.Create(0,RobotParts.Complete);w.Nodes[0].Add(bad);w.Nodes[0].Add(good);
 w.Reservations.Plan(new(0,1,2,0,false,false,new[]{bad.Id,good.Id}));w.RevalidateWorkReservations();
 Check(w.Reservations.PlannedOrders.Single().RobotIds.SequenceEqual(new[]{good.Id}) && w.Reservations.PlannedOrders.Single().Fighters==1,"Only ineligible sortie members are removed");
 w.Retinues[0].Add(bad);w.Nodes[0].Remove(bad.Id);w.Reservations.Plan(new(0,1,1,0,true,true,new[]{bad.Id}));w.RevalidateWorkReservations();
 Check(w.Reservations.PlannedOrders.Any(o=>o.Queen && o.Fighters==0),"Losing a pocket soldier does not cancel the queen's own movement");
}
{
 var w=World();w.Training[0].DeploymentPolicy=1;var mixed=w.Create(0,RobotParts.Complete) with {HeadOrigin=1,DefenseOnly=true,DefenseFlag=new(0,0,0)};w.Nodes[0].Add(mixed);
 w.RevalidateWorkReservations();Check(w.Nodes[0].Robots.Single().DefenseOnly,"A qualified mixed robot retains its job");w.Training[0].DeploymentPolicy=0;w.RevalidateWorkReservations();Check(!w.Nodes[0].Robots.Single().DefenseOnly,"Tightening training releases jobs that no longer qualify");
 var cheap=w.Create(0,RobotParts.Complete) with {CheapParts=RobotParts.Complete,DefenseOnly=true,DefenseFlag=new(0,0,0)};w.Nodes[0].Add(cheap);w.RevalidateWorkReservations();Check(w.Nodes[0].Robots.Single(r=>r.Id==cheap.Id).DefenseOnly,"Healthy cheap native parts remain qualified");
}
foreach(string difference in new[]{"none","country","damage","quality"}){
 var w=World();var a=w.Create(0,RobotParts.Complete);var b=w.Create(0,RobotParts.Complete) with {HeadOrigin=difference=="country"?1:0,DamagedParts=difference=="damage"?RobotParts.Head:0,CheapParts=difference=="quality"?RobotParts.Head:0};w.Nodes[0].Add(a);w.Nodes[0].Add(b);
 w.DirectDelivery.Orders.Add(new(1,0,1,a.Id,RobotParts.Head,null,0,0));w.DirectDelivery.Orders.Add(new(2,0,1,a.Id,RobotParts.Body,null,1,0));
 w.MovePart(0,a.Id,RobotParts.Head,b.Id,0);
 Check(w.Nodes[0].Robots.Select(r=>r.Id).SequenceEqual(new[]{a.Id,b.Id}),"Swap retains both robot IDs");
 Check(w.DirectDelivery.Orders.Any(o=>o.Number==1)==(difference=="none") && w.DirectDelivery.Orders.Any(o=>o.Number==2),"Equivalent swaps retain delivery; changed part attributes cancel only affected orders");
}
{
 var w=World();var a=w.Create(0,RobotParts.Complete);var b=w.Create(1,RobotParts.Complete) with {HeadOrigin=0,LastDomesticPersuasionTurn=7};w.Nodes[0].Add(a);w.Nodes[0].Add(b);
 w.DirectDelivery.Orders.Add(new(1,0,1,a.Id,RobotParts.Head,null,0,0));w.DirectDelivery.Orders.Add(new(2,0,1,b.Id,RobotParts.Head,null,1,1));
 string before=JsonSerializer.Serialize(w.Nodes[0].Robots);w.MovePart(0,a.Id,RobotParts.Head,b.Id,0);
 Check(before==JsonSerializer.Serialize(w.Nodes[0].Robots) && w.DirectDelivery.Orders.Count==2,"Equal physical parts do not retag robot owners or merge histories; both delivery orders survive");
}
{
 var w=World();var a=w.Create(0,RobotParts.Complete) with {DefenseOnly=true,DefenseFlag=new(0,0,0)};var b=w.Create(0,RobotParts.Complete);w.Nodes[0].Add(a);w.Nodes[0].Add(b);
 w.MovePart(0,a.Id,RobotParts.Head,b.Id,0);Check(w.Nodes[0].Robots.Single(r=>r.Id==a.Id).DefenseOnly,"Qualified own-country swaps keep the existing job");
 w.MovePart(0,a.Id,RobotParts.Head,null,0);Check(!w.Nodes[0].Robots.Single(r=>r.Id==a.Id).Stationed,"Removing a part from a complete robot cancels its job");
}
{
 var w=World();var a=w.Create(0,RobotParts.Complete) with {DefenseOnly=true,DefenseFlag=new(0,0,0)};var b=w.Create(0,RobotParts.Complete) with {HeadOrigin=1};w.Nodes[0].Add(a);w.Nodes[0].Add(b);
 w.MovePart(0,a.Id,RobotParts.Head,b.Id,0);Check(!w.Nodes[0].Robots.Single(r=>r.Id==a.Id).DefenseOnly,"A swap that fails training cannot keep its work reservation");
}
{
 var w=World();var a=w.Create(0,RobotParts.Complete);var b=w.Create(1,RobotParts.Complete) with {DefenseOnly=true,DefenseFlag=new(1,0,1)};w.Nodes[0].Add(a);w.Nodes[0].Add(b);w.DirectDelivery.Orders.Add(new(1,0,1,a.Id,RobotParts.Head,null,0,0));
 string before=JsonSerializer.Serialize(w.Nodes[0].Robots);bool rejected=false;try{w.MovePart(0,a.Id,RobotParts.Head,b.Id,0);}catch(InvalidOperationException){rejected=true;}
 Check(rejected && JsonSerializer.Serialize(w.Nodes[0].Robots)==before && w.DirectDelivery.Orders.Count==1,"Failed foreign-shoe swaps preserve both parts and delivery reservations atomically");
}
Console.WriteLine("PASS: qualification loss, training changes, wreck rescue, sortie trimming, queen preservation, equivalent/changed swaps, stable IDs, job continuation and atomic rejection.");

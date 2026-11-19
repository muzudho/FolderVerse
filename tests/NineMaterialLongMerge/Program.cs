using FolderVerse;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("FOLDERVERSE_PROBE_CONTENT_BASE"));
var initial=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};initial.SetParticipantCounts(1,1);initial.SetWorld(1);initial.SetCast(2);initial.SetPlacement(3);initial.SelectPlayer(0);initial.Robots.Initialize(initial.Nodes.All.Length,initial.ActiveCount);initial.Robots.ConfigureFactories(initial);
foreach(var n in initial.Nodes.All)n.Owner=0;int far=initial.Nodes.All.OrderByDescending(n=>RobotLongDelivery.Path(initial,0,n.Id,0).Length).First().Id;int target=RobotLongDelivery.Path(initial,0,far,0)[2];
var saved=AutoSaveStore.Capture(initial);int passed=0;
foreach(var m in Enum.GetValues<RobotMaterial>())foreach(var joint in new[]{RobotParts.Head|RobotParts.Body,RobotParts.Body|RobotParts.Legs}){var w=AutoSaveStore.Restore<WorldSetup>(saved);
var cargo=(w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=m,BodyMaterial=(RobotMaterial)(((int)m+1)%9),LegsMaterial=(RobotMaterial)(((int)m+2)%9),IntegratedParts=joint,DamagedParts=RobotParts.Body}).Materialize();w.Robots.Nodes[0].Add(cargo);
var receiver=(w.Robots.Create(0,RobotParts.Complete & ~joint) with{HeadMaterial=(RobotMaterial)(((int)m+3)%9),BodyMaterial=(RobotMaterial)(((int)m+4)%9),LegsMaterial=(RobotMaterial)(((int)m+5)%9)}).Materialize();w.Robots.Nodes[target].Add(receiver);
var expected=receiver.Merge(cargo.Slice(joint),0) with{Role=RobotRole.Captain};
var order=w.Robots.LongDelivery.Reserve(w,0,target,cargo.Id,RobotParts.Body,receiver.Id,0);
if(order.Parts!=joint)throw new Exception("joint selection not expanded");
for(int hop=0;hop<2;hop++){typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));}var arrived=w.Robots.Nodes[target].Robots.Single();
var left=w.Robots.Nodes[0].Robots.Single();
if(arrived!=expected||left!=cargo.Slice(RobotParts.Complete&~joint)||w.Robots.LongDelivery.Orders.Any())throw new Exception($"m={m} joint={joint} actualRole={arrived.Role} expectedRole={expected.Role} equalExceptRole={(arrived with{Role=expected.Role})==expected} sourceExact={left==cargo.Slice(RobotParts.Complete&~joint)} pending={w.Robots.LongDelivery.Orders.Count}");
Console.WriteLine($"PASS {m} {joint}: body selection keeps joint, two hops/save, exact complementary receiver merge and source remainder");passed++;}Console.WriteLine($"PASS {passed} mixed integrated parcel cases");
foreach(var roles in new[]{(Cargo:RobotRole.Soldier,Receiver:RobotRole.Captain),(Cargo:RobotRole.Soldier,Receiver:RobotRole.Soldier)}){
    var w=AutoSaveStore.Restore<WorldSetup>(saved);
    var cargo=w.Robots.Create(0,RobotParts.Body,roles.Cargo);
    var receiver=w.Robots.Create(0,RobotParts.Head,roles.Receiver);
    w.Robots.Nodes[0].Add(cargo);w.Robots.Nodes[target].Add(receiver);
    w.Robots.LongDelivery.Reserve(w,0,target,cargo.Id,RobotParts.Body,receiver.Id,0);
    for(int hop=0;hop<2;hop++)typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
    var arrived=w.Robots.Nodes[target].Robots.Single();
    if(arrived.Role!=roles.Receiver || arrived.Parts!=(RobotParts.Head|RobotParts.Body))throw new Exception("Receiver role regression");
    Console.WriteLine($"PASS incoming {roles.Cargo}, receiver {roles.Receiver}: role {arrived.Role}");
}
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
for(int hop=0;hop<2;hop++){
 typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
 var flight=w.Robots.Transport.LastTransfers.Last();
 var expectedVisual=cargo.Slice(joint) with{Id=flight.RobotId,LongDeliveryNumber=hop==0?order.Number:null};
 if(flight.Visual!=expectedVisual || flight.Parts!=joint || flight.Target!=(hop==0?RobotLongDelivery.Path(initial,0,target,0)[1]:target))throw new Exception("Actual long delivery flight lost mixed captain material/joint/damage metadata");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
}var arrived=w.Robots.Nodes[target].Robots.Single();
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
foreach(var material in Enum.GetValues<RobotMaterial>())foreach(var joint in new[]{RobotParts.Head|RobotParts.Body,RobotParts.Body|RobotParts.Legs}){
    var w=AutoSaveStore.Restore<WorldSetup>(saved);
    var cargo=(w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=(RobotMaterial)(((int)material+1)%9),LegsMaterial=(RobotMaterial)(((int)material+2)%9),IntegratedParts=joint,DamagedParts=RobotParts.Body}).Materialize();
    var receiver=w.Robots.Create(0,RobotParts.Complete&~joint);w.Robots.Nodes[0].Add(cargo);w.Robots.Nodes[target].Add(receiver);
    w.Robots.LongDelivery.Reserve(w,0,target,cargo.Id,RobotParts.Body,receiver.Id,0);
    typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));var pending=w.Robots.LongDelivery.Orders.Single();int at=pending.Node;
    var transit=w.Robots.Nodes[at].Robots.Single(r=>r.Id==pending.CargoId);
    w.Nodes.All[target].Owner=1;
    typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    if(w.Robots.LongDelivery.Orders.Any() || w.Robots.Nodes[at].Robots.Single(r=>r.Id==transit.Id)!=(transit with{LongDeliveryNumber=null}) ||
       w.Robots.Nodes[0].Robots.Single()!=cargo.Slice(RobotParts.Complete&~joint) || w.Robots.Nodes[target].Robots.Single()!=receiver ||
       w.Robots.Nodes.SelectMany(s=>s.Robots).Count(r=>r.Id==transit.Id)!=1)
        throw new Exception("Captured destination cancellation changed mixed captain parcel");
    Console.WriteLine($"PASS {material} {joint}: captured destination cancels after saved first hop, exact parcel/remainder/receiver preserved");
}
foreach(var material in Enum.GetValues<RobotMaterial>()){
    var w=AutoSaveStore.Restore<WorldSetup>(saved);
    var first=(w.Robots.Create(0,RobotParts.Complete,RobotRole.Captain) with{HeadMaterial=material,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Cardboard,IntegratedParts=RobotParts.Head|RobotParts.Body}).Materialize();
    var second=(first with{Id=w.Robots.Create(0,RobotParts.Complete).Id,IntegratedParts=RobotParts.Body|RobotParts.Legs}).Materialize();
    w.Robots.Nodes[0].Add(first);w.Robots.Nodes[0].Add(second);
    var a=w.Robots.LongDelivery.Reserve(w,0,target,first.Id,RobotParts.Body,null,0);
    var b=w.Robots.LongDelivery.Reserve(w,0,target,second.Id,RobotParts.Body,null,0,targetSlot:1);
    typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    var cancelled=w.Robots.LongDelivery.Orders.Single(o=>o.Number==a.Number);var continuing=w.Robots.LongDelivery.Orders.Single(o=>o.Number==b.Number);
    var parcel=w.Robots.Nodes[cancelled.Node].Robots.Single(r=>r.Id==cancelled.CargoId);
    var onward=w.Robots.Nodes[continuing.Node].Robots.Single(r=>r.Id==continuing.CargoId);
    if(!w.Robots.LongDelivery.Cancel(w.Robots,a.Number))throw new Exception("cancel failed");
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    if(w.Robots.LongDelivery.Orders.Single()!=continuing||w.Robots.Nodes[continuing.Node].Robots.Single(r=>r.Id==onward.Id)!=onward)throw new Exception("cancel modified other delivery");
    typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    if(w.Robots.LongDelivery.Orders.Any()||w.Robots.Nodes[cancelled.Node].Robots.Single(r=>r.Id==parcel.Id)!=(parcel with{LongDeliveryNumber=null})||w.Robots.Nodes[target].Robots.Single()!=(onward with{LongDeliveryNumber=null}))throw new Exception("partial cancel arrival regression");
    Console.WriteLine($"PASS {material}: cancelling one saved mixed captain parcel preserves other order and exact arrival");
}



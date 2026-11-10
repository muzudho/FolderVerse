using FolderVerse;
using System.Reflection;
using System.Text.Json;
var pairs=File.ReadLines(args[1]).Select(line=>JsonDocument.Parse(line)).Where(d=>d.RootElement.TryGetProperty("functionSource",out _))
    .Select(d=>d.RootElement.Clone()).Where(r=>!r.GetProperty("sourceFront").GetBoolean() && r.GetProperty("friendlyImmediateNeighbors").GetBoolean() &&
        !r.GetProperty("busySource").GetBoolean() && !r.GetProperty("busyTarget").GetBoolean() && !r.GetProperty("locked").GetBoolean() &&
        r.GetProperty("capacity").GetInt32()<=60 && r.GetProperty("reservedReturns").GetInt32()==0 && r.GetProperty("overflow").GetInt32()==0).ToArray();
foreach(var pair in pairs){
    object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
    int source=pair.GetProperty("functionSource").GetInt32(),target=pair.GetProperty("target").GetInt32(),owner=w.Nodes.All[target].Owner;
    var stock=w.Robots;var battles=w.Campaign.RobotBattles;
    int[] Outgoing()=>battles.MarchTargets(w,target).Where(t=>w.Nodes.All[t].Owner>=0 && !w.Relations.Allied(owner,w.Nodes.All[t].Owner)).Order().ToArray();
    int[] Incoming()=>w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && !w.Relations.Allied(owner,n.Owner) && battles.CanAttackFrom(w,n.Owner,n.Id,target)).Select(n=>n.Id).Order().ToArray();
    var beforeOut=Outgoing();var beforeIn=Incoming();int beforeCapacity=stock.Nodes[target].TotalCapacity,beforeAvailable=battles.AvailableFrom(w,owner,target);
    long[] ids=stock.Nodes.SelectMany(s=>s.Robots).Select(r=>r.Id).Order().ToArray();
    if(w.Campaign.FunctionTransfers.Orders.Count!=0)throw new Exception("Input already has transfer orders; isolate first.");
    int leader=w.Relations.Leader;bool allowed;
    try{w.Relations.Leader=owner;allowed=w.Campaign.FunctionTransfers.CanReserve(w,source,target);if(allowed)w.Campaign.FunctionTransfers.Reserve(w,source,target);}
    finally{w.Relations.Leader=leader;}
    if(!allowed){Console.WriteLine(JsonSerializer.Serialize(new{source,target,owner,allowed,phase=w.Campaign.Phase.ToString()}));continue;}
    typeof(NodeFunctionTransfers).GetMethod("Resolve",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Campaign.FunctionTransfers,new object[]{w});
    stock.Check();
    if(!ids.SequenceEqual(stock.Nodes.SelectMany(s=>s.Robots).Select(r=>r.Id).Order()))throw new Exception("Transfer changed robot identities.");
    if(w.Nodes.All[source].Functional || stock.Nodes[target].TotalCapacity!=pair.GetProperty("capacity").GetInt32() || w.Relations.Leader!=leader)throw new Exception("Transfer invariants failed.");
    Console.WriteLine(JsonSerializer.Serialize(new{source,target,owner,allowed,beforeCapacity,afterCapacity=stock.Nodes[target].TotalCapacity,
        beforeAvailable,afterAvailable=battles.AvailableFrom(w,owner,target),beforeOut,afterOut=Outgoing(),beforeIn,afterIn=Incoming(),
        newOutgoing=Outgoing().Except(beforeOut).ToArray(),newIncoming=Incoming().Except(beforeIn).ToArray(),sourceFunctional=w.Nodes.All[source].Functional}));
}
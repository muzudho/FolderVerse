using FolderVerse;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

if(args.Contains("--ui")){using var game=new FunctionTransferUi();game.Run();return;}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static void Reject(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Invalid transfer accepted");}
static (WorldSetup W,int A,int B,int C) Fixture()
{
    var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
    foreach(var n in w.Nodes.All)n.Owner=0;
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    foreach(var f in w.Robots.Workshops){f.RemoveLinesForTest();f.AutoAssemblyDisabled=true;}
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>=2);int b=w.Nodes.At(w.Routes.Neighbors(a)[0].Target,w.Routes.Neighbors(a)[0].Entry).Id;
    int c=w.Routes.Neighbors(a).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=b);
    return(w,a.Id,b,c);
}
static Robot Add(WorldSetup w,int n,RobotParts parts){var r=w.Robots.Create(0,parts);w.Robots.Nodes[n].Add(r);return r;}
{
    var(w,a,b,c)=Fixture();for(int i=0;i<12;i++){Add(w,a,RobotParts.Complete);Add(w,b,RobotParts.Complete);}
    w.Robots.Workshops[a].Configure(RobotParts.Head,15);var lane=w.Robots.Workshops[a];int turn=w.Population.Turn;
    w.Campaign.FunctionTransfers.Reserve(w,a,b);
    Check(w.Nodes.All[a].Functional && w.Robots.Nodes[a].Count==12,"Reservation leaves original functions in place until delivery");
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));saved.Robots.ConfigureFactories(saved);
    Check(saved.Campaign.FunctionTransfers.Orders.Count==1,"Function reservations survive saving");
    w.Campaign.FinishDomestic(w);
    Check(!w.Nodes.All[a].Functional && w.Nodes.All[a].FunctionHost==b && w.Nodes.All[b].FunctionUnits==2,"Merge leaves an empty origin referencing its active host");
    Check(w.Robots.Nodes[b].Count==24 && w.Robots.Nodes[b].TotalCapacity==24 && w.Robots.Nodes[a].TotalCapacity==0,"Merge preserves all cargo and combines storage capacity");
    Check(w.Robots.Workshops[b].ProductionLines.Contains(lane) && lane.Manufacturing && lane.ProductionPeriod==15,"Manufacturing plan and lane object survive relocation");
    Check(w.Population.Turn==turn && w.Campaign.Phase==CampaignPhase.Delivery,"Relocation occurs at delivery without consuming a global turn");
    Check(w.Robots.FunctionDisplayGroups(w)[a].Count==12 && NodeFunctionTransfers.Members(w,b).Contains(a),"Original display locations and conquered-sector group membership remain available");
    saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));saved.Robots.ConfigureFactories(saved);
    Check(saved.Robots.Nodes[b].Count==24 && saved.Robots.Workshops[b].ProductionLines.Length==w.Robots.Workshops[b].ProductionLines.Length && saved.Nodes.All[a].FunctionHost==b,"Merged functions and reference origins survive reconfiguration after loading");
    w.Campaign.BeginMilitary();Reject(()=>w.Campaign.FunctionTransfers.Reserve(w,b,a));w.Campaign.Advance(w,-1,0,enemies:false);
    Reject(()=>w.Campaign.FunctionTransfers.Reserve(w,b,a));
    Check(!w.Nodes.All[a].Functional && w.Nodes.All[b].Functional && w.Robots.Nodes[b].Count==24 && w.Robots.Nodes[b].TotalCapacity==24,"A completed transfer cannot be reversed");
}
{
    var(w,a,b,c)=Fixture();w.Campaign.FunctionTransfers.Reserve(w,b,a);w.Campaign.FunctionTransfers.Reserve(w,c,a);
    Reject(()=>w.Campaign.FunctionTransfers.Reserve(w,a,b));w.Campaign.FinishDomestic(w);
    Check(w.Nodes.All[a].FunctionUnits==3 && w.Robots.Nodes[a].TotalCapacity==36 && !w.Nodes.All[b].Functional && !w.Nodes.All[c].Functional,"Several origins can consolidate at the same host in one delivery");
    Check(FunctionalDeliveryRoutes.Targets(w,a).All(n=>w.Nodes.All[n].Functional),"Empty nodes are never delivery endpoints");
}
{
    var(w,a,b,c)=Fixture();w.Campaign.FunctionTransfers.Reserve(w,a,b);w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false);
    var r=Add(w,b,RobotParts.Head);
    Check(FunctionalDeliveryRoutes.Targets(w,b).Contains(c),"A functional destination can be reached through an empty node");
    w.Robots.DirectDelivery.Reserve(w,b,c,r.Id,r.Parts,null,0);w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[c].Robots.Any(x=>x.Id==r.Id) && w.Robots.Nodes[a].Count==0,"One delivery phase crosses the empty node without stopping there");
}
{
    var(w,a,b,c)=Fixture();
    int[] path=null!;
    foreach(var n in w.Nodes.All){
        var stack=new Stack<int[]>();stack.Push(new[]{n.Id});
        while(stack.Count>0){var p=stack.Pop();if(p.Length==4){path=p;break;}
            foreach(var r in w.Routes.Neighbors(w.Nodes.All[p[^1]])){int next=w.Nodes.At(r.Target,r.Entry).Id;if(!p.Contains(next))stack.Push(p.Append(next).ToArray());}}
        if(path!=null)break;
    }
    Check(path!=null,"Four-node logistics fixture exists");
    w.Campaign.FunctionTransfers.Reserve(w,path[1],path[0]);w.Campaign.FunctionTransfers.Reserve(w,path[2],path[3]);w.Campaign.FinishDomestic(w);
    w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false);
    var rbt=Add(w,path[0],RobotParts.Head);w.Robots.DirectDelivery.Reserve(w,path[0],path[3],rbt.Id,rbt.Parts,null,0);w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[path[3]].Robots.Any(r=>r.Id==rbt.Id) && !w.Nodes.All[path[1]].Functional && !w.Nodes.All[path[2]].Functional,"A delivery crosses several empty nodes in one phase");
}
{
    var(w,a,b,c)=Fixture();var cargo=Add(w,a,RobotParts.Head);
    var rule=w.Robots.Transport.AddInstruction(a,0,c);w.Robots.Transport.ConfigurePartChoices(a,0,rule.Number,c,1,false,1);w.Robots.Transport.SetInstructionRate(a,0,rule.Number,360);
    w.Campaign.FunctionTransfers.Reserve(w,a,b);w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[c].Robots.Any(r=>r.Id==cargo.Id) && w.Robots.Transport.Instructions(b,0).Single().Sent==1,"Relocated long-term instructions execute from the new host in the same delivery");
}
{
    var(w,a,b,c)=Fixture();var cargo=Add(w,a,RobotParts.Head);w.Robots.DirectDelivery.Reserve(w,a,b,cargo.Id,cargo.Parts,null,0);
    w.Campaign.FunctionTransfers.Reserve(w,a,b);w.Campaign.FinishDomestic(w);
    Check(w.Robots.DirectDelivery.Orders.Count==0 && w.Robots.Nodes[b].Robots.Any(r=>r.Id==cargo.Id),"A delivery absorbed by relocation does not remain queued against its own host");
}
{
    var(w,a,b,c)=Fixture();Add(w,a,RobotParts.Complete);w.Campaign.FunctionTransfers.Reserve(w,a,b);w.Nodes.All[b].Owner=1;w.Campaign.FinishDomestic(w);
    Check(w.Nodes.All[a].Functional && w.Robots.Nodes[a].Count==1 && w.Campaign.FunctionTransfers.LastResults.Single().Status=="移転失敗","Enemy capture cancels relocation without losing stock or functions");
}
{
    var(w,a,b,c)=Fixture();var legacy=AutoSaveStore.Capture(w);
    foreach(var o in legacy.Objects)if(o.Fields!=null)foreach(var name in new[]{"FunctionUnits","FunctionHost","_capacity","<FunctionDisplayNode>k__BackingField","_disposalAnnexes","TransferredLineLimit","FunctionOriginNode","_storageSlotLimit","<FunctionTransfers>k__BackingField"})o.Fields.Remove(name);
    var loaded=AutoSaveStore.Restore<WorldSetup>(legacy);loaded.Robots.ConfigureFactories(loaded);
    Check(loaded.Nodes.All.All(n=>n.Functional && n.FunctionHost==-1) && loaded.Robots.Nodes.All(s=>s.TotalCapacity==12),"Old saves retain functional nodes and original capacity");
}
{
    var(w,a,b,c)=Fixture();
    var military=w.Campaign.RobotBattles;
    var original=w.Nodes.All[a];
    var host=w.Nodes.All[b];
    var attacker=w.Nodes.All[c];
    w.Campaign.FunctionTransfers.Reserve(w,a,b);
    attacker.Owner=1;
    var preview=(IEnumerable<(int[] Path,bool Threat)>)typeof(RobotCampaign)
        .GetMethod("FunctionTransferRoutes",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(military,new object[]{w,a,b})!;
    Check(preview.Any(r=>r.Threat && r.Path[0]==c && r.Path[^1]==b),
        "Preview shows the hostile approach and its defense endpoint before delivery");
    Check(original.Functional && w.Campaign.FunctionTransfers.Orders.Count==1,"Preview does not change the map or reservation");
    w.Campaign.FinishDomestic(w);
    w.Campaign.BeginMilitary();
    attacker.Owner=1;
    w.ConquerorLocations[1]=attacker.Cell;w.ConquerorPoints[1]=attacker.Center;
    var robot=w.Robots.Create(1,RobotParts.Complete);
    w.Robots.Nodes[c].Add(robot);
    Check(!military.CanAttackFrom(w,1,c,a) && !military.CanSortie(w,1,a),"The army and conqueror cannot target an empty settlement");
    Check(military.CanAttackFrom(w,1,c,b) && military.CanSortie(w,1,b),"An army can cross the enemy settlement to reach its host");

    military.Rules=new(){SegmentTurns=1,PersuasionPercent=0};
    w.Campaign.Resolve(w,new[]{new March(1,b,1,c,Queen:false)});
    var battle=military.Encounters.Single();
    Check(battle.Kind==BattleKind.Node && battle.Target==b && original.Owner==0 && w.Robots.Nodes[a].Count==0,
        "Passing through the settlement leaves its ownership and empty store alone");
    var people=original.Population.People.Sum();
    battle.Battles[0].Winner=1;
    battle.Battles[0].Finished=true;
    typeof(RobotCampaign).GetMethod("Complete",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(military,new object[]{w,battle});
    Check(host.Owner==1 && original.Owner==1 && original.FunctionHost==b && !original.Functional &&
        original.Population.People.Sum()==people && w.Robots.Nodes[a].Count==0,"Capturing the host transfers the settlement and leaves its residents in place");
    military.Check(w);
}
{
    var(w,a,b,c)=Fixture();
    var military=w.Campaign.RobotBattles;
    w.Campaign.FunctionTransfers.Reserve(w,a,b);
    w.Campaign.FinishDomestic(w);
    w.Campaign.BeginMilitary();
    w.Nodes.All[c].Owner=1;
    Add(w,b,RobotParts.Complete);
    w.Robots.Nodes[c].Add(w.Robots.Create(1,RobotParts.Complete));
    military.Rules=new(){SegmentTurns=1,PersuasionPercent=0};
    w.Campaign.Resolve(w,new[]{new March(0,c,1,b,Queen:false),new March(1,b,1,c,Queen:false)});
    var crossing=military.Encounters.Single();
    Check(crossing.Kind==BattleKind.Edge && crossing.MarchDestinations[0]==c && crossing.MarchDestinations[1]==b &&
        w.Nodes.All[a].Owner==0,"Opposing marches meet on an edge without capturing the settlement");
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(saved.Campaign.RobotBattles.Encounters.Single().MarchDestinations[1]==b,"March destinations survive a continued edge battle save");
}
{
    // 0 is the entrance; 1-2-3 form a loop behind it, 4 is foreign land outside.
    int[][] graph={new[]{1,4},new[]{0,2,3},new[]{1,3},new[]{1,2},new[]{0}};
    Check(ComputerFunctionTransfers.EnclosedPocket(graph,1,0,id=>id!=4),"A closed loop with one entrance is safe to consolidate");
    graph[3]=new[]{1,2,4};graph[4]=new[]{0,3};
    Check(!ComputerFunctionTransfers.EnclosedPocket(graph,1,0,id=>id!=4),"A second exit to foreign land prevents consolidation");
    graph[3]=new[]{1,2};graph[4]=new[]{0};
    Check(!ComputerFunctionTransfers.EnclosedPocket(graph,1,0,id=>id!=2 && id!=4),"An enemy inside the pocket prevents consolidation");
}
{
    var(w,a,b,c)=Fixture();
    var graph=w.Nodes.All.Select(n=>w.Routes.Neighbors(n).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().ToArray()).ToArray();
    (int Source,int Target,HashSet<int> Inside)? best=null;
    for(int target=0;target<graph.Length;target++)foreach(int source in graph[target]){
        var seen=new HashSet<int>{target,source};var queue=new Queue<int>();queue.Enqueue(source);
        while(queue.TryDequeue(out int at))foreach(int next in graph[at])if(seen.Add(next))queue.Enqueue(next);
        seen.Remove(target);
        if(graph[target].Any(id=>!seen.Contains(id)) && (best==null || seen.Count<best.Value.Inside.Count))best=(source,target,seen);
    }
    Check(best!=null,"Generated map has an enclosed pocket fixture");
    var pocket=best.Value;
    foreach(var n in w.Nodes.All)n.Owner=pocket.Inside.Contains(n.Id) || n.Id==pocket.Target?0:1;
    w.HumanPlayers.Clear();w.HumanPlayers.Add(0);
    ComputerFunctionTransfers.Plan(w);
    Check(!w.Campaign.FunctionTransfers.Orders.Any(o=>o.Owner==0),"CPU never overrides a human-controlled country");
    w.Campaign.FunctionTransfers.Orders.Clear();
    w.Robots.PartPlanning.Orders.Add(new(1,0,pocket.Target,pocket.Target,0,RobotParts.Head,null));
    ComputerFunctionTransfers.Plan(w,true);
    Check(!w.Campaign.FunctionTransfers.Orders.Any(o=>o.Owner==0),"An existing part operation protects the entrance from CPU relocation");
    w.Robots.PartPlanning.Orders.Clear();w.Campaign.FunctionTransfers.Orders.Clear();
    ComputerFunctionTransfers.Plan(w,true);
    var order=w.Campaign.FunctionTransfers.Orders.Single(o=>o.Owner==0);
    Check(order.Target==pocket.Target && w.Nodes.All[order.Source].Functional,"CPU reserves a safe entrance merge without immediately changing stock");
    int capacity=w.Robots.Nodes[order.Source].TotalCapacity+w.Robots.Nodes[order.Target].TotalCapacity;
    ComputerFunctionTransfers.Plan(w,true);
    Check(w.Campaign.FunctionTransfers.Orders.Count(o=>o.Owner==0)==1,"At most one transfer is planned per country per turn");
    w.Campaign.FinishDomestic(w,automaticAll:true);
    Check(!w.Nodes.All[order.Source].Functional && w.Robots.Nodes[order.Target].TotalCapacity==capacity,"CPU transfer executes during the ordinary delivery phase");
}
Console.WriteLine("PASS: function transfers, logistics, combat, saves and CPU enclosed-loop strategy, second-exit rejection, human control protection and delivery execution.");

static class FixtureExtensions
{
    // Keep tests deterministic while leaving one permanent manufacturing line available.
    public static void RemoveLinesForTest(this RobotWorkshop w){w.LimitLines(3);w.SelectPlan(3);}
}

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
    w.Campaign.FunctionTransfers.Reserve(w,b,a);w.Campaign.FinishDomestic(w);
    Check(w.Nodes.All[a].Functional && !w.Nodes.All[b].Functional && w.Robots.Nodes[a].Count==24 && w.Robots.Nodes[a].TotalCapacity==24,"Moving into an empty node restores it without losing merged capacity");
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
Console.WriteLine("PASS: reservation timing, full-hangar merge, relocation, source references, multi-origin consolidation, empty-node delivery, military guards, hostile destination and legacy saves.");

static class FixtureExtensions
{
    // Keep tests deterministic while leaving one permanent manufacturing line available.
    public static void RemoveLinesForTest(this RobotWorkshop w){w.LimitLines(3);w.SelectPlan(3);}
}

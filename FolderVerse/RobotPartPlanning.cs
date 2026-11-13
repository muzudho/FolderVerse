namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record PlannedPartMove(long Number,int Owner,int Source,int Target,long RobotId,RobotParts Parts,long? TargetRobotId,HangarReservation Role=HangarReservation.None)
{
    public RobotParts ExpectedSource {get;init;}
    public RobotParts ExpectedTarget {get;init;}
    public bool PromoteCaptain {get;init;}
    public WorkerLaneSettings WorkerLane {get;init;}
}
public sealed record WorkerLaneSettings(int Plan,int RestEvery,int RestTurns,bool DoNotRepeat)
{
    public int MapleLoads {get;init;}
    internal void Apply(RobotWorkshop line)
    {
        if(MapleLoads>0){line.SelectMapleHarvest(MapleLoads);return;}
        if (Plan >= 0 && Plan < line.PlanCount) line.SelectPlan(Plan);
        line.SetRest(RestEvery,RestTurns);
        line.SetDoNotRepeat(DoNotRepeat);
    }
}
public sealed record PartPlanResult(long Number,int Owner,int Target,string Status);
public sealed class RobotPartPlanning
{
    private long _next=1;
    private Func<RobotWorld> _preview;
    private Func<long,PlannedPartMove[]> _automaticRecipe;
    private int _signature;
    public List<PlannedPartMove> Orders {get;}=new();
    public List<PartPlanResult> LastResults {get;}=new();
    public List<PartPlanResult> PreviewResults {get;}=new();
    public Dictionary<long,long> CompletedRobots {get;}=new();
    public static long OutputId(long number)=>long.MinValue+number;
    public static bool PlannedId(long id)=>id<long.MinValue/2;
    public bool ReservesRobot(long id)=>Orders.Any(o=>o.RobotId==id || o.TargetRobotId==id || OutputId(o.Number)==id);
    public void Reset(){_next=1;Orders.Clear();LastResults.Clear();PreviewResults.Clear();CompletedRobots.Clear();Invalidate();}
    public bool Undo(int owner)
    {
        int index=Orders.FindLastIndex(o=>o.Owner==owner);if(index<0)return false;Orders.RemoveAt(index);return true;
    }
    public void Invalidate()=>_preview=null;
    internal PlannedPartMove WorkerReservation(long id) =>
        Orders.LastOrDefault(o=>o.RobotId==id && o.Role!=HangarReservation.None) is
            {Role:HangarReservation.Worker} order ? order : null;
    internal void SetWorkerLane(long id, RobotWorkshop lane)
    {
        if (WorkerReservation(id) is not { } order) return;
        Orders[Orders.IndexOf(order)]=order with {WorkerLane=new(lane.SelectedPlan,lane.RestEvery,lane.RestTurns,lane.DoNotRepeat){MapleLoads=lane.MapleRemaining}};
    }
    public RobotWorld Preview(WorldSetup world)
    {
        var hash=new HashCode();hash.Add(world.Population.Turn);hash.Add(world.Campaign.Phase);
        foreach(var store in world.Robots.Nodes){hash.Add(store.TotalCapacity);hash.Add(store.ReservedReturns);foreach(var robot in store.Robots)hash.Add(robot);foreach(var package in store.MaplePackages)hash.Add(package);}
        foreach(var order in world.Campaign.FunctionTransfers.Orders)hash.Add(order);
        foreach(var order in world.Robots.DirectDelivery.Orders)hash.Add(order);
        foreach(var intake in world.Robots.ProductionIntakes)hash.Add(intake);
        foreach(var order in Orders)hash.Add(order);
        int signature=hash.ToHashCode();if(_preview!=null && signature==_signature)return _preview();
        var result=BuildPreview(world);_signature=signature;_preview=()=>result;return result;
    }
    private RobotWorld BuildPreview(WorldSetup world)
    {
        var copy=world.Robots.PlanningCopy();
        var hosts=Enumerable.Range(0,copy.Nodes.Length).ToArray();
        foreach(var order in world.Campaign.FunctionTransfers.Orders){
            if(!world.Campaign.FunctionTransfers.CanReserve(world,order.Source,order.Target))continue;
            int a=order.Source,b=order.Target;
            copy.Nodes[b].SetCapacity(copy.Nodes[b].TotalCapacity+copy.Nodes[a].TotalCapacity);
            copy.Nodes[b].Replace(copy.Nodes[b].Robots.Concat(copy.Nodes[a].Robots));copy.TransferMapleFunctions(a,b);copy.Nodes[a].Replace(Array.Empty<Robot>());copy.Nodes[a].SetCapacity(0);
            copy.Workshops[b].AbsorbFunctions(copy.Workshops[a]);copy.Workshops[a]=RobotWorkshop.EmptyFunctions();
            for(int n=0;n<hosts.Length;n++)if(hosts[n]==a)hosts[n]=b;
        }
        world.Robots.PreviewProductionIntakes(world,copy,hosts);
        foreach(var order in world.Robots.DirectDelivery.Orders){
            int a=hosts[order.Source],b=hosts[order.Target];
            if(a==b && !order.TargetRobotId.HasValue)continue;
            copy.DirectDelivery.Orders.Add(order with {Source=a,Target=b});
        }
        var direct=Enumerable.Range(0,copy.Nodes.Length).Select(n=>copy.DirectDelivery.Preview(copy,n)).ToArray();
        for(int n=0;n<copy.Nodes.Length;n++)if(direct[n].Length+copy.Nodes[n].MaplePackages.Count<=copy.Nodes[n].StorageLimit)copy.Nodes[n].Replace(direct[n]);
        copy.DirectDelivery.Orders.Clear();
        PreviewResults.Clear();
        foreach(var order in Orders)try{
            int host=order.Target>=0 && order.Target<hosts.Length?hosts[order.Target]:order.Target;
            Apply(world,copy,RelocateLocalOrder(order,host),false);
            PreviewResults.Add(new(order.Number,order.Owner,order.Target,"実施予定"));
        }catch(InvalidOperationException e){PreviewResults.Add(new(order.Number,order.Owner,order.Target,e.Message));}
        var recipes=new Dictionary<long,PlannedPartMove[]>();copy.PartPlanning.Orders.AddRange(Orders);
        for(int node=0;node<copy.Nodes.Length;node++){
            int owner=world.Nodes.All[node].Owner;if(owner<0 || copy.Workshops[node].AutoAssemblyDisabled)continue;
            while(copy.AssemblySelection(node,owner) is long[] ids){
                var pieces=copy.Nodes[node].Robots.Where(r=>ids.Contains(r.Id)).OrderBy(r=>r.Id).ToArray();var anchor=pieces[0];
                var recipe=new List<PlannedPartMove>();
                foreach(var piece in pieces.Skip(1)){
                    var step=new PlannedPartMove(0,owner,node,node,piece.Id,piece.Parts,anchor.Id){ExpectedSource=piece.Parts,ExpectedTarget=anchor.Parts,PromoteCaptain=pieces.Any(p=>p.Role==RobotRole.Captain)};
                    Apply(world,copy,step,false);recipe.Add(step);anchor=copy.Nodes[node].Robots.Single(r=>r.Id==anchor.Id);
                }
                recipes[anchor.Id]=recipe.ToArray();
            }
        }
        _automaticRecipe=id=>recipes.GetValueOrDefault(id,Array.Empty<PlannedPartMove>());
        return copy;
    }
    public void EnsureForecastRobot(WorldSetup world,long id)
    {
        Preview(world);var recipe=_automaticRecipe?.Invoke(id)??Array.Empty<PlannedPartMove>();
        foreach(var step in recipe)Orders.Add(step with {Number=_next++});
        if(recipe.Length>0)Invalidate();
    }
    public long Reserve(WorldSetup world,int source,int target,long id,RobotParts parts,long? targetId,int owner)
    {
        if(!world.Relations.Controls(owner))throw new InvalidOperationException("操作できる国ではありません");
        if(targetId==id)return id;
        long start=_next;
        try{
        EnsureForecastRobot(world,id);if(targetId.HasValue)EnsureForecastRobot(world,targetId.Value);
        var copy=Preview(world).PlanningCopy();
        var inputs=copy.Nodes.SelectMany(s=>s.Robots).ToArray();
        var order=new PlannedPartMove(_next,owner,source,target,id,parts,targetId){ExpectedSource=inputs.FirstOrDefault(r=>r.Id==id)?.Parts??0,ExpectedTarget=inputs.FirstOrDefault(r=>r.Id==targetId)?.Parts??0};
        long result=Apply(world,copy,ProjectedLocalOrder(world,order),false);Orders.Add(order);_next++;return result;
        }catch{Orders.RemoveAll(o=>o.Number>=start);_next=start;Invalidate();throw;}
    }
    public bool ReserveRole(WorldSetup world,int node,long id,HangarReservation role,int owner)
    {
        long start=_next;
        EnsureForecastRobot(world,id);
        var copy=Preview(world).PlanningCopy();var order=new PlannedPartMove(_next,owner,node,node,id,0,null,role);
        try{Apply(world,copy,ProjectedLocalOrder(world,order),false);Orders.Add(order);_next++;return true;}catch(InvalidOperationException){Orders.RemoveAll(o=>o.Number>=start);_next=start;Invalidate();return false;}
    }
    private static PlannedPartMove RelocateLocalOrder(PlannedPartMove order,int host)=>
        order.Source==order.Target && host!=order.Target?order with {Source=host,Target=host}:order;
    private static PlannedPartMove ProjectedLocalOrder(WorldSetup world,PlannedPartMove order)
    {
        var transfer=world.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==order.Target);
        return transfer!=null && world.Campaign.FunctionTransfers.CanReserve(world,transfer.Source,transfer.Target)
            ?RelocateLocalOrder(order,transfer.Target):order;
    }
    internal void RelocateLocalOrders(int source,int target)
    {
        for(int i=0;i<Orders.Count;i++)
            if(Orders[i].Target==source)Orders[i]=RelocateLocalOrder(Orders[i],target);
    }
    public long[] Dependencies(long id)
    {
        var ids=new HashSet<long>{id};var numbers=new HashSet<long>();
        bool changed;do{changed=false;foreach(var order in Orders.Where(o=>ids.Contains(o.RobotId) || o.TargetRobotId.HasValue && ids.Contains(o.TargetRobotId.Value) || ids.Contains(OutputId(o.Number)))){
            if(!numbers.Add(order.Number))continue;ids.Add(order.RobotId);if(order.TargetRobotId.HasValue)ids.Add(order.TargetRobotId.Value);changed=true;
        }}while(changed);return numbers.OrderBy(n=>n).ToArray();
    }
    public void EnsureArrival(WorldSetup world,int node,long id,int owner)
    {
        int actual=Find(world.Robots,id);
        if(actual<0 || actual==node)return;
        var expected=Preview(world).Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);
        if(expected!=null)Reserve(world,node,node,id,expected.Parts,null,owner);
    }
    private static int Find(RobotWorld world,long id)=>Array.FindIndex(world.Nodes,s=>s.Robots.Any(r=>r.Id==id));
    private static Robot FreeRole(Robot r)=>r with {DefenseOnly=false,InterceptOnly=false,FactoryWorker=false,DefenseFlag=null};
    private static long Apply(WorldSetup world,RobotWorld stock,PlannedPartMove order,bool checkRoute)
    {
        int source=Find(stock,order.RobotId),target=order.Target;
        if(source<0 || target<0 || target>=stock.Nodes.Length)throw new InvalidOperationException("使用する部品がありません");
        var robot=stock.Nodes[source].Robots.Single(r=>r.Id==order.RobotId);
        if(order.ExpectedSource!=0 && robot.Parts!=order.ExpectedSource)throw new InvalidOperationException("前の組替予約が実施されていません");
        if(order.Role!=HangarReservation.None){
            bool remove=order.Role switch {HangarReservation.Disposal=>robot.Disposal,HangarReservation.Defense=>robot.DefenseOnly,HangarReservation.Worker=>robot.FactoryWorker,_=>robot.InterceptOnly};
            var next=FreeRole(robot) with {Owner=order.Role==HangarReservation.Defense && !remove?order.Owner:robot.Owner,Disposal=order.Role==HangarReservation.Disposal && !remove,
                DefenseOnly=order.Role==HangarReservation.Defense && !remove,InterceptOnly=order.Role==HangarReservation.Intercept && !remove,FactoryWorker=order.Role==HangarReservation.Worker && !remove,
                DefenseFlag=order.Role==HangarReservation.Defense && !remove?new(order.Owner,target,world.Nodes.All[target].Owner):null};
            bool eligible=order.Role==HangarReservation.Worker?stock.Training[order.Owner].WorkerEligible(next with {Disposal=false}):stock.Training[order.Owner].Eligible(next with {Disposal=false});
            if(!remove && order.Role!=HangarReservation.Disposal && !eligible)throw new InvalidOperationException("予約する仕事の調練条件を満たしません");
            if(!remove && order.Role==HangarReservation.Worker && stock.Workshops[target].BaseLineCount+stock.Nodes[target].Robots.Count(r=>r.FactoryWorker && r.Id!=robot.Id)>=stock.Workshops[target].LineLimit)throw new InvalidOperationException("工場作業員のレーンに空きがありません");
            if(source!=target)Apply(world,stock,order with {Parts=robot.Parts,Role=HangarReservation.None},checkRoute);
            stock.Nodes[target].Replace(stock.Nodes[target].Robots.Select(r=>r.Id==robot.Id?next:r));return robot.Id;
        }
        var receiver=order.TargetRobotId.HasValue?stock.Nodes.SelectMany(s=>s.Robots).FirstOrDefault(r=>r.Id==order.TargetRobotId):null;
        if(receiver!=null && order.ExpectedTarget!=0 && receiver.Parts!=order.ExpectedTarget)throw new InvalidOperationException("組立先の前の予約が実施されていません");
        int receiverNode=receiver==null?target:Find(stock,receiver.Id);
        if(order.TargetRobotId.HasValue && receiver==null)throw new InvalidOperationException("組立先のロボットがありません");
        if(receiver?.Id==robot.Id && source==target)return robot.Id;
        bool Reachable(int from)=>from==target || world.Routes.Neighbors(world.Nodes.All[from]).Any(r=>world.Nodes.At(r.Target,r.Entry).Id==target) || FunctionalDeliveryRoutes.Connected(world,from,target);
        if(!Reachable(source) || !Reachable(receiverNode))throw new InvalidOperationException("同地点または隣接地点に入荷できません");
        if(checkRoute && (world.Campaign.RobotBattles.LockedNode(source) || world.Campaign.RobotBattles.LockedNode(target) || world.Campaign.RobotBattles.LockedEdge(source,target)))throw new InvalidOperationException("戦闘中のため入荷できません");
        if((source!=target || receiverNode!=target) && !world.Relations.Allied(order.Owner,world.Nodes.All[target].Owner) || !robot.CanSeparate(order.Parts))throw new InvalidOperationException("所有国・一体型の部位を確認してください");
        if((robot.Parts&order.Parts)!=order.Parts || receiver!=null && (receiver.Disposal!=robot.Disposal || !receiver.CanSeparate(receiver.Parts&order.Parts)))throw new InvalidOperationException("組み替える部位が合いません");
        var stores=stock.Nodes.Select(s=>s.Robots.ToList()).ToArray();stores[source].Remove(robot);
        if(receiver!=null)stores[receiverNode].Remove(receiver);
        Robot output;
        if(receiver!=null && (receiver.Parts&order.Parts)!=0){
            if((receiver.Parts&order.Parts)!=order.Parts)throw new InvalidOperationException("一体型の一部だけは交換できません");
            stores[source].Add(FreeRole(robot.Merge(receiver.Slice(order.Parts),order.Owner)));
            output=FreeRole(receiver.Merge(robot.Slice(order.Parts),order.Owner));
        }else{
            var remaining=robot.Parts&~order.Parts;
            if(remaining!=0)stores[source].Add(FreeRole(robot.Slice(remaining)));
            output=receiver!=null?FreeRole(receiver.Merge(robot.Slice(order.Parts),order.Owner)):FreeRole(robot.Slice(order.Parts)) with {Id=remaining==0?robot.Id:OutputId(order.Number)};
        }
        stores[target].Add(output);
        if(order.PromoteCaptain){stores[target].Remove(output);output=output with {Role=RobotRole.Captain};stores[target].Add(output);}
        if(Enumerable.Range(0,stores.Length).Any(n=>stores[n].Count+stock.Nodes[n].MaplePackages.Count>stock.Nodes[n].StorageLimit))throw new InvalidOperationException("入荷先の格納枠が不足しています");
        for(int n=0;n<stores.Length;n++)stock.Nodes[n].Replace(stores[n]);return output.Id;
    }
    internal void Resolve(WorldSetup world)
    {
        LastResults.Clear();CompletedRobots.Clear();var copy=world.Robots.PlanningCopy();
        var shipments=new List<(RobotShipment,int)>();
        long Bind(long id)=>id<0 && !PlannedId(id)?world.Robots.DirectDelivery.CompletedArrivals.GetValueOrDefault(-id,id):id;
        foreach(var planned in Orders){
            var order=planned with {RobotId=Bind(planned.RobotId),TargetRobotId=planned.TargetRobotId.HasValue?Bind(planned.TargetRobotId.Value):null};
            try{
                int source=Find(copy,order.RobotId);var before=source>=0?copy.Nodes[source].Robots.First(r=>r.Id==order.RobotId):null;
                Apply(world,copy,order,true);LastResults.Add(new(order.Number,order.Owner,order.Target,"実施完了"));
                if(source>=0 && source!=order.Target && before!=null)shipments.Add((new(source,order.Target,order.RobotId,order.Role==HangarReservation.None?order.Parts:before.Parts),order.Owner));
            }
            catch(InvalidOperationException error){LastResults.Add(new(order.Number,order.Owner,order.Target,error.Message));}
        }
        foreach(var robot in copy.Nodes.SelectMany(s=>s.Robots).Where(r=>PlannedId(r.Id)))CompletedRobots[robot.Id]=world.Robots.Create(robot.Owner,robot.Parts).Id;
        for(int n=0;n<copy.Nodes.Length;n++)world.Robots.Nodes[n].Replace(copy.Nodes[n].Robots.Select(r=>CompletedRobots.TryGetValue(r.Id,out long id)?r with {Id=id}:r));
        foreach(var robot in world.Robots.Nodes.SelectMany(s=>s.Robots))CompletedRobots.TryAdd(robot.Id,robot.Id);
        foreach(var role in Orders.Where(o=>o.Role!=HangarReservation.None && LastResults.Any(r=>r.Number==o.Number && r.Status=="実施完了")))
            world.Campaign.RewriteReservations(m=>m.DeliveryMembers==null?m:m with {DeliveryMembers=m.DeliveryMembers.Select(member=>member.RobotId==role.RobotId && !member.PartNumbers.Contains(role.Number)?member with {Cancelled=true}:member).ToArray()});
        world.Robots.Transport.AppendPlannedTransfers(shipments);
        for(int n=0;n<world.Robots.Nodes.Length;n++)world.Robots.SyncWorkers(n);
        foreach(var order in Orders.Where(o=>o.WorkerLane!=null && WorkerReservation(o.RobotId)==o &&
            LastResults.Any(r=>r.Number==o.Number && r.Status=="実施完了")))
        {
            long bound=Bind(order.RobotId);
            long id=CompletedRobots.GetValueOrDefault(bound,bound);
            var workshop=world.Robots.Workshops[order.Target];
            for(int lane=workshop.BaseLineCount;lane<workshop.ProductionLines.Length;lane++)
                if(workshop.WorkerAt(lane)==id)order.WorkerLane.Apply(workshop.ProductionLines[lane]);
        }
        Orders.Clear();world.Robots.FinishPlannedAssembly(world);world.Robots.Check();
    }
}

public sealed partial class RobotWorld
{
    public RobotPartPlanning PartPlanning {get;}=new();
    internal RobotWorld PlanningCopy()
    {
        var result=new RobotWorld();result.Initialize(Nodes.Length,Training.Length);result.Training=Training;
        for(int n=0;n<Nodes.Length;n++){
            result.Nodes[n]=Nodes[n].PlanningCopy();
            result.Workshops[n].AutoAssemblyDisabled=Workshops[n].AutoAssemblyDisabled;
            result.Workshops[n]=RobotWorkshop.PlanningView(new[]{Workshops[n]});
        }
        return result;
    }
    internal void FinishPlannedAssembly(WorldSetup world)
    {for(int n=0;n<Nodes.Length;n++)if(!Workshops[n].AutoAssemblyDisabled && world.Nodes.All[n].Owner>=0 && !world.Campaign.RobotBattles.LockedNode(n))AutoAssemble(n,world.Nodes.All[n].Owner);}
}

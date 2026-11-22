namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

// One unit is 1/9 of a hangar: dismantling one part produces one unit.
public sealed record ScrapLot(int Units,int Node,int ArrivedTurn);
public static class ScrapCargo
{
    public static ScrapLot[] Lots(MaplePackage p)=>p.ScrapLots??new[]{new ScrapLot(p.Units,-1,-1)};
    public static MaplePackage Portion(MaplePackage p,int skip,int count)
    {
        if(skip<0 || count<0 || skip>p.Units-count)throw new ArgumentOutOfRangeException(nameof(count));
        if(p.Material!=RawMaterial.Scrap)return p with {Units=count};
        var lots=new List<ScrapLot>();
        foreach(var lot in Lots(p)){
            int omitted=Math.Min(skip,lot.Units);skip-=omitted;
            int taken=Math.Min(count,lot.Units-omitted);
            if(taken>0){lots.Add(lot with {Units=taken});count-=taken;}
        }
        if(count!=0)throw new InvalidOperationException("Invalid scrap portion.");
        return p with {Units=lots.Sum(l=>l.Units),ScrapLots=lots.ToArray()};
    }
    public static MaplePackage Join(MaplePackage a,MaplePackage b)
    {
        if(a.Material!=RawMaterial.Scrap || b.Material!=RawMaterial.Scrap || a.Units+b.Units>a.MergeCapacity)
            throw new InvalidOperationException("Scrap stack capacity exceeded.");
        return a with {Units=checked(a.Units+b.Units),ScrapLots=Lots(a).Concat(Lots(b)).ToArray()};
    }
    public static MaplePackage Arrive(MaplePackage p,int node,int turn)=>p.Material!=RawMaterial.Scrap?p:
        p with {ScrapLots=Lots(p).Select(l=>l.Node==node?l:l with {Node=node,ArrivedTurn=turn}).ToArray()};
}

public sealed partial record Robot
{
    // Unlike manufacturing WIP, this state can only proceed towards scrap.
    public int? DismantlingAge {get;init;}
    public bool DismantlingWorker {get;init;}
    public bool IsDismantling=>DismantlingAge.HasValue;
    public MaplePackage RecoveredRaw {get;init;}
}

public sealed partial class RobotWorld
{
    public bool ScrapRulesEnabled {get;internal set;} public void ProcessFinalDisposal(WorldSetup w)
    {
        foreach(var node in w.Nodes.All.Where(n=>n.HasFinalDisposal)){
            var store=Nodes[node.Id];
            store.SetMaplePackages(store.MaplePackages.Select(p=>{
                if(p.Material!=RawMaterial.Scrap)return p;
                // Cohorts survive local merge/split; new arrivals never inherit old residence.
                var lots=ScrapCargo.Lots(p).Select(l=>l.Node==node.Id?l:l with {Node=node.Id,ArrivedTurn=w.Population.Turn})
                    .Where(l=>l.ArrivedTurn>=w.Population.Turn).ToArray();
                return p with {Units=lots.Sum(l=>l.Units),ScrapLots=lots};
            }).Where(p=>p.Units>0).ToArray());
        }
    }
    internal bool FinishDismantling(int node,Robot robot)
    {
        var store=Nodes[node];
        int units=SalvagePlan.PartCount(robot.Parts);
        var scrap=new MaplePackage(_nextId,units){Material=RawMaterial.Scrap,
            ScrapLots=new[]{new ScrapLot(units,node,store.ScrapTurn)}};
        // The original cargo stays in its hangar until the conversion can commit atomically.
        var forecast=store.PlanningCopy();
        forecast.Replace(forecast.Robots.Where(r=>r.Id!=robot.Id));
        if(!forecast.TryReceiveMaple(scrap,0,id=>MapleShipping(node,id) || Discarding.Reserved(id)))return false;
        store.Replace(store.Robots.Where(r=>r.Id!=robot.Id));
        store.SetMaplePackages(forecast.MaplePackages.ToArray());_nextId++;
        return true;
    }
}

public sealed record DiscardOrder(int Node,int Owner,long Id,RobotParts Parts,bool Raw,int Ninths);
public sealed record DiscardBudget(int Turn,int Used);
public sealed class DiscardPlan
{
    public List<DiscardOrder> Orders {get;}=new();
    public List<string> Results {get;}=new();
    private readonly Dictionary<int,DiscardBudget> _spent=new();
    public int Spent(int node,int turn)=>_spent.TryGetValue(node,out var b) && b.Turn==turn?b.Used:0;
    internal void Consume(int node,int turn,int ninths)
    {
        int next=Spent(node,turn)+ninths;
        if(ninths<1 || next>9)throw new InvalidOperationException("１全体ターンに廃棄できるのは１格納庫分です");
        _spent[node]=new(turn,next);
    }
    internal void Reset(){Orders.Clear();Results.Clear();_spent.Clear();}
    public void Cancel(int node,long id)=>Orders.RemoveAll(o=>o.Node==node && o.Id==id);
    public bool Reserved(long id)=>Orders.Any(o=>o.Id==id);
    public void Reserve(WorldSetup w,int node,int owner,long id,RobotParts parts=0)
    {
        if(node<0 || node>=w.Nodes.All.Length || w.Campaign.Phase!=CampaignPhase.Domestic || !w.Relations.Controls(owner) || w.Nodes.All[node].Owner!=owner || w.Campaign.RobotBattles.LockedNode(node))throw new InvalidOperationException("廃棄を予約できません");
        var stock=w.Robots;var store=stock.Nodes[node];
        var raw=store.MaplePackages.FirstOrDefault(p=>p.Id==id);
        int amount;
        if(raw!=null){if(raw.Material!=RawMaterial.Scrap || stock.MapleShipments.Any(s=>s.PackageId==id))throw new InvalidOperationException("廃棄できません");amount=raw.Units;}
        else{
            var r=store.Robots.Single(r=>r.Id==id);if(parts==0)parts=r.Parts;parts=r.ConnectedParts(parts);
            if(parts==0 || (parts&~r.Parts)!=0 || r.Owner!=owner || stock.AssemblyLocked(node,r) && !Reserved(id) && !stock.CanMoveDismantling(node,r,parts))throw new InvalidOperationException("予約を解除してください");
            amount=SalvagePlan.PartCount(parts)*3;
        }
        if(Spent(node,w.Population.Turn)+Orders.Where(o=>o.Node==node && o.Id!=id).Sum(o=>o.Ninths)+amount>9)throw new InvalidOperationException("１全体ターンに廃棄できるのは１格納庫分です");
        Cancel(node,id);Orders.Add(new(node,owner,id,parts,raw!=null,amount));
    }
    public void Resolve(WorldSetup w)
    {
        Results.Clear();
        foreach(var o in Orders.ToArray()){
            Orders.Remove(o);
            if(w.Nodes.All[o.Node].Owner!=o.Owner || w.Campaign.RobotBattles.LockedNode(o.Node)){Results.Add("廃棄予約を解除しました");continue;}
            try{
                if(!o.Raw){w.Salvage.Abandon(w,o.Owner,o.Node,o.Id,o.Parts);continue;}
                var store=w.Robots.Nodes[o.Node];var p=store.MaplePackages.FirstOrDefault(p=>p.Id==o.Id);
                if(p==null){Results.Add("荷が無くなったため廃棄予約を解除しました");continue;}
                Consume(o.Node,w.Population.Turn,p.Units);
                var proxy=w.Robots.Create(o.Owner,RobotParts.Head) with {RecoveredRaw=p with {ScrapLots=ScrapCargo.Lots(p).Select(l=>l with {Node=-1,ArrivedTurn=-1}).ToArray()}};
                w.Salvage.AddWorkpiece(w,o.Node,proxy);
                store.SetMaplePackages(store.MaplePackages.Where(p=>p.Id!=o.Id).ToArray());
            }catch(InvalidOperationException ex){Results.Add(ex.Message);}
        }
    }
}

public sealed partial class RobotWorld
{
    public bool CanMoveDismantling(int node,Robot robot,RobotParts parts,bool ownDelivery=false)=>robot.IsDismantling && parts==robot.Parts &&
        !robot.Stationed && !Workshops[node].IsDisposing(robot.Id) && !Discarding.Reserved(robot.Id) &&
        !PendingEdits.Any(e=>e.Ids.Contains(robot.Id)) && !MarchReserved(node,robot) && !PartPlanning.ReservesRobot(robot.Id) &&
        (ownDelivery || DirectDelivery.LockedParts(node,robot.Id)==0);
    public DiscardPlan Discarding {get;}=new();
}

public sealed partial class WorldSetup
{
    private void InitializeFinalDisposal()
    {
        if(TutorialStep!=0)return;
        Robots.ScrapRulesEnabled=true;
        var candidates=Nodes.All.Where(n=>n.Functional && !n.IsHarbor && !Routes.NearWhirlpool(n) && Routes.Neighbors(n).Any()).ToArray();
        // A separate deterministic stream keeps the new landmark choice independent of stock placement.
        var random=new SeedRandom(WorldSeed^0x465350);
        foreach(var n in candidates.OrderBy(n=>n.Id))if(random.Next(40)==0)n.HasFinalDisposal=true;
        if(!candidates.Any(n=>n.HasFinalDisposal) && candidates.Length>0)candidates[random.Next(candidates.Length)].HasFinalDisposal=true;
        foreach(var n in Nodes.All){
            n.PermanentFacilityRules=true;
            Robots.Nodes[n.Id].ScrapNode=n.Id;Robots.Nodes[n.Id].ScrapTurn=Population.Turn;
            if(n.HasFinalDisposal){if(n.Purpose==NodePurpose.Relay)Robots.Workshops[n.Id].LimitLines(n.Terrain==TravelTerrain.Sea?1:3);n.Purpose=NodePurpose.FinalDisposal;Robots.Nodes[n.Id].SetLogisticsRules(relay:false);}
        }
    }
}

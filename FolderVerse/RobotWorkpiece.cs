namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed record WorkpiecePart(int Done,int Total,int RawPaid)
{
    public WorkProgress Progress=>new(Math.Clamp(Done,0,Total),Total);
    public int Remaining=>Math.Max(0,Total-Done);
}
public sealed partial record Robot
{
    public WorkpiecePart HeadWork {get;init;}
    public WorkpiecePart BodyWork {get;init;}
    public WorkpiecePart LegsWork {get;init;}
    public WorkpiecePart Work(RobotParts part)=>part switch{RobotParts.Head=>HeadWork,RobotParts.Body=>BodyWork,RobotParts.Legs=>LegsWork,_=>throw new ArgumentOutOfRangeException(nameof(part))};
    public bool IsWorkpiece=>RobotMaterials.Parts.Any(p=>(Parts&p)!=0 && Work(p)!=null);
    public Robot WithWork(RobotParts part,WorkpiecePart work)=>part switch{RobotParts.Head=>this with {HeadWork=work},RobotParts.Body=>this with {BodyWork=work},RobotParts.Legs=>this with {LegsWork=work},_=>throw new ArgumentOutOfRangeException(nameof(part))};
}
public sealed partial class RobotWorkshop
{
    private Robot _activeWorkpiece;
    private List<Robot> _interruptedWorkpieces;
    private int? _workpieceOwner;
    internal void BindWorkpieceOwner(int owner){if(owner>=0)_workpieceOwner??=owner;}
    public Robot ActiveWorkpiece=>_activeWorkpiece;
    private Robot CaptureWorkpiece()
    {
        if(_activeWorkpiece!=null)return _activeWorkpiece;
        if(!Manufacturing || CycleFinished || ProductionAge==0 && !RobotWorld.RawMaterials.Any(m=>RawConsumed(m)>0))return null;
        var robot=CurrentProductionPlan.Preview(_workpieceOwner??0);int total=_lastEffectiveProductionPeriod??ProductionPeriod;
        var parts=RobotMaterials.Parts.Where(p=>(robot.Parts&p)!=0).ToArray();
        var allocated=parts.ToDictionary(p=>p,p=>0);
        foreach(var material in RobotWorld.RawMaterials){
            int remaining=RawConsumed(material);var costs=parts.ToDictionary(p=>p,p=>RobotWorld.RawCost(robot with {Parts=p,IntegratedParts=0},material));
            var shares=parts.ToDictionary(p=>p,p=>(int)((long)costs[p]*Math.Min(ProductionAge,total)/total));
            foreach(var part in parts){int amount=Math.Min(remaining,shares[part]);allocated[part]+=amount;remaining-=amount;}
            // Preserve the aggregate rounding remainder without assigning all paid work to the first part.
            foreach(var part in parts){int amount=Math.Min(remaining,costs[part]-shares[part]);allocated[part]+=amount;remaining-=amount;}
        }
        foreach(var part in parts){
            int amount=allocated[part];
            int required=RobotWorld.RawMaterials.Sum(m=>RobotWorld.RawCost(robot with {Parts=part,IntegratedParts=0},m));
            robot=robot.WithWork(part,ProductionAge>=total && amount>=required?null:new(Math.Min(ProductionAge,total),total,amount));
        }
        return robot;
    }
    private void PreserveInterruptedWorkpiece()
    {
        if(CaptureWorkpiece() is {} robot)(_interruptedWorkpieces??=new()).Add(robot);
        _activeWorkpiece=null;
    }
    internal void RetireWorkerWorkpiece(RobotWorkshop line){line.PreserveInterruptedWorkpiece();if(line._interruptedWorkpieces!=null){(_interruptedWorkpieces??=new()).AddRange(line._interruptedWorkpieces);line._interruptedWorkpieces.Clear();}}
    internal void CollectWorkpieces(Action<Robot> collect)
    {
        if(_interruptedWorkpieces==null)return;
        foreach(var robot in _interruptedWorkpieces.ToArray()){collect(robot);_interruptedWorkpieces.Remove(robot);}
    }
    internal void ResumeWorkpiece(Robot robot)
    {
        int total=RobotMaterials.Parts.Where(p=>robot.Work(p)!=null).Max(p=>robot.Work(p).Total);
        Configure(robot.Parts,total);_selectedProductPlan=new(robot.Parts,total,false,robot.IntegratedParts){HeadMaterial=robot.HeadMaterial,BodyMaterial=robot.BodyMaterial,LegsMaterial=robot.LegsMaterial,HeadOrigin=robot.Origin(RobotParts.Head),BodyOrigin=robot.Origin(RobotParts.Body),LegsOrigin=robot.Origin(RobotParts.Legs)};
        _activeWorkpiece=robot;_workpieceOwner=robot.Owner;Paused=false;ProductionAge=RobotMaterials.Parts.Where(p=>robot.Work(p)!=null).Min(p=>robot.Work(p).Done);
        _rawConsumed=new int[RobotWorld.RawMaterials.Count];
        foreach(var part in RobotMaterials.Parts.Where(p=>(robot.Parts&p)!=0))foreach(var material in RobotWorld.RawMaterials){int cost=RobotWorld.RawCost(robot with {Parts=part,IntegratedParts=0},material);if(cost>0)_rawConsumed[(int)material]+=robot.Work(part)?.RawPaid??cost;}
    }
    private int WorkpieceStepCost(RawMaterial material)
    {
        if(_activeWorkpiece==null)return 0;int amount=0;
        foreach(var part in RobotMaterials.Parts.Where(p=>(_activeWorkpiece.Parts&p)!=0))if(_activeWorkpiece.Work(part) is {} work){
            int cost=RobotWorld.RawCost(_activeWorkpiece with {Parts=part,IntegratedParts=0},material);
            amount+=Math.Max(0,(int)((long)cost*Math.Min(work.Total,work.Done+1)/work.Total)-work.RawPaid);
        }
        return amount;
    }
    private void AdvanceWorkpiece(RobotWorld world,int node,int owner,int reserved)
    {
        if(RobotWorld.RawMaterials.Any(m=>world.RawRequired(m) && WorkpieceStepCost(m)>world.AvailableRaw(node,m)))return;
        foreach(var m in RobotWorld.RawMaterials.Where(world.RawRequired)){int cost=WorkpieceStepCost(m);if(cost>0){world.SpendMaple(node,cost,m);_rawConsumed[(int)m]+=cost;}}
        foreach(var p in RobotMaterials.Parts.Where(p=>(_activeWorkpiece.Parts&p)!=0))if(_activeWorkpiece.Work(p) is {} work){
            int done=Math.Min(work.Total,work.Done+1),cost=RobotWorld.RawMaterials.Sum(m=>RobotWorld.RawCost(_activeWorkpiece with {Parts=p,IntegratedParts=0},m));
            int paid=Math.Max(work.RawPaid,(int)((long)cost*done/work.Total));
            _activeWorkpiece=_activeWorkpiece.WithWork(p,done==work.Total && paid>=cost?null:new(done,work.Total,paid));
        }
        ProductionAge=_activeWorkpiece.IsWorkpiece?RobotMaterials.Parts.Where(p=>_activeWorkpiece.Work(p)!=null).Min(p=>_activeWorkpiece.Work(p).Done):ProductionPeriod;
        if(_activeWorkpiece.IsWorkpiece)return;
        if(!(world.ProductionIntakeEnabled?world.QueueProduction(this,node,owner,_activeWorkpiece,reserved):world.TryStoreProduction(node,owner,_activeWorkpiece,reserved))){WaitingProductionTurns++;return;}
        _activeWorkpiece=null;ProductionAge=WaitingProductionTurns=0;ResetRawProgress(false);CompletedBatches++;CycleFinished=true;
    }
}
public sealed partial class RobotWorld
{
    private Action _collectInterrupted;
    public void CollectInterruptedWorkpieces(WorldSetup world)
    {
        for(int node=0;node<Workshops.Length;node++){
            int at=node;
            foreach(var line in Workshops[node].ProductionLines.Prepend(Workshops[node]).Distinct()){
                line.BindWorkpieceOwner(world.Nodes.All[node].Owner);
                line.CollectWorkpieces(robot=>world.Salvage.AddWorkpiece(world,at,robot));
            }
        }
    }
    public bool CanResumeWorkpiece(WorldSetup world,int node,RobotWorkshop line,Robot robot)
    {
        if(node<0 || node>=Nodes.Length || !Workshops[node].ProductionLines.Contains(line) || robot==null || !robot.IsWorkpiece || robot.Owner!=world.Nodes.All[node].Owner || AssemblyLocked(node,robot) || world.Campaign.RobotBattles.LockedNode(node) || line.HarvestOnly || DirectDelivery.Orders.Any(o=>o.Target==node && o.TargetRobotId==robot.Id) || LongDelivery.Orders.Any(o=>o.Target==node && o.Receiver==robot.Id))return false;
        return Enumerable.Range(0,line.PlanCount).Where(i=>i!=3).Select(line.PlanAt).Any(plan=>{
            var preview=plan.Preview(robot.Owner);return preview.Parts==robot.Parts && preview.IntegratedParts==robot.IntegratedParts && RobotMaterials.Parts.Where(p=>(robot.Parts&p)!=0).All(p=>preview.Material(p)==robot.Material(p));});
    }
    public bool ResumeWorkpiece(WorldSetup world,int node,RobotWorkshop line,long id)
    {
        var robot=Nodes[node].Robots.FirstOrDefault(r=>r.Id==id);if(!CanResumeWorkpiece(world,node,line,robot))return false;
        line.ResumeWorkpiece(robot);Nodes[node].Remove(id);CollectInterruptedWorkpieces(world);return true;
    }
}

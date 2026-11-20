namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed partial class RobotWorkshop
{
    public WorkProgress WorkDone(NodePurpose purpose)
    {
        int period=EffectiveProductionPeriod(purpose);
        return new(CycleFinished?period:Math.Clamp(ProductionAge,0,period),period);
    }
    public int WorkRemaining(NodePurpose purpose)=>CycleFinished?0:Math.Max(0,EffectiveProductionPeriod(purpose)-ProductionAge);
    private int[] _rawConsumed;
    public int RawConsumed(RawMaterial material)=>_rawConsumed?[(int)material]??0;
    public int RawRemaining(RawMaterial material)=>Math.Max(0,RobotWorld.RawCost(CurrentProductionPlan.Preview(0),material)-RawConsumed(material));
    private void ResetRawProgress()=>_rawConsumed=null;
    private int NextRawCost(RawMaterial material,int period)
        =>Math.Max(0,(int)((long)RobotWorld.RawCost(CurrentProductionPlan.Preview(0),material)*Math.Min(period,ProductionAge+1)/period)-RawConsumed(material));
    public long CompletedBatches {get;private set;}
    public long? StartAfterWorker {get;private set;}
    public long StartAfterBatch {get;private set;}
    public bool StartWaiting=>StartAfterWorker.HasValue;
    internal void RestoreStartCondition(long? worker,long batch){StartAfterWorker=worker;StartAfterBatch=batch;}
    private long LaneKey(int index)=>WorkerAt(index)??-(index+1L);
    private RobotWorkshop Dependency(long key)=>Enumerable.Range(0,ProductionLines.Length).Where(i=>LaneKey(i)==key).Select(i=>ProductionLines[i]).FirstOrDefault();
    public bool CanStartAfter(int lane,int source)
    {
        if(lane<0 || lane>=ProductionLines.Length || source<0 || source>=ProductionLines.Length || lane==source)return false;
        var target=ProductionLines[lane];var next=ProductionLines[source];var seen=new HashSet<RobotWorkshop>();
        while(next!=null && seen.Add(next)){
            if(ReferenceEquals(next,target))return false;
            next=next.StartAfterWorker is long key?Dependency(key):null;
        }
        return next==null;
    }
    public bool SetStartAfter(int lane,int? source)
    {
        if(lane<0 || lane>=ProductionLines.Length)return false;
        var target=ProductionLines[lane];
        if(source==null){target.StartAfterWorker=null;return true;}
        if(!CanStartAfter(lane,source.Value))return false;
        var origin=ProductionLines[source.Value];target.StartAfterWorker=LaneKey(source.Value);target.CycleFinished=false;
        target.StartAfterBatch=origin.CompletedBatches+(origin.CycleFinished?0:1);return true;
    }
    public string StartConditionLabel(RobotWorkshop line)
    {
        if(line.StartAfterWorker is not long key)return "今すぐ開始";
        int index=Array.FindIndex(ProductionLines,l=>ReferenceEquals(l,Dependency(key)));
        return index<0?"待機 / 指定レーンがありません":$"レーン {index+1} 生産完了後に開始";
    }
    private bool ReleaseStartCondition(RobotWorkshop line)
    {
        if(line.StartAfterWorker is not long key)return true;
        var origin=Dependency(key);if(origin==null || origin.CompletedBatches<line.StartAfterBatch)return false;
        line.StartAfterWorker=null;return true;
    }
}

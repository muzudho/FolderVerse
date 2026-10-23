namespace FolderVerse;
using System.Linq;
public sealed record ProductionPlan(RobotParts Product,int Turns,bool Cheap=false);
public sealed partial class RobotWorkshop
{
    private int? _baseLineCount;
    private int _lineLimit;
    private System.Collections.Generic.Dictionary<long,RobotWorkshop> _workerLines;
    public int LineLimit=>_lineLimit==0?3:_lineLimit;
    public int BaseLineCount=>_baseLineCount??ProductionLines.Length;
    public long? WorkerAt(int line)=>line<BaseLineCount?null:_workerLines?.Keys.OrderBy(id=>id).Skip(line-BaseLineCount).Select(id=>(long?)id).FirstOrDefault();
    public void LimitLines(int limit){_lineLimit=limit;_baseLineCount=System.Math.Min(BaseLineCount,limit);ProductionLines=ProductionLines.Take(_baseLineCount.Value).ToArray();}
    internal void SetWorkers(long[] ids){
        _baseLineCount??=ProductionLines.Length;_workerLines??=new();
        foreach(long id in _workerLines.Keys.Where(id=>!ids.Contains(id)).ToArray())_workerLines.Remove(id);
        foreach(long id in ids)if(!_workerLines.ContainsKey(id)){var line=new RobotWorkshop();line.GeneratePlans(unchecked((int)id*7919));line.SelectPlan(0);_workerLines[id]=line;}
        ProductionLines=ProductionLines.Take(BaseLineCount).Concat(_workerLines.OrderBy(p=>p.Key).Select(p=>p.Value)).Take(LineLimit).ToArray();
    }
    public RobotWorkshop[] ProductionLines {get;private set;}
    public RobotWorkshop(){ProductionLines=new[]{this};}
    internal void RemoveProductionLines(){ProductionLines=System.Array.Empty<RobotWorkshop>();_baseLineCount=0;_workerLines=null;Manufacturing=false;ProductionAge=0;}
    public void GenerateLines(int seed)
    {
        Automation=NodeAutomation.Gentle;var random=new SeedRandom(seed);int count=random.Next(4);
        count=System.Math.Min(count,LineLimit);_baseLineCount=count;_workerLines=null;ProductionLines=new RobotWorkshop[count];
        for(int i=0;i<count;i++){
            var line=i==0?this:new RobotWorkshop();ProductionLines[i]=line;
            line.GeneratePlans(seed^((i+1)*7919));line.SelectPlan(random.Next(7));line.SetRest(1+random.Next(9),1+random.Next(9));
        }
    }
    public ProductionPlan[] Plans {get;private set;}={new(RobotParts.Head,2),new(RobotParts.Body,2),new(RobotParts.Head|RobotParts.Body,3)};
    public int SelectedPlan {get;private set;}=-1;
    public bool CheapProduct {get;private set;}
    public ProductionPlan PlanAt(int index)
    {
        if(index<0 || index>6 || index==3)throw new System.ArgumentOutOfRangeException(nameof(index));
        var regular=Plans[index<3?index:index-4];
        return index<3?regular:regular with {Turns=System.Math.Max(1,regular.Turns/2),Cheap=true};
    }
    public int RestEvery {get;private set;}=1;
    public int RestTurns {get;private set;}=1;
    public int RestRemaining {get;private set;}
    public int ProducedSinceRest {get;private set;}
    public int ReservedSlots {get;private set;}
    public bool RestEnabled {get;private set;}=true;
    public void GeneratePlans(int seed)
    {
        var random=new SeedRandom(seed);var masks=new System.Collections.Generic.List<int>{1,2,3,4,5,6,7};
        Plans=new ProductionPlan[3];
        for(int i=0;i<3;i++){int choice=random.Next(masks.Count),mask=masks[choice];masks.RemoveAt(choice);int pieces=System.Numerics.BitOperations.PopCount((uint)mask);Plans[i]=new((RobotParts)mask,pieces+1+random.Next(3));}
    }
    public void SelectPlan(int index)
    {
        if(index<0 || index>6)throw new System.ArgumentOutOfRangeException(nameof(index));
        if(SelectedPlan==index)return;
        SelectedPlan=index;Paused=false;RestRemaining=ProducedSinceRest=0;
        if(index==3){Manufacturing=false;ProductionAge=0;Paused=false;return;}
        var plan=PlanAt(index);Configure(plan.Product,plan.Turns);CheapProduct=plan.Cheap;SelectedPlan=index;RestEnabled=true;
    }
    public void SetRest(int every,int turns)
    {
        if(every<1 || every>9 || turns<1 || turns>9)throw new System.ArgumentOutOfRangeException();
        if(RestEnabled && RestEvery==every && RestTurns==turns)return;RestEvery=every;RestTurns=turns;RestEnabled=true;RestRemaining=ProducedSinceRest=0;
    }
    public void SetReservedSlots(int count)
    {if(count<0 || count>12)throw new System.ArgumentOutOfRangeException(nameof(count));ReservedSlots=count;}
}



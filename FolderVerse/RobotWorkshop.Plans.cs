namespace FolderVerse;
public sealed record ProductionPlan(RobotParts Product,int Turns,bool Cheap=false);
public sealed partial class RobotWorkshop
{
    public RobotWorkshop[] ProductionLines {get;private set;}
    public RobotWorkshop(){ProductionLines=new[]{this};}
    internal void RemoveProductionLines(){ProductionLines=System.Array.Empty<RobotWorkshop>();Manufacturing=false;ProductionAge=0;}
    public void GenerateLines(int seed)
    {
        Automation=NodeAutomation.Gentle;var random=new SeedRandom(seed);int count=random.Next(4);
        ProductionLines=new RobotWorkshop[count];
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



namespace FolderVerse;
using System;
using System.Linq;

public sealed class RobotWorkshop
{
    public RobotParts Product {get;private set;}=RobotParts.Complete;
    public int ProductionPeriod {get;private set;}=3;
    public int ProductionAge {get;private set;}
    public bool Manufacturing {get;private set;}
    public bool Paused {get;set;}
    public bool Waiting=>Manufacturing && ProductionAge>=ProductionPeriod;
    public int DisposalPeriod {get;private set;}
    public bool HasDisposalFactory {get;private set;}
    public int DisposalAge {get;private set;}
    public long? DisposalTarget {get;private set;}
    public void Configure(RobotParts product,int period)
    {RobotWorld.Validate(0,product);if(period<1)throw new ArgumentOutOfRangeException(nameof(period));Product=product;ProductionPeriod=period;ProductionAge=0;Manufacturing=true;}
    public void Dispose(RobotStore store,long id)
    {if(DisposalTarget.HasValue)throw new InvalidOperationException("Disposal lane is busy.");if(!HasDisposalFactory)throw new InvalidOperationException("This node has no disposal factory.");int duration=DisposalTurns(store.Robots.Single(r=>r.Id==id).Parts);if(duration<1 || !store.Robots.Any(r=>r.Id==id))throw new ArgumentException("Invalid disposal order.");DisposalTarget=id;DisposalAge=0;DisposalPeriod=duration;}
    public void ConfigureDisposal()
    {HasDisposalFactory=true;}
    private long[] _disposalOrder=Array.Empty<long>();
    public System.Collections.Generic.IReadOnlyList<long> DisposalOrder=>_disposalOrder;
    public void SetDisposalOrder(System.Collections.Generic.IEnumerable<long> ids)
    {var order=ids.ToArray();if(order.Distinct().Count()!=order.Length)throw new ArgumentException("Duplicate disposal priority.");_disposalOrder=order;}
    public static int DisposalTurns(RobotParts parts)=>((parts&RobotParts.Head)!=0?1:0)+((parts&RobotParts.Body)!=0?2:0)+((parts&RobotParts.Legs)!=0?2:0);
    internal void StartDisposal(RobotStore store,Func<long,bool> reserved)
    {
        if(!HasDisposalFactory || DisposalTarget.HasValue)return;
        int Rank(long id){int rank=Array.IndexOf(_disposalOrder,id);return rank<0?int.MaxValue:rank;}
        var robot=store.Robots.Where(r=>r.Disposal && !reserved(r.Id)).OrderBy(r=>Rank(r.Id)).ThenBy(r=>r.Id).FirstOrDefault();
        if(robot!=null)Dispose(store,robot.Id);
    }
    internal void CompleteDisposal(RobotStore store)
    {
        if(DisposalTarget is not long id)return;
        if(!store.Robots.Any(r=>r.Id==id)){DisposalTarget=null;DisposalAge=0;return;}
        if(++DisposalAge<DisposalPeriod)return;
        store.Remove(id);DisposalTarget=null;DisposalAge=0;
    }
    internal void CompleteProduction(RobotWorld world,int node,int owner)
    {
        if(!Manufacturing || Paused || owner<0)return;
        ProductionAge=Math.Min(ProductionPeriod,ProductionAge+1);
        if(!Waiting || world.Nodes[node].Count>=RobotStore.Capacity)return;
        world.Nodes[node].Add(world.Create(owner,Product));ProductionAge=0;
    }
}

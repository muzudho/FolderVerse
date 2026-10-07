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
    public int DisposalPeriod {get;private set;}=3;
    public bool HasDisposalFactory {get;private set;}
    public int DisposalAge {get;private set;}
    public long? DisposalTarget {get;private set;}
    public void Configure(RobotParts product,int period)
    {RobotWorld.Validate(0,product);if(period<1)throw new ArgumentOutOfRangeException(nameof(period));Product=product;ProductionPeriod=period;ProductionAge=0;Manufacturing=true;}
    public void Dispose(RobotStore store,long id,int? period=null)
    {if(!HasDisposalFactory)throw new InvalidOperationException("This node has no disposal factory.");int duration=period??DisposalPeriod;if(duration<1 || !store.Robots.Any(r=>r.Id==id))throw new ArgumentException("Invalid disposal order.");DisposalTarget=id;DisposalAge=0;DisposalPeriod=duration;}
    public void ConfigureDisposal(int period=3)
    {if(period<1)throw new ArgumentOutOfRangeException(nameof(period));HasDisposalFactory=true;DisposalPeriod=period;DisposalAge=0;}
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

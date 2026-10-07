namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record TransportWeight(int Target,int Twentieths);
public sealed record RobotShipment(int Source,int Target,long RobotId,RobotParts Parts);
public sealed class RobotTransport
{
    private readonly Dictionary<(int Node,int Owner,RobotParts Parts),TransportWeight[]> _plans=new();
    private readonly Dictionary<int,int[]> _priorities=new();
    public IReadOnlyList<RobotShipment> LastTransfers {get;private set;}=Array.Empty<RobotShipment>();
    public Dictionary<long,int> LastOwners {get;}=new();
    public IReadOnlyList<TransportWeight> Plan(int node,int owner,RobotParts parts)=>_plans.GetValueOrDefault((node,owner,parts),Array.Empty<TransportWeight>());
    public IReadOnlyList<int> Priority(int node)=>_priorities.GetValueOrDefault(node,Array.Empty<int>());
    public void Reset(){_plans.Clear();_priorities.Clear();LastTransfers=Array.Empty<RobotShipment>();LastOwners.Clear();}
    public void SetPlan(int node,int owner,RobotParts parts,IEnumerable<TransportWeight> weights)
    {
        RobotWorld.Validate(owner,parts);var values=weights.OrderBy(w=>w.Target).ToArray();
        if(node<0 || values.Any(w=>w.Target<0 || w.Target==node || w.Twentieths<0 || w.Twentieths>20) || values.Sum(w=>w.Twentieths)>20 || values.Select(w=>w.Target).Distinct().Count()!=values.Length)throw new ArgumentException("Invalid transport weights.");
        _plans[(node,owner,parts)]=values;
    }
    public void SetPriority(int target,IEnumerable<int> sources)
    {var values=sources.ToArray();if(target<0 || values.Any(n=>n<0) || values.Distinct().Count()!=values.Length)throw new ArgumentException("Invalid receiving priority.");_priorities[target]=values;}
    private int Rank(RobotShipment shipment)
    {int index=Array.IndexOf(_priorities.GetValueOrDefault(shipment.Target,Array.Empty<int>()),shipment.Source);return index<0?int.MaxValue:index;}
    private IEnumerable<RobotShipment> Ordered(IEnumerable<RobotShipment> shipments)=>shipments.OrderBy(s=>s.Target).ThenBy(Rank).ThenBy(s=>s.Source).ThenBy(s=>s.RobotId);
    public void ResolvePlans(RobotWorld world,Func<int,int,bool> connected,int seed,Func<int,bool> locked=null,Func<int,int,bool> canEnter=null)
    {
        var random=new SeedRandom(seed);var choices=new Dictionary<long,Queue<RobotShipment>>();
        for(int node=0;node<world.Nodes.Length;node++)
        {
            if(locked?.Invoke(node)==true)continue;
            foreach(var robot in world.Nodes[node].Robots.OrderBy(r=>r.Id))
            {
                if(world.Workshops[node].DisposalTarget==robot.Id)continue;
                var queue=new Queue<RobotShipment>();
                for(int mask=7;mask>=1;mask--)
                {
                    if(((int)robot.Parts&mask)!=mask)continue;
                    if(!_plans.TryGetValue((node,robot.Owner,(RobotParts)mask),out var plan))continue;
                    int roll=random.Next(20),sum=0;
                    if(roll>=plan.Sum(w=>w.Twentieths))break; // Explicit stay must not fall through to a lower plan.
                    foreach(var weight in plan)
                    {
                        sum+=weight.Twentieths;if(roll>=sum)continue;
                        if(weight.Target<world.Nodes.Length && connected(node,weight.Target) && locked?.Invoke(weight.Target)!=true && (canEnter?.Invoke(robot.Owner,weight.Target)??true))queue.Enqueue(new(node,weight.Target,robot.Id,(RobotParts)mask));
                        break;
                    }
                }
                if(queue.Count>0)choices[robot.Id]=queue;
            }
        }
        var active=choices.Values.Select(q=>q.Dequeue()).ToList();
        while(true)
        {
            var accepted=Select(world,active);var rejected=active.Where(s=>!accepted.Contains(s)).ToArray();
            if(rejected.Length==0){Commit(world,accepted);return;}
            active=accepted.ToList();
            foreach(var shipment in rejected)if(choices[shipment.RobotId].TryDequeue(out var next))active.Add(next);
        }
    }
    public void Resolve(RobotWorld world,IEnumerable<RobotShipment> shipments,Func<int,int,bool> connected)
    {
        var orders=shipments.ToArray();
        if(orders.Select(s=>s.RobotId).Distinct().Count()!=orders.Length)throw new ArgumentException("One shipment per robot.");
        foreach(var s in orders)
        {
            if(s.Source<0 || s.Source>=world.Nodes.Length || s.Target<0 || s.Target>=world.Nodes.Length || !connected(s.Source,s.Target) || s.Source==s.Target)throw new ArgumentException("Invalid transport route.");
            var robot=world.Nodes[s.Source].Robots.Single(r=>r.Id==s.RobotId);RobotWorld.Validate(robot.Owner,s.Parts);
            if((robot.Parts&s.Parts)!=s.Parts || world.Workshops[s.Source].DisposalTarget==robot.Id)throw new ArgumentException("Unavailable parts.");
        }
        Commit(world,Select(world,orders));
    }
    private List<RobotShipment> Select(RobotWorld world,IEnumerable<RobotShipment> shipments)
    {
        var active=Ordered(shipments).ToList();
        while(true)
        {
            Simulate(world,active,out var overflow);if(overflow.Count==0)return active;
            var reject=overflow.Select(node=>Ordered(active.Where(s=>s.Target==node)).LastOrDefault()).Where(s=>s!=null).ToArray();
            if(reject.Length==0)throw new InvalidOperationException("Source capacity cannot be restored.");
            active.RemoveAll(s=>reject.Contains(s));
        }
    }
    private List<Robot>[] Simulate(RobotWorld world,IReadOnlyList<RobotShipment> active,out List<int> overflow)
    {
        var stores=world.Nodes.Select(s=>s.Robots.ToList()).ToArray();var cargo=new Dictionary<long,Robot>();long temporary=-1;
        foreach(var s in active)
        {
            var source=stores[s.Source];var robot=source.Single(r=>r.Id==s.RobotId);source.Remove(robot);
            if(s.Parts!=robot.Parts){source.Add(robot with {Parts=robot.Parts^s.Parts});robot=robot with {Id=temporary--};}
            cargo[s.RobotId]=robot with {Parts=s.Parts};
        }
        foreach(var s in Ordered(active))
        {
            var robot=cargo[s.RobotId];var target=stores[s.Target];
            var partner=target.Where(r=>r.Owner==robot.Owner && (r.Parts&robot.Parts)==0).OrderBy(r=>r.Id).FirstOrDefault();
            if(partner==null)target.Add(robot);
            else{target.Remove(partner);target.Add(partner with {Parts=partner.Parts|robot.Parts,Role=partner.Role==RobotRole.Captain || robot.Role==RobotRole.Captain?RobotRole.Captain:RobotRole.Soldier});}
        }
        overflow=Enumerable.Range(0,stores.Length).Where(n=>stores[n].Count>RobotStore.Capacity).ToList();return stores;
    }
    private void Commit(RobotWorld world,List<RobotShipment> accepted)
    {
        LastOwners.Clear();foreach(var s in accepted)LastOwners[s.RobotId]=world.Nodes[s.Source].Robots.Single(r=>r.Id==s.RobotId).Owner;
        var stores=Simulate(world,accepted,out var overflow);if(overflow.Count>0)throw new InvalidOperationException("Unresolved transport.");
        for(int n=0;n<stores.Length;n++)world.Nodes[n].Replace(stores[n].Select(r=>r.Id<0?world.Create(r.Owner,r.Parts,r.Role):r).ToArray());
        LastTransfers=accepted.ToArray();world.Check();
    }
}

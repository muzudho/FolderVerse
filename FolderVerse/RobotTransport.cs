namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record TransportWeight(int Target,int Twentieths);
public sealed record RobotShipment(int Source,int Target,long RobotId,RobotParts Parts,bool Disposal=false);
public sealed class RobotTransport
{
    private readonly Dictionary<(int Node,int Owner,RobotParts Parts,bool Disposal),TransportWeight[]> _plans=new();
    private readonly Dictionary<int,int[]> _priorities=new();
    public IReadOnlyList<RobotShipment> LastTransfers {get;private set;}=Array.Empty<RobotShipment>();
    public Dictionary<long,int> LastOwners {get;}=new();
    public IReadOnlyList<TransportWeight> Plan(int node,int owner,RobotParts parts,bool disposal=false)=>_plans.GetValueOrDefault((node,owner,parts,disposal),Array.Empty<TransportWeight>());
    public IReadOnlyList<int> Priority(int node)=>_priorities.GetValueOrDefault(node,Array.Empty<int>());
    public void Reset(){_plans.Clear();_priorities.Clear();LastTransfers=Array.Empty<RobotShipment>();LastOwners.Clear();}
    public void SetPlan(int node,int owner,RobotParts parts,IEnumerable<TransportWeight> weights,bool disposal=false)
    {
        RobotWorld.Validate(owner,parts);var values=weights.OrderBy(w=>w.Target).ToArray();
        if(node<0 || values.Any(w=>w.Target<0 || w.Target==node || w.Twentieths<0 || w.Twentieths>20) || values.Sum(w=>w.Twentieths)>20 || values.Select(w=>w.Target).Distinct().Count()!=values.Length)throw new ArgumentException("Invalid transport weights.");
        if(values.Sum(w=>w.Twentieths)+Plan(node,owner,parts,!disposal).Sum(w=>w.Twentieths)>20)throw new ArgumentException("Combined normal/disposal transport exceeds 100%.");
        _plans[(node,owner,parts,disposal)]=values;
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
                    var plan=Plan(node,robot.Owner,(RobotParts)mask).Select(w=>(Weight:w,Disposal:false))
                        .Concat(Plan(node,robot.Owner,(RobotParts)mask,true).Select(w=>(Weight:w,Disposal:true))).ToArray();
                    if(plan.Length==0){if(_plans.ContainsKey((node,robot.Owner,(RobotParts)mask,false)) || _plans.ContainsKey((node,robot.Owner,(RobotParts)mask,true)))break;continue;}
                    int roll=random.Next(20),sum=0;
                    if(roll>=plan.Sum(w=>w.Weight.Twentieths))break; // Explicit stay must not fall through to a lower plan.
                    foreach(var weight in plan)
                    {
                        sum+=weight.Weight.Twentieths;if(roll>=sum)continue;
                        if(weight.Weight.Target<world.Nodes.Length && connected(node,weight.Weight.Target) && locked?.Invoke(weight.Weight.Target)!=true && (canEnter?.Invoke(robot.Owner,weight.Weight.Target)??true))queue.Enqueue(new(node,weight.Weight.Target,robot.Id,(RobotParts)mask,weight.Disposal));
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
            cargo[s.RobotId]=robot with {Parts=s.Parts,Disposal=robot.Disposal || s.Disposal};
        }
        foreach(var s in Ordered(active))
        {
            var robot=cargo[s.RobotId];var target=stores[s.Target];
            var partner=target.Where(r=>!robot.Disposal && r.Id!=world.Workshops[s.Target].DisposalTarget && r.Owner==robot.Owner && r.Disposal==robot.Disposal && (r.Parts&robot.Parts)==0).OrderBy(r=>r.Id).FirstOrDefault();
            if(partner==null)target.Add(robot);
            else{target.Remove(partner);target.Add(partner with {Parts=partner.Parts|robot.Parts,Role=partner.Role==RobotRole.Captain || robot.Role==RobotRole.Captain?RobotRole.Captain:RobotRole.Soldier});}
        }
        overflow=Enumerable.Range(0,stores.Length).Where(n=>stores[n].Count>RobotStore.Capacity).ToList();return stores;
    }
    private void Commit(RobotWorld world,List<RobotShipment> accepted)
    {
        LastOwners.Clear();foreach(var s in accepted)LastOwners[s.RobotId]=world.Nodes[s.Source].Robots.Single(r=>r.Id==s.RobotId).Owner;
        var stores=Simulate(world,accepted,out var overflow);if(overflow.Count>0)throw new InvalidOperationException("Unresolved transport.");
        for(int n=0;n<stores.Length;n++)world.Nodes[n].Replace(stores[n].Select(r=>r.Id<0?world.Create(r.Owner,r.Parts,r.Role,r.Disposal):r).ToArray());
        LastTransfers=accepted.ToArray();world.Check();
    }
}

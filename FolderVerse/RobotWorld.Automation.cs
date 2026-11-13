namespace FolderVerse;
using System;
using System.Linq;
public sealed partial class RobotWorld
{
    private int _domesticPreparedTurn=-1;
    public void PrepareDomestic(int turn,Func<int,int,bool> connected,Func<int,int> owner,Func<int,bool> locked=null)
    {
        RevalidateWorkReservations();
        if(_domesticPreparedTurn==turn)return;
        bool elapsed=_domesticPreparedTurn>=0;
        _domesticPreparedTurn=turn;
        for(int n=0;n<Nodes.Length;n++){
            if(locked?.Invoke(n)!=true)SyncWorkers(n);
            if(elapsed)Workshops[n].AdvanceSupervisorLeave();
            if(owner(n)>=0 && locked?.Invoke(n)!=true)AdjustNode(n,owner(n),connected,locked,owner);
        }
    }
    public void AdjustNode(int node,int owner,Func<int,int,bool> connected,Func<int,bool> locked=null,Func<int,int> nodeOwner=null,NodeAutomation? policyOverride=null,bool adjustTransport=true)
    {
        var workshop=Workshops[node];var policy=policyOverride??workshop.Automation;if(policy==NodeAutomation.Manual)return;
        bool gentle=policy==NodeAutomation.Gentle,changed=false;
        var stock=Nodes[node].Robots.Where(r=>!r.Disposal).ToArray();
        var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs};
        var destinationCounts=new System.Collections.Generic.Dictionary<int,int[]>();
        // Prefer plans that fill missing native parts. Available plans differ per line/node.
        foreach(var line in workshop.ProductionLines){
            if(line.DoNotRepeat)continue;
            int destination=line.IntakeTarget is int target && target>=0 && target<Nodes.Length?target:node;
            var store=Nodes[destination];
            int usable=Math.Max(0,store.StorageLimit-Workshops[destination].ReservedSlots);
            int occupied=store.Count+ProductionIntakes.Count(i=>(i.Target??i.Source)==destination);
            if(!destinationCounts.TryGetValue(destination,out var counts)){
                var inventory=store.Robots.Concat(ProductionIntakes.Where(i=>(i.Target??i.Source)==destination).Select(i=>i.Product)).Where(r=>!r.Disposal).ToArray();
                counts=parts.Select(p=>inventory.Count(r=>(r.Parts&p)!=0 && r.Affiliation(p)==owner)).ToArray();destinationCounts.Add(destination,counts);
            }
            int Score(int index){
                var preview=line.PlanAt(index).Preview(owner);
                return parts.Select((p,j)=>(p,j)).Where(x=>(preview.Parts&x.p)!=0 && preview.Origin(x.p)==owner)
                    .Sum(x=>(usable-counts[x.j])*(preview.IsPremium(x.p)?3:line.WorkerSnapshot==null?1:preview.IsCheap(x.p)?1:2));
            }
            var choices=line.WorkerSnapshot==null?new[]{0,1,2,7,8}:Enumerable.Range(0,11).Where(i=>i!=3).ToArray();
            if(line.PlanCount>11)choices=choices.Append(11).ToArray();
            int best=choices.OrderByDescending(Score).ThenBy(i=>line.PlanAt(i).Turns).First();
            if(line.MapleRemaining==0 && line.SelectedPlan!=best && (!line.Manufacturing || line.ProductionAge==0) && (!gentle || !changed)){line.SelectPlan(best);changed=true;}
            bool nearlyFull=usable==0 || (long)occupied*6>=(long)usable*5;
            int every=nearlyFull?1:Math.Clamp(usable-occupied,1,9),rest=nearlyFull?9:1;
            if((line.RestEvery!=every || line.RestTurns!=rest) && (!gentle || !changed)){
                line.SetRest(gentle?line.RestEvery+Math.Sign(every-line.RestEvery):every,gentle?line.RestTurns+Math.Sign(rest-line.RestTurns):rest);changed=true;
            }
            for(int j=0;j<3;j++)if((line.Product&parts[j])!=0 && line.CurrentProductionPlan.Preview(owner).Origin(parts[j])==owner)counts[j]++;
        }
        if(workshop.AutoAssemblyDisabled && (!gentle || !changed)){workshop.AutoAssemblyDisabled=false;changed=true;}
        int capacity=Nodes[node].StorageLimit;
        int reserveTarget=Math.Min(workshop.ReservedSlotLimit,Math.Min(Math.Max(0,capacity-1),(capacity+5)/6));
        if(workshop.ReservedSlots!=reserveTarget && (!gentle || !changed)){
            workshop.SetReservedSlots(gentle?workshop.ReservedSlots+Math.Sign(reserveTarget-workshop.ReservedSlots):reserveTarget);
            changed=true;
        }
        if(!adjustTransport)return;
        double Fullness(int n)=>(Nodes[n].Count+ProductionIntakes.Count(i=>(i.Target??i.Source)==n))/(double)Math.Max(1,Nodes[n].StorageLimit-Workshops[n].ReservedSlots);
        var targets=Enumerable.Range(0,Nodes.Length).Where(n=>n!=node && Nodes[n].Count+ProductionIntakes.Count(i=>(i.Target??i.Source)==n)<Nodes[n].StorageLimit-Workshops[n].ReservedSlots &&
            (nodeOwner==null || nodeOwner(n)==owner) && connected(node,n) && locked?.Invoke(n)!=true && Fullness(n)<Fullness(node)).OrderBy(Fullness).ThenBy(n=>n).Take(2).ToArray();
        // A complete soldier may only be dispatched when its ruler's training permits it.
        if(!stock.Any(r=>r.Owner==owner && Training[owner].Eligible(r)))targets=Array.Empty<int>();
        var current=Transport.Plan(node,owner,RobotParts.Complete).ToArray();
        var ids=current.Select(w=>w.Target).Concat(targets).Distinct().OrderBy(n=>n).ToArray();
        int budget=360-Transport.Plan(node,owner,RobotParts.Complete,true).Sum(w=>w.Degrees);
        int total=Math.Min(180,budget);
        var next=ids.Select(n=>new TransportWeight(n,targets.Contains(n)?total/Math.Max(1,targets.Length):0)).ToArray();
        if(gentle){
            // Interpolate the entire pie including stay. Every sector moves at most 5 percentage points per turn.
            int largest=ids.Select(n=>Math.Abs((next.First(w=>w.Target==n).Degrees)-(current.FirstOrDefault(w=>w.Target==n)?.Degrees??0))).DefaultIfEmpty(0).Max();
            largest=Math.Max(largest,Math.Abs(next.Sum(w=>w.Degrees)-current.Sum(w=>w.Degrees)));
            double fraction=largest<=18?1:18.0/largest;
            next=next.Select(w=>{int old=current.FirstOrDefault(x=>x.Target==w.Target)?.Degrees??0;return new TransportWeight(w.Target,old+(int)((w.Degrees-old)*fraction));}).ToArray();
        }
        if(next.Sum(w=>w.Degrees)>budget){int excess=next.Sum(w=>w.Degrees)-budget;for(int i=0;i<next.Length && excess>0;i++){int take=Math.Min(excess,next[i].Degrees);next[i]=next[i] with {Degrees=next[i].Degrees-take};excess-=take;}}
        Transport.SetPlan(node,owner,RobotParts.Complete,next.Where(w=>w.Degrees>0));
    }
}

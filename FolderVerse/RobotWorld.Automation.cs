namespace FolderVerse;
using System;
using System.Linq;
public sealed partial class RobotWorld
{
    private int _domesticPreparedTurn=-1;
    public void PrepareDomestic(int turn,Func<int,int,bool> connected,Func<int,int> owner,Func<int,bool> locked=null)
    {
        if(_domesticPreparedTurn==turn)return;
        bool elapsed=_domesticPreparedTurn>=0;
        _domesticPreparedTurn=turn;
        for(int n=0;n<Nodes.Length;n++){
            if(locked?.Invoke(n)!=true)SyncWorkers(n);
            if(elapsed)Workshops[n].AdvanceSupervisorLeave();
            if(owner(n)>=0 && locked?.Invoke(n)!=true)AdjustNode(n,owner(n),connected,locked,owner);
        }
    }
    public void AdjustNode(int node,int owner,Func<int,int,bool> connected,Func<int,bool> locked=null,Func<int,int> nodeOwner=null)
    {
        var workshop=Workshops[node];if(workshop.Automation==NodeAutomation.Manual)return;
        bool gentle=workshop.Automation==NodeAutomation.Gentle,changed=false;
        var stock=Nodes[node].Robots.Where(r=>!r.Disposal).ToArray();
        var counts=new int[3];var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs};
        for(int i=0;i<3;i++)counts[i]=stock.Count(r=>(r.Parts&parts[i])!=0 && r.Affiliation(parts[i])==owner);
        // Prefer plans that fill missing native parts. Available plans differ per line/node.
        foreach(var line in workshop.ProductionLines){
            int best=new[]{0,1,2,7,8}.OrderByDescending(i=>parts.Select((p,j)=>(p,j)).Where(x=>(line.PlanAt(i).Product&x.p)!=0).Sum(x=>12-counts[x.j])).ThenBy(i=>line.PlanAt(i).Turns).First();
            if(line.SelectedPlan!=best && (!line.Manufacturing || line.ProductionAge==0) && (!gentle || !changed)){line.SelectPlan(best);changed=true;}
            int every=Nodes[node].Count>=10?1:9,rest=Nodes[node].Count>=10?9:1;
            if((line.RestEvery!=every || line.RestTurns!=rest) && (!gentle || !changed)){
                line.SetRest(gentle?line.RestEvery+Math.Sign(every-line.RestEvery):every,gentle?line.RestTurns+Math.Sign(rest-line.RestTurns):rest);changed=true;
            }
            for(int j=0;j<3;j++)if((line.Product&parts[j])!=0)counts[j]++;
        }
        if(workshop.AutoAssemblyDisabled && (!gentle || !changed)){workshop.AutoAssemblyDisabled=false;changed=true;}
        if(workshop.ReservedSlots!=2 && (!gentle || !changed)){workshop.SetReservedSlots(gentle?workshop.ReservedSlots+Math.Sign(2-workshop.ReservedSlots):2);changed=true;}
        var targets=Enumerable.Range(0,Nodes.Length).Where(n=>n!=node && (nodeOwner==null || nodeOwner(n)==owner) && connected(node,n) && locked?.Invoke(n)!=true && Nodes[n].Count<Nodes[node].Count-1).OrderBy(n=>Nodes[n].Count).ThenBy(n=>n).Take(2).ToArray();
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

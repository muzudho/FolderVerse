namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed partial class RobotTransport
{
    internal void RelocateNode(int source,int target)
    {
        int At(int n)=>n==source?target:n;
        var plans=_plans.ToArray();_plans.Clear();
        foreach(var group in plans.GroupBy(p=>(Node:At(p.Key.Node),p.Key.Owner,p.Key.Parts,p.Key.Disposal))){
            _plans[group.Key]=group.SelectMany(p=>p.Value).Select(w=>w with {Target=At(w.Target)}).Where(w=>w.Target!=group.Key.Node)
                .GroupBy(w=>w.Target).Select(g=>new TransportWeight(g.Key,g.Sum(w=>w.Degrees))).ToArray();
        }
        foreach(var group in _plans.Keys.GroupBy(k=>(k.Node,k.Owner,k.Parts)).ToArray()){
            int total=group.Sum(k=>_plans[k].Sum(w=>w.Degrees));if(total<=360)continue;
            foreach(var key in group)_plans[key]=_plans[key].Select(w=>w with {Degrees=(int)((long)w.Degrees*360/total)}).ToArray();
        }
        var instructions=_instructions.ToArray();_instructions.Clear();
        foreach(var group in instructions.GroupBy(p=>(Node:At(p.Key.Node),p.Key.Owner))){
            var list=group.SelectMany(p=>p.Value).ToList();foreach(var rule in list)rule.Target=At(rule.Target);
            list.RemoveAll(r=>r.Target==group.Key.Node);int total=list.Sum(r=>r.Degrees);
            for(int i=0;i<list.Count;i++){list[i].Number=i+1;if(total>360)list[i].Degrees=(int)((long)list[i].Degrees*360/total);}
            _instructions[group.Key]=list;
        }
        var priorities=_priorities.ToArray();_priorities.Clear();
        foreach(var group in priorities.GroupBy(p=>At(p.Key)))_priorities[group.Key]=group.SelectMany(p=>p.Value).Select(At).Where(n=>n!=group.Key).Distinct().ToArray();
    }
}

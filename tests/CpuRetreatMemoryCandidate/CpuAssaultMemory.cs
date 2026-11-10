namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
public sealed record CpuAssaultFailure(int Root,int Target,int Defender,int Force,int Defense,int Turn);
public sealed partial class RobotCampaign
{
    private readonly List<CpuAssaultFailure> _cpuAssaultFailures=new();
    private readonly HashSet<int> _cpuObservedEncounters=new();
    public void ObserveCpuAssaultFailures(WorldSetup w)
    {
        _cpuAssaultFailures.RemoveAll(f=>w.Population.Turn-f.Turn>12 || w.Nodes.All[f.Target].Owner!=f.Defender);
        _cpuObservedEncounters.IntersectWith(Encounters.Select(e=>e.Id));
        foreach(var e in Encounters.Where(e=>e.Finished && _cpuObservedEncounters.Add(e.Id))){
            foreach(var b in e.Battles.Where(b=>b.Finished))
                foreach(var group in b.Armies.Where(a=>a.HomeNode!=e.Target).GroupBy(a=>w.Relations.Root(a.Owner))){
                    if(b.Winner>=0 && w.Relations.Allied(b.Winner,group.Key))continue;
                    var owners=group.Select(a=>a.Owner).ToHashSet();
                    int force=b.Units.Count(u=>owners.Contains(u.Owner) && u.Robot!=null)+group.Sum(a=>a.Waiting.Count(u=>u.Robot!=null));
                    if(force==0)continue;
                    if(!group.Any(a=>a.Withdrawing && a.NoWillCount>=b.Rules.NoWillTurns))continue;
                    int defense=DefendingRobots(w,group.Key,e.Target);
                    if(defense!=0 || !Reserves.Any(r=>r.Node==e.Target && r.Robot.CanFight && !w.Relations.Allied(group.Key,r.Robot.Owner)))continue;
                    _cpuAssaultFailures.RemoveAll(f=>f.Root==group.Key && f.Target==e.Target && f.Force<=force);
                    _cpuAssaultFailures.Add(new(group.Key,e.Target,w.Nodes.All[e.Target].Owner,force,defense,w.Population.Turn));
                }
        }
        if(_cpuAssaultFailures.Count>256)_cpuAssaultFailures.RemoveRange(0,_cpuAssaultFailures.Count-256);
    }
    public int CpuFailedForce(WorldSetup w,int root,int target)=>_cpuAssaultFailures
        .Where(f=>f.Root==root && f.Target==target && w.Population.Turn-f.Turn<=12 && w.Nodes.All[target].Owner==f.Defender && DefendingRobots(w,root,target)==0 && Reserves.Any(r=>r.Node==target && r.Robot.CanFight && !w.Relations.Allied(root,r.Robot.Owner)))
        .Select(f=>f.Force).DefaultIfEmpty(0).Max();
}

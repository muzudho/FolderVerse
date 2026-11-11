using FolderVerse;
public sealed record CpuAssaultRoster(int Turn,int Owner,int Source,int Target,long[] RobotIds);
public static class CpuAssaultAttribution
{
    public static CpuAssaultRoster[] Record(int executionTurn,IEnumerable<March> orders,IReadOnlySet<int> automatedOwners)=>orders
        .Where(o=>automatedOwners.Contains(o.Ruler) && !o.Peaceful && o.RobotIds!=null && o.SourceNode>=0)
        .Select(o=>new CpuAssaultRoster(executionTurn,o.Ruler,o.SourceNode,o.TargetNode,o.RobotIds!.Distinct().ToArray())).ToArray();
    public static int Force(IEnumerable<CpuAssaultRoster> records,BattleState battle,int target,IReadOnlySet<int> family){
        var present=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Robot!=null).Select(u=>u.Id).ToHashSet();
        return records.Where(r=>r.Turn==battle.GlobalTurn && r.Target==target && family.Contains(r.Owner) &&
            battle.Armies.Any(a=>a.Owner==r.Owner && a.HomeNode==r.Source)).SelectMany(r=>r.RobotIds).Distinct().Count(present.Contains);
    }
}
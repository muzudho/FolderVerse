namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
public sealed partial class RobotCampaign
{
    private void CaptureCpuAssaultForces(RobotEncounter encounter){
        encounter.CpuAssaultForces.Clear();
        foreach(var board in encounter.Battles)
            encounter.CpuAssaultForces[board.Id]=board.Units.Concat(board.Armies.SelectMany(a=>a.Waiting)).Where(u=>u.Robot!=null).DistinctBy(u=>u.Id)
                .GroupBy(u=>_homeOwners.GetValueOrDefault(u.Id,u.Owner)).ToDictionary(g=>g.Key,g=>g.Count());
    }
    public int CpuFailedForce(WorldSetup w,int root,int target){
        var family=Enumerable.Range(0,w.ActiveCount).Where(o=>w.Relations.Root(o)==root).ToHashSet();
        return Encounters.Where(e=>e.Finished && e.Target==target).SelectMany(e=>e.Battles.Select(b=>new{Encounter=e,Board=b}))
            .Where(v=>v.Board.Finished && (v.Board.Winner<0 || !w.Relations.Allied(v.Board.Winner,root)) &&
                v.Board.Armies.Any(a=>family.Contains(a.Owner) && a.HomeNode!=target && a.Withdrawing && a.NoWillCount>=v.Board.Rules.NoWillTurns))
            .Select(v=>v.Encounter.CpuAssaultForces.TryGetValue(v.Board.Id,out var original)?original.Where(p=>family.Contains(p.Key)).Sum(p=>p.Value):
                v.Board.Units.Count(u=>family.Contains(u.Owner) && u.Robot!=null)+v.Board.Armies.Where(a=>family.Contains(a.Owner)).Sum(a=>a.Waiting.Count(u=>u.Robot!=null)))
            .DefaultIfEmpty(0).Max();
    }
}
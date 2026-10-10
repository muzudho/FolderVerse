namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

// One slice per battlefield per shared replay round; finished boards drop out.
public sealed record RobotBattleSlice(int Scene,int Start,int End);
public sealed class RobotBattlePlayback
{
    public RobotBattleSlice[][] Rounds {get;}
    public RobotBattlePlayback(IReadOnlyList<BattleState> scenes)
    {
        var turns=scenes.Select(b=>b.Frames.Select((f,i)=>(Frame:f,Index:i))
            .GroupBy(f=>f.Frame.Turn).Select(g=>(Start:g.First().Index,End:g.Last().Index)).ToArray()).ToArray();
        int count=turns.Length==0?0:turns.Max(t=>t.Length);
        Rounds=Enumerable.Range(0,count).Select(r=>turns.Select((t,i)=>(Turns:t,Scene:i))
            .Where(t=>r<t.Turns.Length).Select(t=>new RobotBattleSlice(t.Scene,t.Turns[r].Start,t.Turns[r].End)).ToArray()).ToArray();
    }
    public static int[] Priority(IReadOnlyList<BattleState> scenes,IEnumerable<RobotBattleSlice> slices,IReadOnlyList<int> players)
    {
        return slices.Select(s=>
        {
            var b=scenes[s.Scene];var units=b.Frames[s.Start].Units.Where(u=>!u.Fallen && !u.Retreated && BattleState.Inside(u.Position)).ToArray();
            var owners=units.Select(u=>u.Owner).Distinct().ToArray();
            int player=Enumerable.Range(0,players.Count).FirstOrDefault(i=>owners.Contains(players[i]),int.MaxValue);
            int queens=units.Where(u=>u.Role==BattleRole.Queen).Select(u=>u.Owner).Distinct().Count();
            int minimum=(queens>0?units.Where(u=>u.Role==BattleRole.Queen).Select(u=>u.Owner):b.Armies.Select(a=>a.Owner)).DefaultIfEmpty(int.MaxValue).Min();
            return (s.Scene,Group:player<int.MaxValue?0:queens>0?1:2,Player:player,Queens:queens,Pieces:units.Length,Minimum:minimum);
        }).OrderBy(x=>x.Group).ThenBy(x=>x.Group==0?x.Player:0)
          .ThenByDescending(x=>x.Group==1?x.Queens:x.Group==2?x.Pieces:0)
          .ThenBy(x=>x.Minimum).ThenBy(x=>scenes[x.Scene].Id).ThenBy(x=>x.Scene).Select(x=>x.Scene).ToArray();
    }
}

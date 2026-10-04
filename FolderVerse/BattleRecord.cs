namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum BattleKind { Node,Edge }
public sealed record BattleRecord(BattleKind Kind,int First,int Second,int SourceNode,int TargetNode,
    long FirstBefore,long SecondBefore,long FirstAfter,long SecondAfter)
{
    public long Scale=>FirstBefore>long.MaxValue-SecondBefore?long.MaxValue:FirstBefore+SecondBefore;
}
public static class BattleSchedule
{
    public static BattleRecord[][] Create(IReadOnlyList<BattleRecord> battles)
    {
        var waves=new List<List<BattleRecord>>();var last=new Dictionary<int,int>();
        foreach(var battle in battles)
        {
            int index=Math.Max(last.GetValueOrDefault(battle.First,-1),last.GetValueOrDefault(battle.Second,-1))+1;
            while(index<waves.Count && waves[index].Count>=10)index++;
            while(waves.Count<=index)waves.Add(new());
            waves[index].Add(battle);last[battle.First]=last[battle.Second]=index;
        }
        return waves.Select(w=>w.ToArray()).ToArray();
    }
}

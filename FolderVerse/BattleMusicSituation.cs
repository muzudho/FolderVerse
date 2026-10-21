namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum BattleMusicMood { Defeat=1,Losing,Disadvantage,Balanced,Advantage,Winning,Victory,Comical,Close,Decisive,Suspense,Resolution }

// Read the replay frame, never the simulation's already-computed final army sizes.
public static class BattleMusicSituation
{
    public static double Strength(IEnumerable<BattleUnitView> units)=>units
        .Where(u=>!u.Fallen && !u.Retreated && BattleState.Inside(u.Position))
        .Sum(u=>u.Role==BattleRole.Queen?2d:1+Math.Max(0,u.Shields)*.3+Math.Max(0,u.Daggers)*.2+Math.Max(0,u.Rifles)*.25);
    public static BattleMusicMood Classify(BattleState battle,int frameIndex,IReadOnlyList<int> players,Func<int,int,bool> allied)
    {
        if(battle.Frames.Count==0)return BattleMusicMood.Close;
        int index=Math.Clamp(frameIndex,0,battle.Frames.Count-1);var frame=battle.Frames[index];
        bool final=index==battle.Frames.Count-1 && frame.Phase==BattlePhase.Result;
        bool solo=players.Count==1 && battle.Armies.Any(a=>allied(a.Owner,players[0]));
        if(final){
            if(!battle.Finished || battle.Winner<0)return BattleMusicMood.Suspense;
            return solo?(allied(battle.Winner,players[0])?BattleMusicMood.Victory:BattleMusicMood.Defeat):BattleMusicMood.Resolution;
        }
        if(solo){
            double own=Strength(frame.Units.Where(u=>allied(u.Owner,players[0]))),enemy=Strength(frame.Units.Where(u=>!allied(u.Owner,players[0])));
            double ratio=(own+.1)/(enemy+.1);
            return ratio<.34?BattleMusicMood.Losing:ratio<.8?BattleMusicMood.Disadvantage:ratio<=1.25?BattleMusicMood.Balanced:ratio<3?BattleMusicMood.Advantage:BattleMusicMood.Winning;
        }
        // A long stretch of unchanged material/flag progress is a visible stalemate.
        var results=battle.Frames.Take(index+1).Where(f=>f.Phase==BattlePhase.Result && f.Turn>=frame.Turn-10).ToArray();
        if(results.Length>=10 && results.All(f=>Math.Abs(Strength(f.Units)-Strength(frame.Units))<.001 && f.LastFlagOwner==frame.LastFlagOwner && f.FlagCaptureTurns==frame.FlagCaptureTurns && f.FlagDefenseTurns==frame.FlagDefenseTurns))return BattleMusicMood.Suspense;
        var groups=new List<(int Owner,double Power)>();
        foreach(var army in battle.Armies){
            double power=Strength(frame.Units.Where(u=>u.Owner==army.Owner));
            int group=groups.FindIndex(g=>allied(g.Owner,army.Owner));
            if(group<0)groups.Add((army.Owner,power));else groups[group]=(groups[group].Owner,groups[group].Power+power);
        }
        var sizes=groups.Select(g=>g.Power).OrderBy(s=>s).ToArray();
        if(sizes.Length<2)return BattleMusicMood.Close;
        double spread=(sizes[^1]+.1)/(sizes[0]+.1);
        if(sizes.Length>=3 && spread>=2)return BattleMusicMood.Comical;
        return spread>=2.5?BattleMusicMood.Decisive:BattleMusicMood.Close;
    }
}

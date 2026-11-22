namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
public sealed partial class BattleLessons
{
    private static IEnumerable<(BattleUnit Vertical,BattleUnit Horizontal,Point Target)> LateralCrossings(
        BattleState battle,BattleUnit[] units,IReadOnlyDictionary<long,BattleAction> actions,IReadOnlyDictionary<long,Point> origins)
    {
        foreach(var group in units.Where(u=>actions[u.Id].Step!=Point.Zero).GroupBy(u=>origins[u.Id]+actions[u.Id].Step)){
            var pair=group.ToArray();if(pair.Length!=2)continue;
            if(pair.Any(u=>u.Retreating || u.Role==BattleRole.Queen || battle.Army(u.Owner).Withdrawing))continue;
            if(!battle.Allied(pair[0].Owner,pair[1].Owner))continue;
            var vertical=pair.FirstOrDefault(u=>actions[u.Id].Step.X==0 && Math.Abs(actions[u.Id].Step.Y)==1);
            var horizontal=pair.FirstOrDefault(u=>actions[u.Id].Step.Y==0 && Math.Abs(actions[u.Id].Step.X)==1);
            if(vertical==null || horizontal==null)continue;
            var target=group.Key;
            if(battle.Kind==BattleKind.Node && battle.FlagStanding && target==battle.Flag)continue;
            if(!battle.Contains(target) || units.Any(u=>origins[u.Id]==target) || !battle.WalkableFor(target,vertical.Owner) || !battle.WalkableFor(target,horizontal.Owner))continue;
            if(units.Any(e=>!battle.Allied(e.Owner,vertical.Owner) &&
                (BattleAi.Distance(origins[e.Id],origins[vertical.Id])<=4 || BattleAi.Distance(origins[e.Id],origins[horizontal.Id])<=4 || BattleAi.Distance(origins[e.Id],target)<=4)))continue;
            yield return(vertical,horizontal,target);
        }
    }
    public static void ApplyLateralCourtesy(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions){
        var origins=units.ToDictionary(u=>u.Id,u=>u.Position);var planned=actions.ToDictionary(a=>a.Key,a=>a.Value);
        foreach(var crossing in LateralCrossings(battle,units,planned,origins)){
            var waiter=crossing.Vertical;if(!Has(battle,waiter,BattleLesson.LateralCourtesy))continue;
            actions[waiter.Id]=actions[waiter.Id] with {Step=Point.Zero};
            battle.Events.Add(new(battle.Turn+1,BattlePhase.Choose,waiter.Id,"戦訓#19 / 横移動の味方を先に通す"));
        }
    }
    public static void ObserveLateralCollision(BattleState battle,BattleUnit[] units,IReadOnlyDictionary<long,BattleAction> actions,
        IReadOnlyDictionary<long,Point> origins,HashSet<long> blocked){
        if(LateralCrossings(battle,units,actions,origins).Any(c=>blocked.Contains(c.Vertical.Id)&&blocked.Contains(c.Horizontal.Id)))Witness(battle,BattleLesson.LateralCourtesy);
    }
}

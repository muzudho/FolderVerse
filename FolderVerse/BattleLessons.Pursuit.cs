namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class BattleLessons
{
    // One bounded history per battle, retained across segments and save/resume.
    public static void ObservePursuit(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        var history=battle.PursuitHistory;
        if(units.Any(u=>u.Retreating) ||
            !units.Any(u=>u.Role==BattleRole.Soldier && units.Any(e=>!battle.Allied(u.Owner,e.Owner)))){
            history.Clear();return;
        }
        string positions=string.Join(";",units.Select(u=>$"{u.Id}:{u.Position}"))+"|";
        string state=positions+$"{battle.FlagStanding}:{battle.LastFlagOwner}:{battle.FlagDefenseTurns}:{battle.FlagCaptureTurns}|"+
            string.Join(";",units.Select(u=>$"{u.Id}:{u.Owner}:{u.Role}:{u.Position}:{u.Facing}:{u.Daggers},{u.Rifles},{u.Shields}:{actions[u.Id]}"))+"|"+
            string.Join(";",battle.Armies.SelectMany(a=>a.Waiting).Select(u=>$"{u.Id}:{u.Owner}"))+"|"+
            string.Join(";",battle.Fortifications.Select(f=>$"{f.Position}:{f.Owner}:{f.Health}"));
        int previous=history.FindLastIndex(s=>s==state),period=previous<0?0:history.Count-previous;
        bool moved=previous>=0 && history.Skip(previous).Any(s=>!s.StartsWith(positions,System.StringComparison.Ordinal));
        history.Add(state);if(history.Count>41)history.RemoveAt(0);
        if(period<4 || period>40 || !moved)return;
        var learners=units.Where(u=>u.Role!=BattleRole.Queen && !Has(battle,u,BattleLesson.PursuitLoop)).ToArray();
        if(learners.Length==0)return;
        Witness(battle,BattleLesson.PursuitLoop);
        // The failed patrol returns through the ordinary retreat route. Surviving
        // eyewitnesses still have to bring the lesson home and research it.
        foreach(var unit in learners){unit.Retreating=true;actions[unit.Id]=BattleAi.Choose(battle,unit);}
        battle.Events.Add(new(battle.Turn,BattlePhase.Retreat,0,$"追跡循環 {period}手 / 戦訓を持ち帰るため退却"));
        history.Clear();
    }
}

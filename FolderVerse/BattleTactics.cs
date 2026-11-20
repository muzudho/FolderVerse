namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

// Tactics are available before combat; lessons remain experience acquired in combat.
public static class BattleTactics
{
    public const string Ambush="待ち伏せ：戦術カード";
    public static bool UsesAmbush(int character)=>character%6<3;
    public static BattleAction AmbushAction(BattleState b,BattleUnit u)
    {
        if(u.Owner<0 || u.Owner>=b.AmbushEnabled.Length || !b.AmbushEnabled[u.Owner] ||
            !BattleLessons.EnduranceGuard(b,u) || u.Role!=BattleRole.Soldier ||
            BattleAi.Distance(u.Position,b.Flag)>4 || !b.FlagStanding)return null;
        var enemies=b.Units.Where(e=>e.Active&&!b.Allied(e.Owner,u.Owner)).OrderBy(e=>BattleAi.Distance(e.Position,u.Position)).ThenBy(e=>e.Id).ToArray();
        // A threatened flag takes precedence over waiting behind cover.
        if(enemies.Any(e=>BattleAi.Distance(e.Position,b.Flag)<=2))return null;
        var cover=b.Fortifications.Where(f=>f.Kind is FortificationKind.Tree or FortificationKind.Wall && BattleAi.Distance(f.Position,u.Position)==1).ToArray();
        if(cover.Length==0)return null;
        foreach(var e in enemies){
            var d=u.Position-e.Position;var forward=BattleState.Forward(e.Facing);
            if(d.X*forward.X+d.Y*forward.Y!=0)continue; // Side attack, not a frontal duel.
            int distance=BattleAi.Distance(u.Position,e.Position);var facing=BattleAi.Face(e.Position-u.Position);
            if(distance==1&&u.Daggers>0)return new(Point.Zero,facing,BattleWeapon.Dagger);
            if(distance>=2&&distance<=4&&u.Rifles>0&&BattleAi.FirstTarget(b,u,facing)==e)return new(Point.Zero,facing,BattleWeapon.Rifle);
        }
        // Wait briefly for an enemy screened by this cover; never add endless waiting.
        if(u.Daggers+u.Rifles==0 || b.Turn%7>=3)return null;
        var approaching=enemies.FirstOrDefault(e=>BattleAi.Distance(e.Position,u.Position)<=4 && cover.Any(f=>
            (e.Position.X==u.Position.X&&f.Position.X==u.Position.X || e.Position.Y==u.Position.Y&&f.Position.Y==u.Position.Y) &&
            BattleAi.Distance(e.Position,f.Position)+1==BattleAi.Distance(e.Position,u.Position)));
        return approaching==null?null:new(Point.Zero,BattleAi.Face(approaching.Position-u.Position));
    }
    public static void Observe(BattleState b,BattleUnit[] units,System.Collections.Generic.IReadOnlyDictionary<long,BattleAction> actions)
    {
        foreach(var u in units){var expected=AmbushAction(b,u);if(expected!=null&&actions[u.Id]==expected)
            b.Events.Add(new(b.Turn,BattlePhase.Choose,u.Id,Ambush));}
    }
}

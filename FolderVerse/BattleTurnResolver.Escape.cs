namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static partial class BattleTurnResolver
{
    private static readonly Point[] EscapeSteps={new(0,-1),new(1,0),new(0,1),new(-1,0)};

    private static BattleUnit AttackTarget(BattleState battle,BattleUnit[] units,BattleUnit attacker,BattleWeapon weapon,
        out Point end,IReadOnlyDictionary<long,Point> positions=null)
    {
        Point At(BattleUnit unit)=>positions==null?unit.Position:positions[unit.Id];
        BattleUnit Occupant(Point at)=>units.FirstOrDefault(u=>u.Active && At(u)==at && (positions==null || battle.Contains(at)));
        var from=At(attacker);
        var step=BattleState.Forward(attacker.Facing);
        end=from+step;
        if(weapon==BattleWeapon.Dagger)return Occupant(end);
        for(int distance=1;distance<battle.BoardSize;distance++){
            var at=from+new Point(step.X*distance,step.Y*distance);
            if(battle.BlocksShot(at,attacker.Facing,distance)){
                if(battle.GateAt(at)!=null)end=at;
                break;
            }
            end=at;
            if(distance<2)continue;
            var target=Occupant(at);
            if(target!=null)return target;
        }
        return null;
    }

    private static HashSet<long> ClearEscapePaths(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions,
        Dictionary<long,Point> origin,Dictionary<long,Point> positions,HashSet<long> blocked)
    {
        bool Hit(BattleUnit unit,bool hostileOnly=false,BattleUnit movingVictim=null,BattleUnit movingHelper=null)=>units.Any(attacker=>
            attacker!=movingVictim && attacker!=movingHelper &&
            actions[attacker.Id].Weapon!=BattleWeapon.None && battle.Contains(positions[attacker.Id]) &&
            (!hostileOnly || !battle.Allied(attacker.Owner,unit.Owner)) &&
            AttackTarget(battle,units,attacker,actions[attacker.Id].Weapon,out _,positions)==unit);
        bool Empty(Point at)=>!positions.ContainsValue(at);
        var missed=new HashSet<long>();
        var rescued=new List<BattleUnit>();
        // Apply trained moves first, then evaluate missed escapes against the final placement.
        foreach(bool assist in new[]{true,false})
        foreach(var victim in units){
            if(victim.Shields!=0 || victim.Retreating || positions[victim.Id]!=origin[victim.Id] || !Hit(victim,true))continue;
            var from=positions[victim.Id];
            bool hasFreeEscape=false;
            foreach(var step in EscapeSteps){
                var at=from+step;
                if(!battle.WalkableFor(at,victim.Owner) || !Empty(at))continue;
                positions[victim.Id]=at;
                hasFreeEscape=!Hit(victim,movingVictim:victim);
                positions[victim.Id]=from;
                if(hasFreeEscape)break;
            }
            if(hasFreeEscape)continue;
            foreach(var step in EscapeSteps){
                var escape=from+step;
                if(!battle.WalkableFor(escape,victim.Owner))continue;
                var helper=units.FirstOrDefault(u=>!u.Retreating && battle.Allied(u.Owner,victim.Owner) &&
                    positions[u.Id]==escape && origin[u.Id]==escape);
                if(helper==null || units.Count(u=>positions[u.Id]==escape)!=1)continue;
                if(assist && !BattleLessons.Has(battle,helper,BattleLesson.ClearEscape))continue;
                var side=new Point(-step.Y,step.X);
                foreach(var at in new[]{escape+side,escape-side}){
                    if(!battle.WalkableFor(at,helper.Owner) || !Empty(at))continue;
                    positions[helper.Id]=at;
                    positions[victim.Id]=escape;
                    bool safe=!Hit(victim,movingVictim:victim,movingHelper:helper) &&
                        !Hit(helper,movingVictim:victim,movingHelper:helper);
                    if(assist && safe && rescued.All(u=>!Hit(u,movingVictim:victim,movingHelper:helper))){
                        actions[helper.Id]=new(at-escape,helper.Facing);
                        actions[victim.Id]=new(step,victim.Facing);
                        blocked.Remove(helper.Id);
                        blocked.Remove(victim.Id);
                        rescued.Add(victim);
                        rescued.Add(helper);
                        break;
                    }
                    positions[helper.Id]=escape;
                    positions[victim.Id]=from;
                    if(!assist && safe)missed.Add(victim.Id);
                }
                if(positions[victim.Id]!=from || missed.Contains(victim.Id))break;
            }
        }
        return missed;
    }
}

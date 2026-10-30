namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class BattleLessons
{
    private static readonly Point[] CornerSteps={new(0,-1),new(1,0),new(0,1),new(-1,0)};
    public static bool IsCorner(BattleState battle,Point at,int owner)
    {
        if(!battle.WalkableFor(at,owner))return false;
        for(int i=0;i<4;i++)if(!battle.WalkableFor(at+CornerSteps[i],owner) && !battle.WalkableFor(at+CornerSteps[(i+1)%4],owner))return true;
        return false;
    }
    public static int EnemiesWithinTwo(BattleState battle,Point at,int owner)=>EnemiesWithin(battle,at,owner,2);
    public static int EnemiesWithinThree(BattleState battle,Point at,int owner)=>EnemiesWithin(battle,at,owner,3);
    private static int EnemiesWithin(BattleState battle,Point at,int owner,int radius)
    {
        // Units can move simultaneously; only terrain and gate access constrain walking distance.
        var reachable=new HashSet<Point>{at};var queue=new Queue<(Point At,int Distance)>();queue.Enqueue((at,0));
        while(queue.TryDequeue(out var current)){
            if(current.Distance==radius)continue;
            foreach(var step in CornerSteps){var next=current.At+step;if(battle.WalkableFor(next,owner) && reachable.Add(next))queue.Enqueue((next,current.Distance+1));}
        }
        return battle.Units.Count(u=>u.Active && !battle.Allied(owner,u.Owner) && reachable.Contains(u.Position));
    }
    public static bool DangerousCorner(BattleState battle,Point at,int owner)=>IsCorner(battle,at,owner) && EnemiesWithinTwo(battle,at,owner)>=2;
    public static bool CornerSideTrap(BattleState battle,BattleUnit victim)
    {
        if(!IsCorner(battle,victim.Position,victim.Owner))return false;
        var open=CornerSteps.Where(s=>battle.WalkableFor(victim.Position+s,victim.Owner)).ToArray();
        return open.Length==2 && open.All(s=>battle.Units.Any(e=>e.Active && !battle.Allied(victim.Owner,e.Owner) && e.Position==victim.Position+s));
    }
    public static BattleAction CornerAction(BattleState battle,BattleUnit unit,BattleAction action)
    {
        if(!Has(battle,unit,BattleLesson.CornerEscape) || unit.Retreating || battle.Army(unit.Owner).Withdrawing)return action;
        if(IsCorner(battle,unit.Position,unit.Owner) && EnemiesWithinThree(battle,unit.Position,unit.Owner)>=2){
            var enemies=battle.Units.Where(e=>e.Active && !battle.Allied(unit.Owner,e.Owner)).ToArray();
            var exit=CornerSteps.Select(s=>(Step:s,At:unit.Position+s))
                .Where(p=>battle.WalkableFor(p.At,unit.Owner) && !battle.Units.Any(u=>u.Active && u.Id!=unit.Id && u.Position==p.At) && (!IsCorner(battle,p.At,unit.Owner) || EnemiesWithinThree(battle,p.At,unit.Owner)<2))
                .OrderBy(p=>IsCorner(battle,p.At,unit.Owner)).ThenBy(p=>EnemiesWithinThree(battle,p.At,unit.Owner))
                .ThenByDescending(p=>enemies.Min(e=>BattleAi.Distance(p.At,e.Position))).ThenBy(p=>p.Step==action.Step?0:1)
                .Select(p=>(Point?)p.Step).FirstOrDefault();
            if(exit==null)return action;
            var nearest=enemies.OrderBy(e=>BattleAi.Distance(e.Position,unit.Position)).ThenBy(e=>e.Id).First();
            return new(exit.Value,BattleAi.Face(nearest.Position-unit.Position));
        }
        if(action.Step!=Point.Zero && DangerousCorner(battle,unit.Position+action.Step,unit.Owner))return new(Point.Zero,action.Facing);
        return action;
    }
}

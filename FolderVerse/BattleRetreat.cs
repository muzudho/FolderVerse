namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Retreating pieces pass through units; walls and home exits remain physical.
public static class BattleRetreat
{
    private static readonly Point[] Steps={new(0,-1),new(1,0),new(0,1),new(-1,0)};
    public static void ClearPaths(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        foreach(var unit in units.Where(u=>u.Retreating)){
            var queue=new Queue<Point>();queue.Enqueue(unit.Position);
            var first=new Dictionary<Point,Point>{{unit.Position,Point.Zero}};Point? move=null;
            while(queue.TryDequeue(out var current) && move==null)foreach(var step in Steps){
                var next=current+step;if(first.ContainsKey(next))continue;
                if(!battle.Contains(next)){
                    if(BattleState.HomeExit(next,battle.Army(unit.Owner).Side,battle.BoardSize)){move=current==unit.Position?step:first[current];break;}
                    continue;
                }
                if(!battle.Walkable(next))continue;
                first[next]=current==unit.Position?step:first[current];queue.Enqueue(next);
            }
            var delta=move??Point.Zero;
            var facing=delta==Point.Zero?unit.Facing:BattleAi.Face(delta);
            if(delta!=Point.Zero && BattleLessons.Has(battle,unit,BattleLesson.RetreatBackwards))facing=(BattleFacing)(((int)facing+2)%4);
            actions[unit.Id]=new(delta,facing,BattleWeapon.None);
        }
    }
}

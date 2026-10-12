namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// A retreating piece has priority over allied movement; allies may step aside,
// but enemies and walls remain solid. All displaced pieces still move one cell.
public static class BattleRetreat
{
    private static readonly Point[] Steps={new(0,-1),new(1,0),new(0,1),new(-1,0)};
    public static void ClearPaths(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        var assigned=new Dictionary<long,Point>();
        bool Vacate(BattleUnit unit,Point forbidden,HashSet<long> visiting)
        {
            if(!visiting.Add(unit.Id))return false;
            foreach(var step in Steps.OrderBy(s=>s==actions[unit.Id].Step?0:1)){
                var next=unit.Position+step;
                if(next==forbidden || !battle.Walkable(next) || assigned.Values.Contains(next))continue;
                var blocker=units.FirstOrDefault(u=>u.Active && u.Id!=unit.Id && u.Position==next);
                if(blocker!=null && (!battle.Allied(blocker.Owner,unit.Owner) || assigned.ContainsKey(blocker.Id)))continue;
                var saved=new Dictionary<long,Point>(assigned);
                if(blocker!=null && !Vacate(blocker,unit.Position,new HashSet<long>(visiting))){assigned.Clear();foreach(var e in saved)assigned[e.Key]=e.Value;continue;}
                assigned[unit.Id]=next;return true;
            }
            return false;
        }
        foreach(var unit in units.Where(u=>u.Retreating).OrderBy(u=>u.Id)){
            if(assigned.ContainsKey(unit.Id))continue;
            var queue=new Queue<Point>();queue.Enqueue(unit.Position);
            var first=new Dictionary<Point,Point>{{unit.Position,Point.Zero}};Point? move=null;
            while(queue.TryDequeue(out var current) && move==null)
            foreach(var step in Steps){
                var next=current+step;if(first.ContainsKey(next))continue;
                if(!BattleState.Inside(next)){
                    if(BattleState.HomeExit(next,battle.Army(unit.Owner).Side)){move=current==unit.Position?step:first[current];break;}
                    continue;
                }
                if(!battle.Walkable(next) || units.Any(u=>u.Active && !battle.Allied(u.Owner,unit.Owner) && u.Position==next))continue;
                first[next]=current==unit.Position?step:first[current];queue.Enqueue(next);
            }
            if(move==null)continue;
            var destination=unit.Position+move.Value;
            if(assigned.Values.Contains(destination))continue;
            var blocker=units.FirstOrDefault(u=>u.Active && u.Id!=unit.Id && u.Position==destination);
            if(blocker!=null && !Vacate(blocker,unit.Position,new HashSet<long>{unit.Id}))continue;
            assigned[unit.Id]=destination;
        }
        // Keep non-priority pieces from entering a reserved retreat corridor.
        foreach(var unit in units){
            if(assigned.TryGetValue(unit.Id,out var next)){
                var step=next-unit.Position;actions[unit.Id]=new(step,BattleAi.Face(step),BattleWeapon.None);
            }else if(assigned.Values.Contains(unit.Position+actions[unit.Id].Step))actions[unit.Id]=new(Point.Zero,unit.Facing);
        }
    }
}
namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Retreating pieces pass through units; walls and home exits remain physical.
public static class BattleRetreat
{
    // The battle is already decided: animate the losing armies home without more combat.
    public static void AfterFlagDefense(BattleState battle)
    {
        foreach(var army in battle.Armies.Where(a=>!battle.Allied(a.Owner,battle.Defender))){
            army.Withdrawing=true;
            foreach(var unit in battle.Units.Where(u=>u.Active && u.Owner==army.Owner))unit.Retreating=true;
            while(army.Waiting.TryDequeue(out var waiting)){waiting.Retreated=true;battle.Units.Add(waiting);}
        }
        var retreaters=battle.Units.Where(u=>u.Active && !battle.Allied(u.Owner,battle.Defender)).ToArray();
        if(retreaters.Length==0)return;
        battle.Capture(BattlePhase.DefenseRetreat);
        // Each shortest path has at most one visit per board square. Units may overlap.
        for(int step=0;step<=battle.BoardSize*battle.BoardSize && retreaters.Any(u=>u.Active);step++){
            var units=retreaters.Where(u=>u.Active).ToArray();var actions=new Dictionary<long,BattleAction>();
            ClearPaths(battle,units,actions);bool moved=false;
            foreach(var unit in units){
                var action=actions[unit.Id];unit.Facing=action.Facing;unit.Position+=action.Step;moved|=action.Step!=Point.Zero;
                if(battle.ReachedHome(unit))unit.Retreated=true;
            }
            if(!moved)break;
            battle.Capture(BattlePhase.DefenseRetreat);
        }
        for(int hold=0;hold<5;hold++)battle.Capture(BattlePhase.DefenseRetreat);
    }
    private static readonly Point[] Steps={new(0,-1),new(1,0),new(0,1),new(-1,0)};
    public static void ClearPaths(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        foreach(var unit in units.Where(u=>u.Retreating)){
            if (BattleAi.ProtectQueen(battle, unit) && BattleAi.SafeQueenRetreat(battle, unit) is { } safe)
            {
                actions[unit.Id] = new(safe, safe == Point.Zero ? unit.Facing : BattleAi.Face(safe));
                continue;
            }
            var queue=new Queue<Point>();queue.Enqueue(unit.Position);
            var first=new Dictionary<Point,Point>{{unit.Position,Point.Zero}};Point? move=null;
            bool central=battle.BoardSize==BattleState.Size && battle.Army(unit.Owner).Side==4;
            if(central && unit.Position==battle.Flag)move=Point.Zero;
            while(queue.TryDequeue(out var current) && move==null)foreach(var step in Steps){
                var next=current+step;if(first.ContainsKey(next))continue;
                if(central && next==battle.Flag){move=current==unit.Position?step:first[current];break;}
                if(!battle.Contains(next)){
                    if(battle.HomeExit(next,battle.Army(unit.Owner))){move=current==unit.Position?step:first[current];break;}
                    continue;
                }
                if(!battle.WalkableFor(next,unit.Owner))continue;
                first[next]=current==unit.Position?step:first[current];queue.Enqueue(next);
            }
            var delta=move??Point.Zero;
            var facing=delta==Point.Zero?unit.Facing:BattleAi.Face(delta);
            if(delta!=Point.Zero && BattleLessons.Has(battle,unit,BattleLesson.RetreatBackwards))facing=(BattleFacing)(((int)facing+2)%4);
            actions[unit.Id]=new(delta,facing,BattleWeapon.None);
        }
    }
}

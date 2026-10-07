namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static class BattleAi
{
    private static readonly Point[] Steps={new(0,-1),new(1,0),new(0,1),new(-1,0)};
    public static BattleFacing Face(Point delta)=>Math.Abs(delta.X)>Math.Abs(delta.Y)?delta.X>0?BattleFacing.East:BattleFacing.West:delta.Y>0?BattleFacing.South:BattleFacing.North;
    public static BattleAction Choose(BattleState battle,BattleUnit unit)
    {
        var enemies=battle.Units.Where(u=>u.Active && !battle.Allied(u.Owner,unit.Owner)).OrderBy(u=>Distance(u.Position,unit.Position)).ThenBy(u=>u.Id).ToArray();
        var army=battle.Army(unit.Owner);var enemy=enemies.FirstOrDefault();
        bool armed=unit.Daggers>0 || unit.Rifles>0;
        if(unit.Role==BattleRole.Captain && !battle.Units.Any(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Soldier && (u.Daggers>0 || u.Rifles>0)) && !army.Waiting.Any(u=>u.Role==BattleRole.Soldier))unit.Retreating=true;
        if(!armed && unit.Role==BattleRole.Soldier)
        {
            var flagStep=battle.Kind==BattleKind.Node?Path(battle,unit,battle.Flag):null;
            if(battle.Kind!=BattleKind.Node || battle.Allied(unit.Owner,battle.Defender) && battle.FlagStanding || flagStep==null)unit.Retreating=true;
        }
        if(unit.Retreating)
        {
            var exit=army.Side switch{0=>new Point(unit.Position.X,-1),1=>new Point(10,unit.Position.Y),2=>new Point(unit.Position.X,10),_=>new Point(-1,unit.Position.Y)};
            var step=Path(battle,unit,exit)??new Point(0,0);return new(step,Face(exit-unit.Position));
        }
        if(enemy!=null && unit.Role!=BattleRole.Queen && armed)
        {
            var delta=enemy.Position-unit.Position;var facing=Face(delta);int distance=Distance(enemy.Position,unit.Position);
            if(distance==1 && unit.Daggers>0)return new(Point.Zero,facing,BattleWeapon.Dagger);
            bool aligned=delta.X==0 || delta.Y==0;
            if(aligned && distance>=2 && unit.Rifles>0 && FirstTarget(battle,unit,facing)==enemy)
            {
                bool threatened=enemies.Any(e=>e.Rifles>0 && FirstTarget(battle,e,e.Facing)==unit);
                if(!threatened || unit.Shields==0)return new(Point.Zero,facing,BattleWeapon.Rifle);
                return new(Point.Zero,facing); // Facing the shooter enables the passive shield.
            }
            if(distance<=4)
            {
                if(unit.Role==BattleRole.Captain)return new(Point.Zero,facing);
                var sideCells=Steps.Select(s=>enemy.Position+s).Where(battle.Walkable)
                    .OrderBy(p=>p==enemy.Position+BattleState.Forward(enemy.Facing)?1:0).ThenBy(p=>Distance(p,unit.Position));
                foreach(var goal in sideCells){var step=Path(battle,unit,goal);if(step!=null)return new(step.Value,facing);}
            }
        }
        if(unit.Role==BattleRole.Queen)
        {
            if(enemy==null)return new(Point.Zero,unit.Facing);
            // Queens keep a buffer while staying near enough to attempt persuasion.
            if(Distance(enemy.Position,unit.Position)<2)
            {
                var away=Steps.Where(s=>Free(battle,unit,unit.Position+s)).OrderByDescending(s=>Distance(unit.Position+s,enemy.Position)).FirstOrDefault();return new(away,Face(enemy.Position-unit.Position));
            }
            if(Distance(enemy.Position,unit.Position)<=battle.Rules.PersuasionRadius)
                return new(Point.Zero,Face(enemy.Position-unit.Position));
            return new(Path(battle,unit,enemy.Position)??Point.Zero,Face(enemy.Position-unit.Position));
        }
        Point objective=enemy?.Position??unit.Position;
        if(battle.Kind==BattleKind.Node)
        {
            bool defending=battle.Allied(unit.Owner,battle.Defender);
            if(!defending || !battle.FlagStanding || enemies.Any(e=>Distance(e.Position,battle.Flag)<=2))objective=battle.Flag;
        }
        if(enemy==null || Distance(enemy.Position,unit.Position)>4)
        {
            var captain=battle.Units.Where(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Captain).OrderBy(u=>u.Id).FirstOrDefault();
            if(unit.Role==BattleRole.Soldier && captain!=null && armed)
            {
                var soldiers=battle.Units.Where(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Soldier && (u.Daggers>0 || u.Rifles>0)).OrderBy(u=>u.Id).ToArray();
                int index=Array.IndexOf(soldiers,unit);var forward=BattleState.Forward(captain.Facing);var lateral=new Point(-forward.Y,forward.X);
                var formation=captain.Position+new Point(forward.X*2,forward.Y*2)+new Point(lateral.X*(index-soldiers.Length/2),lateral.Y*(index-soldiers.Length/2));
                if(battle.Walkable(formation))return new(Path(battle,unit,formation)??Point.Zero,captain.Facing);
            }
            if(unit.Role==BattleRole.Captain && enemy!=null && Distance(enemy.Position,unit.Position)<=5)return new(Point.Zero,Face(enemy.Position-unit.Position));
        }
        return new(Path(battle,unit,objective)??Point.Zero,objective==unit.Position?unit.Facing:Face(objective-unit.Position));
    }
    public static int Distance(Point a,Point b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);
    public static BattleUnit FirstTarget(BattleState battle,BattleUnit unit,BattleFacing facing)
    {
        var forward=BattleState.Forward(facing);
        for(int i=1;i<10;i++)
        {
            var p=unit.Position+new Point(forward.X*i,forward.Y*i);if(!battle.Walkable(p))break;if(i==1)continue;
            var target=battle.Units.FirstOrDefault(u=>u.Active && u.Position==p);if(target!=null)return target;
        }
        return null;
    }
    private static bool Free(BattleState battle,BattleUnit unit,Point p)=>battle.Walkable(p) && !battle.Units.Any(u=>u.Active && u.Id!=unit.Id && u.Position==p);
    public static Point? Path(BattleState battle,BattleUnit unit,Point target)
    {
        if(unit.Position==target)return Point.Zero;
        var queue=new Queue<Point>();queue.Enqueue(unit.Position);var first=new Dictionary<Point,Point>{{unit.Position,Point.Zero}};
        while(queue.TryDequeue(out var current))foreach(var step in Steps.OrderBy(s=>Distance(current+s,target)))
        {
            var next=current+step;if(first.ContainsKey(next))continue;
            if(!Free(battle,unit,next) && !(next==target && (!BattleState.Inside(target) && unit.Retreating || battle.Units.Any(u=>u.Active && u.Position==target && !battle.Allied(u.Owner,unit.Owner)))))continue;
            first[next]=current==unit.Position?step:first[current];if(next==target)return first[next];queue.Enqueue(next);
        }
        return null;
    }
}

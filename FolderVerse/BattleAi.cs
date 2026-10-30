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
        var action=BattleLessons.WaitingDaggerAction(battle,unit,BattleLessons.RelieveFrontAction(battle,unit,BattleLessons.CollisionTempoAction(battle,unit,ChooseCore(battle,unit))));
        if(action.Step!=Point.Zero && battle.GateAt(unit.Position+action.Step) is {} gate && !battle.Allied(unit.Owner,gate.Owner) && !unit.Retreating && unit.Role!=BattleRole.Queen && unit.Daggers+unit.Rifles>=gate.Health)
            return new(Point.Zero,Face(action.Step),unit.Daggers>0?BattleWeapon.Dagger:BattleWeapon.Rifle);
        // A short-of-ammo soldier can still contribute one hit for another ally to finish.
        if(action.Step==Point.Zero && action.Weapon==BattleWeapon.None && !unit.Retreating && unit.Role!=BattleRole.Queen && unit.Daggers+unit.Rifles>0){
            var nearby=battle.Fortifications.FirstOrDefault(f=>f.Kind==FortificationKind.Gate && f.Health>0 && !battle.Allied(unit.Owner,f.Owner) && Distance(unit.Position,f.Position)==1);
            if(nearby!=null)return new(Point.Zero,Face(nearby.Position-unit.Position),unit.Daggers>0?BattleWeapon.Dagger:BattleWeapon.Rifle);
        }
        return action;
    }
    private static BattleAction ChooseCore(BattleState battle,BattleUnit unit)
    {
        var enemies=battle.Units.Where(u=>u.Active && !battle.Allied(u.Owner,unit.Owner)).OrderBy(u=>Distance(u.Position,unit.Position)).ThenBy(u=>u.Id).ToArray();
        var army=battle.Army(unit.Owner);var enemy=enemies.FirstOrDefault();
        if(army.Wanderer && army.WandererPolicy==WandererPolicy.Persuade && !army.Withdrawing)
        {
            var queen=battle.Units.FirstOrDefault(u=>u.Owner==unit.Owner && u.Role==BattleRole.Queen && u.Active);
            bool danger=queen==null && !army.Waiting.Any(u=>u.Role==BattleRole.Queen) || queen!=null && enemies.Any(u=>(u.Daggers>0 && Distance(u.Position,queen.Position)<=2) || (u.Rifles>0 && Distance(u.Position,queen.Position)<=3));
            if(danger){army.Withdrawing=true;foreach(var friend in battle.Units.Where(u=>u.Owner==unit.Owner && u.Active))friend.Retreating=true;while(army.Waiting.TryDequeue(out var waiting)){waiting.Retreated=true;battle.Units.Add(waiting);}}
        }
        bool armed=unit.Daggers>0 || unit.Rifles>0;
        bool securingFlag=battle.Kind==BattleKind.Node && !battle.SplitBoard && battle.FlagStanding && !battle.Allied(unit.Owner,battle.Defender) &&
            !battle.Armies.Any(a=>battle.Allied(a.Owner,battle.Defender) && (a.Waiting.Count>0 || battle.Units.Any(u=>u.Active && u.Owner==a.Owner)));
        if(securingFlag && !unit.Retreating && !army.Withdrawing)return new(Path(battle,unit,battle.Flag)??Point.Zero,Face(battle.Flag-unit.Position));
        if(unit.Role==BattleRole.Captain && !battle.Units.Any(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Soldier && (u.Daggers>0 || u.Rifles>0)) && !army.Waiting.Any(u=>u.Role==BattleRole.Soldier))unit.Retreating=true;
        if(unit.Role==BattleRole.Soldier && unit.Daggers==0 && unit.Rifles==0 && unit.Shields==0 &&
            (!battle.Units.Any(u=>u.Active && u.Owner==unit.Owner && u.Role is BattleRole.Captain or BattleRole.Queen) ||
             battle.Units.Any(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Soldier && (u.Daggers>0 || u.Rifles>0 || u.Shields>0))))
            unit.Retreating=true;
        if(unit.Retreating)
        {
            var exit=army.Side switch{0=>new Point(unit.Position.X,-1),1=>new Point(battle.BoardSize,unit.Position.Y),2=>new Point(unit.Position.X,battle.BoardSize),_=>new Point(-1,unit.Position.Y)};
            var step=Path(battle,unit,exit)??new Point(0,0);return new(step,Face(exit-unit.Position));
        }
        if(enemy!=null && unit.Role!=BattleRole.Queen && armed)
        {
            var delta=enemy.Position-unit.Position;var facing=Face(delta);int distance=Distance(enemy.Position,unit.Position);
            if(distance==1 && unit.Daggers>0)return BattleLessons.AvoidSideDagger(battle,unit,new(Point.Zero,facing,BattleWeapon.Dagger));
            bool aligned=delta.X==0 || delta.Y==0;
            if(aligned && distance>=2 && unit.Rifles>0 && FirstTarget(battle,unit,facing)==enemy)
            {
                bool threatened=enemies.Any(e=>e.Rifles>0 && FirstTarget(battle,e,e.Facing)==unit);
                if(!threatened || unit.Shields==0)return new(Point.Zero,facing,BattleWeapon.Rifle);
                if(unit.Daggers>0 && BattleLessons.Has(battle,unit,BattleLesson.CloseForDagger)){
                    var step=Path(battle,unit,enemy.Position);
                    if(step.HasValue && step.Value!=Point.Zero)return new(step.Value,facing);
                }
                return new(Point.Zero,facing); // Facing the shooter enables the passive shield.
            }
            if(distance<=4)
            {
                if(unit.Role==BattleRole.Captain)return new(Point.Zero,facing);
                var sideCells=Steps.Select(s=>enemy.Position+s).Where(p=>battle.WalkableFor(p,unit.Owner))
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
            if(defending?(!battle.FlagStanding || enemies.Any(e=>Distance(e.Position,battle.Flag)<=2)):battle.FlagStanding)objective=battle.Flag;
        }
        if(enemy==null || Distance(enemy.Position,unit.Position)>4)
        {
            var captain=battle.Units.Where(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Captain).OrderBy(u=>u.Id).FirstOrDefault();
            if(unit.Role==BattleRole.Soldier && captain!=null && armed)
            {
                var soldiers=battle.Units.Where(u=>u.Active && u.Owner==unit.Owner && u.Role==BattleRole.Soldier && (u.Daggers>0 || u.Rifles>0)).OrderBy(u=>u.Id).ToArray();
                int index=Array.IndexOf(soldiers,unit);var forward=BattleState.Forward(captain.Facing);var lateral=new Point(-forward.Y,forward.X);
                var formation=captain.Position+new Point(forward.X*2,forward.Y*2)+new Point(lateral.X*(index-soldiers.Length/2),lateral.Y*(index-soldiers.Length/2));
                if(battle.WalkableFor(formation,unit.Owner))return new(Path(battle,unit,formation)??Point.Zero,captain.Facing);
            }
            if(unit.Role==BattleRole.Captain && enemy!=null && Distance(enemy.Position,unit.Position)<=5)return new(Point.Zero,Face(enemy.Position-unit.Position));
        }
        return new(Path(battle,unit,objective)??Point.Zero,objective==unit.Position?unit.Facing:Face(objective-unit.Position));
    }
    public static int Distance(Point a,Point b)=>Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);
    public static BattleUnit FirstTarget(BattleState battle,BattleUnit unit,BattleFacing facing)
    {
        var forward=BattleState.Forward(facing);
        for(int i=1;i<battle.BoardSize;i++)
        {
            var p=unit.Position+new Point(forward.X*i,forward.Y*i);if(battle.BlocksShot(p,facing,i))break;if(i==1)continue;
            var target=battle.Units.FirstOrDefault(u=>u.Active && u.Position==p);if(target!=null)return target;
        }
        return null;
    }
    private static bool Free(BattleState battle,BattleUnit unit,Point p)=>battle.WalkableFor(p,unit.Owner) && (unit.Retreating || !battle.Units.Any(u=>u.Active && !u.Retreating && u.Id!=unit.Id && u.Position==p));
    public static Point? Path(BattleState battle,BattleUnit unit,Point target)
    {
        if(unit.Position==target)return Point.Zero;
        // Weighted search compares walking around a gate with spending attacks to break it.
        var queue=new PriorityQueue<(Point At,int Ammo),int>();queue.Enqueue((unit.Position,0),0);
        var costs=new Dictionary<(Point,int),int>{{(unit.Position,0),0}};
        var first=new Dictionary<(Point,int),Point>{{(unit.Position,0),Point.Zero}};
        int budget=unit.Retreating || unit.Role==BattleRole.Queen?0:unit.Daggers+unit.Rifles;
        while(queue.TryDequeue(out var state,out int cost)){
            if(cost!=costs[state])continue;
            if(state.At==target)return first[state];
            foreach(var step in Steps.OrderBy(s=>Distance(state.At+s,target))){
                var next=state.At+step;int attacks=0;
                if(battle.GateAt(next) is {} gate && !battle.Allied(unit.Owner,gate.Owner))attacks=gate.Health;
                if(state.Ammo+attacks>budget)continue;
                bool occupied=battle.Units.Any(u=>u.Active && !u.Retreating && u.Id!=unit.Id && u.Position==next);
                bool enemyGoal=next==target && battle.Units.Any(u=>u.Active && u.Position==target && !battle.Allied(u.Owner,unit.Owner));
                bool exit=next==target && !battle.Contains(target) && unit.Retreating;
                if(!exit && (!battle.Walkable(next) || attacks==0 && !battle.WalkableFor(next,unit.Owner) || occupied && !unit.Retreating && !enemyGoal))continue;
                var key=(next,state.Ammo+attacks);int nextCost=cost+10+attacks*10+(attacks>0?1:0);
                if(costs.TryGetValue(key,out int prior) && prior<=nextCost)continue;
                costs[key]=nextCost;first[key]=state.At==unit.Position?step:first[state];queue.Enqueue(key,nextCost);
            }
        }
        return null;
    }
}

namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static class BattleTurnResolver
{
    private sealed class Hits {public int Dagger,Rifle,Side;public bool DaggerHit,HostileHit;}
    public static void Advance(BattleState battle,IReadOnlyDictionary<long,BattleAction> orders=null)
    {
        if(battle.Finished)return;
        battle.Check();var units=battle.Units.Where(u=>u.Active).OrderBy(u=>u.Id).ToArray();
        var actions=units.ToDictionary(u=>u.Id,u=>orders!=null && orders.TryGetValue(u.Id,out var action)?action:BattleAi.Choose(battle,u));
        foreach(var unit in units)
        {
            var action=actions[unit.Id];
            if(Math.Abs(action.Step.X)+Math.Abs(action.Step.Y)>1 || (int)action.Facing<0 || (int)action.Facing>3 || !Enum.IsDefined(action.Weapon) || action.Weapon==BattleWeapon.Dagger && unit.Daggers==0 || action.Weapon==BattleWeapon.Rifle && unit.Rifles==0 || unit.Role==BattleRole.Queen && action.Weapon!=BattleWeapon.None)throw new ArgumentException("Invalid battle action.");
        }
        battle.Turn++;battle.Events.Add(new(battle.Turn,BattlePhase.Choose,0,"全軍行動確定"));
        battle.Capture(BattlePhase.Choose);
        var origin=units.ToDictionary(u=>u.Id,u=>u.Position);var positions=units.ToDictionary(u=>u.Id,u=>u.Position+actions[u.Id].Step);
        var blocked=new HashSet<long>();
        foreach(var u in units)
        {
            var p=positions[u.Id];var army=battle.Army(u.Owner);
            if(!battle.Walkable(p) && !(u.Retreating && BattleState.HomeExit(p,army.Side)))blocked.Add(u.Id);
            u.Facing=actions[u.Id].Facing;
        }
        battle.Events.Add(new(battle.Turn,BattlePhase.Place,0,"仮配置"));
        foreach(var u in units)u.Position=positions[u.Id];battle.Capture(BattlePhase.Place);
        foreach(var a in units)foreach(var b in units.Where(b=>b.Id>a.Id))
            if(!battle.Allied(a.Owner,b.Owner) && positions[a.Id]==origin[b.Id] && positions[b.Id]==origin[a.Id]){blocked.Add(a.Id);blocked.Add(b.Id);}
        while(true)
        {
            foreach(var id in blocked)positions[id]=origin[id];
            var collisions=units.Where(u=>BattleState.Inside(positions[u.Id])).GroupBy(u=>positions[u.Id]).Where(g=>g.Count()>1).SelectMany(g=>g).ToArray();
            bool added=false;foreach(var u in collisions)added|=blocked.Add(u.Id);if(!added)break;
        }
        foreach(var u in units)
        {
            u.Position=positions[u.Id];if(!BattleState.Inside(u.Position))u.Retreated=true;
            battle.Events.Add(new(battle.Turn,blocked.Contains(u.Id)?BattlePhase.Rollback:BattlePhase.Place,u.Id,$"{u.Position.X},{u.Position.Y} / {u.Facing}"));
        }
        foreach(var captain in units.Where(u=>u.Active && u.Role==BattleRole.Captain))
        {
            var soldiers=units.Where(u=>u.Active && u.Role==BattleRole.Soldier && u.Owner==captain.Owner && u.Facing==captain.Facing).ToArray();
            if(soldiers.Any(a=>soldiers.Any(b=>a.Id!=b.Id && BattleAi.Distance(a.Position,b.Position)==1)))battle.Events.Add(new(battle.Turn,BattlePhase.Place,captain.Id,"隊形成 / 剣を振り上げた"));
        }
        var hits=units.ToDictionary(u=>u.Id,_=>new Hits());bool will=false;
        battle.Capture(BattlePhase.Rollback);
        battle.Events.Add(new(battle.Turn,BattlePhase.Attack,0,"同時攻撃"));
        battle.Capture(BattlePhase.Attack);
        foreach(var attacker in units.Where(u=>u.Active))
        {
            var weapon=actions[attacker.Id].Weapon;if(weapon==BattleWeapon.None)continue;
            var step=BattleState.Forward(attacker.Facing);BattleUnit target=null;var end=attacker.Position+step;
            if(weapon==BattleWeapon.Dagger)target=units.FirstOrDefault(u=>u.Active && u.Position==end);
            else for(int distance=1;distance<10;distance++)
            {
                var p=attacker.Position+new Point(step.X*distance,step.Y*distance);if(!BattleState.Inside(p) || battle.Walls.Contains(p))break;
                end=p;if(distance==1)continue;target=units.FirstOrDefault(u=>u.Active && u.Position==p);if(target!=null)break;
            }
            battle.Attacks.Add(new(battle.Turn,attacker.Id,target?.Id,attacker.Position,end,weapon));
            if(target==null)continue;
            hits[target.Id].HostileHit|=!battle.Allied(attacker.Owner,target.Owner);
            if(weapon==BattleWeapon.Dagger)hits[attacker.Id].DaggerHit=true;
            var front=BattleState.Forward(target.Facing);var delta=attacker.Position-target.Position;bool frontal=front.X!=0?delta.Y==0 && Math.Sign(delta.X)==front.X:delta.X==0 && Math.Sign(delta.Y)==front.Y;
            if(!frontal)hits[target.Id].Side++;
            else if(weapon==BattleWeapon.Dagger){if(actions[target.Id].Weapon!=BattleWeapon.Dagger)hits[target.Id].Dagger++;}
            else hits[target.Id].Rifle++;
            if(!battle.Allied(attacker.Owner,target.Owner) && weapon==BattleWeapon.Dagger)will=true;
            battle.Events.Add(new(battle.Turn,BattlePhase.Receive,target.Id,$"{weapon} ← {attacker.Id}"));
        }
        battle.Capture(BattlePhase.Receive);
        foreach(var u in units.Where(u=>u.Active))
        {
            var h=hits[u.Id];var weapon=actions[u.Id].Weapon;
            if(h.DaggerHit)u.Daggers--;if(weapon==BattleWeapon.Rifle)u.Rifles--;
            if(weapon!=BattleWeapon.Rifle)
            {
                int dagger=Math.Min(u.Shields,h.Dagger);u.Shields-=dagger;h.Dagger-=dagger;
                int rifle=Math.Min(u.Shields,h.Rifle);u.Shields-=rifle;h.Rifle-=rifle;
                if(dagger+rifle>0 && h.HostileHit)will=true;
            }
            battle.Events.Add(new(battle.Turn,BattlePhase.Consume,u.Id,$"短剣 {u.Daggers} / 小銃 {u.Rifles} / 盾 {u.Shields}"));
        }
        battle.Capture(BattlePhase.Consume);
        foreach(var u in units.Where(u=>u.Active))
        {
            var h=hits[u.Id];if(h.Dagger+h.Rifle+h.Side==0)continue;
            u.Fallen=true;battle.Events.Add(new(battle.Turn,BattlePhase.Fall,u.Id,"転倒"));
            if(h.HostileHit)will=true;
        }
        battle.Capture(BattlePhase.Fall);
        Persuade(battle);
        if(battle.Kind==BattleKind.Node)
        {
            var occupant=battle.Units.FirstOrDefault(u=>u.Active && u.Position==battle.Flag);
            if(occupant!=null)
            {
                if(occupant.Owner==battle.Defender && !battle.FlagStanding){battle.FlagStanding=true;battle.Events.Add(new(battle.Turn,BattlePhase.Flag,occupant.Id,"旗を立てた"));}
                else if(!battle.Allied(occupant.Owner,battle.Defender) && battle.FlagStanding){battle.FlagStanding=false;battle.LastFlagOwner=occupant.Owner;battle.Events.Add(new(battle.Turn,BattlePhase.Flag,occupant.Id,"旗を倒した"));}
            }
        }
        battle.Capture(BattlePhase.Flag);
        bool reinforced=battle.Reinforce();battle.NoWillCount=will || reinforced?0:battle.NoWillCount+1;
        battle.Capture(BattlePhase.Reinforce);Finish(battle);battle.Capture(BattlePhase.Result);battle.Check();
    }
    private static void Persuade(BattleState battle)
    {
        foreach(var queen in battle.Units.Where(u=>u.Active && u.Role==BattleRole.Queen).OrderBy(u=>u.Id))
        {
            foreach(var enemy in battle.Units.Where(u=>u.Active && u.Role!=BattleRole.Queen && !battle.Allied(u.Owner,queen.Owner)).OrderBy(u=>u.Id).ToArray())
            {
                if(Math.Abs(enemy.Position.X-queen.Position.X)+Math.Abs(enemy.Position.Y-queen.Position.Y)>battle.Rules.PersuasionRadius || battle.Random.Next(100)>=battle.Rules.PersuasionPercent)continue;
                if(battle.Units.Count(u=>u.Active && u.Owner==queen.Owner)>=battle.Army(queen.Owner).Limit)continue;
                enemy.Owner=queen.Owner;if(enemy.Robot!=null)enemy.Robot=enemy.Robot with {Owner=queen.Owner};enemy.Retreating=false;
                battle.Events.Add(new(battle.Turn,BattlePhase.Receive,enemy.Id,"説得"));
            }
        }
    }
    // Judgment is already fixed. Replay every deployed survivor leaving the board,
    // without changing the victorious garrison or split-board survivor bookkeeping.
    private static void ShowWithdrawal(BattleState battle)
    {
        var views=battle.Units.Select(u=>new BattleUnitView(u.Id,u.Owner,u.Role,u.Position,u.Facing,u.Daggers,u.Rifles,u.Shields,u.Fallen,u.Retreated)).ToArray();
        var steps=new[]{new Point(0,-1),new Point(1,0),new Point(0,1),new Point(-1,0)};
        battle.Events.Add(new(battle.Turn,BattlePhase.Retreat,0,"戦意喪失 / 全軍撤退 / 判定確定"));
        while(views.Any(u=>!u.Fallen && !u.Retreated))
        {
            battle.Turn++;
            for(int i=0;i<views.Length;i++)
            {
                var u=views[i];if(u.Fallen || u.Retreated)continue;
                var queue=new Queue<Point>();queue.Enqueue(u.Position);
                var first=new Dictionary<Point,Point>{{u.Position,Point.Zero}};Point? move=null;
                // Prefer the home edge; congestion may use another reachable edge.
                for(int pass=0;pass<2 && move==null;pass++)
                {
                    queue.Clear();queue.Enqueue(u.Position);first.Clear();first[u.Position]=Point.Zero;
                    while(queue.TryDequeue(out var p) && move==null)
                    foreach(var step in steps)
                    {
                        var next=p+step;if(first.ContainsKey(next))continue;
                        if(!BattleState.Inside(next))
                        {if(pass==0 && !BattleState.HomeExit(next,battle.Army(u.Owner).Side))continue;move=p==u.Position?step:first[p];break;}
                        if(!battle.Walkable(next) || views.Any(v=>v.Id!=u.Id && !v.Fallen && !v.Retreated && v.Position==next))continue;
                        first[next]=p==u.Position?step:first[p];queue.Enqueue(next);
                    }
                }
                if(move==null)continue;
                var at=u.Position+move.Value;
                views[i]=u with {Position=at,Facing=BattleAi.Face(move.Value),Retreated=!BattleState.Inside(at)};
            }
            battle.Frames.Add(new(battle.Turn,BattlePhase.Retreat,views.ToArray(),battle.FlagStanding,battle.LastFlagOwner));
        }
        // Give the final departing pieces time to fade before the result screen.
        for(int i=0;i<5;i++)battle.Frames.Add(new(battle.Turn,BattlePhase.Retreat,views.ToArray(),battle.FlagStanding,battle.LastFlagOwner));
    }
    public static void Finish(BattleState battle)
    {
        var remaining=battle.Armies.Where(a=>battle.Units.Any(u=>u.Active && u.Owner==a.Owner) || a.Waiting.Count>0).Select(a=>a.Owner).ToArray();
        if(remaining.Length==0){battle.Finished=true;battle.Winner=-1;}
        else if(remaining.All(o=>battle.Allied(o,remaining[0])))
        {battle.Finished=true;battle.Winner=battle.Kind==BattleKind.Node && battle.FlagStanding && remaining.Contains(battle.Defender)?battle.Defender:remaining[0];}
        else if(battle.NoWillCount>=battle.Rules.NoWillTurns)
        {
            battle.Finished=true;
            battle.Winner=battle.Kind==BattleKind.Edge?-1:battle.FlagStanding?battle.Defender:
                battle.Units.Any(u=>u.Active && u.Owner==battle.LastFlagOwner)?battle.LastFlagOwner:-1;
            ShowWithdrawal(battle);
            foreach(var u in battle.Units.Where(u=>u.Active && u.Owner!=battle.Winner))u.Retreated=true;
            foreach(var army in battle.Armies.Where(a=>a.Owner!=battle.Winner))while(army.Waiting.TryDequeue(out var unit)){unit.Retreated=true;battle.Units.Add(unit);}
        }
        if(battle.Finished)battle.Events.Add(new(battle.Turn,BattlePhase.Result,0,$"勝者 {battle.Winner}"));
    }
}

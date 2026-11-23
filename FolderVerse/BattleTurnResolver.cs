namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static partial class BattleTurnResolver
{
    private sealed class Hits {public int Dagger,Rifle,Side;public bool DaggerHit,HostileHit,RetreatBackShot;}
    public static void Advance(BattleState battle,IReadOnlyDictionary<long,BattleAction> orders=null)
    {
        if(battle.Finished)return;
        battle.Check();var units=battle.Units.Where(u=>u.Active).OrderBy(u=>u.Id).ToArray();
        foreach(var u in units.Where(u=>battle.Army(u.Owner).Withdrawing))u.Retreating=true;
        var actions=units.ToDictionary(u=>u.Id,u=>orders!=null && orders.TryGetValue(u.Id,out var action)?action:BattleAi.Choose(battle,u));
        var crossfire=orders==null?BattleTactics.PlanCrossfire(battle,units,actions):Array.Empty<CrossfirePlan>();
        if(orders==null)BattleTactics.Observe(battle,units,actions);
        if(orders==null){BattleLessons.ObservePursuit(battle,units,actions);BattleLessons.ObserveDefenseEndurance(battle,units,actions);}
        else battle.PursuitHistory.Clear();
        foreach(var unit in units)
        {
            var action=actions[unit.Id];
            if(Math.Abs(action.Step.X)+Math.Abs(action.Step.Y)>1 || (int)action.Facing<0 || (int)action.Facing>3 || !Enum.IsDefined(action.Weapon) || action.Weapon==BattleWeapon.Dagger && unit.Daggers==0 || action.Weapon==BattleWeapon.Rifle && unit.Rifles==0 || unit.Role==BattleRole.Queen && action.Weapon!=BattleWeapon.None)throw new ArgumentException("Invalid battle action.");
        }
        BattleRetreat.ClearPaths(battle,units,actions);
        BattleLessons.Apply(battle,units,actions);
        var sideDaggerExposures=units.SelectMany(u=>units.Where(e=>BattleLessons.SideDaggerExposed(battle,u,actions[u.Id],e)).Select(e=>(Shooter:e.Id,Target:u.Id))).ToHashSet();
        battle.Turn++;battle.Events.Add(new(battle.Turn,BattlePhase.Choose,0,"全軍行動確定"));
        battle.Capture(BattlePhase.Choose);
        var origin=units.ToDictionary(u=>u.Id,u=>u.Position);var positions=units.ToDictionary(u=>u.Id,u=>u.Position+actions[u.Id].Step);
        var flagEntrants=units.Where(u=>!u.Retreating && u.Role!=BattleRole.Queen && !battle.Allied(u.Owner,battle.Defender) && u.Position!=battle.Flag && positions[u.Id]==battle.Flag).ToArray();
        var blocked=new HashSet<long>();
        foreach(var u in units)
        {
            var p=positions[u.Id];var army=battle.Army(u.Owner);
            if(p!=u.Position && !battle.WalkableFor(p,u.Owner) && !(u.Retreating && battle.HomeExit(p,army)))blocked.Add(u.Id);
            u.Facing=actions[u.Id].Facing;
        }
        battle.Events.Add(new(battle.Turn,BattlePhase.Place,0,"仮配置"));
        foreach(var u in units)u.Position=positions[u.Id];battle.Capture(BattlePhase.Place);
        foreach(var a in units)foreach(var b in units.Where(b=>b.Id>a.Id))
            if(!a.Retreating && !b.Retreating && !battle.AlliedUnits(a,b) && positions[a.Id]==origin[b.Id] && positions[b.Id]==origin[a.Id]){blocked.Add(a.Id);blocked.Add(b.Id);}
        while(true)
        {
            foreach(var id in blocked)positions[id]=origin[id];
            var collisions=units.Where(u=>!u.Retreating && battle.Contains(positions[u.Id])).GroupBy(u=>positions[u.Id]).Where(g=>g.Count()>1).SelectMany(g=>g).ToArray();
            bool added=false;foreach(var u in collisions)added|=blocked.Add(u.Id);if(!added)break;
        }
        var blockedEscapes=ClearEscapePaths(battle,units,actions,origin,positions,blocked);
        foreach(var u in units)
        {
            u.Position=positions[u.Id];if(battle.ReachedHome(u))u.Retreated=true;
            battle.Events.Add(new(battle.Turn,blocked.Contains(u.Id)?BattlePhase.Rollback:BattlePhase.Place,u.Id,$"{u.Position.X},{u.Position.Y} / {u.Facing}"));
        }
        if(units.Any(a=>!a.Retreating && blocked.Contains(a.Id) && actions[a.Id].Step!=Point.Zero &&
            units.Any(b=>b.Id>a.Id && !b.Retreating && blocked.Contains(b.Id) && actions[b.Id].Step!=Point.Zero && !battle.AlliedUnits(a,b) &&
                BattleAi.Distance(origin[a.Id],origin[b.Id])==2 && (origin[a.Id].X==origin[b.Id].X || origin[a.Id].Y==origin[b.Id].Y) &&
                origin[a.Id]+actions[a.Id].Step==origin[b.Id]+actions[b.Id].Step && !origin.Values.Contains(origin[a.Id]+actions[a.Id].Step) &&
                battle.WalkableFor(origin[a.Id]+actions[a.Id].Step,a.Owner) && battle.WalkableFor(origin[b.Id]+actions[b.Id].Step,b.Owner))))
            BattleLessons.Witness(battle,BattleLesson.CollisionTempo);
        BattleLessons.ObserveLateralCollision(battle,units,actions,origin,blocked);
        if(battle.Kind==BattleKind.Node && flagEntrants.Length>=2 && flagEntrants.All(u=>blocked.Contains(u.Id) && u.Position!=battle.Flag))BattleLessons.Witness(battle,BattleLesson.FlagCourtesy);
        foreach(var u in units.Where(u=>u.Active && u.ResumeAfterRetreatOverlap))if(!units.Any(other=>other.Active && other.Id!=u.Id && !other.Retreating && other.Position==u.Position)){
            u.ResumeAfterRetreatOverlap=false;if(!battle.Army(u.Owner).Withdrawing)u.Retreating=false;
        }
        foreach(var captain in units.Where(u=>u.Active && u.Role==BattleRole.Captain))
        {
            var soldiers=units.Where(u=>u.Active && u.Role==BattleRole.Soldier && u.Owner==captain.Owner && u.Facing==captain.Facing).ToArray();
            if(soldiers.Any(a=>soldiers.Any(b=>a.Id!=b.Id && BattleAi.Distance(a.Position,b.Position)==1)))battle.Events.Add(new(battle.Turn,BattlePhase.Place,captain.Id,"隊形成 / 剣を振り上げた"));
        }
        var hits=units.ToDictionary(u=>u.Id,_=>new Hits());var willing=new HashSet<int>();
        var crossfireShooters=new HashSet<long>();
        foreach(var plan in crossfire){
            var captain=units.Single(u=>u.Id==plan.Captain);
            foreach(long id in new[]{plan.Horizontal,plan.Vertical}){
                var shooter=units.Single(u=>u.Id==id);if(!shooter.Active||actions[id].Weapon!=BattleWeapon.Rifle)continue;
                bool invalid=!captain.Active||captain.Retreating||!BattleTactics.SeesFocus(battle,captain.Position,plan.Focus);
                var target=AttackTarget(battle,units,shooter,BattleWeapon.Rifle,out _);
                bool friendly=target!=null&&battle.AlliedUnits(shooter,target);
                if(invalid||BattleLessons.Has(battle,shooter,BattleLesson.CrossfireSafety)&&friendly){
                    actions[id]=actions[id] with {Weapon=BattleWeapon.None};continue;
                }
                crossfireShooters.Add(id);
            }
            if(crossfireShooters.Contains(plan.Horizontal)||crossfireShooters.Contains(plan.Vertical)){
                battle.Events.Add(new(battle.Turn,BattlePhase.Place,captain.Id,BattleTactics.CrossfireSignal));
                battle.Events.Add(new(battle.Turn,BattlePhase.Choose,captain.Id,BattleTactics.Crossfire));
            }
        }
        battle.Capture(BattlePhase.Rollback);
        battle.Events.Add(new(battle.Turn,BattlePhase.Attack,0,"同時攻撃"));
        battle.Capture(BattlePhase.Attack);
        var gateDamage=new Dictionary<Point,int>();
        foreach(var attacker in units.Where(u=>u.Active))
        {
            var weapon=actions[attacker.Id].Weapon;if(weapon==BattleWeapon.None)continue;
            var step=BattleState.Forward(attacker.Facing);
            var target=AttackTarget(battle,units,attacker,weapon,out var end);
            battle.Attacks.Add(new(battle.Turn,attacker.Id,target?.Id,attacker.Position,end,weapon));
            if(target==null){
                var gate=battle.GateAt(end);
                if(gate!=null && !battle.Allied(attacker.Owner,gate.Owner)){
                    gateDamage[end]=gateDamage.GetValueOrDefault(end)+1;
                    battle.GateHits.Add(new(battle.Turn,end,attacker.Position));willing.Add(attacker.Owner);
                    if(weapon==BattleWeapon.Dagger)hits[attacker.Id].DaggerHit=true;
                }
                continue;
            }
            if(weapon==BattleWeapon.Rifle && sideDaggerExposures.Contains((attacker.Id,target.Id)))BattleLessons.Witness(battle,BattleLesson.LeaveFiringLine);
            if(battle.AlliedUnits(attacker,target))BattleLessons.Witness(battle,BattleLesson.FireDiscipline);
            if(weapon==BattleWeapon.Rifle&&crossfireShooters.Contains(attacker.Id)&&battle.AlliedUnits(attacker,target))
                BattleLessons.Witness(battle,BattleLesson.CrossfireSafety);
            if(weapon==BattleWeapon.Dagger && actions[attacker.Id].Step==Point.Zero && battle.AlliedUnits(attacker,target) && origin[target.Id]!=target.Position &&
                units.Any(enemy=>!battle.AlliedUnits(attacker,enemy) && origin[enemy.Id]==origin[attacker.Id]+step && enemy.Position!=origin[enemy.Id] &&
                    (enemy.Position.X-origin[enemy.Id].X)*step.X+(enemy.Position.Y-origin[enemy.Id].Y)*step.Y==0))
                BattleLessons.Witness(battle,BattleLesson.WaitingDaggerSafety);
            hits[target.Id].HostileHit|=!battle.AlliedUnits(attacker,target);
            if(weapon==BattleWeapon.Dagger)hits[attacker.Id].DaggerHit=true;
            var front=BattleState.Forward(target.Facing);var delta=attacker.Position-target.Position;bool frontal=front.X!=0?delta.Y==0 && Math.Sign(delta.X)==front.X:delta.X==0 && Math.Sign(delta.Y)==front.Y;
            if(weapon==BattleWeapon.Rifle && target.Retreating && delta.X*front.X+delta.Y*front.Y<0)hits[target.Id].RetreatBackShot=true;
            if(weapon==BattleWeapon.Dagger && !frontal && !battle.AlliedUnits(attacker,target) && BattleLessons.CornerSideTrap(battle,target))BattleLessons.Witness(battle,BattleLesson.CornerEscape);
            if(!frontal)hits[target.Id].Side++;
            else if(weapon==BattleWeapon.Dagger){if(actions[target.Id].Weapon!=BattleWeapon.Dagger)hits[target.Id].Dagger++;}
            else hits[target.Id].Rifle++;
            if(!battle.AlliedUnits(attacker,target))willing.Add(attacker.Owner);
            battle.Events.Add(new(battle.Turn,BattlePhase.Receive,target.Id,$"{weapon} ← {attacker.Id}"));
        }
        if(units.Any(u=>u.Active && u.Role!=BattleRole.Queen && !u.Retreating && u.Daggers==0 && u.Position==origin[u.Id] &&
            units.Any(e=>e.Active && e.Role!=BattleRole.Queen && !e.Retreating && !battle.Allied(u.Owner,e.Owner) && e.Position==origin[e.Id] &&
                e.Position==u.Position+BattleState.Forward(u.Facing) && u.Position==e.Position+BattleState.Forward(e.Facing) &&
                !battle.Attacks.Any(a=>a.Turn==battle.Turn && (a.Attacker==u.Id || a.Attacker==e.Id) && a.Target.HasValue) &&
                !battle.GateHits.Any(h=>h.Turn==battle.Turn && (h.From==u.Position || h.From==e.Position)))))
            BattleLessons.Witness(battle,BattleLesson.RelieveFront);
        foreach(var damage in gateDamage){
            int index=battle.Fortifications.FindIndex(f=>f.Kind==FortificationKind.Gate && f.Position==damage.Key && f.Health>0);
            if(index>=0){var gate=battle.Fortifications[index];battle.Fortifications[index]=gate with {Health=Math.Max(0,gate.Health-damage.Value)};}
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
                if(dagger+rifle>0){battle.Events.Add(new(battle.Turn,BattlePhase.Consume,u.Id,"盾で防いだ"));if(h.HostileHit)willing.Add(u.Owner);}
            }
            battle.Events.Add(new(battle.Turn,BattlePhase.Consume,u.Id,$"短剣 {u.Daggers} / 小銃 {u.Rifles} / 盾 {u.Shields}"));
        }
        battle.Capture(BattlePhase.Consume);
        if(units.Any(u=>u.Active && hits[u.Id].RetreatBackShot && hits[u.Id].Dagger+hits[u.Id].Rifle+hits[u.Id].Side>0))BattleLessons.Witness(battle,BattleLesson.RetreatBackwards);
        foreach(var u in units.Where(u=>u.Active))
        {
            var h=hits[u.Id];if(h.Dagger+h.Rifle+h.Side==0)continue;
            if(h.HostileHit && blockedEscapes.Contains(u.Id))BattleLessons.Witness(battle,BattleLesson.ClearEscape);
            if (u.Role == BattleRole.Queen)
                u.QueenFellToEnemy = h.HostileHit;
            u.Fallen=true;battle.Events.Add(new(battle.Turn,BattlePhase.Fall,u.Id,"転倒"));
            if(h.HostileHit)willing.Add(u.Owner);
        }
        battle.Capture(BattlePhase.Fall);
        if(battle.Units.Any(u=>u.Active && u.Role==BattleRole.Queen))
            for(int hold=0;hold<2;hold++)battle.Capture(BattlePhase.Persuade);
        Persuade(battle);
        if(battle.Persuasions.Any(p=>p.Turn==battle.Turn))
            for(int hold=0;hold<(battle.Persuasions.Any(p=>p.Turn==battle.Turn && p.Success)?4:1);hold++)battle.Capture(BattlePhase.Converted);
        if(battle.Kind==BattleKind.Node)
        {
            bool wasStanding=battle.FlagStanding;
            var occupant=battle.Units.FirstOrDefault(u=>u.Active && u.Position==battle.Flag);
            if(occupant!=null)
            {
                if(battle.Allied(occupant.Owner,battle.Defender) && !battle.FlagStanding){battle.FlagStanding=true;battle.Events.Add(new(battle.Turn,BattlePhase.Flag,occupant.Id,"国旗を立てた"));}
                else if(!battle.Allied(occupant.Owner,battle.Defender) && battle.FlagStanding){battle.FlagStanding=false;battle.LastFlagOwner=occupant.Owner;willing.Add(occupant.Owner);battle.Events.Add(new(battle.Turn,BattlePhase.Flag,occupant.Id,"国旗を倒した"));}
            }
            battle.FlagDefenseTurns=battle.FlagStanding?(wasStanding?battle.FlagDefenseTurns+1:0):0;
            battle.FlagCaptureTurns=!battle.FlagStanding?(!wasStanding?battle.FlagCaptureTurns+1:0):0;
        }
        battle.Capture(BattlePhase.Flag);
        if(FlagVictory(battle)){battle.Capture(BattlePhase.Result);battle.Check();return;}
        battle.Reinforce();
        foreach(var army in battle.Armies.Where(a=>!a.Withdrawing)){
            bool defender=battle.Kind==BattleKind.Node && battle.Allied(army.Owner,battle.Defender);
            bool active=willing.Any(o=>battle.Allied(o,army.Owner)) ||
                battle.Events.Any(e=>e.Turn==battle.Turn && e.Phase==BattlePhase.Reinforce && battle.Units.Any(u=>u.Id==e.Unit && battle.Allied(u.Owner,army.Owner)));
            // Holding the fallen flag no longer forces evacuation or loss of will.
            active|=battle.Kind==BattleKind.Node && !battle.FlagStanding && battle.Allied(battle.LastFlagOwner,army.Owner);
            active|=battle.Kind==BattleKind.Node && battle.FlagStanding && !defender &&
                !battle.Armies.Any(a=>battle.Allied(a.Owner,battle.Defender) && (battle.ReadyWaiting(a).Any() || battle.Units.Any(u=>u.Active && u.Owner==a.Owner))) &&
                units.Any(u=>u.Active && !u.Retreating && u.Owner==army.Owner && origin.ContainsKey(u.Id) && u.Position!=origin[u.Id]);
            bool enRoute=army.Waiting.Any(u=>u.Active && u.ArrivalTurn>battle.Turn) && !battle.Units.Any(u=>u.Active && u.Owner==army.Owner);
            army.NoWillCount=defender || enRoute?0:active?0:army.NoWillCount+1;
            if(army.NoWillCount<battle.Rules.NoWillTurns)continue;
            army.Withdrawing=true;
            var departing=battle.Units.Where(u=>u.Active && u.Owner==army.Owner).ToArray();
            if(departing.Length==1 && departing[0].Role==BattleRole.Queen &&
                !battle.Units.Any(u=>u.Owner==army.Owner && u.Role!=BattleRole.Queen) &&
                !battle.ReadyWaiting(army).Any(u=>u.Role!=BattleRole.Queen) &&
                !HasPersuasionTarget(battle,departing[0]) &&
                !battle.Persuasions.Any(p=>p.Queen==departing[0].Id))
                departing[0].WitnessedLessons|=BattleLesson.QueenWithoutTargets;
            foreach(var u in departing)u.Retreating=true;
            while(army.Waiting.TryDequeue(out var waiting)){waiting.Retreated=true;battle.Units.Add(waiting);}
            battle.Events.Add(new(battle.Turn,BattlePhase.Retreat,0,$"陣営 {army.Owner+1} 戦意喪失 / 撤退"));
        }
        battle.NoWillCount=battle.Armies.Where(a=>!a.Withdrawing).Select(a=>a.NoWillCount).DefaultIfEmpty(0).Max();
        battle.Capture(BattlePhase.Reinforce);Finish(battle);battle.Capture(BattlePhase.Result);battle.Check();
    }
    private static bool FlagVictory(BattleState battle)
    {
        if(battle.Kind!=BattleKind.Node || battle.SplitBoard)return false;
        bool defended=battle.FlagStanding && battle.FlagDefenseTurns>=battle.Rules.FlagDefenseTurns && battle.Armies.Any(a=>battle.Allied(a.Owner,battle.Defender) && (battle.ReadyWaiting(a).Any() || battle.Units.Any(u=>u.Active && u.Owner==a.Owner)));
        bool captured=!battle.FlagStanding && battle.FlagCaptureTurns>=battle.Rules.FlagCaptureTurns;
        // A departed captor cannot receive territory; a surviving ally of that captor may finish capture.
        int captor=battle.Armies.Where(a=>!a.Withdrawing && !battle.Allied(a.Owner,battle.Defender) && battle.Allied(a.Owner,battle.LastFlagOwner) &&
            (battle.Units.Any(u=>u.Active && u.Owner==a.Owner) || battle.ReadyWaiting(a).Any()))
            .OrderBy(a=>battle.Allied(a.Owner,battle.LastFlagOwner)?0:1).ThenBy(a=>a.Owner).Select(a=>a.Owner).DefaultIfEmpty(-1).First();
        bool cleared=!battle.FlagStanding && captor>=0 && !battle.Armies.Any(a=>battle.Allied(a.Owner,battle.Defender) && (battle.ReadyWaiting(a).Any() || battle.Units.Any(u=>u.Active && u.Owner==a.Owner)));
        if(!defended && !captured && !cleared)return false;
        battle.Winner=defended?battle.Defender:captor;battle.Finished=true;battle.CancelUnarrivedRelief();
        if(defended)BattleRetreat.AfterFlagDefense(battle);
        battle.Events.Add(new(battle.Turn,BattlePhase.Result,0,defended?"国旗の防衛成功":"国旗の奪取成功 / 守備側降伏"));
        return true;
    }
    private static bool PersuasionTarget(BattleState battle,BattleUnit queen,BattleUnit unit) =>
        unit.Active && unit.Role!=BattleRole.Queen &&
        (unit.Owner==queen.BattleOwner?unit.Robot!=null:!battle.AlliedUnits(unit,queen)) &&
        (unit.Robot!=null?unit.Robot.LastBattlePersuasionTurn!=battle.GlobalTurn:!battle.PersuadedUnits.Contains(unit.Id)) &&
        (unit.Robot?.PersuadableParts(queen.BattleOwner)??RobotParts.Complete)!=0;

    internal const int QueenPersuasionPercent=1;

    private static bool QueenTarget(BattleState battle,BattleUnit queen,BattleUnit target) =>
        target.Active && target.Role==BattleRole.Queen && target.Id!=queen.Id &&
        !battle.AlliedUnits(target,queen) && !battle.Allied(target.BattleOwner,queen.Owner) &&
        battle.CanPersuadeQueen?.Invoke(target.Owner)==true;

    internal static bool HasPersuasionTarget(BattleState battle,BattleUnit queen) =>
        battle.Units.Concat(battle.Armies.SelectMany(army=>battle.ReadyWaiting(army)))
            .Any(unit=>PersuasionTarget(battle,queen,unit) || QueenTarget(battle,queen,unit));

    private static void PersuadeQueens(BattleState battle,BattleUnit[] queens)
    {
        // Choose from the affiliations at the start of the persuasion step.
        var attempts=queens.Select(target=>(Target:target,Sources:queens.Where(queen=>QueenTarget(battle,queen,target) &&
            Math.Abs(target.Position.X-queen.Position.X)+Math.Abs(target.Position.Y-queen.Position.Y)<=battle.Rules.PersuasionRadius).ToArray()))
            .Where(attempt=>attempt.Sources.Length>0).ToArray();
        foreach(var (target,sources) in attempts)
        {
            if(battle.Random.Next(100)>=QueenPersuasionPercent)continue;
            var queen=sources.Length==1?sources[0]:sources[battle.Random.Next(sources.Length)];
            battle.Persuasions.Add(new(battle.Turn,queen.Id,target.Id,queen.Owner,target.BattleOwner,queen.Position,target.Position,true));
            target.PersuadedByQueen=queen.Owner;
            battle.Events.Add(new(battle.Turn,BattlePhase.Converted,target.Id,"説得"));
        }
    }

    private static void Persuade(BattleState battle)
    {
        var queens=battle.Units.Where(u=>u.Active && u.Role==BattleRole.Queen).OrderBy(u=>u.Id).ToArray();
        foreach(var queen in queens)
        {
            int owner=queen.BattleOwner;
            var army=battle.Armies.FirstOrDefault(a=>a.Owner==owner);
            if(army==null)continue;
            foreach(var enemy in battle.Units.Where(u=>PersuasionTarget(battle,queen,u)).OrderBy(u=>u.Id).ToArray())
            {
                if(Math.Abs(enemy.Position.X-queen.Position.X)+Math.Abs(enemy.Position.Y-queen.Position.Y)>battle.Rules.PersuasionRadius)continue;
                bool own=enemy.Owner==owner;
                var parts=enemy.Robot?.PersuadableParts(owner)??RobotParts.Complete;
                bool success=battle.Random.Next(100)<battle.Rules.PersuasionPercent;
                success&=own || battle.Units.Count(u=>u.Active && u.Owner==owner)<army.Limit;
                battle.Persuasions.Add(new(battle.Turn,queen.Id,enemy.Id,owner,enemy.Owner,queen.Position,enemy.Position,success));
                battle.Events.Add(new(battle.Turn,BattlePhase.Persuade,queen.Id,"説得中"));
                if(!success)continue;
                if(own)parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.First(p=>(parts&p)!=0);
                enemy.Owner=owner;
                if(enemy.Robot!=null)enemy.Robot=enemy.Robot.Persuaded(owner,parts,true) with {LastBattlePersuasionTurn=battle.GlobalTurn};
                battle.PersuadedUnits.Add(enemy.Id);
                bool overlap=battle.Units.Any(u=>u.Active && u.Id!=enemy.Id && !u.Retreating && u.Position==enemy.Position);
                enemy.Retreating=overlap;enemy.ResumeAfterRetreatOverlap=overlap;
                battle.Events.Add(new(battle.Turn,BattlePhase.Converted,enemy.Id,"説得"));
            }
        }
        PersuadeQueens(battle,queens);
    }
    public static void Finish(BattleState battle)
    {
        if(battle.Units.Any(u=>u.Active && battle.Army(u.Owner).Withdrawing))return;
        var remaining=battle.Armies.Where(a=>!a.Withdrawing && (battle.Units.Any(u=>u.Active && u.Owner==a.Owner) || battle.ReadyWaiting(a).Any())).Select(a=>a.Owner).ToArray();
        if(remaining.Length==0){battle.Finished=true;battle.Winner=-1;}
        else if(remaining.All(o=>battle.Allied(o,remaining[0])))
        {
            if(battle.Kind==BattleKind.Node && !battle.SplitBoard && battle.FlagStanding && !battle.Allied(remaining[0],battle.Defender))return;
            battle.Finished=true;battle.Winner=battle.Kind==BattleKind.Node && battle.FlagStanding && remaining.Contains(battle.Defender)?battle.Defender:remaining[0];
        }
        if(battle.Finished)battle.CancelUnarrivedRelief();
        if(battle.Finished)battle.Events.Add(new(battle.Turn,BattlePhase.Result,0,$"勝者 {battle.Winner}"));
    }
}

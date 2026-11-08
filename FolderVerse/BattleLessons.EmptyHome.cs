namespace FolderVerse;
using System.Linq;

public sealed partial class BattleLessons
{
    public void ObserveEmptyFlag(BattleState battle)
    {
        if(battle.Kind!=BattleKind.Node || battle.Defender<0 || battle.FlagStanding)return;
        var units=battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).ToArray();
        // Fallen or withdrawn defenders still prove that this was a defended battle.
        if(units.Any(u=>u.Robot!=null && battle.Allied(u.Owner,battle.Defender)))return;
        foreach(var flag in battle.Events.Where(e=>e.Phase==BattlePhase.Flag && e.Detail is "旗を倒した" or "国旗を倒した")){
            var attacker=units.FirstOrDefault(u=>u.Id==flag.Unit);
            if(attacker==null || battle.Allied(attacker.Owner,battle.Defender) || (Owned[attacker.Owner]&BattleLesson.EmptyHome)!=0 || Pending.Any(a=>a.Owner==attacker.Owner && a.Lesson==BattleLesson.EmptyHome))continue;
            Pending.Add(new(attacker.Owner,BattleLesson.EmptyHome));
        }
    }
    public static int ReserveHomeGuards(WorldSetup w,int owner,int? onlyNode=null)
    {
        if((w.Lessons.Owned[owner]&BattleLesson.EmptyHome)==0)return 0;
        int count=0;
        foreach(var node in w.Nodes.All.Where(n=>(!onlyNode.HasValue || n.Id==onlyNode.Value) && n.Functional && n.Owner==owner && !w.Campaign.RobotBattles.LockedNode(n.Id))){
            var store=w.Robots.Nodes[node.Id];
            if(store.Robots.Any(r=>r.Owner==owner && r.DefenseOnly && r.CanFight))continue;
            var robot=store.Robots.Where(r=>r.Owner==owner && !r.Stationed && !w.Robots.AssemblyLocked(node.Id,r) && w.Robots.CanSetReservation(node.Id,r,HangarReservation.Defense,owner))
                .OrderBy(r=>r.CombatParts(owner)).ThenBy(r=>r.Id).FirstOrDefault();
            if(robot!=null && w.Robots.ToggleReservation(node.Id,robot.Id,HangarReservation.Defense,owner))count++;
        }
        return count;
    }
}

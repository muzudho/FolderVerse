namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

[Flags]
public enum BattleLesson { None=0,FlagCourtesy=1,RetreatBackwards=2,FireDiscipline=4,LeaveFiringLine=8,ShieldSupport=16 }
public sealed record LessonAward(int Owner,BattleLesson Lesson);
public sealed class BattleLessons
{
    public static readonly BattleLesson[] All={BattleLesson.FlagCourtesy,BattleLesson.RetreatBackwards,BattleLesson.FireDiscipline,BattleLesson.LeaveFiringLine,BattleLesson.ShieldSupport};
    public static string Title(BattleLesson lesson)=>lesson switch {
        BattleLesson.FlagCourtesy=>"戦訓#1 旗でお見合い",
        BattleLesson.RetreatBackwards=>"戦訓#2 撤退したら背中を撃たれた",
        BattleLesson.LeaveFiringLine=>"戦訓#4 射線にいるのに横向いて短剣振ってしまって撃たれた",
        BattleLesson.ShieldSupport=>"戦訓#5 前に盾持った仲間がいるのに後ろのやつが撃たない",
        _=>"戦訓#3 射線上の仲間を撃った"};
    public BattleLesson[] Owned {get;}=new BattleLesson[WorldSetup.CastCount];
    public List<LessonAward> Pending {get;}=new();
    public void Reset(){Array.Clear(Owned);Pending.Clear();}
    public void BringHome(BattleUnit unit)
    {
        if(unit.Fallen || unit.Role==BattleRole.Queen || unit.Robot==null)return;
        foreach(var lesson in All)if((unit.WitnessedLessons&lesson)!=0 && (Owned[unit.Owner]&lesson)==0 && !Pending.Any(a=>a.Owner==unit.Owner && a.Lesson==lesson))Pending.Add(new(unit.Owner,lesson));
        unit.WitnessedLessons=BattleLesson.None;
    }
    public LessonAward[] Research()
    {
        var awards=Pending.Where(a=>(Owned[a.Owner]&a.Lesson)==0).OrderBy(a=>a.Lesson).ThenBy(a=>a.Owner).ToArray();
        foreach(var award in awards)Owned[award.Owner]|=award.Lesson;
        Pending.Clear();return awards;
    }
    public static bool Has(BattleState battle,BattleUnit unit,BattleLesson lesson)=>unit.Role!=BattleRole.Queen && (battle.Lessons[unit.Owner]&lesson)!=0;
    public static void Witness(BattleState battle,BattleLesson lesson)
    {
        foreach(var unit in battle.Units.Where(u=>u.Active && u.Role!=BattleRole.Queen && battle.Contains(u.Position)))unit.WitnessedLessons|=lesson;
    }
    // Decisions use the starting positions: a friend can still enter the line during simultaneous movement.
    public static bool SafeShot(BattleState battle,BattleUnit shooter,BattleFacing facing)
    {
        var forward=BattleState.Forward(facing);var lateral=new Point(-forward.Y,forward.X);
        foreach(var friend in battle.Units.Where(u=>u.Active && u.Id!=shooter.Id && battle.Allied(u.Owner,shooter.Owner)))
        {
            var delta=friend.Position-shooter.Position;int along=delta.X*forward.X+delta.Y*forward.Y,side=delta.X*lateral.X+delta.Y*lateral.Y;
            if(along<=0)continue;
            bool visible=true;
            for(int i=1;i<=along;i++)if(battle.BlocksShot(shooter.Position+new Point(forward.X*i,forward.Y*i))){visible=false;break;}
            if(!visible)continue;
            if(side==0){if(along==1 && ShieldInFront(battle,shooter,facing) && Has(battle,shooter,BattleLesson.ShieldSupport))continue;return false;}
            var look=BattleState.Forward(friend.Facing);
            if(Math.Abs(side)==1 && (look.X*lateral.X+look.Y*lateral.Y)*side<0)return false;
        }
        return true;
    }
    public static bool ShieldInFront(BattleState battle,BattleUnit shooter,BattleFacing facing)=>battle.Units.Any(u=>u.Active && battle.Allied(u.Owner,shooter.Owner) && u.Position==shooter.Position+BattleState.Forward(facing) && u.Shields>0 && u.Facing==facing);
    public static void Apply(BattleState battle,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        if(units.Any(u=>actions[u.Id].Weapon==BattleWeapon.Rifle && Has(battle,u,BattleLesson.FireDiscipline) && !Has(battle,u,BattleLesson.ShieldSupport) && ShieldInFront(battle,u,actions[u.Id].Facing) && BattleAi.FirstTarget(battle,u,actions[u.Id].Facing) is {} target && !battle.Allied(u.Owner,target.Owner)))Witness(battle,BattleLesson.ShieldSupport);
        foreach(var unit in units)actions[unit.Id]=AvoidSideDagger(battle,unit,actions[unit.Id]);
        foreach(var unit in units)if(actions[unit.Id].Weapon==BattleWeapon.Rifle && Has(battle,unit,BattleLesson.FireDiscipline) && !SafeShot(battle,unit,actions[unit.Id].Facing))actions[unit.Id]=actions[unit.Id] with {Weapon=BattleWeapon.None};
        if(battle.Kind!=BattleKind.Node)return;
        var entrants=units.Where(u=>!u.Retreating && u.Position!=battle.Flag && u.Position+actions[u.Id].Step==battle.Flag).ToArray();
        foreach(var unit in entrants.Where(u=>Has(battle,u,BattleLesson.FlagCourtesy)))
        {
            var friends=entrants.Where(u=>u.Role!=BattleRole.Queen && battle.Allied(u.Owner,unit.Owner));
            int Priority(BattleUnit u)=>u.Position.Y<battle.Flag.Y?0:u.Position.X>battle.Flag.X?1:u.Position.Y>battle.Flag.Y?2:3;
            var first=friends.OrderBy(Priority).ThenBy(u=>u.Id).FirstOrDefault();
            if(first!=null && first.Id!=unit.Id)actions[unit.Id]=actions[unit.Id] with {Step=Point.Zero};
        }
    }
    public static bool SideDaggerExposed(BattleState battle,BattleUnit unit,BattleAction action,BattleUnit shooter)
    {
        if(action.Weapon!=BattleWeapon.Dagger || shooter.Rifles<=0 || battle.Allied(unit.Owner,shooter.Owner) || BattleAi.FirstTarget(battle,shooter,shooter.Facing)!=unit)return false;
        var toward=BattleState.Forward(unit.Facing);var delta=shooter.Position-unit.Position;
        var swing=BattleState.Forward(action.Facing);
        return toward.X*delta.X+toward.Y*delta.Y>0 && swing.X*delta.X+swing.Y*delta.Y==0;
    }
    public static BattleAction AvoidSideDagger(BattleState battle,BattleUnit unit,BattleAction action)
    {
        if(!Has(battle,unit,BattleLesson.LeaveFiringLine) || unit.Retreating || action.Weapon!=BattleWeapon.Dagger)return action;
        var threats=battle.Units.Where(e=>e.Active && SideDaggerExposed(battle,unit,action,e)).ToArray();
        if(threats.Length==0)return action;
        var face=BattleAi.Face(threats[0].Position-unit.Position);
        var forward=BattleState.Forward(face);var side=new Point(-forward.Y,forward.X);
        bool InLine(Point p,BattleUnit e){
            var f=BattleState.Forward(e.Facing);var d=p-e.Position;int distance=d.X*f.X+d.Y*f.Y;
            if(distance<2 || d.X*f.Y!=d.Y*f.X)return false;
            for(int i=1;i<distance;i++){var q=e.Position+new Point(f.X*i,f.Y*i);if(battle.BlocksShot(q) || i>1 && battle.Units.Any(u=>u.Active && u.Id!=unit.Id && u.Position==q))return false;}
            return true;
        }
        foreach(var step in new[]{side,new Point(-side.X,-side.Y)}){
            var p=unit.Position+step;
            if(battle.Walkable(p) && !battle.Units.Any(u=>u.Active && u.Id!=unit.Id && u.Position==p) && !battle.Units.Any(e=>e.Active && e.Rifles>0 && !battle.Allied(e.Owner,unit.Owner) && InLine(p,e)))return new(step,face);
        }
        return new(Point.Zero,face);
    }
}

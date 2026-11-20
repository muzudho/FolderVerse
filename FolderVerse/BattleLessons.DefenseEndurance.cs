namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class BattleLessons
{
    public static bool EnduranceGuard(BattleState b,BattleUnit u)=>u.Active && !u.Retreating && u.Role!=BattleRole.Queen &&
        u.Robot?.DefenseOnly==true && b.Kind==BattleKind.Node && !b.SplitBoard && b.Defender>=0 && b.Allied(u.Owner,b.Defender);

    public static void ObserveDefenseEndurance(BattleState b,BattleUnit[] units,IReadOnlyDictionary<long,BattleAction> actions)
    {
        foreach(var guard in units.Where(u=>EnduranceGuard(b,u) && !Has(b,u,BattleLesson.DefenseEndurance))){
            var step=actions[guard.Id].Step;
            if(step==Point.Zero || BattleAi.Distance(guard.Position+step,b.Flag)<=BattleAi.Distance(guard.Position,b.Flag))continue;
            if(!units.Any(e=>!b.Allied(e.Owner,guard.Owner) && BattleAi.Distance(e.Position,b.Flag)<=3))continue;
            foreach(var witness in units.Where(u=>u.Owner==guard.Owner && u.Role!=BattleRole.Queen))witness.WitnessedLessons|=BattleLesson.DefenseEndurance;
        }
    }

    public static BattleAction EnduranceAction(BattleState b,BattleUnit u,BattleUnit[] enemies)
    {
        var nearest=enemies.FirstOrDefault();
        if(nearest!=null && BattleAi.Distance(u.Position,b.Flag)<=4){
            var facing=BattleAi.Face(nearest.Position-u.Position);
            if(BattleAi.Distance(u.Position,nearest.Position)==1 && u.Daggers>0)
                return AvoidSideDagger(b,u,new(Point.Zero,facing,BattleWeapon.Dagger));
            foreach(var enemy in enemies){
                facing=BattleAi.Face(enemy.Position-u.Position);
                if(u.Rifles>0 && (enemy.Position.X==u.Position.X || enemy.Position.Y==u.Position.Y) &&
                    BattleAi.Distance(u.Position,enemy.Position)>=2 && BattleAi.FirstTarget(b,u,facing)==enemy)
                    return new(Point.Zero,facing,BattleWeapon.Rifle);
            }
        }
        bool urgent=!b.FlagStanding || enemies.Any(e=>BattleAi.Distance(e.Position,b.Flag)<=2);
        bool Free(Point p)=>b.WalkableFor(p,u.Owner) && !b.Units.Any(other=>other.Active && other.Id!=u.Id && other.Position==p);
        int Threat(Point p)=>enemies.Sum(e=>{
            int distance=BattleAi.Distance(e.Position,p);int danger=e.Daggers>0 && distance<=1?12:0;
            if(e.Rifles==0 || distance<2 || e.Position.X!=p.X && e.Position.Y!=p.Y)return danger;
            var direction=BattleAi.Face(p-e.Position);var forward=BattleState.Forward(direction);
            for(int i=1;i<=distance;i++)if(b.BlocksShot(e.Position+new Point(forward.X*i,forward.Y*i),direction,i))return danger;
            return danger+8;
        });
        int Score(Point p){
            int distance=BattleAi.Distance(p,b.Flag);
            bool behind=b.Fortifications.Any(f=>f.Kind==FortificationKind.Wall && b.Allied(u.Owner,f.Owner) && f.Position-BattleState.Forward(f.Facing)==p);
            int support=Math.Min(2,b.Units.Count(friend=>friend.Active && friend.Id!=u.Id && b.Allied(u.Owner,friend.Owner) && BattleAi.Distance(friend.Position,p)<=2));
            return -distance*(urgent?20:2)-Threat(p)+(behind && !urgent?10:0)+support;
        }
        var goals=(from y in Enumerable.Range(0,b.BoardSize) from x in Enumerable.Range(0,b.BoardSize) let p=new Point(x,y)
            where BattleAi.Distance(p,b.Flag)<=3 && Free(p) orderby Score(p) descending,p==u.Position?0:1,BattleAi.Distance(u.Position,p),y,x select p);
        foreach(var goal in goals){var step=BattleAi.Path(b,u,goal);if(!step.HasValue || step.Value!=Point.Zero && !Free(u.Position+step.Value))continue;
            return new(step.Value,nearest==null?u.Facing:BattleAi.Face(nearest.Position-u.Position));}
        return new(Point.Zero,nearest==null?u.Facing:BattleAi.Face(nearest.Position-u.Position));
    }
}

namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed record CrossfirePlan(long Captain,long Horizontal,long Vertical,Point Focus);
public static partial class BattleTactics
{
    public const string Crossfire="クロスファイア：戦術カード";
    public const string CrossfireSignal="クロスファイア / 軍刀の合図";
    public static bool UsesCrossfire(int character)=>character%6 is 1 or 2 or 4;
    public static bool SeesFocus(BattleState b,Point from,Point focus)
    {
        // Supercover traversal: a corner touching an opaque tile also blocks observation.
        int dx=focus.X-from.X,dy=focus.Y-from.Y,nx=Math.Abs(dx),ny=Math.Abs(dy),sx=Math.Sign(dx),sy=Math.Sign(dy),ix=0,iy=0;var p=from;
        while(ix<nx||iy<ny){int decision=(1+2*ix)*ny-(1+2*iy)*nx;
            if(decision==0){if(b.BlocksShot(p+new Point(sx,0))||b.BlocksShot(p+new Point(0,sy)))return false;p+=new Point(sx,sy);ix++;iy++;}
            else if(decision<0){p.X+=sx;ix++;}else{p.Y+=sy;iy++;}
            if(b.BlocksShot(p))return false;
        }
        return true;
    }
    public static bool CanAim(BattleState b,BattleUnit shooter,Point focus)
    {
        var delta=focus-shooter.Position;int distance=BattleAi.Distance(focus,shooter.Position);
        if(distance<2||delta.X!=0&&delta.Y!=0||!b.Contains(focus))return false;
        var facing=BattleAi.Face(delta);var forward=BattleState.Forward(facing);
        for(int i=1;i<=distance;i++){var p=shooter.Position+new Point(forward.X*i,forward.Y*i);
            if(b.BlocksShot(p,facing,i)||i<distance&&b.Units.Any(u=>u.Active&&u.Id!=shooter.Id&&u.Position==p))return false;}
        return !b.Units.Any(u=>u.Active&&b.Allied(shooter.Owner,u.Owner)&&u.Position==focus);
    }
    public static CrossfirePlan[] PlanCrossfire(BattleState b,BattleUnit[] units,Dictionary<long,BattleAction> actions)
    {
        var plans=new List<CrossfirePlan>();var used=new HashSet<long>();
        foreach(var captain in units.Where(u=>u.Role==BattleRole.Captain&&!u.Retreating&&b.CrossfireEnabled[u.Owner]).OrderBy(u=>u.Id)){
            var shooters=units.Where(u=>u.Owner==captain.Owner&&u.Role==BattleRole.Soldier&&!u.Retreating&&u.Rifles>0&&!used.Contains(u.Id)).OrderBy(u=>u.Id).ToArray();
            var focuses=units.Where(e=>!b.Allied(e.Owner,captain.Owner)).OrderBy(e=>e.Id).SelectMany(e=>new[]{e.Position+actions[e.Id].Step,e.Position}).Distinct();
            foreach(var focus in focuses){
                if(!b.Contains(focus)||!SeesFocus(b,captain.Position,focus))continue;
                var horizontal=shooters.FirstOrDefault(u=>u.Position.Y==focus.Y&&CanAim(b,u,focus));
                var vertical=shooters.FirstOrDefault(u=>u.Position.X==focus.X&&CanAim(b,u,focus));
                if(horizontal==null||vertical==null)continue;
                foreach(var u in new[]{horizontal,vertical}){actions[u.Id]=new(Point.Zero,BattleAi.Face(focus-u.Position),BattleWeapon.Rifle);used.Add(u.Id);}
                actions[captain.Id]=new(Point.Zero,BattleAi.Face(focus-captain.Position));
                plans.Add(new(captain.Id,horizontal.Id,vertical.Id,focus));break;
            }
        }
        return plans.ToArray();
    }
}

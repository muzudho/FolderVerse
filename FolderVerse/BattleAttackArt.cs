namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public static class BattleAttackArt
{
    public static float ImpactProgress(BattleState battle,int frameIndex,double framePosition)
    {
        var frame=battle.Frames[frameIndex];
        if(frame.Phase is not (BattlePhase.Receive or BattlePhase.Consume or BattlePhase.Fall))return -1;
        int start=battle.Frames.FindIndex(f=>f.Turn==frame.Turn && f.Phase==BattlePhase.Receive);
        int end=battle.Frames.FindIndex(f=>f.Turn==frame.Turn && f.Phase==BattlePhase.Fall);
        if(start<0 || end<start)return -1;
        return (float)Math.Clamp((framePosition-start)/(end-start+1),0,1);
    }
    public static Vector2 Recoil(BattleState battle,int turn,long target,float progress)
    {
        if(progress<0)return Vector2.Zero;
        var direction=Vector2.Zero;
        foreach(var hit in battle.Attacks.Where(a=>a.Turn==turn && a.Target==target && CausesDaggerFall(battle,a))){
            var delta=new Vector2(hit.To.X-hit.From.X,hit.To.Y-hit.From.Y);
            if(delta.LengthSquared()>0)direction+=Vector2.Normalize(delta);
        }
        if(direction.LengthSquared()>1)direction.Normalize();
        return direction*(14*MathF.Sin(progress*MathHelper.Pi));
    }
    public static bool CausesDaggerFall(BattleState battle,BattleAttack attack)
    {
        if(attack.Weapon!=BattleWeapon.Dagger || attack.Target==null || battle.Attacks.Any(other=>Clashes(attack,other)))return false;
        var before=battle.Frames.FirstOrDefault(f=>f.Turn==attack.Turn && f.Phase==BattlePhase.Attack)?.Units.FirstOrDefault(u=>u.Id==attack.Target);
        var after=battle.Frames.FirstOrDefault(f=>f.Turn==attack.Turn && f.Phase==BattlePhase.Fall)?.Units.FirstOrDefault(u=>u.Id==attack.Target);
        if(before==null || before.Fallen || after==null || !after.Fallen)return false;
        var front=BattleState.Forward(before.Facing);var delta=attack.From-attack.To;
        bool frontal=delta==front;
        bool firing=battle.Attacks.Any(a=>a.Turn==attack.Turn && a.Attacker==attack.Target && a.Weapon==BattleWeapon.Rifle);
        return !frontal || before.Shields==0 || firing;
    }
    public static bool Clashes(BattleAttack a,BattleAttack b)=>
        a.Turn==b.Turn && a.Weapon==BattleWeapon.Dagger && b.Weapon==BattleWeapon.Dagger &&
        a.Target==b.Attacker && b.Target==a.Attacker && a.From==b.To && a.To==b.From &&
        Math.Abs(a.From.X-a.To.X)+Math.Abs(a.From.Y-a.To.Y)==1;
}

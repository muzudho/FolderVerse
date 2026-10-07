namespace FolderVerse;
using System;

public static class BattleAttackArt
{
    public static bool Clashes(BattleAttack a,BattleAttack b)=>
        a.Turn==b.Turn && a.Weapon==BattleWeapon.Dagger && b.Weapon==BattleWeapon.Dagger &&
        a.Target==b.Attacker && b.Target==a.Attacker && a.From==b.To && a.To==b.From &&
        Math.Abs(a.From.X-a.To.X)+Math.Abs(a.From.Y-a.To.Y)==1;
}

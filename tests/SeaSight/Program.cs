using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}

for(int rotation=0;rotation<4;rotation++){
 var sea=new BattleState(17);sea.BuildSeaLayout(1,rotation);
 Point Rot(Point p){for(int i=0;i<rotation;i++)p=new(9-p.Y,p.X);return p;}
 Check(sea.Walls.Count(p=>!sea.SeaClearWater.Contains(p))==3,"Exactly three reefs remain");
 Check(!sea.Walkable(Rot(new(4,3))) && !sea.BlocksShot(Rot(new(4,3))),"Water blocks feet but not shots");
 Check(sea.BlocksShot(Rot(new(4,1))),"Reef still blocks shots");
 sea.Armies.Add(new(){Owner=0,Side=0});sea.Armies.Add(new(){Owner=1,Side=2});
 var shooter=new BattleUnit{Id=1,Owner=0,Position=Rot(new(3,3))};
 var target=new BattleUnit{Id=2,Owner=1,Position=Rot(new(6,3))};sea.Units.Add(shooter);sea.Units.Add(target);
 Check(BattleAi.FirstTarget(sea,shooter,(BattleFacing)((1+rotation)%4))==target,"Rifle sees across ships");
 var orders=sea.Units.ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u==shooter?(BattleFacing)((1+rotation)%4):BattleFacing.North,u==shooter?BattleWeapon.Rifle:BattleWeapon.None));
 BattleTurnResolver.Advance(sea,orders);
 Check(sea.Attacks.Any(a=>a.Attacker==1 && a.Target==2),"Actual shot reaches other deck");
}
Console.WriteLine("PASS: three rotated reefs and rifle sight across unwalkable water.");

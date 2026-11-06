using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}

for(int rotation=0;rotation<4;rotation++){
 var sea=new BattleState(17){BoardSize=10};sea.BuildSeaLayout(1,rotation);
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

foreach(int[] approaches in new[]{new[]{0,2},new[]{1,2},new[]{0,1,3},new[]{0,1,2,3}})
{
    var sea=new BattleState(17){BoardSize=BattleState.Size,Kind=BattleKind.Edge};
    sea.BuildTerrain(BattleTerrain.Sea);
    foreach(int side in approaches)sea.AddArmy(side,side,Array.Empty<Robot>(),preferredSide:side);
    sea.ChooseLayout();
    sea.Deploy();
    Check(sea.Armies.Select(a=>a.Side).SequenceEqual(approaches),"Deployment retains route bearings");
    for(int side=0;side<4;side++)
        Check(sea.Region(side).All(p=>sea.Walkable(p)==approaches.Contains(side)),"Only participating directions have ships");
    var reached=new HashSet<Point>{sea.Flag};
    var queue=new Queue<Point>();queue.Enqueue(sea.Flag);
    while(queue.TryDequeue(out var point))
        foreach(var step in new[]{new Point(0,-1),new Point(1,0),new Point(0,1),new Point(-1,0)})
            if(sea.Walkable(point+step) && reached.Add(point+step))queue.Enqueue(point+step);
    Check(sea.Armies.All(a=>sea.Region(a.Side).All(reached.Contains)),"All ships connect through the boarding lanes");
    Check(sea.Walls.Count(p=>!sea.SeaClearWater.Contains(p))==3,"The 13-cell arena has three reefs");
    if(approaches.SequenceEqual(new[]{1,2}))Check(sea.SeaLogs.Contains(new(9,9)),"Adjacent ships have a side boarding plank");
}
Console.WriteLine("PASS: 13x13 naval boards deploy only the participating approaches and keep them connected.");

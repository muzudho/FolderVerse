namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public enum FortificationKind { Wall,Moat,Barrel,Gate }
public sealed record BattleFortification(Point Position,FortificationKind Kind,int Owner,BattleFacing Facing,int Health=2);
public sealed record BattleGateHit(int Turn,Point Position,Point From);
public sealed partial class BattleState
{
    public List<BattleFortification> Fortifications {get;}=new();
    public List<BattleGateHit> GateHits {get;}=new();
    public bool FortificationsBuilt;
    public int InitialDefenseCount;
    private void ResetFortifications(){Fortifications.Clear();GateHits.Clear();FortificationsBuilt=false;InitialDefenseCount=0;}
    public BattleFortification GateAt(Point p)=>Fortifications.FirstOrDefault(f=>f.Position==p && f.Kind==FortificationKind.Gate && f.Health>0);
    public bool WalkableFor(Point p,int owner)=>Walkable(p) && (GateAt(p) is not {} gate || Allied(owner,gate.Owner));
    public bool MoatAt(Point p)=>Fortifications.Any(f=>f.Position==p && f.Kind==FortificationKind.Moat);
    public void BuildFortifications(int count)
    {
        if(FortificationsBuilt || Kind!=BattleKind.Node || SplitBoard || Defender<0)return;
        FortificationsBuilt=true;InitialDefenseCount=Math.Clamp(count,0,12);
        var steps=new[]{new Point(0,-1),new Point(1,0),new Point(0,1),new Point(-1,0)};
        var protectedCells=Units.Where(u=>u.Active).Select(u=>u.Position).ToHashSet();protectedCells.Add(Flag);
        foreach(var army in Armies)foreach(var p in Region(army.Side))
            if(army.Side==4 || p.X==0 || p.Y==0 || p.X==BoardSize-1 || p.Y==BoardSize-1)protectedCells.Add(p);
        bool Connected(){
            var seen=new HashSet<Point>{Flag};var queue=new Queue<Point>();queue.Enqueue(Flag);
            while(queue.TryDequeue(out var p))foreach(var step in steps){var next=p+step;if(Walkable(next) && seen.Add(next))queue.Enqueue(next);}
            return seen.Count==Enumerable.Range(0,BoardSize).Sum(y=>Enumerable.Range(0,BoardSize).Count(x=>Walkable(new(x,y))));
        }
        bool SafeSolid(Point p){
            Walls.Add(p);bool safe=Connected();
            // Avoid making a new one-cell dead end beside the obstacle.
            safe &= steps.Select(s=>p+s).Where(Walkable).All(n=>steps.Count(s=>Walkable(n+s))>=2);
            Walls.Remove(p);return safe;
        }
        var candidates=(from y in Enumerable.Range(1,BoardSize-2) from x in Enumerable.Range(1,BoardSize-2) let p=new Point(x,y)
            where Walkable(p) && !protectedCells.Contains(p) && !SeaLogs.Contains(p)
            orderby Math.Abs(BattleAi.Distance(p,Flag)-4),y,x select p).ToList();
        for(int i=0;i<InitialDefenseCount;i++){
            var solid=candidates.Where(p=>GateAt(p)==null && SafeSolid(p)).ToArray();
            Point? chosen=solid.Length>0?solid[(i*7)%solid.Length]:candidates.Select(p=>(Point?)p).FirstOrDefault();
            if(chosen==null){
                // Gates can share the defending deployment area because its allies can pass them.
                // This also fits twelve defenses on a fully boarded ship without blocking boarding lanes.
                chosen=Region(4).Where(p=>p!=Flag && Walkable(p) && !Fortifications.Any(f=>f.Position==p) &&
                    !Units.Any(u=>u.Active && u.Position==p && !Allied(u.Owner,Defender)))
                    .OrderByDescending(p=>BattleAi.Distance(p,Flag)).Select(p=>(Point?)p).FirstOrDefault();
            }
            if(chosen==null)break;
            var p=chosen.Value;bool gate=solid.Length==0 || i%4==3;
            var kind=gate?FortificationKind.Gate:Terrain==BattleTerrain.Sea?FortificationKind.Barrel:i%2==0?FortificationKind.Wall:FortificationKind.Moat;
            var facing=BattleAi.Face(p-Flag);
            Fortifications.Add(new(p,kind,Defender,facing));candidates.Remove(p);
            if(!gate){Walls.Add(p);if(kind==FortificationKind.Wall && i%3!=0){
                // Fire outwards through a wall only when the shooter is immediately behind it.
                WallShotDirections[p]=facing switch{BattleFacing.East=>1,BattleFacing.West=>2,BattleFacing.South=>3,_=>6};
            }}
        }
    }
    public void InheritFortifications(BattleState previous)
    {
        FortificationsBuilt=previous.FortificationsBuilt;InitialDefenseCount=previous.InitialDefenseCount;
        Fortifications.Clear();Fortifications.AddRange(previous.Fortifications);
        foreach(var f in Fortifications.Where(f=>f.Kind!=FortificationKind.Gate)){
            Walls.Add(f.Position);if(previous.WallShotDirections.TryGetValue(f.Position,out int direction))WallShotDirections[f.Position]=direction;
        }
    }
}

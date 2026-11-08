namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class BattleState
{
    public const int ArmiesPerEdge=3;
    public bool CompactFronts=>BoardSize==Size && Armies.Where(a=>a.Side<4).GroupBy(a=>a.Side).Any(g=>g.Count()>1);
    public int FieldLimit(BattleArmy army)=>CompactFronts?Math.Min(3,army.Limit):army.Limit;
    public bool CanAddArmy(int owner)=>!Armies.Any(a=>a.Owner==owner) && (BoardSize==Size && Kind==BattleKind.Node && owner==Defender
        ?Armies.All(a=>a.Side!=4):EntrySides.Any(side=>Armies.Count(a=>a.Side==side)<(BoardSize==Size?ArmiesPerEdge:1)));
    private int FreeEntrySide(int preferred)
    {
        if(BoardSize!=Size)return EntrySides.First(side=>Armies.All(a=>a.Side!=side));
        if(preferred>=0)return new[]{0,1,3,2}.Select(offset=>(preferred+offset)%4).Where(EntrySides.Contains).First(side=>Armies.Count(a=>a.Side==side)<ArmiesPerEdge);
        return new[]{0,2,1,3}.Where(EntrySides.Contains).OrderBy(side=>Armies.Count(a=>a.Side==side)).First(side=>Armies.Count(a=>a.Side==side)<ArmiesPerEdge);
    }
    private int FreeEntrySlot(int side)=>new[]{1,0,2}.First(slot=>Armies.All(a=>a.Side!=side || a.EntrySlot!=slot));
    private static Point FrontPoint(Point p,int side)
    {
        for(int i=0;i<side;i++)p=new(Size-1-p.Y,p.X);
        return p;
    }
    public IEnumerable<Point> ArmyRegion(BattleArmy army)
    {
        if(!CompactFronts || army.Side==4)return Region(army.Side);
        // Each attacking army starts at distances 6, 7, 7 from the flag.
        Point[] cells=army.EntrySlot switch{
            0=>new[]{new Point(2,4),new Point(2,3),new Point(1,4),new Point(2,2),new Point(2,1),new Point(2,0)},
            2=>new[]{new Point(10,4),new Point(10,3),new Point(11,4),new Point(10,2),new Point(10,1),new Point(10,0)},
            _=>new[]{new Point(6,0),new Point(5,0),new Point(7,0)}};
        return cells.Select(p=>FrontPoint(p,army.Side));
    }
    public IEnumerable<Point> ArmyEntrance(BattleArmy army)=>army.Side==4 && BoardSize==Size?new[]{Flag}:
        CompactFronts?new[]{FrontPoint(new(army.EntrySlot switch{0=>3,2=>9,_=>1},0),army.Side)}:
        ArmyRegion(army).Where(p=>army.Side switch{0=>p.Y==0,1=>p.X==BoardSize-1,2=>p.Y==BoardSize-1,_=>p.X==0});
    public IEnumerable<Point> ApproachPath(BattleArmy army)
    {
        if(army.Side==4 || !CompactFronts)yield break;
        var p=army.EntrySlot switch{0=>new Point(2,4),2=>new Point(10,4),_=>new Point(6,0)};
        yield return FrontPoint(p,army.Side);
        while(p.Y!=6){p.Y++;yield return FrontPoint(p,army.Side);}
        while(p.X!=6){p.X+=Math.Sign(6-p.X);yield return FrontPoint(p,army.Side);}
    }
    public IEnumerable<Point> RetreatPath(BattleArmy army)
    {
        if(army.Side==4 || !CompactFronts)yield break;
        var p=army.EntrySlot switch{0=>new Point(2,4),2=>new Point(10,4),_=>new Point(6,0)};
        int exit=army.EntrySlot switch{0=>3,2=>9,_=>1};
        yield return FrontPoint(p,army.Side);
        while(p.Y!=0){p.Y--;yield return FrontPoint(p,army.Side);}
        while(p.X!=exit){p.X+=Math.Sign(exit-p.X);yield return FrontPoint(p,army.Side);}
    }
    private void PrepareCompactMountain()
    {
        if(!CompactFronts || Terrain!=BattleTerrain.Mountain)return;
        // Move the four old offset passes onto the protected central approaches.
        // Every ridge retains a single one-cell pass instead of accumulating extra gaps.
        for(int side=0;side<4;side++)for(int offset=-3;offset<=3;offset++)Walls.Add(FrontPoint(new(6+offset,3),side));
    }
    private void OpenCompactApproaches()
    {
        if(!CompactFronts)return;
        foreach(var army in Armies.Where(a=>a.Side<4)){
            foreach(var p in ArmyRegion(army).Concat(ApproachPath(army)).Concat(RetreatPath(army))){
                // Existing defenses survive reinforcement; new layouts protect these routes before fortification.
                if(Fortifications.Any(f=>f.Position==p))continue;
                if(Terrain==BattleTerrain.Sea)OpenSeaTile(p,bridge:!ArmyRegion(army).Contains(p) && BattleAi.Distance(p,Flag)>2);
                else{Walls.Remove(p);WallShotDirections.Remove(p);}
            }
        }
    }
    private void DecorateCompactTerrain()
    {
        if(!CompactFronts || Terrain==BattleTerrain.Sea)return;
        var protectedCells=Armies.SelectMany(a=>ArmyRegion(a).Concat(ApproachPath(a)).Concat(RetreatPath(a))).Append(Flag).ToHashSet();
        bool Connected(){
            var seen=new HashSet<Point>{Flag};var queue=new Queue<Point>();queue.Enqueue(Flag);
            while(queue.TryDequeue(out var p))foreach(BattleFacing face in Enum.GetValues<BattleFacing>()){
                var next=p+Forward(face);if(Walkable(next) && seen.Add(next))queue.Enqueue(next);
            }
            return seen.Count==Enumerable.Range(0,BoardSize).Sum(y=>Enumerable.Range(0,BoardSize).Count(x=>Walkable(new(x,y))));
        }
        var candidates=(from y in Enumerable.Range(1,BoardSize-2) from x in Enumerable.Range(1,BoardSize-2)
            let p=new Point(x,y) where Walkable(p) && !protectedCells.Contains(p) select p).ToList();
        int remaining=Terrain==BattleTerrain.Mountain?5:Terrain==BattleTerrain.Fort?3:2;
        while(remaining>0 && candidates.Count>0){
            int index=Random.Next(candidates.Count);var p=candidates[index];candidates.RemoveAt(index);Walls.Add(p);
            if(!Connected()){Walls.Remove(p);continue;}
            remaining--;
        }
    }
    public bool ReachedHome(BattleUnit unit)=>!Contains(unit.Position) || unit.Retreating && BoardSize==Size && Army(unit.Owner).Side==4 && unit.Position==Flag;
    public bool HomeExit(Point outside,BattleArmy army)=>HomeExit(outside,army.Side,BoardSize) &&
        (!CompactFronts || ArmyEntrance(army).Contains(outside+Forward(Inward(army.Side))));
}

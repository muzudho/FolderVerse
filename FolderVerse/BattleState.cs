namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public enum BattleFacing { North,East,South,West }
public enum BattleWeapon { None,Dagger,Rifle }
public enum BattleRole { Soldier,Captain,Queen }
public enum BattlePhase { Choose,Place,Rollback,Attack,Receive,Consume,Fall,Flag,Reinforce,Result,Retreat,Persuade,Converted }
public enum BattleTerrain { Field,Sea,Fort }
public sealed record BattleAction(Point Step,BattleFacing Facing,BattleWeapon Weapon=BattleWeapon.None);
public sealed record BattleEvent(int Turn,BattlePhase Phase,long Unit,string Detail);
public sealed record BattleUnitView(long Id,int Owner,BattleRole Role,Point Position,BattleFacing Facing,int Daggers,int Rifles,int Shields,bool Fallen,bool Retreated,Robot Robot=null);
public sealed record BattleFrame(int Turn,BattlePhase Phase,BattleUnitView[] Units,bool FlagStanding,int LastFlagOwner,int FlagDefenseTurns=0,int FlagCaptureTurns=0);
public sealed record BattlePersuasion(int Turn,long Queen,long Target,int Owner,int PreviousOwner,Point From,Point To,bool Success);
public sealed record BattleAttack(int Turn,long Attacker,long? Target,Point From,Point To,BattleWeapon Weapon);
public sealed class BattleRules
{
    public int NoWillTurns {get;init;}=10;
    public int FlagCaptureTurns {get;init;}=10;
    public int FlagDefenseTurns {get;init;}=50;
    public int SegmentTurns {get;init;}=100;
    public int PersuasionRadius {get;init;}=4;
    public int PersuasionPercent {get;init;}=5;
}
public sealed class BattleUnit
{
    public long Id;
    public int Owner;
    public BattleRole Role;
    public Point Position;
    public BattleFacing Facing;
    public int Daggers=3,Rifles=4,Shields=3;
    public bool Fallen,Retreated,Retreating;
    public bool ResumeAfterRetreatOverlap;
    public BattleLesson WitnessedLessons;
    public bool Active=>!Fallen && !Retreated;
    public Robot Robot;
}
public sealed class BattleArmy
{
    public int Owner,HomeNode,Side,Limit=12;
    public int NoWillCount;
    public bool Withdrawing;
    public bool InvadingQueen;
    public bool Wanderer;
    public WandererPolicy WandererPolicy;
    public Queue<BattleUnit> Waiting {get;}=new();
    public Dictionary<int,List<Point[]>> Patterns {get;}=new();
}
public sealed partial class BattleState
{
    public const int Size=13;
    public int BoardSize=Size;
    public int Id,SourceNode,TargetNode,Defender=-1,Turn,NoWillCount;
    public BattleKind Kind;
    public bool SplitBoard;
    public BattleTerrain Terrain;
    public BattleRules Rules {get;}
    public SeedRandom Random {get;private set;}
    public List<BattleArmy> Armies {get;}=new();
    public List<BattleUnit> Units {get;}=new();
    public HashSet<Point> Walls {get;}=new();
    // Horizontal: 0/1 east/2 west; vertical: 0/3 south/6 north.
    public Dictionary<Point,int> WallShotDirections {get;}=new();
    public List<BattleEvent> Events {get;}=new();
    public List<BattleFrame> Frames {get;}=new();
    public List<BattleAttack> Attacks {get;}=new();
    public List<BattlePersuasion> Persuasions {get;}=new();
    public int GlobalTurn;
    public BattleLesson[] Lessons=new BattleLesson[WorldSetup.CastCount];
    public HashSet<long> PersuadedUnits {get;}=new();
    public Point Flag=new(6,6);
    public bool FlagStanding=true,Finished;
    public int LastFlagOwner=-1,Winner=-1;
    public int FlagDefenseTurns,FlagCaptureTurns;
    public Dictionary<int,int> NewspaperRanks {get;}=new();
    public Func<int,int,bool> Allied {get;set;}=(a,b)=>a==b;
    public BattleState(int seed,BattleRules rules=null)
    {Rules=rules??new();if(Rules.FlagCaptureTurns<1 || Rules.FlagDefenseTurns<1 || Rules.NoWillTurns<1 || Rules.SegmentTurns<1 || Rules.PersuasionRadius<0 || Rules.PersuasionPercent<0 || Rules.PersuasionPercent>100)throw new ArgumentException("Invalid battle rules.");Random=new(seed);}
    public static Point Forward(BattleFacing facing)=>facing switch{BattleFacing.North=>new(0,-1),BattleFacing.East=>new(1,0),BattleFacing.South=>new(0,1),_=>new(-1,0)};
    public static bool Inside(Point p)=>p.X>=0 && p.X<Size && p.Y>=0 && p.Y<Size;
    public bool Contains(Point p)=>p.X>=0 && p.X<BoardSize && p.Y>=0 && p.Y<BoardSize;
    public bool BlocksShot(Point p)=>!Contains(p) || Walls.Contains(p) && !(Terrain==BattleTerrain.Sea && SeaClearWater.Contains(p));
    public bool WallPassesShot(Point p,BattleFacing facing)
    {
        if(!Walls.Contains(p))return false;
        int code=WallShotDirections.GetValueOrDefault(p);
        if(code<0 || code>8)return false;
        return facing switch {BattleFacing.East=>code%3==1,BattleFacing.West=>code%3==2,BattleFacing.South=>code/3==1,_=>code/3==2};
    }
    public bool BlocksShot(Point p,BattleFacing facing,int distance)
        =>BlocksShot(p) && !(Contains(p) && distance==1 && WallPassesShot(p,facing));
    public bool Walkable(Point p)=>Contains(p) && !Walls.Contains(p);
    public static bool HomeExit(Point p,int side,int size=Size)=>side switch{0=>p.Y<0,1=>p.X>=size,2=>p.Y>=size,_=>p.X<0};
    public BattleArmy Army(int owner)=>Armies.Single(a=>a.Owner==owner);
    public void AddArmy(int owner,int home,IEnumerable<Robot> robots,bool queen=false)
    {
        if(owner<0 || owner>=20 || home<0)throw new ArgumentOutOfRangeException();
        if(Armies.Count>=EntryCapacity || Armies.Any(a=>a.Owner==owner))throw new InvalidOperationException("Battle supports distinct armies, at most five.");
        int side=BoardSize==Size && Kind==BattleKind.Node && owner==Defender?4:Armies.Count==1 && EntrySides.Contains(2) && !Armies.Any(a=>a.Side==2)?2:EntrySides.First(i=>!Armies.Any(a=>a.Side==i));
        var army=new BattleArmy{Owner=owner,HomeNode=home,Side=side,InvadingQueen=queen};
        Armies.Add(army);int index=0;
        foreach(var robot in robots.OrderBy(r=>r.Id))
        {
            if(!robot.CanFight || robot.Owner!=owner)throw new ArgumentException("Only complete owned robots may enter battle.");
            army.Waiting.Enqueue(new(){Id=robot.Id,Owner=owner,Robot=robot,Daggers=robot.CombatParts(owner),Shields=robot.CombatParts(owner),Rifles=robot.NativeParts(owner)==3?4:robot.CombatParts(owner),Role=index++==1?BattleRole.Captain:BattleRole.Soldier,Facing=Inward(army.Side)});
        }
        if(queen)army.Waiting.Enqueue(new(){Id=-(owner+1),Owner=owner,Role=BattleRole.Queen,Facing=Inward(army.Side),Daggers=0,Rifles=0});
    }
    public static BattleFacing Inward(int side)=>(BattleFacing)((side+2)%4);
    public IEnumerable<Point> Region(int side)
    {
        if(BoardSize==10)return from y in Enumerable.Range(0,10) from x in Enumerable.Range(0,10)
            let distance=new[]{y,9-x,9-y,x} where distance[side]<distance.Where((_,i)=>i!=side).Min()
            orderby distance[side],Math.Abs((side%2==0?x:y)-4.5),y,x select new Point(x,y);
        if(side==4)return from y in Enumerable.Range(4,5) from x in Enumerable.Range(4,5)
            where Math.Abs(x-6)+Math.Abs(y-6)<=2 orderby Math.Abs(x-6)+Math.Abs(y-6),y,x select new Point(x,y);
        var north=Enumerable.Range(3,7).Select(x=>new Point(x,0)).Concat(Enumerable.Range(3,6).Select(x=>new Point(x,1)));
        return north.Select(p=>side switch{0=>p,1=>new Point(12-p.Y,p.X),2=>new Point(12-p.X,12-p.Y),_=>new Point(p.Y,12-p.X)});
    }
    public void Deploy(int initial=12)
    {
        int edge=0;
        foreach(var army in Armies){
            army.Side=BoardSize==Size && Kind==BattleKind.Node && army.Owner==Defender?4:
                EntrySides.SequenceEqual(new[]{0,1,2,3}) && Armies.Count==2 && edge==1?2:EntrySides[edge];
            if(army.Side!=4)edge++;
            foreach(var unit in army.Waiting)unit.Facing=Inward(army.Side==4?0:army.Side);
            int count=Math.Min(initial,Math.Min(army.Limit,army.Waiting.Count(u=>u.Role!=BattleRole.Queen)));
            var region=Region(army.Side).ToHashSet();var candidates=army.Patterns.GetValueOrDefault(count,new List<Point[]>());
            bool Available(Point p)=>Walkable(p) && !Units.Any(u=>u.Active && u.Position==p) && !(army.Side==4 && p==Flag);
            var pattern=candidates.Where(p=>p.Length==count && p.Distinct().Count()==p.Length && p.All(region.Contains)).OrderByDescending(p=>p.Count(Available)).FirstOrDefault();
            var cells=(pattern??DefaultPattern(army.Side,count)).Where(Available).Take(count).ToList();
            foreach(var p in Region(army.Side).Where(Available))if(cells.Count<count && !cells.Contains(p))cells.Add(p);
            foreach(var p in cells){if(army.Waiting.Count==0 || army.Waiting.Peek().Role==BattleRole.Queen)break;var unit=army.Waiting.Dequeue();unit.Position=p;Units.Add(unit);}
            if(army.Waiting.TryPeek(out var queen) && queen.Role==BattleRole.Queen){
                var place=army.Side==4?(Point?)Flag:Region(army.Side).Where(Available).Select(p=>(Point?)p).FirstOrDefault();
                if(place.HasValue && Walkable(place.Value) && !Units.Any(u=>u.Active && u.Position==place.Value)){army.Waiting.Dequeue();queen.Position=place.Value;Units.Add(queen);}
            }
        }
        if(Terrain==BattleTerrain.Sea && BoardSize==10)PlaceSeaFlag();
        Check();
    }
    public Point[] DefaultPattern(int side,int count)=>Region(side).Where(p=>side!=4 || p!=Flag).Take(count).ToArray();
    public void Capture(BattlePhase phase)=>Frames.Add(new(Turn,phase,phase==BattlePhase.Result && Finished && Frames.LastOrDefault()?.Phase==BattlePhase.Retreat?Frames[^1].Units:Units.Select(u=>new BattleUnitView(u.Id,u.Owner,u.Role,u.Position,u.Facing,u.Daggers,u.Rifles,u.Shields,u.Fallen,u.Retreated,u.Robot)).ToArray(),FlagStanding,LastFlagOwner,FlagDefenseTurns,FlagCaptureTurns));
    public void InheritHistory(IEnumerable<BattleState> previous)
    {
        var states=previous.OrderBy(b=>b.Id).ToArray();if(states.Length==0)return;
        Turn=states.Max(b=>b.Turn);NoWillCount=states.Min(b=>b.NoWillCount);Random=states[0].Random.Clone();
        GlobalTurn=states.Max(b=>b.GlobalTurn);PersuadedUnits.UnionWith(states.Where(b=>b.GlobalTurn==GlobalTurn).SelectMany(b=>b.PersuadedUnits));
        foreach(var army in Armies){var prior=states.SelectMany(b=>b.Armies).Where(a=>a.Owner==army.Owner).ToArray();if(prior.Length>0){army.NoWillCount=prior.Min(a=>a.NoWillCount);army.Withdrawing=prior.All(a=>a.Withdrawing);}}
        var flag=states.Where(b=>b.Kind==BattleKind.Node && !b.SplitBoard).OrderByDescending(b=>b.Turn).FirstOrDefault();
        if(flag!=null){FlagStanding=flag.FlagStanding;LastFlagOwner=flag.LastFlagOwner;FlagDefenseTurns=flag.FlagDefenseTurns;FlagCaptureTurns=flag.FlagCaptureTurns;}
    }
    public bool Reinforce()
    {
        bool entered=false;
        foreach(var army in Armies.OrderBy(a=>a.Owner))
        {
            if(army.Withdrawing || army.Waiting.Count==0 || Units.Count(u=>u.Active && u.Owner==army.Owner && u.Role!=BattleRole.Queen)>=army.Limit && army.Waiting.Peek().Role!=BattleRole.Queen)continue;
            var cells=Region(army.Side).Where(p=>army.Side switch{0=>p.Y==0,1=>p.X==BoardSize-1,2=>p.Y==BoardSize-1,3=>p.X==0,_=>true});
            var free=cells.Where(Walkable).Where(p=>!Units.Any(u=>u.Active && u.Position==p)).Select(p=>(Point?)p).FirstOrDefault();
            if(free==null)continue;
            var unit=army.Waiting.Dequeue();unit.Position=free.Value;unit.Facing=Inward(army.Side);Units.Add(unit);entered=true;
            Events.Add(new(Turn,BattlePhase.Reinforce,unit.Id,"増援"));
        }
        return entered;
    }
    public void BuildTerrain(BattleTerrain terrain)
    {
        Terrain=terrain;Walls.Clear();WallShotDirections.Clear();
        if(terrain==BattleTerrain.Fort)
        {
            if(BoardSize==10){foreach(int x in new[]{2,3,6,7})Walls.Add(new(x,3));Flag=new(5,2);}
            else {Flag=new(6,6);for(int y=2;y<=10;y++)for(int x=2;x<=10;x++)if(Math.Abs(x-6)+Math.Abs(y-6)==3 && x!=6 && y!=6)Walls.Add(new(x,y));}
        }
        if(terrain==BattleTerrain.Fort)foreach(var p in Walls)
            WallShotDirections[p]=BoardSize==10?3:(p.X>Flag.X?1:p.X<Flag.X?2:0)+(p.Y>Flag.Y?3:p.Y<Flag.Y?6:0);
        if(terrain==BattleTerrain.Sea)BuildSeaLayout(0);
    }
    public void Check()
    {
        if(BoardSize is not (10 or Size) || Armies.Count>5 || Armies.Select(a=>a.Owner).Distinct().Count()!=Armies.Count || Armies.Select(a=>a.Side).Distinct().Count()!=Armies.Count || Armies.Any(a=>a.Owner<0 || a.Owner>=20 || a.Side<0 || a.Side>(BoardSize==Size && (SplitBoard || Kind==BattleKind.Node && a.Owner==Defender)?4:3) || a.Limit<1 || a.Limit>12))throw new InvalidOperationException("Invalid battle armies.");
        var active=Units.Where(u=>u.Active).ToArray();
        var solid=active.Where(u=>!u.Retreating).ToArray();
        if(active.Any(u=>!Walkable(u.Position)) || solid.Select(u=>u.Position).Distinct().Count()!=solid.Length)throw new InvalidOperationException("Battle occupancy is invalid.");
        var all=Units.Concat(Armies.SelectMany(a=>a.Waiting)).ToArray();
        if(all.Select(u=>u.Id).Distinct().Count()!=all.Length || all.Any(u=>u.Daggers<0 || u.Rifles<0 || u.Shields<0))throw new InvalidOperationException("Invalid battle units.");
    }
}




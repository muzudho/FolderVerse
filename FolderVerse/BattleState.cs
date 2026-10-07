namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public enum BattleFacing { North,East,South,West }
public enum BattleWeapon { None,Dagger,Rifle }
public enum BattleRole { Soldier,Captain,Queen }
public enum BattlePhase { Choose,Place,Rollback,Attack,Receive,Consume,Fall,Flag,Reinforce,Result }
public enum BattleTerrain { Field,Sea,Fort }
public sealed record BattleAction(Point Step,BattleFacing Facing,BattleWeapon Weapon=BattleWeapon.None);
public sealed record BattleEvent(int Turn,BattlePhase Phase,long Unit,string Detail);
public sealed record BattleUnitView(long Id,int Owner,BattleRole Role,Point Position,BattleFacing Facing,int Daggers,int Rifles,int Shields,bool Fallen,bool Retreated);
public sealed record BattleFrame(int Turn,BattlePhase Phase,BattleUnitView[] Units,bool FlagStanding,int LastFlagOwner);
public sealed record BattleAttack(int Turn,long Attacker,long? Target,Point From,Point To,BattleWeapon Weapon);
public sealed class BattleRules
{
    public int NoWillTurns {get;init;}=10;
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
    public int Daggers=3,Rifles=3,Shields=4;
    public bool Fallen,Retreated,Retreating;
    public bool Active=>!Fallen && !Retreated;
    public Robot Robot;
}
public sealed class BattleArmy
{
    public int Owner,HomeNode,Side,Limit=12;
    public bool InvadingQueen;
    public Queue<BattleUnit> Waiting {get;}=new();
    public Dictionary<int,List<Point[]>> Patterns {get;}=new();
}
public sealed class BattleState
{
    public int Id,SourceNode,TargetNode,Defender=-1,Turn,NoWillCount;
    public BattleKind Kind;
    public bool SplitBoard;
    public BattleTerrain Terrain;
    public BattleRules Rules {get;}
    public SeedRandom Random {get;private set;}
    public List<BattleArmy> Armies {get;}=new();
    public List<BattleUnit> Units {get;}=new();
    public HashSet<Point> Walls {get;}=new();
    public List<BattleEvent> Events {get;}=new();
    public List<BattleFrame> Frames {get;}=new();
    public List<BattleAttack> Attacks {get;}=new();
    public Point Flag=new(5,5);
    public bool FlagStanding=true,Finished;
    public int LastFlagOwner=-1,Winner=-1;
    public Func<int,int,bool> Allied {get;set;}=(a,b)=>a==b;
    public BattleState(int seed,BattleRules rules=null)
    {Rules=rules??new();if(Rules.NoWillTurns<1 || Rules.SegmentTurns<1 || Rules.PersuasionRadius<0 || Rules.PersuasionPercent<0 || Rules.PersuasionPercent>100)throw new ArgumentException("Invalid battle rules.");Random=new(seed);}
    public static Point Forward(BattleFacing facing)=>facing switch{BattleFacing.North=>new(0,-1),BattleFacing.East=>new(1,0),BattleFacing.South=>new(0,1),_=>new(-1,0)};
    public static bool Inside(Point p)=>p.X>=0 && p.X<10 && p.Y>=0 && p.Y<10;
    public bool Walkable(Point p)=>Inside(p) && !Walls.Contains(p);
    public static bool HomeExit(Point p,int side)=>side switch{0=>p.Y<0,1=>p.X>=10,2=>p.Y>=10,_=>p.X<0};
    public BattleArmy Army(int owner)=>Armies.Single(a=>a.Owner==owner);
    public void AddArmy(int owner,int home,IEnumerable<Robot> robots,bool queen=false)
    {
        if(owner<0 || owner>=20 || home<0)throw new ArgumentOutOfRangeException();
        if(Armies.Count>=4 || Armies.Any(a=>a.Owner==owner))throw new InvalidOperationException("Battle supports distinct armies, at most four.");
        int side=Armies.Count==1?2:Enumerable.Range(0,4).First(i=>!Armies.Any(a=>a.Side==i));
        var army=new BattleArmy{Owner=owner,HomeNode=home,Side=side,InvadingQueen=queen};
        Armies.Add(army);int index=0;
        foreach(var robot in robots.OrderBy(r=>r.Id))
        {
            if(!robot.CanFight || robot.Owner!=owner)throw new ArgumentException("Only complete owned robots may enter battle.");
            army.Waiting.Enqueue(new(){Id=robot.Id,Owner=owner,Robot=robot,Role=index++==1?BattleRole.Captain:BattleRole.Soldier,Facing=Inward(army.Side)});
        }
        if(queen)army.Waiting.Enqueue(new(){Id=-(owner+1),Owner=owner,Role=BattleRole.Queen,Facing=Inward(army.Side),Daggers=0,Rifles=0});
    }
    public static BattleFacing Inward(int side)=>(BattleFacing)((side+2)%4);
    // The nearest board edge defines the country's triangular initial deployment region.
    public IEnumerable<Point> Region(int side)=>from y in Enumerable.Range(0,10) from x in Enumerable.Range(0,10)
        let distance=new[]{y,9-x,9-y,x}
        where distance[side]<distance.Where((_,i)=>i!=side).Min()
        orderby distance[side],Math.Abs((side%2==0?x:y)-4.5),y,x select new Point(x,y);
    public void Deploy(int initial=12)
    {
        for(int i=0;i<Armies.Count;i++)Armies[i].Side=Armies.Count==2 && i==1?2:i;
        foreach(var army in Armies)
        {
            foreach(var unit in army.Waiting)unit.Facing=Inward(army.Side);
            int count=Math.Min(initial,Math.Min(army.Limit,army.Waiting.Count(u=>u.Role!=BattleRole.Queen)));
            var region=Region(army.Side).ToHashSet();
            var candidates=army.Patterns.GetValueOrDefault(count,new List<Point[]>());
            var pattern=candidates.Where(p=>p.Length==count && p.Distinct().Count()==p.Length && p.All(region.Contains)).OrderByDescending(p=>p.Count(Walkable)).FirstOrDefault();
            var cells=(pattern??DefaultPattern(army.Side,count)).Where(Walkable).Where(p=>!Units.Any(u=>u.Active && u.Position==p)).ToList();
            // If no registered pattern fits, place each remaining unit on the available region.
            foreach(var p in Region(army.Side).Where(Walkable))if(cells.Count<count && !cells.Contains(p) && !Units.Any(u=>u.Active && u.Position==p))cells.Add(p);
            foreach(var p in cells)
            {
                if(army.Waiting.Count==0 || army.Waiting.Peek().Role==BattleRole.Queen)break;
                var unit=army.Waiting.Dequeue();unit.Position=p;Units.Add(unit);
            }
        }
        Check();
    }
    public Point[] DefaultPattern(int side,int count)
    {
        var north=new[]{new Point(4,1),new Point(4,0),new Point(5,1),new Point(3,1),new Point(6,1),new Point(2,1),new Point(7,1),new Point(3,2),new Point(4,2),new Point(5,2),new Point(6,2),new Point(5,0)};
        return north.Take(count).Select(p=>side switch{0=>p,1=>new Point(9-p.Y,p.X),2=>new Point(9-p.X,9-p.Y),_=>new Point(p.Y,9-p.X)}).ToArray();
    }
    public void Capture(BattlePhase phase)=>Frames.Add(new(Turn,phase,Units.Select(u=>new BattleUnitView(u.Id,u.Owner,u.Role,u.Position,u.Facing,u.Daggers,u.Rifles,u.Shields,u.Fallen,u.Retreated)).ToArray(),FlagStanding,LastFlagOwner));
    public void InheritHistory(IEnumerable<BattleState> previous)
    {
        var states=previous.OrderBy(b=>b.Id).ToArray();if(states.Length==0)return;
        Turn=states.Max(b=>b.Turn);NoWillCount=states.Min(b=>b.NoWillCount);Random=states[0].Random.Clone();
        var flag=states.Where(b=>b.Kind==BattleKind.Node && !b.SplitBoard).OrderByDescending(b=>b.Turn).FirstOrDefault();
        if(flag!=null){FlagStanding=flag.FlagStanding;LastFlagOwner=flag.LastFlagOwner;}
    }
    public bool Reinforce()
    {
        bool entered=false;
        foreach(var army in Armies.OrderBy(a=>a.Owner))
        {
            if(army.Waiting.Count==0 || Units.Count(u=>u.Active && u.Owner==army.Owner)>=army.Limit)continue;
            var cells=Region(army.Side).Where(p=>army.Side switch{0=>p.Y==0,1=>p.X==9,2=>p.Y==9,_=>p.X==0});
            var free=cells.Where(Walkable).Where(p=>!Units.Any(u=>u.Active && u.Position==p)).Select(p=>(Point?)p).FirstOrDefault();
            if(free==null)continue;
            var unit=army.Waiting.Dequeue();unit.Position=free.Value;unit.Facing=Inward(army.Side);Units.Add(unit);entered=true;
            Events.Add(new(Turn,BattlePhase.Reinforce,unit.Id,"増援"));
        }
        return entered;
    }
    public void BuildTerrain(BattleTerrain terrain)
    {
        Terrain=terrain;Walls.Clear();
        if(terrain==BattleTerrain.Fort)
        {
            foreach(int x in new[]{2,3,6,7})Walls.Add(new(x,3));
            Flag=new(5,2);
        }
        if(terrain==BattleTerrain.Sea)
        {
            for(int y=0;y<10;y++)for(int x=0;x<10;x++)
            {
                bool bridge=x is 4 or 5 || y is 4 or 5;
                bool ship=(y<3 && x is >=2 and <=7)||(y>6 && x is >=2 and <=7)||(x<3 && y is >=2 and <=7)||(x>6 && y is >=2 and <=7);
                if(!bridge && !ship)Walls.Add(new(x,y));
            }
        }
    }
    public void Check()
    {
        if(Armies.Count>4 || Armies.Select(a=>a.Owner).Distinct().Count()!=Armies.Count || Armies.Select(a=>a.Side).Distinct().Count()!=Armies.Count || Armies.Any(a=>a.Owner<0 || a.Owner>=20 || a.Side<0 || a.Side>3 || a.Limit<1 || a.Limit>12))throw new InvalidOperationException("Invalid battle armies.");
        var active=Units.Where(u=>u.Active).ToArray();
        if(active.Any(u=>!Walkable(u.Position)) || active.Select(u=>u.Position).Distinct().Count()!=active.Length)throw new InvalidOperationException("Battle occupancy is invalid.");
        var all=Units.Concat(Armies.SelectMany(a=>a.Waiting)).ToArray();
        if(all.Select(u=>u.Id).Distinct().Count()!=all.Length || all.Any(u=>u.Daggers<0 || u.Rifles<0 || u.Shields<0))throw new InvalidOperationException("Invalid battle units.");
    }
}

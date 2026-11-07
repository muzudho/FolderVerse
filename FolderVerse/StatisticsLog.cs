namespace FolderVerse;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

public sealed class StatisticsLog
{
    private readonly string _directory;
    private readonly long _fileBytes;
    private readonly int _fileCount;
    private readonly string _session=Guid.NewGuid().ToString("N");
    private WorldSetup _world;
    private (int World,int Cast,int Placement) _worldKey;
    private int _lastTurn=-1,_lastEncounter;
    private int[] _previousOwners;
    private HashSet<int> _activeEncounters=new();
    public StatisticsLog(string directory,long fileBytes=4*1024*1024,int fileCount=4)
    {
        if(fileBytes<1 || fileCount<1)throw new ArgumentOutOfRangeException();
        _directory=Path.GetFullPath(directory);_fileBytes=fileBytes;_fileCount=fileCount;
    }
    public bool Write(WorldSetup world)
    {
        var key=(world.WorldSeed,world.CastSeed,world.PlacementSeed);
        bool newGame=!ReferenceEquals(world,_world) || key!=_worldKey || world.Population.Turn<_lastTurn;
        if(!newGame && world.Population.Turn==_lastTurn)return false;
        if(newGame){
            _previousOwners=null;_lastEncounter=0;_activeEncounters.Clear();
        }
        var encounters=world.Campaign.RobotBattles.Encounters;
        var started=encounters.Where(e=>e.Id>_lastEncounter).ToArray();
        var finished=encounters.Where(e=>e.Finished && (e.Id>_lastEncounter || _activeEncounters.Contains(e.Id))).ToArray();
        var ranking=world.Relations.ProvisionalRanking(world).ToDictionary(r=>r.Ruler);
        var countries=Enumerable.Range(0,world.ActiveCount).Select(owner=>{
            var nodes=world.Nodes.All.Where(n=>n.Owner==owner).ToArray();
            var stores=nodes.Select(n=>world.Robots.Nodes[n.Id]).ToArray();
            var stock=stores.SelectMany(s=>s.Robots).ToArray();
            var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs};
            int Count(Func<Robot,RobotParts,bool> predicate)=>stock.Sum(r=>parts.Count(p=>(r.Parts&p)!=0 && predicate(r,p)));
            var lines=nodes.SelectMany(n=>world.Robots.Workshops[n.Id].ProductionLines).ToArray();
            int won=nodes.Count(n=>_previousOwners!=null && _previousOwners[n.Id]!=owner);
            int lost=_previousOwners==null?0:world.Nodes.All.Count(n=>_previousOwners[n.Id]==owner && n.Owner!=owner);
            return new{
                owner,powered=world.Relations.Powered[owner],eliminationOrder=world.Relations.EliminationOrders[owner],superior=world.Relations.Superiors[owner],sectors=ranking[owner].Cells,
                nodes=nodes.Length,functional=nodes.Count(n=>n.Functional),won,lost,
                capacity=stores.Sum(s=>s.TotalCapacity),occupied=stores.Sum(s=>s.Count),returnReservations=stores.Sum(s=>s.ReservedReturns),
                robots=stock.Length,complete=stock.Count(r=>r.Parts==RobotParts.Complete),
                deployable=nodes.Sum(n=>world.Campaign.RobotBattles.AvailableFrom(world,owner,n.Id)),
                damagedParts=Count((r,p)=>r.IsDamaged(p)),cheapParts=Count((r,p)=>r.IsCheap(p)),premiumParts=Count((r,p)=>r.IsPremium(p)),
                disposal=stock.Count(r=>r.Disposal),stationed=stock.Count(r=>r.Stationed),pocket=world.Robots.Retinues[owner].Count,
                intakeReservations=world.Robots.ProductionIntakes.Count(i=>i.Product.Owner==owner),
                manufacturing=lines.Count(l=>l.Manufacturing && !l.Paused && !l.CycleFinished),
                blockedProduction=lines.Count(l=>l.Waiting),resting=lines.Count(l=>l.RestRemaining>0),
                pausedProduction=lines.Count(l=>l.Paused),
                marches=world.Campaign.RobotBattles.LastMarches.Count(m=>m.Owner==owner),
                battlesStarted=started.Count(e=>e.Battles.Any(b=>b.Armies.Any(a=>a.Owner==owner))),
                battlesWon=finished.Count(e=>e.Battles.Any(b=>b.Winner==owner)),
                inBattle=encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting)))
                    .Count(u=>u.Owner==owner && u.Active && u.Robot!=null),
                battlesUnresolved=encounters.Count(e=>!e.Finished && e.Battles.Any(b=>b.Armies.Any(a=>a.Owner==owner)))
            };
        }).ToArray();
        var row=new{schemaVersion=1,session=_session,time=DateTimeOffset.Now,version=typeof(Game1).Assembly.GetName().Version?.ToString(),
            worldSeed=world.WorldSeed,castSeed=world.CastSeed,placementSeed=world.PlacementSeed,turn=world.Population.Turn,
            industryTurn=world.Robots.IndustryTurn,industryResult=world.Industry.Result,exhibition=world.ComputerOnly,countries};
        var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(row)+"\n");
        Directory.CreateDirectory(_directory);
        string PathAt(int index)=>Path.Combine(_directory,$"statistics-{index}.jsonl");
        var path=PathAt(0);
        if(File.Exists(path) && new FileInfo(path).Length>0 && new FileInfo(path).Length+bytes.Length>_fileBytes){
            File.Delete(PathAt(_fileCount-1));
            for(int index=_fileCount-2;index>=0;index--)if(File.Exists(PathAt(index)))File.Move(PathAt(index),PathAt(index+1),true);
        }
        using(var output=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read))output.Write(bytes);
        _world=world;_worldKey=key;_lastTurn=world.Population.Turn;
        _previousOwners=world.Nodes.All.Select(n=>n.Owner).ToArray();
        _lastEncounter=encounters.Select(e=>e.Id).DefaultIfEmpty(0).Max();
        _activeEncounters=encounters.Where(e=>!e.Finished).Select(e=>e.Id).ToHashSet();
        return true;
    }
}

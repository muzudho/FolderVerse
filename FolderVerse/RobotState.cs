namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

[Flags]
public enum RobotParts { Legs=1, Body=2, Head=4, Complete=7 }
public enum RobotRole { Soldier, Captain }
public sealed record RobotWorkshopEdit(int Node,long[] Ids,RobotParts? SplitPart);
public sealed record Robot(long Id,int Owner,RobotParts Parts,RobotRole Role=RobotRole.Soldier,bool Disposal=false)
{
    public bool CanFight=>Parts==RobotParts.Complete && !Disposal;
}
public sealed class RobotStore
{
    public const int Capacity=12;
    private readonly List<Robot> _robots=new();
    public IReadOnlyList<Robot> Robots=>_robots;
    public int Count=>_robots.Count;
    public void Add(Robot robot)
    {
        RobotWorld.Validate(robot.Owner,robot.Parts);
        if(Count>=Capacity || _robots.Any(r=>r.Id==robot.Id))throw new InvalidOperationException("Robot store is full or ID is duplicated.");
        _robots.Add(robot);
    }
    public Robot Remove(long id)
    {
        var robot=_robots.Single(r=>r.Id==id);_robots.Remove(robot);return robot;
    }
    internal void Replace(IEnumerable<Robot> robots)
    {
        var next=robots.ToArray();
        if(next.Length>Capacity || next.Select(r=>r.Id).Distinct().Count()!=next.Length)throw new InvalidOperationException("Invalid robot store.");
        _robots.Clear();_robots.AddRange(next);
    }
}
public sealed partial class RobotWorld
{
    private long _nextId=1;
    public RobotStore[] Nodes {get;private set;}=Array.Empty<RobotStore>();
    public RobotStore[] Retinues {get;private set;}=Array.Empty<RobotStore>();
    public RobotWorkshop[] Workshops {get;private set;}=Array.Empty<RobotWorkshop>();
    public RobotTransport Transport {get;}=new();
    public List<RobotWorkshopEdit> PendingEdits {get;}=new();
    public List<(int Node,string Message)> WorkshopFailures {get;}=new();
    public static void Validate(int owner,RobotParts parts)
    {
        if(owner<0 || owner>=20 || (int)parts<1 || (int)parts>7)throw new ArgumentOutOfRangeException();
    }
    public Robot Create(int owner,RobotParts parts,RobotRole role=RobotRole.Soldier,bool disposal=false)
    {Validate(owner,parts);return new(_nextId++,owner,parts,role,disposal);}
    public void Initialize(int nodeCount,int rulerCount)
    {
        _nextId=1;Nodes=Enumerable.Range(0,nodeCount).Select(_=>new RobotStore()).ToArray();
        Retinues=Enumerable.Range(0,rulerCount).Select(_=>new RobotStore()).ToArray();
        Workshops=Enumerable.Range(0,nodeCount).Select(_=>new RobotWorkshop()).ToArray();Transport.Reset();PendingEdits.Clear();WorkshopFailures.Clear();
    }
    public void QueueAssembly(int node,IEnumerable<long> ids)=>QueueEdit(new(node,ids.ToArray(),null));
    public void QueueSplit(int node,long id,RobotParts part)=>QueueEdit(new(node,new[]{id},part));
    private void QueueEdit(RobotWorkshopEdit edit)
    {
        if(edit.Node<0 || edit.Node>=Nodes.Length || edit.Ids.Length<(edit.SplitPart==null?2:1) || edit.Ids.Distinct().Count()!=edit.Ids.Length)throw new ArgumentException("Invalid workshop order.");
        var robots=edit.Ids.Select(id=>Nodes[edit.Node].Robots.Single(r=>r.Id==id)).ToArray();
        if(robots.Any(r=>r.Owner!=robots[0].Owner) || edit.Ids.Any(id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || Workshops[edit.Node].DisposalTarget==id))throw new InvalidOperationException("Robot is already reserved or belongs to another owner.");
        if(edit.SplitPart is RobotParts part)
        {Validate(robots[0].Owner,part);if((robots[0].Parts&part)!=part || robots[0].Parts==part)throw new InvalidOperationException("Invalid split parts.");}
        else{var parts=(RobotParts)0;foreach(var robot in robots){if((parts&robot.Parts)!=0)throw new InvalidOperationException("Duplicate parts.");parts|=robot.Parts;}}
        PendingEdits.Add(edit);
    }
    public Robot Assemble(int node,IEnumerable<long> ids)
    {
        var store=Nodes[node];var selected=ids.ToArray();
        if(selected.Length<2 || selected.Distinct().Count()!=selected.Length)throw new ArgumentException("Select distinct robots.");
        var robots=selected.Select(id=>store.Robots.Single(r=>r.Id==id)).ToArray();var parts=(RobotParts)0;
        if(selected.Contains(Workshops[node].DisposalTarget??-1))throw new InvalidOperationException("Disposal target is reserved.");
        foreach(var robot in robots)
        {if(robot.Owner!=robots[0].Owner || robot.Disposal!=robots[0].Disposal || (parts&robot.Parts)!=0)throw new InvalidOperationException("Parts must be disjoint and have the same owner.");parts|=robot.Parts;}
        var merged=robots.OrderBy(r=>r.Id).First() with {Parts=parts,Role=robots.Any(r=>r.Role==RobotRole.Captain)?RobotRole.Captain:RobotRole.Soldier};
        store.Replace(store.Robots.Where(r=>!selected.Contains(r.Id)).Append(merged));return merged;
    }
    public void Split(int node,long id,RobotParts first)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);Validate(robot.Owner,first);
        if(Workshops[node].DisposalTarget==id)throw new InvalidOperationException("Disposal target is reserved.");
        if((robot.Parts&first)!=first || first==robot.Parts || store.Count>=RobotStore.Capacity)throw new InvalidOperationException("Cannot split this robot.");
        var remainder=Create(robot.Owner,robot.Parts^first,disposal:robot.Disposal);
        store.Replace(store.Robots.Where(r=>r.Id!=id).Append(robot with {Parts=first}).Append(remainder));
    }
    public void Advance(Func<int,int,bool> connected,Func<int,int> owner,int seed,Func<int,bool> locked=null,Func<int,int,bool> canEnter=null)
    {
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true){Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)));Workshops[n].CompleteDisposal(Nodes[n]);}
        WorkshopFailures.Clear();
        foreach(var edit in PendingEdits.ToArray())
        {
            if(locked?.Invoke(edit.Node)==true)continue;
            try{if(edit.SplitPart is RobotParts part)Split(edit.Node,edit.Ids.Single(),part);else Assemble(edit.Node,edit.Ids);}
            catch(InvalidOperationException error){WorkshopFailures.Add((edit.Node,error.Message));}
            PendingEdits.Remove(edit);
        }
        Transport.ResolvePlans(this,connected,seed,locked,canEnter);
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].CompleteProduction(this,n,owner(n));
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)));
        Check();
    }
    public void Check()
    {
        var all=Nodes.Concat(Retinues).SelectMany(s=>s.Robots).ToArray();
        if(all.Select(r=>r.Id).Distinct().Count()!=all.Length)throw new InvalidOperationException("Robot has multiple locations.");
        foreach(var robot in all)Validate(robot.Owner,robot.Parts);
    }
}

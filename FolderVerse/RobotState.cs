namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

[Flags]
public enum RobotParts { Legs=1, Body=2, Head=4, Complete=7 }
public enum RobotRole { Soldier, Captain }
public sealed record RobotWorkshopEdit(int Node,long[] Ids,RobotParts? SplitPart,int? Owner=null);
public sealed record Robot(long Id,int Owner,RobotParts Parts,RobotRole Role=RobotRole.Soldier,bool Disposal=false)
{
    public RobotParts DamagedParts {get;init;}=0;
    public bool IsDamaged(RobotParts part)=>(Parts&part)!=0 && (DamagedParts&part)!=0;
    // Origin retains the country even while damage makes a part unaffiliated.
    public int Affiliation(RobotParts part)=>(Parts&part)==0 || IsDamaged(part)?-1:Origin(part);
    public int HeadOrigin {get;init;}=-1;
    public int BodyOrigin {get;init;}=-1;
    public int LegsOrigin {get;init;}=-1;
    public int Origin(RobotParts part)=>part switch{RobotParts.Head=>HeadOrigin<0?Owner:HeadOrigin,RobotParts.Body=>BodyOrigin<0?Owner:BodyOrigin,_=>LegsOrigin<0?Owner:LegsOrigin};
    public int NativeParts(int owner)=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(Parts&p)!=0 && owner>=0 && Affiliation(p)==owner);
    public Robot Materialize()=>this with {HeadOrigin=Origin(RobotParts.Head),BodyOrigin=Origin(RobotParts.Body),LegsOrigin=Origin(RobotParts.Legs),DamagedParts=DamagedParts&Parts};
    public Robot Merge(Robot other,int owner)
    {
        var result=Materialize() with {Parts=Parts|other.Parts,DamagedParts=(DamagedParts&Parts&~other.Parts)|(other.DamagedParts&other.Parts),HeadOrigin=(other.Parts&RobotParts.Head)!=0?other.Origin(RobotParts.Head):Origin(RobotParts.Head),BodyOrigin=(other.Parts&RobotParts.Body)!=0?other.Origin(RobotParts.Body):Origin(RobotParts.Body),LegsOrigin=(other.Parts&RobotParts.Legs)!=0?other.Origin(RobotParts.Legs):Origin(RobotParts.Legs)};
        return result with {Owner=result.NativeParts(owner)>0?owner:Owner};
    }
    public bool CanFight=>Parts==RobotParts.Complete && !Disposal && NativeParts(Owner)>0;
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
    public RobotTraining[] Training {get;private set;}=Array.Empty<RobotTraining>();
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
        _domesticPreparedTurn=-1;Training=Enumerable.Range(0,rulerCount).Select(_=>new RobotTraining()).ToArray();_nextId=1;Nodes=Enumerable.Range(0,nodeCount).Select(_=>new RobotStore()).ToArray();
        Retinues=Enumerable.Range(0,rulerCount).Select(_=>new RobotStore()).ToArray();
        Workshops=Enumerable.Range(0,nodeCount).Select(_=>new RobotWorkshop()).ToArray();Transport.Reset();PendingEdits.Clear();WorkshopFailures.Clear();
    }
    public void QueueAssembly(int node,IEnumerable<long> ids,int? owner=null)=>QueueEdit(new(node,ids.ToArray(),null,owner));
    public void QueueSplit(int node,long id,RobotParts part)=>QueueEdit(new(node,new[]{id},part));
    private void QueueEdit(RobotWorkshopEdit edit)
    {
        if(edit.Node<0 || edit.Node>=Nodes.Length || edit.Ids.Length<(edit.SplitPart==null?2:1) || edit.Ids.Distinct().Count()!=edit.Ids.Length)throw new ArgumentException("Invalid workshop order.");
        var robots=edit.Ids.Select(id=>Nodes[edit.Node].Robots.Single(r=>r.Id==id)).ToArray();
        if(edit.Ids.Any(id=>PendingEdits.Any(e=>e.Ids.Contains(id)) || Workshops[edit.Node].DisposalTarget==id))throw new InvalidOperationException("Robot is already reserved or belongs to another owner.");
        if(edit.SplitPart is RobotParts part)
        {Validate(robots[0].Owner,part);if((robots[0].Parts&part)!=part || robots[0].Parts==part)throw new InvalidOperationException("Invalid split parts.");}
        else{var parts=(RobotParts)0;foreach(var robot in robots){if((parts&robot.Parts)!=0)throw new InvalidOperationException("Duplicate parts.");parts|=robot.Parts;}}
        PendingEdits.Add(edit);
    }
    public Robot Assemble(int node,IEnumerable<long> ids,int? owner=null)
    {
        var store=Nodes[node];var selected=ids.ToArray();
        if(selected.Length<2 || selected.Distinct().Count()!=selected.Length)throw new ArgumentException("Select distinct robots.");
        var robots=selected.Select(id=>store.Robots.Single(r=>r.Id==id)).ToArray();var parts=(RobotParts)0;
        if(selected.Contains(Workshops[node].DisposalTarget??-1))throw new InvalidOperationException("Disposal target is reserved.");
        foreach(var robot in robots)
        {if(robot.Disposal!=robots[0].Disposal || (parts&robot.Parts)!=0)throw new InvalidOperationException("Parts must be disjoint and have the same owner.");parts|=robot.Parts;}
        int affiliation=owner??robots[0].Owner;var merged=robots.OrderBy(r=>r.Id).Aggregate((a,b)=>a.Merge(b,affiliation)) with {Role=robots.Any(r=>r.Role==RobotRole.Captain)?RobotRole.Captain:RobotRole.Soldier};
        store.Replace(store.Robots.Where(r=>!selected.Contains(r.Id)).Append(merged));return merged;
    }
    public void Split(int node,long id,RobotParts first)
    {
        var store=Nodes[node];var robot=store.Robots.Single(r=>r.Id==id);Validate(robot.Owner,first);
        if(Workshops[node].DisposalTarget==id)throw new InvalidOperationException("Disposal target is reserved.");
        if((robot.Parts&first)!=first || first==robot.Parts || store.Count>=RobotStore.Capacity)throw new InvalidOperationException("Cannot split this robot.");
        var remainder=robot.Materialize() with {Id=Create(robot.Owner,robot.Parts^first,disposal:robot.Disposal).Id,Parts=robot.Parts^first,DamagedParts=robot.DamagedParts&(robot.Parts^first)};
        store.Replace(store.Robots.Where(r=>r.Id!=id).Append(robot with {Parts=first,DamagedParts=robot.DamagedParts&first}).Append(remainder));
    }
    public void Advance(Func<int,int,bool> connected,Func<int,int> owner,int seed,Func<int,bool> locked=null,Func<int,int,bool> canEnter=null)
    {
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true){Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)));Workshops[n].CompleteDisposal(Nodes[n]);}
        WorkshopFailures.Clear();
        foreach(var edit in PendingEdits.ToArray())
        {
            if(locked?.Invoke(edit.Node)==true)continue;
            try{if(edit.SplitPart is RobotParts part)Split(edit.Node,edit.Ids.Single(),part);else Assemble(edit.Node,edit.Ids,edit.Owner);}
            catch(InvalidOperationException error){WorkshopFailures.Add((edit.Node,error.Message));}
            PendingEdits.Remove(edit);
        }
        Transport.ResolvePlans(this,connected,seed,locked,canEnter);
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].CompleteProduction(this,n,owner(n));
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true)Workshops[n].StartDisposal(Nodes[n],id=>PendingEdits.Any(e=>e.Ids.Contains(id)));
        for(int n=0;n<Nodes.Length;n++)if(locked?.Invoke(n)!=true && owner(n)>=0 && !Workshops[n].AutoAssemblyDisabled)AutoAssemble(n,owner(n));
        Check();
    }
    private void AutoAssemble(int node,int owner)
    {
        var policy=Training[owner];
        while(true){
            var parts=Nodes[node].Robots.Where(r=>!r.Disposal && r.Parts!=RobotParts.Complete && Workshops[node].DisposalTarget!=r.Id && !PendingEdits.Any(e=>e.Ids.Contains(r.Id))).OrderBy(r=>r.Id).ToArray();
            long[] selection=null;
            for(int a=0;a<parts.Length && selection==null;a++)for(int b=a+1;b<parts.Length && selection==null;b++){
                if((parts[a].Parts&parts[b].Parts)!=0)continue;
                var merged=parts[a].Merge(parts[b],owner);
                if(merged.Parts==RobotParts.Complete && merged.NativeParts(owner)>=policy.RequiredNative){selection=new[]{parts[a].Id,parts[b].Id};break;}
                for(int c=b+1;c<parts.Length;c++)if((merged.Parts&parts[c].Parts)==0){var complete=merged.Merge(parts[c],owner);if(complete.Parts==RobotParts.Complete && complete.NativeParts(owner)>=policy.RequiredNative){selection=new[]{parts[a].Id,parts[b].Id,parts[c].Id};break;}}
            }
            if(selection==null)return;
            Assemble(node,selection,owner);
        }
    }
    public void Check()
    {
        var all=Nodes.Concat(Retinues).SelectMany(s=>s.Robots).ToArray();
        if(all.Select(r=>r.Id).Distinct().Count()!=all.Length)throw new InvalidOperationException("Robot has multiple locations.");
        foreach(var robot in all)Validate(robot.Owner,robot.Parts);
    }
}

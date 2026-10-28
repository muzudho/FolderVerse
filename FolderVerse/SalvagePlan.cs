namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public enum SalvagePolicy { Early,Balanced,Bulk,Automatic }
// Recovery goods have no owner. Provenance and part condition remain on the physical robot.
public sealed record SalvageItem(long Id,int Node,Robot Robot,int AbandonedBy=-1)
{
    public int RequestedBy=-1;
}
public sealed class SalvageAgent
{
    public int Owner,Node,PreviousNode,Destination=-1,DeliveryNode=-1,DeliveryTurns,CargoAge;
    public bool Paused;
    public SalvagePolicy Policy=SalvagePolicy.Automatic;
    public string Status="待機中";
    public List<Robot> Cargo=new();
    public int[] Route=Array.Empty<int>();
    public int Load=>Cargo.Sum(r=>SalvagePlan.PartCount(r.Parts));
}
public sealed class SalvagePlan
{
    public const int Capacity=6;
    public List<SalvageItem> Items {get;}=new();
    public List<SalvageAgent> Agents {get;}=new();
    private long _next=1;
    private Func<int,bool> _incomingBattle;
    public int LastTurn=-1;
    public bool LegacyPlacementPending;
    public static int PartCount(RobotParts parts)=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>(parts&p)!=0);
    public void Reset(){Items.Clear();Agents.Clear();_next=1;LastTurn=-1;LegacyPlacementPending=false;}
    private static Robot Stopped(Robot r)=>r.Materialize() with {Disposal=false,DefenseOnly=false,InterceptOnly=false,FactoryWorker=false};
    public void Initialize(WorldSetup w)
    {
        Reset();if(w.TutorialStep is 1 or 2)return;
        var random=new SeedRandom(w.WorldSeed^w.PlacementSeed^0x534352);
        foreach(var cell in w.Cells){
            var nodes=w.Nodes.InCell(cell.Id).OrderBy(n=>n.Id).ToArray();if(nodes.Length==0)continue;
            int count=1+random.Next(2);
            for(int i=0;i<count;i++){
                var r=w.Robots.Create(random.Next(w.ActiveCount),(RobotParts)(1+random.Next(7)));
                var pairs=new[]{RobotParts.Head|RobotParts.Body,RobotParts.Body|RobotParts.Legs}.Where(p=>(r.Parts&p)==p).ToArray();
                if(pairs.Length>0 && random.Next(3)==0)r=r with {IntegratedParts=pairs[random.Next(pairs.Length)]};
                r=r with {CheapParts=random.Next(3)==0?r.Parts:0,DamagedParts=random.Next(5)==0?r.Parts:0};
                Items.Add(new(_next++,nodes[random.Next(nodes.Length)].Id,r));
            }
        }
        EnsureAgents(w);
    }
    public void EnsureAgents(WorldSetup w)
    {
        for(int owner=0;owner<w.ActiveCount;owner++){
            if(Agents.Any(a=>a.Owner==owner))continue;
            var home=w.Nodes.All.Where(n=>n.Owner==owner).OrderByDescending(n=>w.Capitals.Length>owner && n.Cell==w.Capitals[owner]).ThenBy(n=>n.Id).FirstOrDefault();
            if(home!=null)Agents.Add(new(){Owner=owner,Node=home.Id,PreviousNode=home.Id});
        }
    }
    public SalvageItem Abandon(WorldSetup w,int owner,int node,long id,RobotParts parts)
    {
        if(node<0 || node>=w.Nodes.All.Length || w.Nodes.All[node].Owner!=owner || w.Campaign.RobotBattles.LockedNode(node))throw new InvalidOperationException("Abandonment is unavailable here.");
        var store=w.Robots.Nodes[node];var robot=store.Robots.SingleOrDefault(r=>r.Id==id);
        if(robot!=null)parts=robot.ConnectedParts(parts);
        if(robot==null || robot.Owner!=owner || parts==0 || (parts&robot.Parts)!=parts || new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(parts&p)!=0 && w.Robots.PartLocked(node,robot,p)))throw new InvalidOperationException("Reserved parts cannot be abandoned.");
        var cargo=Stopped(robot).Slice(parts);
        var remaining=robot.Parts&~parts;
        if(remaining==0 && w.Robots.DirectDelivery.Orders.Any(o=>o.Target==node && o.TargetRobotId==id))throw new InvalidOperationException("An incoming delivery requires its arrival anchor.");
        if(remaining==0)store.Remove(id);
        else{
            cargo=cargo with {Id=w.Robots.Create(owner,parts).Id};
            store.Replace(store.Robots.Select(r=>r.Id==id?r.Slice(remaining):r).ToArray());
        }
        var item=new SalvageItem(_next++,node,cargo,owner);Items.Add(item);return item;
    }
    private bool Accessible(WorldSetup w,int owner,int node)=>node>=0 && node<w.Nodes.All.Length &&
        _incomingBattle?.Invoke(node)!=true && !w.Campaign.RobotBattles.LockedNode(node) && (w.Nodes.All[node].Owner<0 || w.Relations.Allied(owner,w.Nodes.All[node].Owner));
    private Dictionary<int,int> Search(WorldSetup w,int owner,int start,out Dictionary<int,int> previous)
    {
        previous=new();var distance=new Dictionary<int,int>();if(start<0 || start>=w.Nodes.All.Length || _incomingBattle?.Invoke(start)==true || w.Campaign.RobotBattles.LockedNode(start))return distance;
        var queue=new Queue<int>();distance[start]=0;queue.Enqueue(start);
        while(queue.TryDequeue(out int node))foreach(var r in w.Routes.Neighbors(w.Nodes.All[node])){
            var next=w.Nodes.At(r.Target,r.Entry);if(next==null || !Accessible(w,owner,next.Id) || w.Campaign.RobotBattles.LockedEdge(node,next.Id) || distance.ContainsKey(next.Id))continue;
            distance[next.Id]=distance[node]+1;previous[next.Id]=node;queue.Enqueue(next.Id);
        }
        return distance;
    }
    public int[] Path(WorldSetup w,int owner,int start,int end)
    {
        var d=Search(w,owner,start,out var previous);if(!d.ContainsKey(end))return Array.Empty<int>();
        var path=new List<int>{end};while(path[^1]!=start)path.Add(previous[path[^1]]);path.Reverse();return path.ToArray();
    }
    public int NearestHome(WorldSetup w,int owner,int start)
    {
        var d=Search(w,owner,start,out _);
        return w.Nodes.All.Where(n=>n.Owner==owner && Accessible(w,owner,n.Id) && w.Robots.Nodes[n.Id].HasSpace && d.ContainsKey(n.Id)).OrderBy(n=>d[n.Id]).ThenBy(n=>n.Id).Select(n=>n.Id).FirstOrDefault(-1);
    }
    public bool CanRecover(WorldSetup w,int owner,SalvageItem item)
    {
        var agent=Agents.FirstOrDefault(a=>a.Owner==owner);return agent!=null && Accessible(w,owner,item.Node) && Path(w,owner,agent.Node,item.Node).Length>0;
    }
    public bool Request(WorldSetup w,int owner,long id)
    {
        EnsureAgents(w);var item=Items.FirstOrDefault(i=>i.Id==id);if(item==null || !CanRecover(w,owner,item))return false;
        item.RequestedBy=owner;var agent=Agents.First(a=>a.Owner==owner);agent.Paused=false;Preview(w,agent);return true;
    }
    public bool PickUp(WorldSetup w,int owner,long id)
    {
        var item=Items.FirstOrDefault(i=>i.Id==id);var current=w.Nodes.Current(owner);
        if(item==null || current?.Id!=item.Node || w.Campaign.RobotBattles.LockedNode(item.Node) || !w.Robots.Retinues[owner].HasSpace)return false;
        w.Robots.Retinues[owner].Add(Stopped(item.Robot) with {Owner=owner});Items.Remove(item);return true;
    }
    private SalvagePolicy EffectivePolicy(WorldSetup w,SalvageAgent a)
    {
        if(a.Policy!=SalvagePolicy.Automatic)return a.Policy;
        int stock=w.Nodes.All.Where(n=>n.Owner==a.Owner).Sum(n=>w.Robots.Nodes[n.Id].Count);
        return stock<4?SalvagePolicy.Early:Items.Count(i=>CanRecover(w,a.Owner,i))>=4?SalvagePolicy.Bulk:SalvagePolicy.Balanced;
    }
    private (int Node,bool Unload) Choose(WorldSetup w,SalvageAgent a)
    {
        var distances=Search(w,a.Owner,a.Node,out _);int home=NearestHome(w,a.Owner,a.Node);
        if(!Accessible(w,a.Owner,a.Node))return(home,true);
        var policy=EffectivePolicy(w,a);int maxAge=policy==SalvagePolicy.Early?1:policy==SalvagePolicy.Balanced?3:6;
        var candidates=Items.Where(i=>(i.AbandonedBy!=a.Owner || i.RequestedBy==a.Owner) &&
            PartCount(i.Robot.Parts)+a.Load<=Capacity && distances.ContainsKey(i.Node) && Accessible(w,a.Owner,i.Node)).ToArray();
        if(a.Cargo.Count>0 && (a.Load==Capacity || a.CargoAge>=maxAge || policy==SalvagePolicy.Early || candidates.Length==0))return(home,true);
        // Compute delivery distance once per candidate node, not per physical item.
        var delivery=new Dictionary<int,int>();
        foreach(int n in candidates.Select(i=>i.Node).Distinct()){
            int target=NearestHome(w,a.Owner,n);delivery[n]=target<0?int.MaxValue:Path(w,a.Owner,n,target).Length-1;
        }
        candidates=candidates.Where(i=>delivery[i.Node]!=int.MaxValue).ToArray();
        var requested=candidates.Where(i=>i.RequestedBy==a.Owner).OrderBy(i=>distances[i.Node]).ThenBy(i=>i.Id).FirstOrDefault();
        if(requested!=null && a.Load<Capacity)return(requested.Node,false);
        if(a.Cargo.Count>0 && policy==SalvagePolicy.Balanced && home>=0)candidates=candidates.Where(i=>distances[i.Node]+delivery[i.Node]<=distances[home]+2).ToArray();
        double Score(SalvageItem i)=>policy==SalvagePolicy.Bulk?distances[i.Node]+delivery[i.Node]*.3-Items.Where(j=>j.Node==i.Node).Sum(j=>PartCount(j.Robot.Parts))*2:distances[i.Node]+delivery[i.Node];
        var selected=candidates.OrderBy(Score).ThenBy(i=>i.Id).FirstOrDefault();
        return selected!=null?(selected.Node,false):a.Cargo.Count>0?(home,true):(-1,false);
    }
    public void Preview(WorldSetup w,SalvageAgent a)
    {
        if(a.Paused){a.Destination=a.DeliveryNode=-1;a.DeliveryTurns=0;a.Route=Array.Empty<int>();a.Status="休止中";return;}
        var choice=Choose(w,a);a.Destination=choice.Node;a.Route=choice.Node>=0?Path(w,a.Owner,a.Node,choice.Node):Array.Empty<int>();
        a.DeliveryNode=choice.Node<0?-1:NearestHome(w,a.Owner,choice.Node);
        a.DeliveryTurns=a.DeliveryNode<0?0:Math.Max(1,a.Route.Length-1)+(choice.Unload?0:Math.Max(1,Path(w,a.Owner,choice.Node,a.DeliveryNode).Length-1));
        a.Status=choice.Node<0?(a.Cargo.Count>0 || NearestHome(w,a.Owner,a.Node)<0?"空き枠・経路待機":"回収対象待機"):choice.Unload?(a.Cargo.Count>0?"搬入へ移動中":"帰還中"):"回収へ移動中";
    }
    public void Advance(WorldSetup w,int turn,Func<int,bool> incomingBattle=null)
    {
        if(LastTurn==turn)return;LastTurn=turn;EnsureAgents(w);
        _incomingBattle=incomingBattle;
        try{
        foreach(var a in Agents.OrderBy(a=>a.Owner)){
            a.PreviousNode=a.Node;
            if(a.Paused || !w.Relations.CanAct(a.Owner)){a.Status="休止中";continue;}
            if(a.Cargo.Count>0)a.CargoAge++;
            var choice=Choose(w,a);a.Destination=choice.Node;
            if(choice.Node<0){Preview(w,a);continue;}
            a.Route=Path(w,a.Owner,a.Node,choice.Node);if(a.Route.Length==0){a.Status="経路待機";continue;}
            if(a.Route.Length>1)a.Node=a.Route[1];
            if(a.Node==choice.Node){
                if(choice.Unload){
                    var store=w.Robots.Nodes[a.Node];
                    foreach(var robot in a.Cargo.ToArray())if(store.HasSpace){store.Add(Stopped(robot) with {Owner=a.Owner});a.Cargo.Remove(robot);}
                    if(a.Cargo.Count==0)a.CargoAge=0;
                }else foreach(var item in Items.Where(i=>i.Node==a.Node && (i.AbandonedBy!=a.Owner || i.RequestedBy==a.Owner)).OrderByDescending(i=>i.RequestedBy==a.Owner).ThenBy(i=>i.Id).ToArray()){
                    if(a.Load+PartCount(item.Robot.Parts)>Capacity)continue;
                    a.Cargo.Add(Stopped(item.Robot) with {Owner=a.Owner});Items.Remove(item);
                }
            }
            Preview(w,a);
        }
        }finally{_incomingBattle=null;}
    }
}

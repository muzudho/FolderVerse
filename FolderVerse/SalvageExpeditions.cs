namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public enum RecoveryShape { Head,Body,Legs,HeadBody,BodyLegs,Complete,Manufacturing,Dismantling,Scrap }
public sealed class RecoveryFilter
{
    public bool Anything=true,IncludeUnreachable,AllowDamaged;
    public HashSet<RecoveryShape> Shapes=new(Enum.GetValues<RecoveryShape>());
    public HashSet<RobotMaterial> Materials=new(Enum.GetValues<RobotMaterial>());
    public string Error=>Anything?"":Shapes.Count==0?"ものを１つ以上選んでください":Materials.Count==0?"素材を１つ以上選んでください":"";
}
public sealed record RecoveryTarget(long Item,RobotParts Parts);
public sealed record RecoveryChoice(RecoveryTarget Target,int Node,Robot Robot,RecoveryShape Shape,bool Reachable,int Distance);
public sealed class RecoveryMission
{
    public long Id;public int Owner,Source,Node,PreviousNode,Destination=-1;
    // Null preserves the source/pocket return rule used by older saves.
    public int? DeliveryNode;
    public bool Pocket,Pending=true,ReturnRequested,Returning,PocketFallback,Finished;
    public Robot Worker;
    public string Status="出発予約";
    public List<RecoveryTarget> Targets=new();public List<Robot> Cargo=new();public List<string> History=new();public List<long> LoanTokens=new();
    public int LoanCount=>Finished?0:LoanTokens.Count+(Pending&&Pocket?1:0);
}
public sealed partial class SalvagePlan
{
    public bool ExpeditionsEnabled;
    public List<RecoveryMission> Missions {get;}=new();
    public void EnableExpeditions(WorldSetup w)
    {
        if(ExpeditionsEnabled)return;
        // Old free agents retire without destroying the goods they were carrying.
        foreach(var agent in Agents)foreach(var robot in agent.Cargo)Items.Add(new(_next++,agent.Node,robot,agent.Owner));
        Agents.Clear();ExpeditionsEnabled=true;
    }
    public int LoanCount(int owner)=>Missions.Where(m=>m.Owner==owner&&!m.Finished).Sum(m=>m.LoanCount);
    public IEnumerable<(int Node,bool Pocket,Robot Robot)> RecoveryWorkers(WorldSetup w,int owner)
    {
        if(owner<0||owner>=w.ActiveCount)yield break;
        foreach(var n in w.Nodes.All.Where(n=>n.Functional))foreach(var robot in w.Robots.Nodes[n.Id].Robots)
            if(EligibleWorker(w,owner,n.Id,false,robot))yield return(n.Id,false,robot);
        int home=w.Nodes.Current(owner)?.Id??-1;
        if(home>=0)foreach(var robot in w.Robots.Retinues[owner].Robots)if(EligibleWorker(w,owner,home,true,robot))yield return(home,true,robot);
    }
    private static bool EligibleWorker(WorldSetup w,int owner,int node,bool pocket,Robot r)=>r.Owner==owner&&r.Parts==RobotParts.Complete&&!r.Stationed&&!r.Disposal&&!r.IsWorkpiece&&!r.IsDismantling&&r.RecoveredRaw==null&&
        !w.Campaign.RobotBattles.LockedNode(node)&&(pocket?!w.Robots.PartPlanning.ReservesRobot(r.Id)&&!w.Campaign.PlannedOrders.Any(o=>o.Ruler==owner&&o.Queen):w.Robots.CanShip(node,r));
    public int[] RecoveryPath(WorldSetup w,int start,int target,bool ignoreObstacles=false,Func<int,bool> incoming=null)
    {
        if(start<0||target<0||start>=w.Nodes.All.Length||target>=w.Nodes.All.Length)return Array.Empty<int>();
        bool Blocked(int n)=>!ignoreObstacles&&(w.Campaign.RobotBattles.LockedNode(n)||incoming?.Invoke(n)==true);
        if(Blocked(start)||Blocked(target))return Array.Empty<int>();
        var previous=new Dictionary<int,int>{{start,-1}};var queue=new Queue<int>();queue.Enqueue(start);
        while(queue.TryDequeue(out int at)){
            if(at==target){var path=new List<int>();for(int n=target;n>=0;n=previous[n])path.Add(n);path.Reverse();return path.ToArray();}
            foreach(var route in w.Routes.Neighbors(w.Nodes.All[at])){var next=w.Nodes.At(route.Target,route.Entry);if(next==null||previous.ContainsKey(next.Id)||Blocked(next.Id)||!ignoreObstacles&&w.Campaign.RobotBattles.LockedEdge(at,next.Id))continue;previous[next.Id]=at;queue.Enqueue(next.Id);}
        }
        return Array.Empty<int>();
    }
    public RecoveryChoice[] RecoveryChoices(WorldSetup w,int start,RecoveryFilter filter,int? deliveryNode=null)
    {
        if(filter.Error.Length>0)return Array.Empty<RecoveryChoice>();
        var result=new List<RecoveryChoice>();
        foreach(var item in Items){
            bool reachable=RecoveryPath(w,start,item.Node).Length>0&&(!deliveryNode.HasValue||RecoveryPath(w,item.Node,deliveryNode.Value).Length>0);if(!filter.IncludeUnreachable&&!reachable)continue;
            int distance=RecoveryPath(w,start,item.Node,true).Length;distance=distance==0?int.MaxValue:distance-1;
            var r=item.Robot;
            bool Match(RobotParts parts)=>(filter.AllowDamaged||(r.EffectiveDamagedParts&parts)==0)&&(filter.Anything||RobotMaterials.Parts.Where(p=>(parts&p)!=0).All(p=>filter.Materials.Contains(r.Material(p))));
            void Add(RobotParts parts,RecoveryShape shape){if((filter.Anything||filter.Shapes.Contains(shape))&&Match(parts))result.Add(new(new(item.Id,parts),item.Node,r.Slice(parts),shape,reachable,distance));}
            if(r.RecoveredRaw!=null){if(filter.Anything||filter.Shapes.Contains(RecoveryShape.Scrap))result.Add(new(new(item.Id,r.Parts),item.Node,r,RecoveryShape.Scrap,reachable,distance));continue;}
            var special=r.IsDismantling?RecoveryShape.Dismantling:r.IsWorkpiece?RecoveryShape.Manufacturing:(RecoveryShape?)null;
            if(special.HasValue){if(filter.Anything||filter.Shapes.Contains(special.Value))AddSpecial();continue;}
            if(r.Parts==RobotParts.Complete&&(filter.Anything||filter.Shapes.Contains(RecoveryShape.Complete))&&Match(r.Parts)){Add(r.Parts,RecoveryShape.Complete);continue;}
            foreach(var unit in Units(r))Add(unit,Shape(unit));
            void AddSpecial(){foreach(var unit in Units(r))if(Match(unit))result.Add(new(new(item.Id,unit),item.Node,r.Slice(unit),special.Value,reachable,distance));}
        }
        return result.OrderBy(c=>c.Distance).ThenBy(c=>c.Target.Item).ThenBy(c=>c.Target.Parts).ToArray();
    }
    private static IEnumerable<RobotParts> Units(Robot r){if(r.IntegratedParts!=0)yield return r.IntegratedParts;foreach(var p in RobotMaterials.Parts)if((r.Parts&p)!=0&&(r.IntegratedParts&p)==0)yield return p;}
    private static RecoveryShape Shape(RobotParts p)=>p switch{RobotParts.Head=>RecoveryShape.Head,RobotParts.Body=>RecoveryShape.Body,RobotParts.Legs=>RecoveryShape.Legs,RobotParts.Head|RobotParts.Body=>RecoveryShape.HeadBody,RobotParts.Body|RobotParts.Legs=>RecoveryShape.BodyLegs,_=>RecoveryShape.Complete};
    public RecoveryMission ReserveExpedition(WorldSetup w,int owner,int source,bool pocket,long workerId,IEnumerable<RecoveryTarget> targets,int? deliveryNode=null)
    {
        if(owner<0||owner>=w.ActiveCount||!w.Relations.CanAct(owner))throw new InvalidOperationException("この征服者は出発予約できません");
        EnableExpeditions(w);
        if(w.Campaign.Phase!=CampaignPhase.Domestic)throw new InvalidOperationException("内政フェーズで予約してください");
        var selected=targets.Distinct().ToArray();if(selected.Length==0)throw new InvalidOperationException("回収する品を１つ以上選んでください");
        foreach(var group in selected.GroupBy(t=>t.Item)){RobotParts used=0;foreach(var t in group){var item=Items.FirstOrDefault(i=>i.Id==t.Item);if(item==null||(used&t.Parts)!=0||(item.Robot.Parts&t.Parts)!=t.Parts||!item.Robot.CanSeparate(t.Parts))throw new InvalidOperationException("品物が変わりました。選び直してください");used|=t.Parts;}}
        if(source<0||source>=w.Nodes.All.Length||pocket&&w.Nodes.Current(owner)?.Id!=source)throw new InvalidOperationException("出発地点を確認してください");
        if(deliveryNode is int goal && (goal<0||goal>=w.Nodes.All.Length||!w.Nodes.All[goal].Functional||w.Nodes.All[goal].Owner!=owner))throw new InvalidOperationException("受け取り先は自国の機能する地点を選んでください");
        var store=pocket?w.Robots.Retinues[owner]:w.Robots.Nodes[source];var worker=store.Robots.FirstOrDefault(r=>r.Id==workerId);
        if(worker==null||!EligibleWorker(w,owner,source,pocket,worker))throw new InvalidOperationException("予約のない自国の完全体を選んでください");
        var bag=w.Robots.Retinues[owner];if(Missions.Count(m=>m.Owner==owner&&!m.Finished)>=6||1+selected.Length>bag.StorageLimit-bag.Count+(pocket?1:0))throw new InvalidOperationException("ポケットに本人と荷物の貸出枠が足りません");
        var mission=new RecoveryMission{Id=_next++,Owner=owner,Source=source,Node=source,PreviousNode=source,Pocket=pocket,DeliveryNode=deliveryNode,Worker=worker,Targets=selected.ToList()};
        store.Replace(store.Robots.Select(r=>r.Id==workerId?r with{SalvageReserved=true}:r));Missions.Add(mission);RefreshLoans(w,mission);return mission;
    }
    private void RefreshLoans(WorldSetup w,RecoveryMission m)
    {
        var bag=w.Robots.Retinues[m.Owner];int desired=m.Finished?0:1+m.Cargo.Count+m.Targets.Count-(m.Pending&&m.Pocket?1:0);
        while(m.LoanTokens.Count>desired){bag.ReleaseReturn(m.LoanTokens[^1]);m.LoanTokens.RemoveAt(m.LoanTokens.Count-1);}
        while(m.LoanTokens.Count<desired){long token=-(m.Id*32+m.LoanTokens.Count+1000000);bag.ReserveStorage(token);m.LoanTokens.Add(token);}
    }
    public bool CancelExpedition(WorldSetup w,long id,int owner)
    {
        var m=Missions.FirstOrDefault(m=>m.Id==id&&m.Owner==owner&&m.Pending&&!m.Finished);if(m==null)return false;
        var store=m.Pocket?w.Robots.Retinues[owner]:w.Robots.Nodes[m.Source];store.Replace(store.Robots.Select(r=>r.Id==m.Worker.Id?r with{SalvageReserved=false}:r));m.Finished=true;m.Status="予約取消";RefreshLoans(w,m);return true;
    }
    public bool ReserveRecoveryReturn(long id,int owner){var m=Missions.FirstOrDefault(m=>m.Id==id&&m.Owner==owner&&!m.Finished&&!m.Pending);if(m==null||m.Returning)return false;m.ReturnRequested=!m.ReturnRequested;return true;}
    public void CommitExpeditions(WorldSetup w)
    {
        if(!ExpeditionsEnabled)return;
        foreach(var m in Missions.Where(m=>!m.Finished)){
            if(m.Pending){var store=m.Pocket?w.Robots.Retinues[m.Owner]:w.Robots.Nodes[m.Source];var worker=store.Robots.FirstOrDefault(r=>r.Id==m.Worker.Id);if(worker==null){m.Finished=true;m.Status="出発取消：ロボットがありません";RefreshLoans(w,m);continue;}store.Remove(worker.Id);m.Worker=worker with{SalvageReserved=false};m.Pending=false;m.Status="回収中";RefreshLoans(w,m);}
            if(m.ReturnRequested){m.Returning=true;m.Targets.Clear();m.ReturnRequested=false;m.History.Add("帰還指示：未回収の予定を終了しました");RefreshLoans(w,m);}
        }
    }
    private void AbandonTarget(WorldSetup w,RecoveryMission m,RecoveryTarget target,string reason){m.Targets.Remove(target);m.History.Add("#"+target.Item+" / "+reason);RefreshLoans(w,m);}
    private void AdvanceExpeditions(WorldSetup w,int turn,Func<int,bool> incoming)
    {
        if(LastTurn==turn)return;LastTurn=turn;
        for(int n=0;n<w.Robots.Nodes.Length;n++){w.Robots.Nodes[n].ScrapNode=n;w.Robots.Nodes[n].ScrapTurn=turn;}
        foreach(var m in Missions)m.PreviousNode=m.Node;
        foreach(var m in Missions.Where(m=>!m.Finished&&!m.Pending).OrderBy(m=>m.Id)){
            m.PreviousNode=m.Node;
            if(!w.Relations.Powered[m.Owner]){m.Returning=true;m.Targets.Clear();RefreshLoans(w,m);}
            if(m.Returning||m.Targets.Count==0){m.Returning=true;ReturnExpedition(w,m,incoming);continue;}
            bool moved=false;
            foreach(var target in m.Targets.OrderBy(t=>{var item=Items.FirstOrDefault(i=>i.Id==t.Item);var path=item==null?Array.Empty<int>():RecoveryPath(w,m.Node,item.Node,true);return path.Length==0?int.MaxValue:path.Length-1;}).ThenBy(t=>t.Item).ToArray()){
                var item=Items.FirstOrDefault(i=>i.Id==target.Item);
                if(item==null||(item.Robot.Parts&target.Parts)!=target.Parts||!item.Robot.CanSeparate(target.Parts)){AbandonTarget(w,m,target,"他の班が回収済み、または品物が変わりました");continue;}
                var path=RecoveryPath(w,m.Node,item.Node,false,incoming);m.Destination=item.Node;
                if(path.Length==0){AbandonTarget(w,m,target,"到達できる経路がありません");continue;}
                if(path.Length>1){if(moved)break;m.Node=path[1];moved=true;}
                if(m.Node!=item.Node){m.Status="回収へ移動中";break;}
                var cargo=Stopped(item.Robot).Slice(target.Parts) with{Owner=m.Owner,Id=w.Robots.Create(m.Owner,target.Parts).Id,SalvageReserved=false};
                var remainder=item.Robot.Parts&~target.Parts;
                if(remainder==0)Items.Remove(item);else Items[Items.IndexOf(item)]=item with{Robot=item.Robot.Slice(remainder)};
                m.Cargo.Add(cargo);m.Targets.Remove(target);m.History.Add("#"+target.Item+" / 回収しました");RefreshLoans(w,m);
            }
            if(m.Targets.Count==0){m.Returning=true;m.Status="帰還中";}
        }
    }
    private void ReturnExpedition(WorldSetup w,RecoveryMission m,Func<int,bool> incoming)
    {
        bool eliminated=!w.Relations.Powered[m.Owner];int destination=-1;bool bag=false;
        bool Fits(int n)=>w.Nodes.All[n].Functional&&w.Robots.Nodes[n].StorageLimit-w.Robots.Nodes[n].Count>=1+m.Cargo.Count;
        if(eliminated){int country=w.Nodes.Current(m.Owner)?.Owner??-1;if(country<0)country=w.Nodes.All[m.Source].Owner;destination=w.Nodes.All.Where(n=>n.Functional&&country>=0&&n.Owner==country&&Fits(n.Id)).Select(n=>new{n.Id,Path=RecoveryPath(w,m.Node,n.Id,false,incoming)}).Where(p=>p.Path.Length>0).OrderBy(p=>p.Path.Length).ThenBy(p=>p.Id).Select(p=>p.Id).FirstOrDefault(-1);}
        else{
            int goal=m.DeliveryNode??m.Source;bool pocketGoal=m.DeliveryNode==null&&m.Pocket;
            if(!pocketGoal&&!m.PocketFallback&&(!m.DeliveryNode.HasValue||w.Nodes.All[goal].Owner==m.Owner)&&Fits(goal)&&RecoveryPath(w,m.Node,goal,false,incoming).Length>0)destination=goal;
            else {m.PocketFallback=true;destination=w.Nodes.Current(m.Owner)?.Id??-1;bag=true;}
        }
        m.Destination=destination;var path=RecoveryPath(w,m.Node,destination,false,incoming);
        if(path.Length==0){m.Status="帰還経路待ち / 貸出枠を保持";return;}
        if(path.Length>1)m.Node=path[1];m.Status="帰還中";if(m.Node!=destination)return;
        var store=bag?w.Robots.Retinues[m.Owner]:w.Robots.Nodes[destination];
        if(!bag&&!Fits(destination)){m.PocketFallback=true;m.Status="満杯のためポケットへ帰還";return;}
        foreach(long token in m.LoanTokens)w.Robots.Retinues[m.Owner].ReleaseReturn(token);m.LoanTokens.Clear();
        // Each selected object had one full slot reserved; unloading is all-or-nothing.
        store.Add(m.Worker with{SalvageReserved=false});
        foreach(var robot in m.Cargo){if(robot.RecoveredRaw is {} raw){if(!store.TryReceiveMaple(raw,0,_=>false))throw new InvalidOperationException("Reserved scrap slot lost.");}else store.Add(robot with{SalvageReserved=false});}
        m.Cargo.Clear();m.Finished=true;m.Status="帰還完了";m.History.Add(bag?"本人と荷物をポケットへ届けました":"本人と荷物を地点へ届けました");
    }
    public void PlanComputerExpeditions(WorldSetup w,bool automaticAll)
    {
        EnableExpeditions(w);
        var manual=w.ManualRulers().ToHashSet();
        foreach(int owner in Enumerable.Range(0,w.ActiveCount).Where(w.Relations.CanAct)){
            if(!automaticAll&&manual.Contains(owner)||Missions.Any(m=>m.Owner==owner&&!m.Finished))continue;
            var worker=RecoveryWorkers(w,owner).OrderByDescending(p=>p.Pocket).FirstOrDefault();if(worker.Robot==null)continue;
            int free=w.Robots.Retinues[owner].StorageLimit-w.Robots.Retinues[owner].Count+(worker.Pocket?1:0)-1;if(free<1)continue;
            var targets=RecoveryChoices(w,worker.Node,new RecoveryFilter()).Take(Math.Min(2,free)).Select(c=>c.Target).ToArray();if(targets.Length==0)continue;
            try{ReserveExpedition(w,owner,worker.Node,worker.Pocket,worker.Robot.Id,targets);}catch(InvalidOperationException){/* Other reservations take priority. */}
        }
    }
}
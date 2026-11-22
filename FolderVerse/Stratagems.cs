namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public enum StratagemKind { WallSortie, WallRelief, ConcentratedAttack }
public enum StratagemCategory { Attack, Defense, Transport, Harvest, Manufacturing }
public sealed record StratagemSite(int Node,NodePurpose Before,NodePurpose After);
public sealed record StratagemTroop(int Node,Robot Before,HangarReservation Reservation);
public sealed record StratagemMatch(StratagemKind Kind,int Target,int Wall,StratagemSite[] Sites,StratagemTroop[] Troops,March[] Marches)
{
    public string Key=>$"{Kind}:{Target}:{Wall}:"+string.Join(",",Sites.Select(s=>s.Node));
}
public sealed record StratagemReservation(long Id,int Controller,StratagemMatch Match,bool Computer=false);

// A card owns a deferred bundle, never a replacement for existing manual orders.
public sealed class StratagemPlans
{
    public List<StratagemReservation> Pending {get;}=new();
    private long _next=1;
    public static string Title(StratagemKind kind)=>kind switch{
        StratagemKind.WallSortie=>"城壁越しの出撃",StratagemKind.WallRelief=>"城壁への救援",_=>"多地点からの集中攻撃"};
    public static StratagemCategory Category(StratagemKind kind)=>kind==StratagemKind.WallRelief?StratagemCategory.Defense:StratagemCategory.Attack;
    public bool Cancel(long id,int controller)=>Pending.RemoveAll(p=>p.Id==id && p.Controller==controller)>0;
    public StratagemMatch[] Find(WorldSetup w,int controller)
        =>FindCore(w,w.ControlledParty(controller));
    internal static int[] ComputerParty(WorldSetup w,int ruler,bool automaticAll)
    {
        if(ruler<0||ruler>=w.ActiveCount||w.Relations.Root(ruler)!=ruler)return Array.Empty<int>();
        var manual=w.ManualRulers().ToHashSet();
        return Enumerable.Range(0,w.ActiveCount).Where(owner=>w.Relations.CanAct(owner)&&w.Relations.Root(owner)==ruler&&(automaticAll||!manual.Contains(owner))).ToArray();
    }
    public StratagemMatch[] FindComputer(WorldSetup w,int ruler,bool automaticAll=false)
        =>FindCore(w,ComputerParty(w,ruler,automaticAll));
    private StratagemMatch[] FindCore(WorldSetup w,IEnumerable<int> owners)
    {
        if(w.Campaign.Phase!=CampaignPhase.Domestic)return Array.Empty<StratagemMatch>();
        var party=owners.ToHashSet();
        var battle=w.Campaign.RobotBattles;
        var claimed=Pending.SelectMany(p=>p.Match.Sites.Select(s=>s.Node)).ToHashSet();
        var busy=w.Campaign.PlannedOrders.Where(o=>!o.PocketOnly).Select(o=>o.SourceNode>=0?o.SourceNode:w.Nodes.Current(o.Ruler)?.Id??-1).ToHashSet();
        foreach(var order in w.Campaign.FunctionTransfers.Orders){busy.Add(order.Source);busy.Add(order.Target);}
        var sites=w.Nodes.All.Where(n=>n.Functional && party.Contains(n.Owner) && n.Purpose!=NodePurpose.Relay &&
            !battle.LockedNode(n.Id) && !claimed.Contains(n.Id) && !busy.Contains(n.Id)).ToArray();
        var free=sites.ToDictionary(n=>n.Id,n=>battle.SelectableRobots(w,n.Owner,n.Id,false)
            .Where(r=>!r.Disposal && !w.Robots.MarchReserved(n.Id,r) && !w.Robots.AssemblyLocked(n.Id,r)).OrderBy(r=>r.Id).ToArray());
        var guards=sites.ToDictionary(n=>n.Id,n=>w.Robots.Nodes[n.Id].Robots.OrderBy(r=>r.Id).FirstOrDefault(r=>r.Owner==n.Owner && r.DefenseOnly && r.CanFight && w.Robots.Training[r.Owner].Eligible(r))??free[n.Id].FirstOrDefault());
        var routes=sites.ToDictionary(n=>n.Id,n=>battle.MarchTargets(w,n.Id).ToHashSet());
        bool Enemy(int owner,int target)=>w.Nodes.All[target].Owner>=0 && !w.Relations.Allied(owner,w.Nodes.All[target].Owner) && !battle.LockedNode(target);
        var result=new List<StratagemMatch>();
        foreach(var rear in sites.Where(n=>free[n.Id].Length>0))
        foreach(var front in sites.Where(n=>n.Id!=rear.Id && routes[rear.Id].Contains(n.Id) && w.Relations.Allied(rear.Owner,n.Owner) && guards[n.Id]!=null))
        {
            var targets=routes[front.Id].Where(t=>t!=rear.Id && !routes[rear.Id].Contains(t) && Enemy(rear.Owner,t)).OrderBy(t=>t).ToArray();
            foreach(int target in targets){
                var troops=free[rear.Id].Take(Math.Max(1,free[rear.Id].Length-1)).ToArray();
                var changes=new[]{new StratagemSite(rear.Id,rear.Purpose,NodePurpose.Frontline),new StratagemSite(front.Id,front.Purpose,NodePurpose.Defense)};
                var guard=new StratagemTroop(front.Id,guards[front.Id],HangarReservation.Defense);
                if(free[rear.Id].Length>=2)
                    result.Add(new(StratagemKind.WallSortie,target,front.Id,changes,new[]{guard}.Concat(troops.Select(r=>new StratagemTroop(rear.Id,r,HangarReservation.None))).ToArray(),
                        new[]{new March(rear.Owner,target,troops.Length,rear.Id,Queen:false,RobotIds:troops.Select(r=>r.Id).ToArray())}));
                // One relief candidate per rear/front pair; its target identifies the threatened approach.
                if(target==targets[0])result.Add(new(StratagemKind.WallRelief,target,front.Id,changes,new[]{guard}.Concat(troops.Select(r=>new StratagemTroop(rear.Id,r,HangarReservation.Intercept))).ToArray(),Array.Empty<March>()));
            }
        }
        foreach(int target in sites.SelectMany(n=>battle.AttackTargets(w,n.Owner,n.Id)).Distinct().OrderBy(t=>t)){
            var origins=sites.Where(n=>free[n.Id].Length>=2 && Enemy(n.Owner,target) && battle.CanAttackFrom(w,n.Owner,n.Id,target))
                .GroupBy(n=>w.Relations.Root(n.Owner));
            foreach(var faction in origins){
                var selected=faction.OrderByDescending(n=>free[n.Id].Length).ThenBy(n=>n.Id).Take(3).ToArray();
                if(selected.Length<2)continue;
                var troops=selected.SelectMany(n=>free[n.Id].Take(free[n.Id].Length-1).Select(r=>new StratagemTroop(n.Id,r,HangarReservation.None))).ToArray();
                result.Add(new(StratagemKind.ConcentratedAttack,target,-1,selected.Select(n=>new StratagemSite(n.Id,n.Purpose,n.Purpose)).ToArray(),troops,
                    selected.Select(n=>new March(n.Owner,target,troops.Count(t=>t.Node==n.Id),n.Id,Queen:false,RobotIds:troops.Where(t=>t.Node==n.Id).Select(t=>t.Before.Id).ToArray())).ToArray()));
            }
        }
        return result.OrderBy(m=>m.Kind).ThenBy(m=>m.Target).ThenBy(m=>m.Key).ToArray();
    }
    public bool Reserve(WorldSetup w,int controller,StratagemMatch match,out string reason)
        =>ReserveCore(w,controller,match,false,false,out reason);
    public bool ReserveComputer(WorldSetup w,int ruler,StratagemMatch match,bool automaticAll,out string reason)
        =>ReserveCore(w,ruler,match,true,automaticAll,out reason);
    private bool ReserveCore(WorldSetup w,int controller,StratagemMatch match,bool computer,bool automaticAll,out string reason)
    {
        var current=(computer?FindComputer(w,controller,automaticAll):Find(w,controller)).FirstOrDefault(m=>m.Key==match.Key);
        if(current==null || !SameTroops(current,match)){reason="配置や予約が変わりました。候補を選び直してください";return false;}
        if(!Valid(w,controller,current,out reason,computer,automaticAll))return false;
        Pending.Add(new(_next++,controller,current,computer));return true;
    }
    private static bool SameTroops(StratagemMatch a,StratagemMatch b)=>a.Troops.SequenceEqual(b.Troops) && a.Sites.SequenceEqual(b.Sites);
    private bool Valid(WorldSetup w,int controller,StratagemMatch match,out string reason,bool computer=false,bool automaticAll=false)
    {
        reason="";
        bool Fail(){return false;}
        if(w.Campaign.Phase!=CampaignPhase.Domestic){reason="内政フェーズで予約してください";return Fail();}
        if(match.Target<0 || match.Target>=w.Nodes.All.Length || w.Campaign.RobotBattles.LockedNode(match.Target)){
            reason="目標地点が戦闘中、または存在しません";return false;
        }
        var controlled=(computer?ComputerParty(w,controller,automaticAll):w.ControlledParty(controller)).ToHashSet();
        foreach(var site in match.Sites){
            if(site.Node<0 || site.Node>=w.Nodes.All.Length){reason="対象地点がありません";return false;}
            var n=w.Nodes.All[site.Node];
            if(computer&&w.Robots.Workshops[n.Id].Automation==NodeAutomation.Manual){reason="手動管理の地点です";return false;}
            if(n.PermanentLandmark && site.After!=site.Before || !n.Functional || !controlled.Contains(n.Owner) || n.Purpose!=site.Before || w.Campaign.RobotBattles.LockedNode(n.Id) ||
                w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==n.Id || o.Target==n.Id) ||
                w.Campaign.PlannedOrders.Any(o=>!o.PocketOnly && (o.SourceNode>=0?o.SourceNode:w.Nodes.Current(o.Ruler)?.Id??-1)==n.Id)){
                reason="地点の用途・所有国・予約が変わりました";return false;
            }
        }
        foreach(var troop in match.Troops){
            var robot=w.Robots.Nodes[troop.Node].Robots.FirstOrDefault(r=>r.Id==troop.Before.Id);
            bool existing=robot!=null && (troop.Reservation==HangarReservation.Defense && robot.DefenseOnly || troop.Reservation==HangarReservation.Intercept && robot.InterceptOnly);
            if(robot==null || robot!=troop.Before || !controlled.Contains(robot.Owner) ||
                (!existing && !w.Campaign.RobotBattles.SelectableRobots(w,robot.Owner,troop.Node,false).Any(r=>r.Id==robot.Id)) ||
                !robot.CanFight || !w.Robots.Training[robot.Owner].Eligible(robot) || robot.Disposal || w.Robots.MarchReserved(troop.Node,robot) || !existing && w.Robots.AssemblyLocked(troop.Node,robot) ||
                troop.Reservation!=HangarReservation.None && !w.Robots.CanSetReservation(troop.Node,robot,troop.Reservation,robot.Owner)){
                reason="使用するロボットに別の予約があります";return false;
            }
        }
        // Check reach against intended purposes without changing the live world.
        var battle=w.Campaign.RobotBattles;
        foreach(var order in match.Marches){
            bool reachable=match.Kind==StratagemKind.WallSortie
                ?battle.PotentialOutpostTargets(w,order.Ruler,order.SourceNode).Contains(order.TargetNode)
                :battle.CanAttackFrom(w,order.Ruler,order.SourceNode,order.TargetNode);
            if(!reachable || w.Nodes.All[order.TargetNode].Owner<0 || w.Relations.Allied(order.Ruler,w.Nodes.All[order.TargetNode].Owner)){
                reason="目標までの経路・所有国が変わりました";return false;
            }
        }
        if(match.Wall>=0){
            int rear=match.Sites[0].Node,owner=w.Nodes.All[rear].Owner;
            if(!battle.MarchTargets(w,rear).Contains(match.Wall) || !battle.MarchTargets(w,match.Wall).Contains(match.Target) ||
                w.Nodes.All[match.Target].Owner<0 || w.Relations.Allied(owner,w.Nodes.All[match.Target].Owner)){
                reason="城壁と詰所の経路が変わりました";return false;
            }
        }
        return true;
    }
    public string[] CancelInvalid(WorldSetup w,int controller)
    {
        var cancelled=new System.Collections.Generic.List<string>();
        foreach(var plan in Pending.Where(p=>p.Controller==controller && !p.Computer).ToArray())
            if(!Valid(w,controller,plan.Match,out var reason)){
                Pending.Remove(plan);cancelled.Add($"{Title(plan.Match.Kind)} / {reason}");
            }
        return cancelled.ToArray();
    }
    // Validation precedes every mutation. A stale bundle stays cancellable, with no partial apply.
    public bool Commit(WorldSetup w,int controller,out string reason)
        =>CommitCore(w,controller,false,false,out reason);
    public bool CommitComputer(WorldSetup w,int ruler,bool automaticAll,out string reason)
        =>CommitCore(w,ruler,true,automaticAll,out reason);
    private bool CommitCore(WorldSetup w,int controller,bool computer,bool automaticAll,out string reason)
    {
        var batch=Pending.Where(p=>p.Controller==controller&&p.Computer==computer).ToArray();
        foreach(var plan in batch)if(!Valid(w,controller,plan.Match,out reason,computer,automaticAll)){
            reason=$"予約{plan.Id} {Title(plan.Match.Kind)} / {reason}";return false;
        }
        foreach(var plan in batch){
            foreach(var site in plan.Match.Sites)w.Nodes.All[site.Node].Purpose=site.After;
            foreach(var troop in plan.Match.Troops.Where(t=>t.Reservation!=HangarReservation.None))
                if(!(troop.Reservation==HangarReservation.Defense && troop.Before.DefenseOnly || troop.Reservation==HangarReservation.Intercept && troop.Before.InterceptOnly) &&
                    !w.Robots.ToggleReservation(troop.Node,troop.Before.Id,troop.Reservation,troop.Before.Owner))throw new InvalidOperationException("Validated stratagem reservation failed.");
            foreach(var order in plan.Match.Marches)w.Campaign.Plan(order);
        }
        Pending.RemoveAll(p=>p.Controller==controller&&p.Computer==computer);reason="";return true;
    }
}

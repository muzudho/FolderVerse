namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record March(int Ruler,int TargetNode,long Fighters,int SourceNode=-1,bool Queen=true,bool PocketOnly=false,long[] RobotIds=null,bool Peaceful=false);
public sealed class ConquestCampaign
{
    public bool UseRobotCombat {get;set;}=true;
    public RobotCampaign RobotBattles {get;}=new();
    public string Report {get;private set;}="";
    public List<BattleRecord> Battles {get;}=new();
    private readonly List<March> _partyOrders=new();
    private readonly HashSet<int> _submitted=new();
    private readonly List<March> _plannedOrders=new();
    public IReadOnlyList<March> PlannedOrders=>_plannedOrders.Concat(_partyOrders).ToArray();
    internal void RewriteReservations(Func<March,March> rewrite)
    {
        foreach(var orders in new[]{_plannedOrders,_partyOrders})for(int i=orders.Count-1;i>=0;i--){
            var next=rewrite(orders[i]);if(next==null)orders.RemoveAt(i);else orders[i]=next;
        }
        Report=PlannedOrders.Count>0?$"予約 {PlannedOrders.Count} 件 / ターン終了で実行":"";
    }
    public void CancelSource(int owner,int source){_plannedOrders.RemoveAll(o=>o.Ruler==owner && o.SourceNode==source);_partyOrders.RemoveAll(o=>o.Ruler==owner && o.SourceNode==source);}
    public void CancelReservation(int owner,int source,bool queen,int target)
    {
        bool Matches(March o)=>o.Ruler==owner && (o.Queen || o.PocketOnly)==queen && o.TargetNode==target && (queen || o.SourceNode==source);
        _plannedOrders.RemoveAll(o=>Matches(o));_partyOrders.RemoveAll(o=>Matches(o));
    }
    public void Plan(March order)
    {
        order=order with {RobotIds=order.RobotIds?.ToArray()};
        bool SameStore(March o)=>o.Ruler==order.Ruler && (o.Queen || o.PocketOnly)==(order.Queen || order.PocketOnly) && ((order.Queen || order.PocketOnly) || o.SourceNode==order.SourceNode);
        void Update(List<March> orders)
        {
            for(int i=orders.Count-1;i>=0;i--)
            {
                var prior=orders[i];if(!SameStore(prior))continue;
                if(prior.TargetNode==order.TargetNode || order.RobotIds==null || prior.RobotIds==null){orders.RemoveAt(i);continue;}
                var remaining=prior.RobotIds.Except(order.RobotIds).ToArray();
                prior=prior with {RobotIds=remaining,Fighters=remaining.Length,Queen=prior.Queen && !order.Queen};
                if(remaining.Length==0 && !prior.Queen)orders.RemoveAt(i);else orders[i]=prior;
            }
        }
        Update(_partyOrders);Update(_plannedOrders);_plannedOrders.Add(order);Report=$"予約 {_plannedOrders.Count} 件 / ターン終了で実行";
    }
    public int[] Submitted=>_submitted.OrderBy(r=>r).ToArray();
    public bool AwaitingPlayers=>_submitted.Count>0;
    public void Reset(){Report="";Battles.Clear();_partyOrders.Clear();_plannedOrders.Clear();_submitted.Clear();RobotBattles.Reset();}
    public void Advance(WorldSetup world,int targetNode,long fighters,bool enemies=true,bool automaticAll=false)
    {
        world.Robots.PersuadeAtHome(world);
        Battles.Clear();
        var orders=_plannedOrders.Where(o=>automaticAll || o.Ruler==world.PlayerSlot || world.HumanPlayers.Count==0 && !o.Queen).ToList();
        if(automaticAll){orders.AddRange(_partyOrders);_partyOrders.Clear();_submitted.Clear();}
        if(targetNode>=0)
        {
            if(world.Routes.ToNode(world.PlayerSlot,targetNode)==null || UseRobotCombat && !RobotBattles.CanMarch(world,world.PlayerSlot,targetNode)){Report="交通路がない、または戦闘中のため移動できない";return;}
            orders.Add(new(world.PlayerSlot,targetNode,fighters));
        }
        _plannedOrders.RemoveAll(o=>orders.Contains(o));
        if(enemies)
        {
            var party=world.ManualRulers();
            if(party.Length>1 && !automaticAll)
            {
                _submitted.Add(world.PlayerSlot);_partyOrders.AddRange(orders);
                int next=party.FirstOrDefault(r=>!_submitted.Contains(r),-1);
                if(next>=0){world.SelectTurnRuler(next);Report="次の担当の行動を選択 / 全員の行動後にターン確定";return;}
                orders=_partyOrders.ToList();_partyOrders.Clear();_submitted.Clear();world.SelectTurnRuler(party[0]);
            }
            var random=new SeedRandom(world.PlacementSeed^world.Population.Turn^0x57421);
            for(int ruler=0;ruler<world.ActiveCount;ruler++)
            {
                if(!automaticAll && world.ManualRulers().Contains(ruler) || !world.Relations.CanAct(ruler))continue;
                var home=world.Nodes.Current(ruler);
                long army=UseRobotCombat?RobotBattles.Available(world,ruler):home!=null && world.Relations.Allied(home.Owner,ruler)?home.Population.People[0]/2:0;
                var move=world.Routes.NodeChoices(ruler).Where(path=>!UseRobotCombat || RobotBattles.CanMarch(world,ruler,world.Nodes.At(path.Target,path.Entry).Id)).OrderBy(path=>
                {
                    var target=world.Nodes.At(path.Target,path.Entry);
                    return world.TerritoryCounts[ruler]==0?random.Next(100):
                        home==null || home.Owner!=ruler?(target.Owner==ruler?0:100):
                        target.Owner!=ruler && target.Population.People[0]<army?0:50+random.Next(50);
                }).FirstOrDefault();
                                if(move!=null && !orders.Any(o=>o.Ruler==ruler && (o.Queen || o.SourceNode==home?.Id)))
                {
                    int target=world.Nodes.At(move.Target,move.Entry).Id;
                    bool nodeAttack=home?.Owner==ruler && world.Nodes.All[target].Owner>=0 && !world.Relations.Allied(ruler,world.Nodes.All[target].Owner);
                    orders.Add(nodeAttack?new(ruler,target,army,home.Id,false):world.Nodes.All[target].Owner<0 && world.TerritoryCounts[ruler]>0?new(ruler,target,0):new(ruler,target,0,PocketOnly:true,Peaceful:true));
                }
                if(UseRobotCombat)
                foreach(var node in world.Nodes.All.Where(n=>n.Owner==ruler && !orders.Any(o=>o.Ruler==ruler && (o.SourceNode==n.Id || o.Queen && home?.Id==n.Id))))
                {
                    int available=RobotBattles.AvailableFrom(world,ruler,node.Id);if(available==0 || random.Next(100)>=35)continue;
                    var targets=world.Routes.Neighbors(node).Select(r=>world.Nodes.At(r.Target,r.Entry)).Where(n=>n.Owner>=0 && !world.Relations.Allied(ruler,n.Owner) && RobotBattles.CanAttackFrom(world,ruler,node.Id,n.Id)).ToArray();
                    if(targets.Length>0)orders.Add(new(ruler,targets[random.Next(targets.Length)].Id,available,node.Id,false));
                }
            }
        }
        for(int i=orders.Count-1;i>=0;i--)if(orders[i].RobotIds!=null)
        {
            var order=orders[i];int home=order.SourceNode>=0?order.SourceNode:world.Nodes.Current(order.Ruler)?.Id??-1;
            var available=RobotBattles.SelectableRobots(world,order.Ruler,home,order.PocketOnly).Select(r=>r.Id).ToHashSet();
            var ids=order.RobotIds.Where(available.Contains).Distinct().ToArray();
            if(ids.Length==0 && !order.Queen)orders.RemoveAt(i);else orders[i]=order with {RobotIds=ids,Fighters=ids.Length};
        }
        Resolve(world,orders);
    }
    public void Resolve(WorldSetup world,IReadOnlyList<March> orders)
    {
        if(UseRobotCombat)
        {
            RobotBattles.Advance(world,orders);
            Report=$"ロボット輸送 {world.Robots.Transport.LastTransfers.Count} / 観戦 {RobotBattles.Scenes.Count} / 継戦 {RobotBattles.Encounters.Count(e=>!e.Finished)}";
            return;
        }
        if(orders.Select(o=>o.Ruler).Distinct().Count()!=orders.Count)throw new ArgumentException("One march per ruler per turn.");
        foreach(var order in orders)if(order.Ruler<0 || order.Ruler>=world.ActiveCount || order.TargetNode<0 || order.TargetNode>=world.Nodes.All.Length || order.Fighters<0)throw new ArgumentOutOfRangeException(nameof(orders));
        if(orders.Any(o=>!world.Relations.CanAct(o.Ruler)))throw new InvalidOperationException("Inactive conqueror cannot march.");
        var routes=orders.ToDictionary(o=>o.Ruler,o=>world.Routes.ToNode(o.Ruler,o.TargetNode));
        if(routes.Values.Any(r=>r==null))throw new InvalidOperationException("March has no reachable terrain route.");
        var arrivals=new Dictionary<int,Dictionary<int,long>>();
        var previousFull=world.Cells.Select(c=>world.Nodes.FullOwner(c.Id)).ToArray();
        var sources=orders.ToDictionary(o=>o.Ruler,o=>world.Nodes.Current(o.Ruler)?.Id??-1);
        Battles.Clear();
        var armies=new Dictionary<int,long>();
        foreach(var order in orders)
        {
            var source=world.Nodes.Current(order.Ruler);
            long available=source!=null && world.Relations.Allied(source.Owner,order.Ruler)?source.Population.People[0]:0;
            long army=Math.Min(order.Fighters,available);
            if(source!=null)source.Population.People[0]-=army;
            armies[order.Ruler]=army;
        }
        var stopped=new HashSet<int>();
        foreach(var first in orders.OrderBy(o=>o.Ruler))
        {
            var second=orders.FirstOrDefault(o=>o.Ruler>first.Ruler && sources[o.Ruler]==first.TargetNode && o.TargetNode==sources[first.Ruler]);
            if(second==null || world.Relations.Allied(first.Ruler,second.Ruler) || stopped.Contains(first.Ruler) || stopped.Contains(second.Ruler) || armies[first.Ruler]==0 || armies[second.Ruler]==0)continue;
            long a=armies[first.Ruler],b=armies[second.Ruler],loss=Math.Min(a,b);
            armies[first.Ruler]-=loss;armies[second.Ruler]-=loss;
            Battles.Add(new(BattleKind.Edge,first.Ruler,second.Ruler,sources[first.Ruler],first.TargetNode,a,b,a-loss,b-loss));
            if(a<=b)stopped.Add(first.Ruler);
            if(b<=a)stopped.Add(second.Ruler);
        }
        foreach(var order in orders)
        {
            if(stopped.Contains(order.Ruler))continue;
            long army=armies[order.Ruler];
            int target=routes[order.Ruler].Target;
            world.ConquerorLocations[order.Ruler]=target;
            world.ConquerorPoints[order.Ruler]=routes[order.Ruler].Entry;
            int post=world.Nodes.At(target,routes[order.Ruler].Entry).Id;
            if(!arrivals.TryGetValue(post,out var forces))arrivals[post]=forces=new();
            forces[order.Ruler]=army;
        }
        world.Population.Advance(world);
        world.Industry.Queue(world.Population.Turn);
        int conquests=0;
        foreach(var entry in arrivals.OrderBy(e=>e.Key))
        {
            var post=world.Nodes.All[entry.Key];int cell=post.Cell,defender=post.Owner;var forces=entry.Value;
            long neutralResidents=defender<0?post.Population.People[0]:0;
            if(defender>=0)forces[defender]=forces.GetValueOrDefault(defender)+post.Population.People[0];
            var survivors=forces.GroupBy(f=>world.Relations.Root(f.Key)).ToDictionary(g=>g.Any(f=>f.Key==defender)?defender:g.Min(f=>f.Key),g=>g.Sum(f=>f.Value));
            while(survivors.Count(f=>f.Value>0)>1)
            {
                var pair=survivors.Where(f=>f.Value>0).OrderBy(f=>f.Key==defender?0:1).ThenBy(f=>f.Key).Take(2).ToArray();
                long loss=Math.Min(pair[0].Value,pair[1].Value);
                survivors[pair[0].Key]-=loss;survivors[pair[1].Key]-=loss;
                Battles.Add(new(BattleKind.Node,pair[0].Key,pair[1].Key,entry.Key,entry.Key,pair[0].Value,pair[1].Value,survivors[pair[0].Key],survivors[pair[1].Key]));
            }
            long largest=survivors.Count==0?0:survivors.Values.Max();
            int winner=largest>0?survivors.Where(f=>f.Value==largest).OrderBy(f=>f.Key).First().Key:defender<0?forces.Keys.Min():defender;
            post.Population.People[0]=survivors.GetValueOrDefault(winner)+neutralResidents;
            if(winner!=defender)
            {
                post.Owner=winner;
                conquests++;
                Array.Clear(post.Population.Conversion);Array.Clear(post.Population.Migration);
            }
            if(largest>0 || defender<0)
            foreach(int ruler in Enumerable.Range(0,world.ActiveCount))
                if(world.Nodes.Current(ruler).Id==post.Id && !world.Relations.Allied(ruler,winner))
                    world.Relations.Capture(world,ruler,winner,post.Id);
        }
        world.Nodes.Recount();
        string result=orders.Any(o=>o.Ruler==world.PlayerSlot)?
            world.TerritoryCounts[world.PlayerSlot]==0?"あなたは放浪者":world.Nodes.Current(world.PlayerSlot)?.Owner==world.PlayerSlot?"あなたの現在地は自国地点":"あなたの現在地は他国地点":"あなたは待機";
        int cellsWon=world.Cells.Count(c=>world.Nodes.FullOwner(c.Id)>=0 && world.Nodes.FullOwner(c.Id)!=previousFull[c.Id]);
        Report=$"{result} / 戦闘 {Battles.Count} 件 / 地点確保 {conquests} / セル征服 {cellsWon}";
    }
}

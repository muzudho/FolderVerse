namespace FolderVerse;

using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct TradeEndpoint(int Ruler,int Node,bool Pocket);
public readonly record struct TradeCargo(long Id,bool Raw);
public sealed record TradeOffer(int Turn,TradeEndpoint From,TradeEndpoint To,TradeCargo Give,TradeCargo Want);
public sealed record TradeReply(TradeOffer Offer,string Result);

public sealed class TradePlan
{
    public List<TradeOffer> Offers {get;}=new();
    public List<TradeReply> Replies {get;}=new();

    public IEnumerable<TradeEndpoint> Partners(WorldSetup world,TradeEndpoint from)
    {
        if(!Available(world,from))yield break;
        if(!from.Pocket)
        {
            foreach(int id in world.Routes.Neighbors(world.Nodes.All[from.Node])
                .Select(route=>world.Nodes.At(route.Target,route.Entry)?.Id??-1).Distinct())
            {
                if(id<0 || id==from.Node)continue;
                var to=new TradeEndpoint(world.Nodes.All[id].Owner,id,false);
                if(to.Ruler!=from.Ruler && Available(world,to))yield return to;
            }
            yield break;
        }

        var local=world.Nodes.All[from.Node];
        var site=new TradeEndpoint(local.Owner,local.Id,false);
        if(Available(world,site))yield return site;
        for(int ruler=0;ruler<world.ActiveCount;ruler++)
        {
            var visitor=new TradeEndpoint(ruler,from.Node,true);
            if(ruler!=from.Ruler && Available(world,visitor))yield return visitor;
        }
    }

    private static bool Available(WorldSetup world,TradeEndpoint endpoint)
    {
        if(endpoint.Ruler<0 || endpoint.Ruler>=world.ActiveCount || endpoint.Node<0 ||
            endpoint.Node>=world.Nodes.All.Length || !world.Relations.CanAct(endpoint.Ruler))return false;
        var node=world.Nodes.All[endpoint.Node];
        return node.Functional && !world.Campaign.RobotBattles.LockedNode(node.Id) &&
            (endpoint.Pocket?world.Nodes.Current(endpoint.Ruler)?.Id==node.Id:node.Owner==endpoint.Ruler);
    }

    private static RobotStore Store(WorldSetup world,TradeEndpoint endpoint)=>
        endpoint.Pocket?world.Robots.Retinues[endpoint.Ruler]:world.Robots.Nodes[endpoint.Node];

    public IEnumerable<TradeCargo> Cargo(WorldSetup world,TradeEndpoint endpoint)
    {
        if(!Available(world,endpoint))yield break;
        var store=Store(world,endpoint);
        foreach(var robot in store.Robots)
            if(robot.Owner==endpoint.Ruler && !robot.Disposal && !robot.Stationed &&
                (endpoint.Pocket
                    ? !world.Campaign.PlannedOrders.Any(order=>order.Ruler==endpoint.Ruler && order.Queen &&
                        (order.PocketOnly || order.RobotIds?.Contains(robot.Id)==true))
                    : world.Robots.CanShip(endpoint.Node,robot)))
                yield return new(robot.Id,false);
        foreach(var pack in store.MaplePackages)
            if(!world.Robots.Discarding.Reserved(pack.Id) && (endpoint.Pocket || !world.Robots.MapleShipments.Any(order=>order.Source==endpoint.Node && order.PackageId==pack.Id)))
                yield return new(pack.Id,true);
    }

    public bool Book(WorldSetup world,TradeEndpoint from,TradeEndpoint to,TradeCargo give,TradeCargo want)
        =>BookCore(world,from,to,give,want,false);

    internal bool BookComputer(WorldSetup world,TradeEndpoint from,TradeEndpoint to,TradeCargo give,TradeCargo want)
        =>BookCore(world,from,to,give,want,true);

    private bool BookCore(WorldSetup world,TradeEndpoint from,TradeEndpoint to,TradeCargo give,TradeCargo want,bool computer)
    {
        if(world.Industry.Pending || world.Campaign.Phase!=CampaignPhase.Domestic || !computer && !world.Relations.Controls(from.Ruler) ||
            !Partners(world,from).Contains(to) || !Cargo(world,from).Contains(give) || !Cargo(world,to).Contains(want) ||
            Offers.Any(offer=>offer.From==from && offer.Give==give))return false;
        Offers.Add(new(world.Population.Turn,from,to,give,want));
        return true;
    }

    public bool Possible(WorldSetup world,TradeOffer offer)=>
        Partners(world,offer.From).Contains(offer.To) &&
        Cargo(world,offer.From).Contains(offer.Give) && Cargo(world,offer.To).Contains(offer.Want);

    internal Robot Robot(WorldSetup world,TradeEndpoint endpoint,TradeCargo cargo)=>
        cargo.Raw?null:Store(world,endpoint).Robots.FirstOrDefault(r=>r.Id==cargo.Id);

    internal MaplePackage Package(WorldSetup world,TradeEndpoint endpoint,TradeCargo cargo)=>
        cargo.Raw?Store(world,endpoint).MaplePackages.FirstOrDefault(p=>p.Id==cargo.Id):null;

    internal bool ComputerAccepts(WorldSetup world,TradeOffer offer)
    {
        // A first complete robot can turn a weak neighbor into an immediate threat.
        var wanted=Robot(world,offer.To,offer.Want);
        if(wanted?.Parts==RobotParts.Complete && !world.Relations.Allied(offer.From.Ruler,offer.To.Ruler) &&
            !Store(world,offer.From).Robots.Any(r=>r.Parts==RobotParts.Complete && !r.Stationed))return false;
        return ComputerTrade.Value(world,offer.To,offer.From,offer.Give)>=
            ComputerTrade.Value(world,offer.To,offer.To,offer.Want);
    }

    internal void ResolveComputerReplies(WorldSetup world)
    {
        if(world.Industry.Pending)return;
        foreach(var offer in Offers.Where(o=>o.Turn<world.Population.Turn).ToArray())
        {
            if(Possible(world,offer) && world.ControllerOf(offer.To.Ruler)>=0)continue;
            Replies.Add(new(offer,Answer(world,offer,Possible(world,offer) && ComputerAccepts(world,offer))));
        }
        if(world.ComputerOnly && Replies.Count>200)Replies.RemoveRange(0,Replies.Count-200);
    }

    public string Answer(WorldSetup world,TradeOffer offer,bool accept)
    {
        if(world.Industry.Pending || world.Population.Turn<=offer.Turn || !Offers.Remove(offer))return "返事待ち";
        if(!Possible(world,offer))return "不成立 / 相手・経路・荷が変わりました";
        if(!accept)return "辞退";

        var left=Store(world,offer.From);
        var right=Store(world,offer.To);
        var leftRobot=Robot(world,offer.From,offer.Give);
        var rightRobot=Robot(world,offer.To,offer.Want);
        var leftRaw=Package(world,offer.From,offer.Give);
        var rightRaw=Package(world,offer.To,offer.Want);
        var leftRobots=left.Robots.Where(r=>r.Id!=leftRobot?.Id).ToList();
        var rightRobots=right.Robots.Where(r=>r.Id!=rightRobot?.Id).ToList();
        if(rightRobot!=null)leftRobots.Add(rightRobot.Materialize() with {Owner=offer.From.Ruler});
        if(leftRobot!=null)rightRobots.Add(leftRobot.Materialize() with {Owner=offer.To.Ruler});
        var leftPackages=left.MaplePackages.Where(p=>p.Id!=leftRaw?.Id).ToList();
        var rightPackages=right.MaplePackages.Where(p=>p.Id!=rightRaw?.Id).ToList();
        if(rightRaw!=null)leftPackages.Add(ScrapCargo.Arrive(rightRaw,offer.From.Pocket?-1:offer.From.Node,world.Population.Turn));
        if(leftRaw!=null)rightPackages.Add(ScrapCargo.Arrive(leftRaw,offer.To.Pocket?-1:offer.To.Node,world.Population.Turn));

        int Reserved(TradeEndpoint endpoint,RobotStore store)=>store.ReservedReturns+(endpoint.Pocket?0:
            world.Robots.ProductionIntakes.Count(intake=>(intake.Target??intake.Source)==endpoint.Node));
        if(!left.Fits(leftRobots,leftPackages,Reserved(offer.From,left)) ||
            !right.Fits(rightRobots,rightPackages,Reserved(offer.To,right)))return "不成立 / 格納庫の空き不足";

        // Both inventories have been checked with the outgoing cargo already removed.
        left.SetMaplePackages(Array.Empty<MaplePackage>());
        right.SetMaplePackages(Array.Empty<MaplePackage>());
        left.Replace(leftRobots);
        right.Replace(rightRobots);
        left.SetMaplePackages(leftPackages.ToArray());
        right.SetMaplePackages(rightPackages.ToArray());
        return "成立";
    }
}

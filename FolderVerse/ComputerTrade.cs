namespace FolderVerse;
using System;
using System.Linq;

internal static class ComputerTrade
{
    internal static int Value(WorldSetup world,TradeEndpoint recipient,TradeEndpoint source,TradeCargo cargo)
    {
        var trade=world.Campaign.Trade;
        var store=recipient.Pocket?world.Robots.Retinues[recipient.Ruler]:world.Robots.Nodes[recipient.Node];
        var robot=trade.Robot(world,source,cargo);
        if(robot!=null)
        {
            int value=SalvagePlan.PartCount(robot.Parts)*16;
            if(robot.Parts==RobotParts.Complete)return value+32;
            bool completes=store.Robots.Any(other=>other.Id!=robot.Id && !other.Stationed && !other.Disposal &&
                (other.Parts&robot.Parts)==0 && (other.Parts|robot.Parts)==RobotParts.Complete);
            return value+(completes?48:0);
        }
        var raw=trade.Package(world,source,cargo);
        if(raw==null)return 0;
        bool needed=!recipient.Pocket && world.Robots.Workshops[recipient.Node].ProductionLines.Any(line=>
            line.Manufacturing && !line.Paused && !line.CycleFinished &&
            RobotWorld.RawCost(line.CurrentProductionPlan.Preview(recipient.Ruler),raw.Material)>
                world.Robots.AvailableRaw(recipient.Node,raw.Material));
        return raw.Units+(needed?32:0);
    }

    internal static void Plan(WorldSetup world,bool automaticAll)
    {
        if(world.Industry.Pending || world.TutorialStep!=0)return;
        var trade=world.Campaign.Trade;
        var manual=world.ManualRulers().ToHashSet();
        foreach(int ruler in WorldDayCycle.Order(world,Enumerable.Range(0,world.ActiveCount)))
        {
            if(!world.Relations.CanAct(ruler) || !automaticAll && manual.Contains(ruler) ||
                trade.Offers.Any(o=>o.From.Ruler==ruler))continue;
            var sources=world.Nodes.All.Where(n=>n.Owner==ruler && n.Functional)
                .Select(n=>new TradeEndpoint(ruler,n.Id,false));
            var current=world.Nodes.Current(ruler);
            if(current!=null)sources=sources.Prepend(new(ruler,current.Id,true));
            TradeOffer best=null;
            int bestGain=0;
            foreach(var from in sources)
            {
                var gives=trade.Cargo(world,from).Where(g=>trade.Robot(world,from,g)?.Parts!=RobotParts.Complete)
                    .Where(g=>!trade.Offers.Any(o=>o.From==from && o.Give==g || o.To==from && o.Want==g)).ToArray();
                foreach(var to in trade.Partners(world,from).Where(to=>to.Ruler!=ruler))
                foreach(var want in trade.Cargo(world,to))
                {
                    if(trade.Offers.Any(o=>o.To==to && o.Want==want || o.From==to && o.Give==want))continue;
                    foreach(var give in gives)
                    {
                        int gain=Value(world,from,to,want)-Value(world,from,from,give);
                        if(gain<=bestGain)continue;
                        var offer=new TradeOffer(world.Population.Turn,from,to,give,want);
                        if(!trade.ComputerAccepts(world,offer))continue;
                        best=offer;bestGain=gain;
                    }
                }
            }
            if(best!=null)trade.BookComputer(world,best.From,best.To,best.Give,best.Want);
        }
    }
}

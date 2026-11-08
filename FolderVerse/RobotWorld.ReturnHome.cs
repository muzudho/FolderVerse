namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record ReturningRobot(Robot Robot,int Node,int Country,int Source,int Commander,string Status="帰国中");
public sealed partial class RobotWorld
{
    public List<ReturningRobot> ReturningRobots {get;}=new();
    public const string NoHomeCountry="帰る国がもうありません。";
    private static readonly RobotParts[] HomeParts={RobotParts.Head,RobotParts.Body,RobotParts.Legs};
    public bool CanReturnHome(WorldSetup world,int node,Robot robot,int commander)=>CanReturnHomeCore(world,node,robot,commander,true);
    private bool CanReturnHomeCore(WorldSetup world,int node,Robot robot,int commander,bool authority)
        =>node>=0 && node<Nodes.Length && robot!=null && commander>=0 && commander<world.ActiveCount &&
          robot.Parts==RobotParts.Complete && !robot.Disposal &&
          HomeParts.All(p=>robot.Origin(p)!=commander) && !AssemblyLocked(node,robot) &&
          world.Campaign.Phase==CampaignPhase.Domestic && world.Nodes.All[node].Owner>=0 && (authority?world.Relations.Controls(world.Nodes.All[node].Owner):world.Nodes.All[node].Owner==commander) && !world.Campaign.RobotBattles.LockedNode(node);

    public bool ToggleReturnHome(WorldSetup world,long id,int commander,out string message)
    {
        int node=Array.FindIndex(Nodes,s=>s.Robots.Any(r=>r.Id==id));
        var robot=node<0?null:Nodes[node].Robots.Single(r=>r.Id==id);
        if(robot?.ReturnHomeCommander==commander){
            Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==id?r with {ReturnHomeCommander=null,ReturnHomeStatus=null}:r));
            PartPlanning.Invalidate();message="国へ帰らせる予約を取り消しました";return true;
        }
        if(!CanReturnHome(world,node,robot,commander)){message="自国パーツを含まない、完全体を選んでください。ほかの予約中は使えません。";return false;}
        string status=HomeCountries(world,robot).Length==0?NoHomeCountry:"国へ帰る予約";
        Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==id?r with {ReturnHomeCommander=commander,ReturnHomeStatus=status}:r));
        PartPlanning.Invalidate();message=status;return true;
    }
    internal bool ReserveComputerReturnHome(WorldSetup world,int node,Robot robot,int owner)
    {
        if(!CanReturnHomeCore(world,node,robot,owner,false) || HomeCountries(world,robot).Length==0)return false;
        Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {ReturnHomeCommander=owner,ReturnHomeStatus="国へ帰る予約"}:r));PartPlanning.Invalidate();return true;
    }
    private static int[] HomeCountries(WorldSetup world,Robot robot)
    {
        var groups=HomeParts.Select(robot.Origin).Where(c=>c>=0 && c<world.ActiveCount && world.Relations.Powered[c] &&
            world.Nodes.All.Any(n=>n.Functional && n.Owner==c)).GroupBy(c=>c).ToArray();
        return groups.Length==0?Array.Empty<int>():groups.Where(g=>g.Count()==groups.Max(g=>g.Count())).Select(g=>g.Key).OrderBy(c=>c).ToArray();
    }
    // Unlike ordinary delivery, repatriation may pass through foreign territory.
    private static int[] HomeNeighbors(WorldSetup world,int source)
    {
        var seen=new HashSet<int>{source};var queue=new Queue<int>();var result=new HashSet<int>();queue.Enqueue(source);
        var battles=world.Campaign.RobotBattles;
        while(queue.TryDequeue(out int at))foreach(var route in world.Routes.Neighbors(world.Nodes.All[at])){
            int next=world.Nodes.At(route.Target,route.Entry).Id;
            if(battles.LockedNode(next) || battles.LockedEdge(at,next) || !seen.Add(next))continue;
            if(world.Nodes.All[next].Functional)result.Add(next);else queue.Enqueue(next);
        }
        return result.OrderBy(n=>n).ToArray();
    }
    private static (int Country,int Next)? HomeStep(WorldSetup world,int start,int[] countries,long id)
    {
        if(countries.Length==0)return null;
        var seen=new HashSet<int>{start};var queue=new Queue<(int Node,int First,int Distance)>();queue.Enqueue((start,start,0));
        var choices=new List<(int Country,int Next)>();int best=int.MaxValue;
        while(queue.TryDequeue(out var at)){
            if(at.Distance>best)break;
            int owner=world.Nodes.All[at.Node].Owner;
            if(world.Nodes.All[at.Node].Functional && countries.Contains(owner)){best=at.Distance;choices.Add((owner,at.First));continue;}
            if(at.Distance>=best)continue;
            foreach(int next in HomeNeighbors(world,at.Node))if(seen.Add(next))queue.Enqueue((next,at.Distance==0?next:at.First,at.Distance+1));
        }
        var options=choices.GroupBy(c=>c.Country).Select(g=>g.First()).OrderBy(c=>c.Country).ToArray();
        return options.Length==0?null:options[new SeedRandom(unchecked((int)id^world.PlacementSeed^world.Population.Turn)).Next(options.Length)];
    }
    internal void AdvanceReturnHome(WorldSetup world)
    {
        var moved=new List<(RobotShipment Shipment,int Owner)>();
        // Existing travellers move first. New departures get exactly one hop too.
        foreach(var journey in ReturningRobots.ToArray()){
            ReturningRobots.Remove(journey);MoveHome(world,journey,moved);
        }
        for(int node=0;node<Nodes.Length;node++)foreach(var robot in Nodes[node].Robots.Where(r=>r.ReturnHomeCommander.HasValue).ToArray()){
            if(world.Campaign.RobotBattles.LockedNode(node))continue;
            int commander=robot.ReturnHomeCommander.Value;
            var candidates=HomeCountries(world,robot);
            var step=HomeStep(world,node,candidates,robot.Id);
            if(step==null){
                string status=candidates.Length==0?NoHomeCountry:"帰国経路が通れないため待機しています。";
                Nodes[node].Replace(Nodes[node].Robots.Select(r=>r.Id==robot.Id?r with {ReturnHomeStatus=status}:r));continue;
            }
            Nodes[node].Remove(robot.Id);
            MoveHome(world,new(robot.Materialize(),node,step.Value.Country,node,commander),moved);
        }
        Transport.AppendPlannedTransfers(moved);PartPlanning.Invalidate();Check();
    }
    private void MoveHome(WorldSetup world,ReturningRobot journey,List<(RobotShipment Shipment,int Owner)> moved)
    {
        if(world.Campaign.RobotBattles.LockedNode(journey.Node)){ReturningRobots.Add(journey with {Status="経路上の戦闘が終わるまで待機"});return;}
        var countries=HomeCountries(world,journey.Robot);
        int[] targets=world.Relations.Powered[journey.Country] && world.Nodes.All.Any(n=>n.Functional && n.Owner==journey.Country)?new[]{journey.Country}:countries;
        var step=HomeStep(world,journey.Node,targets,journey.Robot.Id);
        if(step==null){ReturningRobots.Add(journey with {Status=targets.Length==0?NoHomeCountry:"帰国経路が通れないため待機しています。"});return;}
        int next=step.Value.Next,country=step.Value.Country;
        if(next!=journey.Node)moved.Add((new(journey.Node,next,journey.Robot.Id,journey.Robot.Parts),country));
        if(world.Nodes.All[next].Owner==country && Nodes[next].HasSpace && !world.Campaign.RobotBattles.LockedNode(next)){
            Nodes[next].Add(journey.Robot with {Owner=country,ReturnHomeCommander=null,ReturnHomeStatus=null,FunctionDisplayNode=null});return;
        }
        ReturningRobots.Add(journey with {Node=next,Country=country,Status=world.Nodes.All[next].Owner==country?"帰国先の格納庫が満杯のため到着待ち":"帰国中"});
    }
}

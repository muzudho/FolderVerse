namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;

public sealed record DomesticPersuasion(int Ruler,int Node,Robot Before,Robot After,bool Success);
public sealed partial class RobotWorld
{
    public static int DomesticPersuasionPercent(Robot robot,int ruler,int configuredPercent=5)
        // Preserve explicit simulation/test overrides; the ordinary rule uses the four affiliation tiers.
        =>configuredPercent!=5?configuredPercent:robot.NativeParts(ruler) switch {0=>5,1=>35,2=>70,_=>95};
    public Dictionary<int,int> DomesticPersuasionTurns {get;}=new();
    public List<DomesticPersuasion> PersuadeAtHome(WorldSetup world)
    {
        RevalidateWorkReservations();
        var events=new List<DomesticPersuasion>();int turn=world.Population.Turn;
        foreach(int ruler in Enumerable.Range(0,world.ActiveCount).OrderBy(r=>r==world.PlayerSlot?0:1).ThenBy(r=>r))
        {
            if(DomesticPersuasionTurns.TryGetValue(ruler,out int previous) && previous==turn)continue;
            DomesticPersuasionTurns[ruler]=turn;
            if(!world.Relations.CanAct(ruler))continue;
            var node=world.Nodes.Current(ruler);if(node==null || world.Campaign.RobotBattles.LockedNode(node.Id))continue;
            var store=Nodes[node.Id];var random=new SeedRandom(world.PlacementSeed^turn*7919^ruler*104729^0x504552);
            foreach(var robot in store.Robots.OrderBy(r=>r.Owner==ruler?1:0).ThenBy(r=>r.Id).ToArray())
            {
                var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(p=>(robot.PersuadableParts(ruler)&p)!=0).ToArray();
                int work=WorkCountry(node.Id,robot);bool foreignShoes=work>=0 && work!=ruler;
                bool canEnsureNative=robot.NativeParts(ruler)>0 || parts.Length>0;
                if(!canEnsureNative || parts.Length==0 && !foreignShoes || robot.LastDomesticPersuasionTurn==turn || robot.Disposal || Workshops[node.Id].DisposalTarget==robot.Id || PendingEdits.Any(e=>e.Ids.Contains(robot.Id)))continue;
                bool success=random.Next(100)<DomesticPersuasionPercent(robot,ruler,world.Campaign.RobotBattles.Rules.PersuasionPercent);
                var after=robot;
                if(success){
                    after=parts.Length>0?robot.Persuaded(ruler,parts[random.Next(parts.Length)],true):robot.Materialize() with {Owner=ruler};
                    CancelRobotWorkOrders(robot);after=Barefoot(after);
                }
                after=after with {LastDomesticPersuasionTurn=turn};
                if(success)CancelChangedDeliveries(node.Id,new[]{robot},store.Robots.Select(r=>r.Id==robot.Id?after:r));
                store.Replace(store.Robots.Select(r=>r.Id==robot.Id?after:r));
                events.Add(new(ruler,node.Id,robot,after,success));
            }
            SyncWorkers(node.Id);
        }
        return events;
    }
}

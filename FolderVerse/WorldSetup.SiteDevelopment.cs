namespace FolderVerse;
using System.Linq;

public sealed partial class WorldSetup
{
    // Older saves keep their capacity rules; enabled when a new world completes its setup.
    public bool RelayLogisticsEnabled {get;private set;}
    private void InitializeSitePurposes(int seed)
    {
        foreach(var node in Nodes.All.Where(n=>n.Functional)){
            if(!node.IsHarbor && !Routes.NearWhirlpool(node)){
                int choice=new SeedRandom(unchecked(seed^node.Id*13007^0x3915)).Next(10);
                node.Purpose=choice switch{0=>NodePurpose.Relay,1 or 2=>NodePurpose.Defense,3=>NodePurpose.Frontline,
                    4 or 5=>NodePurpose.Manufacturing,6 when RobotWorld.RawMaterials.Any(m=>Robots.RawDeposit(node.Id,m)>0)=>NodePurpose.Harvest,_=>NodePurpose.General};
                // A starting freight depot must not strand an untouched resource deposit.
                if(node.Purpose==NodePurpose.Relay && RobotWorld.RawMaterials.Any(m=>Robots.RawDeposit(node.Id,m)>0))node.Purpose=NodePurpose.Harvest;
            }
            var store=Robots.Nodes[node.Id];store.SetLogisticsRules(node.Purpose==NodePurpose.Relay);
            if(node.Purpose==NodePurpose.Relay){Robots.Workshops[node.Id].RemoveProductionLines();Robots.Workshops[node.Id].LimitLines(0);Robots.Workshops[node.Id].Automation=NodeAutomation.Auto;}
        }
    }
}

public static class FacilityDevelopment
{
    public static int Grade(WorldSetup world,Node node)
    {
        if(node.Id<0 || node.Id>=world.Robots.Nodes.Length)return 0;
        int capacity=world.Robots.Nodes[node.Id].TotalCapacity;
        return node.Purpose==NodePurpose.Relay?System.Math.Min(5,capacity/10):System.Math.Min(2,capacity/10);
    }
}

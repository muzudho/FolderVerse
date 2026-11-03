namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;

public sealed partial class RobotWorld
{
    public Dictionary<int,List<Robot>> FunctionDisplayGroups(WorldSetup w)
    {
        var groups=new Dictionary<int,List<Robot>>();
        for(int host=0;host<Nodes.Length;host++)foreach(var robot in Nodes[host].Robots){
            int at=host;
            if(robot.FunctionDisplayNode is int origin && origin>=0 && origin<w.Nodes.All.Length &&
                (origin==host || w.Nodes.All[origin].FunctionHost==host) && w.Relations.Allied(w.Nodes.All[host].Owner,w.Nodes.All[origin].Owner))at=origin;
            if(!groups.TryGetValue(at,out var list))groups[at]=list=new();list.Add(robot);
        }
        return groups;
    }
}

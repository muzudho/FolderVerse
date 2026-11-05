namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed partial class RobotCampaign
{
    // Only empty settlements can be crossed without ending the march.
    private Dictionary<int,int> MarchRoutes(WorldSetup world,int source)
    {
        var previous=new Dictionary<int,int>();
        if(source<0 || source>=world.Nodes.All.Length || LockedNode(source))return previous;
        previous.Add(source,-1);
        var queue=new Queue<int>();
        queue.Enqueue(source);
        while(queue.TryDequeue(out int from))
            foreach(var route in world.Routes.Neighbors(world.Nodes.All[from]))
            {
                int to=world.Nodes.At(route.Target,route.Entry).Id;
                if(LockedEdge(from,to) || !previous.TryAdd(to,from))continue;
                if(!world.Nodes.All[to].Functional && !LockedNode(to))queue.Enqueue(to);
            }
        return previous;
    }

    public IEnumerable<int> MarchTargets(WorldSetup world,int source)=>
        MarchRoutes(world,source).Keys.Where(id=>id!=source && world.Nodes.All[id].Functional && !LockedEdge(source,id));

    private int[] MarchPath(WorldSetup world,int source,int target)
    {
        if(source==target)return new[]{source};
        var previous=MarchRoutes(world,source);
        var path=new List<int>();
        for(int at=target;at>=0;at=previous[at])path.Add(at);
        path.Reverse();
        return path.ToArray();
    }
}

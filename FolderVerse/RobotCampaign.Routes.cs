namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed partial class RobotCampaign
{
    // Only empty settlements can be crossed without ending the march.
    private Dictionary<int,int> MarchRoutes(WorldSetup world,int source,ISet<int> emptied=null)
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
                if(!MarchEndpoint(world,to,emptied) && !LockedNode(to))queue.Enqueue(to);
            }
        return previous;
    }

    public IEnumerable<int> MarchTargets(WorldSetup world,int source)=>
        MarchRoutes(world,source).Keys.Where(id=>IsMarchTarget(world,source,id,null));

    private static bool MarchEndpoint(WorldSetup world,int node,ISet<int> emptied)=>
        world.Nodes.All[node].Functional && !(emptied?.Contains(node)??false);

    private bool IsMarchTarget(WorldSetup world,int source,int target,ISet<int> emptied)=>
        target!=source && MarchEndpoint(world,target,emptied) && !LockedEdge(source,target);

    internal IEnumerable<(int[] Path,bool Threat)> FunctionTransferRoutes(WorldSetup world,int source,int target)
    {
        var before=world.Campaign.FunctionTransfers.ValidSources(world).Where(id=>id!=source).ToHashSet();
        var after=new HashSet<int>(before){source};
        var frontier=MarchRoutes(world,source,after).Keys.Where(id=>MarchEndpoint(world,id,after)).ToHashSet();
        frontier.UnionWith(MarchRoutes(world,target,after).Keys.Where(id=>MarchEndpoint(world,id,after)));
        frontier.Add(target);
        foreach(int from in frontier)
        {
            var oldRoutes=MarchRoutes(world,from,before);
            var newRoutes=MarchRoutes(world,from,after);
            foreach(int to in newRoutes.Keys)
            {
                if(!IsMarchTarget(world,from,to,after))continue;
                bool newlyOpen=!oldRoutes.ContainsKey(to);
                int attacker=world.Nodes.All[from].Owner,defender=world.Nodes.All[to].Owner;
                bool threat=attacker>=0 && defender>=0 && world.Relations.Controls(defender) &&
                    !world.Relations.Allied(attacker,defender) && (to==target || newlyOpen);
                if(newlyOpen || threat)yield return (MarchPath(newRoutes,to),threat);
            }
        }
    }

    private int[] MarchPath(WorldSetup world,int source,int target)
    {
        if(source==target)return new[]{source};
        return MarchPath(MarchRoutes(world,source),target);
    }

    private static int[] MarchPath(Dictionary<int,int> previous,int target)
    {
        var path=new List<int>();
        for(int at=target;at>=0;at=previous[at])path.Add(at);
        path.Reverse();
        return path.ToArray();
    }
}

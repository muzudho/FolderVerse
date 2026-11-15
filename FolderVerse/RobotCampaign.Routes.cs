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

    public IEnumerable<int> AttackTargets(WorldSetup world,int owner,int source)
    {
        var routes=MarchRoutes(world,source);
        var adjacent=routes.Keys.Where(id=>IsMarchTarget(world,source,id,null));
        return adjacent.Concat(OutpostRoutes(world,owner,source,routes).Keys);
    }

    internal IEnumerable<int> PotentialOutpostTargets(WorldSetup world,int owner,int source)=>
        OutpostRoutes(world,owner,source,MarchRoutes(world,source),planning:true).Keys;

    private Dictionary<int,int[]> OutpostRoutes(WorldSetup world,int owner,int source,Dictionary<int,int> adjacent,bool planning=false)
    {
        var result=new Dictionary<int,int[]>();
        if(source<0 || source>=world.Nodes.All.Length || owner<0 || LockedNode(source))return result;
        var outpost=world.Nodes.All[source];
        if(!outpost.Functional || outpost.Owner!=owner || !planning && outpost.Purpose!=NodePurpose.Frontline)return result;
        foreach(int via in adjacent.Keys.Where(id=>IsMarchTarget(world,source,id,null)).OrderBy(id=>id))
        {
            var ally=world.Nodes.All[via];
            if(ally.Owner<0 || !world.Relations.Allied(owner,ally.Owner))continue;
            var onward=MarchRoutes(world,via);
            foreach(int target in onward.Keys)
            {
                if(!IsMarchTarget(world,via,target,null) || target==source || adjacent.ContainsKey(target) ||
                    LockedEdge(source,target) || result.ContainsKey(target))continue;
                int defender=world.Nodes.All[target].Owner;
                if(defender>=0 && world.Relations.Allied(owner,defender))continue;
                result.Add(target,MarchPath(adjacent,via).Concat(MarchPath(onward,target).Skip(1)).ToArray());
            }
        }
        return result;
    }

    internal int[] OutpostAttackPath(WorldSetup world,int owner,int source,int target)=>
        OutpostRoutes(world,owner,source,MarchRoutes(world,source)).GetValueOrDefault(target);

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

    private int[] AttackPath(WorldSetup world,int owner,int source,int target)
    {
        if(source==target)return new[]{source};
        var routes=MarchRoutes(world,source);
        return routes.ContainsKey(target)?MarchPath(routes,target):OutpostRoutes(world,owner,source,routes)[target];
    }

    private int[] MarchPath(WorldSetup world,int source,int target)=>
        source==target?new[]{source}:MarchPath(MarchRoutes(world,source),target);

    private static int[] MarchPath(Dictionary<int,int> previous,int target)
    {
        var path=new List<int>();
        for(int at=target;at>=0;at=previous[at])path.Add(at);
        path.Reverse();
        return path.ToArray();
    }
}

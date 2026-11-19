namespace FolderVerse;
using System.Collections.Generic;
using System.Linq;

public sealed partial class RobotCampaign
{
    private IEnumerable<Robot> InterceptRobots(WorldSetup world,int home)=>
        world.Robots.Nodes[home].Robots.Where(r=>r.InterceptOnly &&
            world.Relations.Allied(r.Owner,world.Nodes.All[home].Owner) && world.Robots.Training[r.Owner].Eligible(r) &&
            world.Robots.DirectDelivery.LockedParts(home,r.Id)==0 && !world.Robots.Workshops[home].IsDisposing(r.Id) &&
            !world.Robots.PendingEdits.Any(e=>e.Ids.Contains(r.Id)));

    private Dictionary<int,int> ReliefDistances(WorldSetup world,int home)
    {
        var result=new Dictionary<int,int>{{home,0}};
        var node=world.Nodes.All[home];
        if(node.Purpose!=NodePurpose.Frontline || LockedNode(home))return result;
        for(int depth=0;depth<2;depth++)
        foreach(int from in result.Where(pair=>pair.Value==depth).Select(pair=>pair.Key).ToArray())
        foreach(int next in MarchTargets(world,from))
            if(world.Nodes.All[next].Owner>=0 && world.Relations.Allied(node.Owner,world.Nodes.All[next].Owner))
                result.TryAdd(next,depth+1);
        return result;
    }

    private void DispatchInterceptRelief(WorldSetup world)
    {
        foreach(var origin in world.Nodes.All.Where(n=>n.Functional && n.Owner>=0).OrderBy(n=>n.Id))
        {
            if(LockedNode(origin.Id))continue;
            var distances=ReliefDistances(world,origin.Id);
            var random=new SeedRandom(world.PlacementSeed ^ world.Population.Turn ^ (origin.Id*7919));
            var robots=InterceptRobots(world,origin.Id).ToList();
            for(int i=robots.Count-1;i>0;i--){int j=random.Next(i+1);(robots[i],robots[j])=(robots[j],robots[i]);}
            foreach(var ownership in robots.GroupBy(r=>r.Owner))
            {
                var choices=new List<(RobotEncounter Encounter,int Delay)>();
                foreach(var encounter in Encounters.Where(e=>!e.Finished))
                {
                    if(encounter.WithdrawalOwners.Contains(ownership.Key) || encounter.Battles.SelectMany(b=>b.Armies).Any(a=>a.Owner==ownership.Key && a.Withdrawing))continue;
                    if(!encounter.Battles.SelectMany(b=>b.Armies).Any(a=>!world.Relations.Allied(a.Owner,ownership.Key)))continue;
                    if(encounter.Kind==BattleKind.Node){
                        if(distances.TryGetValue(encounter.Target,out int depth) && depth>0)
                            choices.Add((encounter,depth*8-4));
                    }else{
                        var depths=new[]{encounter.Source,encounter.Target}.Where(distances.ContainsKey).Select(n=>distances[n]).ToArray();
                        if(depths.Length>0)choices.Add((encounter,depths.Min()*8));
                    }
                }
                for(int i=choices.Count-1;i>0;i--){int j=random.Next(i+1);(choices[i],choices[j])=(choices[j],choices[i]);}
                if(choices.Count==0)continue;
                int index=0;
                foreach(var robot in ownership){
                    var choice=choices[index++%choices.Count];
                    Depart(world,origin.Id,robot);
                    AddReinforcement(world,choice.Encounter,robot.Owner,origin.Id,new(){robot},false);
                    var state=choice.Encounter.Battles.First(b=>b.Armies.Any(a=>a.Waiting.Any(u=>u.Id==robot.Id)));
                    var unit=state.Army(robot.Owner).Waiting.Single(u=>u.Id==robot.Id);
                    unit.ArrivalTurn=state.Turn+choice.Delay;
                    if(state.Turn==0 && choice.Delay==0)state.DeployImmediateRelief(robot.Owner);
                }
            }
        }
    }
}

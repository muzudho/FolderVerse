using FolderVerse;
using Microsoft.Xna.Framework;

static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
int maximum=0,total=0,local=0,crossFace=0,harbors=0;bool localBattle=false,remoteBattle=false,harborBattle=false;
for(int seed=0;seed<64;seed++)
{
    var world=new WorldSetup();world.SetWorld(seed);world.SetCast(123);world.SetPlacement(456);world.SelectPlayer(0);
    world.Campaign.UseRobotCombat=false; // Preserve the legacy population-combat regression fixture.
    var graph=world.Nodes.All.Select(world.Routes.Neighbors).ToArray();
    var ports=world.Nodes.All.Where(p=>p.IsHarbor).ToArray();harbors+=ports.Length;
    Check(ports.Select(p=>p.Name).Distinct().Count()==ports.Length,"Duplicate port name");
    foreach(var port in ports)
    {
        Check(port.Points.Length==1 && port.Center==port.Points[0] && port.Population.Land,"Port must be a land-side singleton Node");
        Check(graph[port.Id].Any(r=>world.Nodes.At(r.Target,r.Entry).Terrain==TravelTerrain.Sea),"Port lacks a sea connection");
    }
    foreach(var harbor in world.Cells.SelectMany(c=>world.Routes.Harbors(c.Id)))
    {
        var land=world.Routes.Land(harbor.Cell,harbor.Point)?world.Nodes.At(harbor.Cell,harbor.Point):world.Nodes.At(harbor.Target,harbor.Other);
        Check(land.IsHarbor,"Coastal connector is missing its port Node");
    }
    var names=ports.Select(p=>(p.Cell,p.Center,p.Name)).ToArray();
    var visited=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);
    while(queue.Count>0)foreach(var route in graph[queue.Dequeue()])
    {int next=world.Nodes.At(route.Target,route.Entry).Id;if(visited.Add(next))queue.Enqueue(next);}
    Check(visited.Count==world.Nodes.All.Length,$"Disconnected Node graph: seed {seed}");
    foreach(var source in world.Nodes.All)
    {
        total++;maximum=Math.Max(maximum,graph[source.Id].Length);
        var targets=new HashSet<int>();
        foreach(var route in graph[source.Id])
        {
            var target=world.Nodes.At(route.Target,route.Entry);
            Check(target.Id!=source.Id && targets.Add(target.Id),"Duplicate or self destination");
            Check(route.Entry==target.Center && route.Path[0]==source.Center,"Move must start/end at Node centres");
            Check(graph[target.Id].Any(r=>world.Nodes.At(r.Target,r.Entry).Id==source.Id),"Node connection must be reciprocal");
            Check(world.Routes.Bearing(source,route) is >=0 and <16,"Invalid bearing");
            Check(route.Path.All(p=>world.Nodes.At(source.Cell,p)?.Id==source.Id || (route.Portal==null && world.Nodes.At(source.Cell,p)?.Id==target.Id)),"Skipped an intermediate Node");
            if(route.Target==source.Cell){local++;Check(route.Portal==null,"Local move has portal");}
            else
            {
                Check(route.Portal!=null && route.Portal.Open,"Cross-cell move needs an open portal");
                Check(route.ArrivalPath.All(p=>world.Nodes.At(target.Cell,p)?.Id==target.Id),"Cross-cell move skipped a Node");
                if(world.Cells[source.Cell].Face!=world.Cells[target.Cell].Face)crossFace++;
            }
            if((route.Target==source.Cell && !localBattle)||(route.Target!=source.Cell && !remoteBattle)||(!harborBattle && target.IsHarbor))
            {
                world.ConquerorLocations[0]=source.Cell;world.ConquerorPoints[0]=source.Center;
                source.Owner=0;source.Population.People[0]=1000;target.Owner=1;target.Population.People[0]=10;
                var owners=world.Nodes.All.Select(p=>p.Owner).ToArray();int turn=world.Population.Turn;
                world.Campaign.Advance(world,target.Id,100,enemies:false);
                Check(world.Population.Turn==turn+1 && world.Nodes.Current(0).Id==target.Id && world.ConquerorPoints[0]==target.Center,"Move did not finish in target Node");
                Check(target.Owner==0,"Target battle failed");
                Check(world.Nodes.All.All(p=>p.Id==target.Id || p.Owner==owners[p.Id]),"Battle affected an unrelated Node");
                if(route.Target==source.Cell)localBattle=true;else remoteBattle=true;
                if(target.IsHarbor)harborBattle=true;
                var home=world.Nodes.Current(0);int before=world.Population.Turn;
                world.Campaign.Advance(world,home.Id,0,enemies:false);
                Check(world.Population.Turn==before,"Invalid self move advanced turn");
            }
        }
    }
    for(int turn=0;turn<3;turn++)world.Campaign.Advance(world,-1,0);
    Check(Enumerable.Range(0,world.ActiveCount).All(r=>world.ConquerorPoints[r]==world.Nodes.Current(r).Center),"Enemy failed to arrive at Node centre");
    world.SetPlacement(789);Check(world.Routes.ComponentsAfterRepair==1,"Regeneration failed");
    Check(names.All(p=>world.Nodes.At(p.Cell,p.Center).Name==p.Name),"Port names changed on replacement");
    foreach(var node in world.Nodes.All)Check(world.Routes.Neighbors(node).All(r=>world.Nodes.At(r.Target,r.Entry)!=null),"Stale graph cache after regeneration");
}
Check(localBattle && remoteBattle && harborBattle && harbors>0 && local>0 && crossFace>0,"Required movement scenarios absent");
Console.WriteLine($"PASS: 64 worlds, {total} Nodes including {harbors} ports; maximum destinations {maximum}; reciprocal connected graph, port sea links/names/conquest, local/remote battles, enemy turns and regeneration.");

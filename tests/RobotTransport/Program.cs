using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static void Reject(Action action){try{action();}catch(ArgumentException){return;}catch(InvalidOperationException){return;}throw new Exception("Invalid operation accepted");}
RobotWorld World(int nodes=3){var w=new RobotWorld();w.Initialize(nodes,20);return w;}
Robot Add(RobotWorld w,int node,int mask=7,int owner=0){var r=w.Create(owner,(RobotParts)mask);w.Nodes[node].Add(r);return r;}
void Fill(RobotWorld w,int node,int count=12){for(int i=0;i<count;i++)Add(w,node);}
var world=World();
for(int mask=1;mask<=7;mask++)Check(world.Create(0,(RobotParts)mask).CanFight==(mask==7),"Seven parts states");
Reject(()=>world.Create(0,0));Reject(()=>world.Create(20,RobotParts.Head));Fill(world,0);Reject(()=>Add(world,0));
world.Retinues[0].Add(world.Create(0,RobotParts.Complete));Check(world.Retinues[0].Count==1,"Independent retinue capacity");
world=World();var h=Add(world,0,4);var b=Add(world,0,2);var l=Add(world,0,1);var merged=world.Assemble(0,new[]{h.Id,b.Id,l.Id});Check(world.Nodes[0].Count==1 && merged.CanFight,"Assembly");
world.Split(0,merged.Id,RobotParts.Head);Check(world.Nodes[0].Count==2,"Split");var foreign=Add(world,0,2,1);Reject(()=>world.Assemble(0,new[]{merged.Id,foreign.Id}));
world=World();Fill(world,0);var splitAfterDisposal=world.Nodes[0].Robots.Last();Reject(()=>world.Workshops[0].Dispose(world.Nodes[0],world.Nodes[0].Robots.First().Id,1));world.Workshops[0].ConfigureDisposal(1);world.Workshops[0].Dispose(world.Nodes[0],world.Nodes[0].Robots.First().Id,1);world.QueueSplit(0,splitAfterDisposal.Id,RobotParts.Head);
world.Advance((a,b)=>true,_=>0,1);Check(world.Nodes[0].Count==12 && world.Nodes[0].Robots.Single(r=>r.Id==splitAfterDisposal.Id).Parts==RobotParts.Head,"Disposal frees capacity before queued split");
world=World();Fill(world,0);var workshop=world.Workshops[0];workshop.Configure(RobotParts.Head,3);
for(int i=0;i<8;i++)world.Advance((a,b)=>true,_=>0,i);Check(workshop.Waiting && workshop.ProductionAge==3,"Full factory holds one finished product");
workshop.ConfigureDisposal(1);workshop.Dispose(world.Nodes[0],world.Nodes[0].Robots[0].Id,1);world.Advance((a,b)=>true,_=>0,9);Check(world.Nodes[0].Count==12 && workshop.ProductionAge==0 && !workshop.Waiting,"Disposal precedes shipping");
world=World();var disposeAtThree=Add(world,0);world.Workshops[0].ConfigureDisposal(3);world.Workshops[0].Dispose(world.Nodes[0],disposeAtThree.Id);
world.Advance((a,b)=>true,_=>0,1);world.Advance((a,b)=>true,_=>0,2);Check(world.Nodes[0].Count==1,"Disposal is not instant");world.Advance((a,b)=>true,_=>0,3);Check(world.Nodes[0].Count==0,"Disposal uses node's configured period");
world=World();Fill(world,0);Fill(world,1);var a=world.Nodes[0].Robots[0];var c=world.Nodes[1].Robots[0];
world.Transport.Resolve(world,new[]{new RobotShipment(0,1,a.Id,a.Parts),new RobotShipment(1,0,c.Id,c.Parts)},(x,y)=>true);
Check(world.Transport.LastTransfers.Count==2 && world.Nodes[1].Robots.Contains(a),"Full exchange");
world=World();var first=Add(world,0,4);var second=Add(world,2,4);Add(world,1,3);Fill(world,1,11);world.Transport.SetPriority(1,new[]{2,0});
world.Transport.Resolve(world,new[]{new RobotShipment(0,1,first.Id,first.Parts),new RobotShipment(2,1,second.Id,second.Parts)},(x,y)=>true);
Check(world.Transport.LastTransfers.Count==1 && world.Transport.LastTransfers[0].Source==2 && world.Nodes[0].Robots.Contains(first),"Merge target priority");
world=World();var partial=Add(world,0,5);Fill(world,1);
world.Transport.Resolve(world,new[]{new RobotShipment(0,1,partial.Id,RobotParts.Head)},(x,y)=>true);
Check(world.Nodes[0].Robots.Single()==partial && world.Transport.LastTransfers.Count==0,"Failed split restores original");
world=World();Fill(world,0);Fill(world,1);Fill(world,2);var x=world.Nodes[0].Robots[0];var y=world.Nodes[1].Robots[0];
world.Transport.Resolve(world,new[]{new RobotShipment(0,1,x.Id,x.Parts),new RobotShipment(1,2,y.Id,y.Parts)},(s,t)=>true);
Check(world.Transport.LastTransfers.Count==0 && world.Nodes[0].Robots.Contains(x) && world.Nodes[1].Robots.Contains(y),"Cascading rollback");
world=World();partial=Add(world,0,5);Fill(world,1);Add(world,2,3);Fill(world,2,11);
world.Transport.SetPlan(0,0,(RobotParts)5,new[]{new TransportWeight(1,20)});world.Transport.SetPlan(0,0,RobotParts.Head,new[]{new TransportWeight(2,20)});
world.Transport.ResolvePlans(world,(s,t)=>true,42);Check(world.Transport.LastTransfers.Single().Parts==RobotParts.Head && world.Nodes[0].Robots.Single().Parts==RobotParts.Legs,"Lower plan after failed complete route");
Reject(()=>world.Transport.SetPlan(0,0,RobotParts.Head,new[]{new TransportWeight(1,21)}));world.Check();
world=World();partial=Add(world,0,5);world.Transport.SetPlan(0,0,(RobotParts)5,Array.Empty<TransportWeight>());world.Transport.SetPlan(0,0,RobotParts.Head,new[]{new TransportWeight(1,20)});
world.Transport.ResolvePlans(world,(s,t)=>true,1);Check(world.Transport.LastTransfers.Count==0 && world.Nodes[0].Robots.Single()==partial,"Explicit stay does not select lower plan");
for(int seed=0;seed<100;seed++)
{
    var rng=new SeedRandom(seed);world=World(4);
    for(int node=0;node<4;node++)for(int i=0;i<12;i++)Add(world,node,1+rng.Next(7),rng.Next(2));
    string Parts(RobotWorld w)=>string.Join(",",from owner in Enumerable.Range(0,2) from part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs} select w.Nodes.SelectMany(s=>s.Robots).Count(r=>r.Owner==owner && (r.Parts&part)!=0));
    var beforeParts=Parts(world);var shipments=new List<RobotShipment>();
    for(int node=0;node<4;node++)foreach(var robot in world.Nodes[node].Robots)
    {
        var subsets=Enumerable.Range(1,7).Where(mask=>((int)robot.Parts&mask)==mask).ToArray();
        shipments.Add(new(node,(node+1+rng.Next(3))%4,robot.Id,(RobotParts)subsets[rng.Next(subsets.Length)]));
    }
    var reversed=World(4);for(int node=0;node<4;node++)foreach(var robot in world.Nodes[node].Robots)reversed.Nodes[node].Add(reversed.Create(robot.Owner,robot.Parts,robot.Role));
    reversed.Transport.Resolve(reversed,shipments,(s,t)=>true);world.Transport.Resolve(world,shipments.OrderByDescending(s=>s.RobotId),(s,t)=>true);
    string Snapshot(RobotWorld w)=>string.Join(";",w.Nodes.SelectMany((s,node)=>s.Robots.OrderBy(r=>r.Id).Select(r=>$"{node},{r.Id},{r.Owner},{r.Parts}")));
    Check(Snapshot(world)==Snapshot(reversed),"Transport order does not change resolution");
    Check(Parts(world)==beforeParts && world.Nodes.All(s=>s.Count<=12),"Random partial transport conserves every owner's parts");world.Check();
}
Console.WriteLine("PASS: robot parts, capacity, assembly, split, factories, full exchange, receiving priorities, partial rollback, cascades and lower transport plans.");

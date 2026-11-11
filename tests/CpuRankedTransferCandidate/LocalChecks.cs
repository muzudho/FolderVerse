using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
WorldSetup Load(){object saved=AutoSaveStore.Read<object>(args[0]);return saved is WorldSetup w?w:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;}
var w=Load();int leader=w.Relations.Leader;
ComputerFunctionTransfers.Plan(w,true);
Check(w.Campaign.FunctionTransfers.Orders.Count==1,"At most one local recovery transfer is reserved in stalled world");
var order=w.Campaign.FunctionTransfers.Orders.Single();
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{order.Source,order.Target,order.Owner}));
Check(w.Nodes.All[order.Source].Owner==order.Owner && w.Nodes.All[order.Target].Owner==order.Owner,"Recovery is same-country");
Check(w.Robots.Nodes[order.Source].TotalCapacity+w.Robots.Nodes[order.Target].TotalCapacity<=60,"Capacity ceiling preserved");
Check(w.Relations.Leader==leader,"Planner does not change display leader");
w=Load();w.SetParticipantCounts(1,w.ActiveCount-1);w.HumanPlayers.Clear();foreach(int o in Enumerable.Range(0,w.ActiveCount))w.HumanPlayers.Add(o);
ComputerFunctionTransfers.Plan(w);
Check(w.Campaign.FunctionTransfers.Orders.Count==0,"Manual countries are never consolidated");
w=Load();var b=w.Campaign.RobotBattles;
var march=(from n in w.Nodes.All where n.Functional && n.Owner>=0 && w.Relations.CanAct(n.Owner)
    let robots=b.SelectableRobots(w,n.Owner,n.Id,false)
    where robots.Length>0 from target in b.MarchTargets(w,n.Id)
    where w.Nodes.All[target].Owner>=0 && !w.Relations.Allied(n.Owner,w.Nodes.All[target].Owner)
    select new March(n.Owner,target,1,n.Id,Queen:false,RobotIds:new[]{robots[0].Id})).First();
w.Campaign.Plan(march);ComputerFunctionTransfers.Plan(w,true);
Check(w.Campaign.FunctionTransfers.Orders.Count==0,"Existing hostile plan prevents irreversible local recovery");
Console.WriteLine("PASS stalled-only local consolidation, one global recovery, same-country, capacity, unchanged leader, manual and active-plan protection");
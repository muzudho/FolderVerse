using FolderVerse;
using System.Text.Json;
object saved=AutoSaveStore.Read<object>(args[0]);
var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
w.Robots.ConfigureFactories(w);
foreach(var b in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))b.Allied=w.Relations.Allied;
int observed=0;
w.Campaign.RobotBattles.BeforeSegments=(world,campaign)=>{
    var queens=campaign.Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles.Where(b=>!b.Finished).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))
        .Where(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated).Select(u=>new{u.Id,u.Owner,encounter=e.Id,board=b.Id,target=e.Target})));
    var duplicated=queens.GroupBy(q=>q.Id).Where(g=>g.Select(q=>q.board).Distinct().Count()>1).SelectMany(g=>g).ToArray();
    observed=duplicated.Count(q=>q.Id==-7);
    Console.WriteLine(JsonSerializer.Serialize(new{stage="before-any-segment",turn=world.Population.Turn,duplicated}));
};
w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();
w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true,deferResolution:orders=>{
    var selected=new List<March>{new(17,174,1,176,Queen:false,RobotIds:new long[]{760}),
        new(17,174,2,169,Queen:true,PocketOnly:true,RobotIds:new long[]{219,220}),
        new(6,184,1,174,Queen:true,PocketOnly:true,RobotIds:new long[]{198})};
    if(args.Contains("--stay-home"))selected.RemoveAll(o=>o.Ruler==6);
    if(args.Contains("--outgoing-only"))selected.RemoveAll(o=>o.Ruler==17);
    var list=(List<March>)orders;list.Clear();list.AddRange(selected);
    Console.WriteLine(JsonSerializer.Serialize(new{stage="three-explicit-orders",orders=selected}));return false;
});
int expected=args.Contains("--stay-home") || args.Contains("--outgoing-only")?0:2;
if(observed!=expected)throw new Exception("Expected queen6 in exactly two simultaneous unfinished boards before resolution.");
w.Robots.Check();
Console.WriteLine(expected==2?"REPRODUCED: one queen exists in two unfinished battles before resolution; rules and RNG unchanged.":"PASS: no duplicate when only defense or only departure occurs.");
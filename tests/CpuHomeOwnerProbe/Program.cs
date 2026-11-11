using FolderVerse;
using System.Text.Json;
object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
w.Robots.ConfigureFactories(w);foreach(var b in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))b.Allied=w.Relations.Allied;
int recovered=0;
RobotCampaign.BeforeCompleteProbe=(world,e,homes)=>{
    if(e.Target!=37)return;
    var units=e.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Robot!=null).DistinctBy(u=>u.Id).ToArray();
    var originally20=units.Where(u=>homes.TryGetValue(u.Id,out var h) && h.Owner==20).Select(u=>u.Id).Order().ToArray();
    int final20=units.Count(u=>u.Owner==20);
    Console.WriteLine(JsonSerializer.Serialize(new{world.Population.Turn,e.Id,e.Target,originally20,final20,
        knownHomes=units.Count(u=>homes.ContainsKey(u.Id)),totalRobots=units.Length,
        originalSources=originally20.Select(id=>new{id,homes[id].Owner,homes[id].Node})}));
    if(originally20.Length==4 && final20==0)recovered++;
};
try{for(int i=0;i<3;i++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
    w.Lessons.Research();w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);
    foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles)){battle.Frames.Clear();battle.Events.Clear();battle.Persuasions.Clear();battle.Attacks.Clear();}
    w.Robots.Check();
}}finally{RobotCampaign.BeforeCompleteProbe=null;}
if(recovered!=1)throw new Exception("Expected one real four-unit all-converted assault with original home attribution intact.");
Console.WriteLine("PASS live copied-world trace: original owners and home sources remain available before completion even after all four units convert.");
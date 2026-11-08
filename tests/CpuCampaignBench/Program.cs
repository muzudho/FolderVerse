using FolderVerse;
using System.Text.Json;
if(args.Length==0)throw new ArgumentException("Usage: CpuCampaignBench saved-game [turns]");
object saved=AutoSaveStore.Read<object>(args[0]);var w=(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;int steps=args.Length>1?int.Parse(args[1]):20;
w.Robots.ConfigureFactories(w);foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))battle.Allied=w.Relations.Allied;
void Report(int step){var all=w.Robots.Nodes.SelectMany(n=>n.Robots).ToArray();Console.WriteLine(JsonSerializer.Serialize(new{step,turn=w.Population.Turn,independent=Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.CanWinConquest(o)),powered=w.Relations.Powered.Take(w.ActiveCount).Count(p=>p),occupied=all.Length,capacity=w.Robots.Nodes.Sum(n=>n.TotalCapacity),deployable=w.Nodes.All.Where(n=>n.Owner>=0).Sum(n=>w.Campaign.RobotBattles.AvailableFrom(w,n.Owner,n.Id)),blocked=w.Robots.Workshops.Sum(s=>s.ProductionLines.Count(l=>l.Waiting)),disposal=all.Count(r=>r.Disposal),returning=all.Count(r=>r.ReturnHomeCommander.HasValue)+w.Robots.ReturningRobots.Count,longDelivery=w.Robots.LongDelivery.Orders.Count,ongoing=w.Campaign.RobotBattles.Encounters.Count(e=>!e.Finished),maxBattle=w.Campaign.RobotBattles.Encounters.Where(e=>!e.Finished).SelectMany(e=>e.Battles).Select(b=>b.Turn).DefaultIfEmpty(0).Max()}));}
Report(0);
for(int step=1;step<=steps;step++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
    w.Lessons.Research();
    w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);
    foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles)){battle.Frames.Clear();battle.Events.Clear();battle.Persuasions.Clear();battle.Attacks.Clear();}
    w.Robots.Check();if(step%5==0 || step==steps)Report(step);
    if(Enumerable.Range(0,w.ActiveCount).Count(o=>w.Relations.CanWinConquest(o))==1)break;
}

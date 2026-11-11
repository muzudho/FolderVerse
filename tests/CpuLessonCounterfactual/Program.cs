using FolderVerse;
using System.Text.Json;
object saved=AutoSaveStore.Read<object>(args[0]);var w=saved is WorldSetup direct?direct:(WorldSetup)saved.GetType().GetProperty("World")!.GetValue(saved)!;
w.Robots.ConfigureFactories(w);foreach(var b in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles))b.Allied=w.Relations.Allied;
int recovered=0;
RobotCampaign.BeforeSegmentProbe=(world,e)=>{
 if(e.Target!=127 || world.Population.Turn!=22)return;
 foreach(var board in e.Battles){
   var path=Path.Combine(args[1],"heartbeat-2031-before127.json.gz");AutoSaveStore.Write(path,board);
   foreach(bool trained in new[]{false,true}){
     var clone=AutoSaveStore.Read<BattleState>(path);clone.Allied=world.Relations.Allied;
     clone.Lessons[10]=trained?clone.Lessons[10]|BattleLesson.FireDiscipline:clone.Lessons[10]&~BattleLesson.FireDiscipline;
     int budget=100;while(!clone.Finished && budget-->0)BattleTurnResolver.Advance(clone);
     if(!clone.Finished)throw new Exception("Clone unfinished");
     Console.WriteLine(JsonSerializer.Serialize(new{trained,clone.Turn,clone.Winner,conversions=clone.Persuasions.Where(p=>p.Success).Select(p=>new{p.Turn,p.Target,p.Owner,p.PreviousOwner}),owners=clone.Units.Where(u=>u.Id is 143 or 144 or 594).Select(u=>new{u.Id,u.Owner,u.Fallen,u.Retreated})}));
     recovered++;
   }
 }
};
try{for(int i=0;i<22;i++){
    w.Robots.PrepareDomestic(w.Population.Turn,(a,b)=>FunctionalDeliveryRoutes.Connected(w,a,b),n=>w.Nodes.All[n].Owner,w.Campaign.RobotBattles.LockedNode);
    w.Campaign.FinishDomestic(w,true);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,automaticAll:true,allPlanned:true);
    foreach(var captive in w.Relations.Pending.ToArray())w.Relations.Choose(w,captive,w.Relations.Accepts(w,captive.Ruler)?DefeatedChoice.Recruit:w.Relations.CanMercy(w,captive)?DefeatedChoice.Release:DefeatedChoice.RemoveBattery);
    w.Lessons.Research();w.Industry.Roll(w.PlacementSeed^w.FortuneSeed);w.Industry.Apply(w);
    foreach(var battle in w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles)){battle.Frames.Clear();battle.Events.Clear();battle.Persuasions.Clear();battle.Attacks.Clear();}
    w.Robots.Check();
}}finally{RobotCampaign.BeforeSegmentProbe=null;}
if(recovered!=2)throw new Exception("Expected two cloned counterfactuals");
Console.WriteLine("PASS isolated pre-segment clones, single FireDiscipline bit toggle");
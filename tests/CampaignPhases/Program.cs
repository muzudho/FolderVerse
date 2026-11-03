using FolderVerse;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

if(args.Contains("--ui")){using var game=new CampaignPhasesUi();game.Run();return;}

static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static (WorldSetup W,int A,int B,int C) Fixture()
{
    var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    foreach(var s in w.Robots.Workshops)s.AutoAssemblyDisabled=true;
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);
    var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);
    int c=w.Routes.Neighbors(b).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=a.Id);
    a.Owner=b.Owner=0;w.Nodes.All[c].Owner=1;return(w,a.Id,b.Id,c);
}
static Robot Add(WorldSetup w,int node,RobotParts parts){var r=w.Robots.Create(0,parts);w.Robots.Nodes[node].Add(r);return r;}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Complete);int turn=w.Population.Turn,industry=w.Robots.IndustryTurn;
    w.Robots.DirectDelivery.ReserveParts(w,a,b,r.Id,r.Parts,null,0);
    Check(w.Campaign.FinishDomestic(w),"Single controller finishes domestic phase");
    Check(w.Campaign.Phase==CampaignPhase.Delivery && w.Population.Turn==turn && w.Robots.IndustryTurn==industry && !w.Industry.Pending,"Delivery does not advance global or industry time");
    Check(w.Robots.Nodes[a].Count==0 && w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete && w.Robots.DirectDelivery.Orders.Count==0,"All reserved parts arrive before military decisions");
    Check(!w.Campaign.FinishDomestic(w),"Delivery cannot run twice");
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(saved.Campaign.Phase==CampaignPhase.Delivery,"Delivery phase survives model save/load");
    w.Campaign.BeginMilitary();var arrival=w.Robots.Nodes[b].Robots.Single();
    Check(w.Campaign.RobotBattles.SelectableRobots(w,0,b,false).Any(r=>r.Id==arrival.Id),"Arrival is immediately selectable for attack");
    w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    w.Campaign.Plan(new(0,c,1,b,false,false,new[]{arrival.Id}));
    w.Campaign.Advance(w,-1,0,enemies:false);
    Check(w.Population.Turn==turn+1 && w.Campaign.Phase==CampaignPhase.Domestic && w.Industry.Pending,"Military completion advances exactly one global turn and queues industry");
    Check(w.Campaign.RobotBattles.Scenes.Any(s=>s.Units.Any(u=>u.Robot?.Id==arrival.Id)),"Delivered robot attacks during the same global turn");
}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Head);
    w.Robots.DirectDelivery.Reserve(w,a,b,r.Id,r.Parts,null,0);
    // A return plan would expose an accidental second delivery at military end.
    w.Robots.Transport.SetPlan(b,0,RobotParts.Head,new[]{new TransportWeight(a,360)});
    w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false);
    Check(w.Robots.Nodes[b].Robots.Any(x=>x.Id==r.Id) && w.Robots.Nodes[a].Count==0,"Military completion never transports arrivals a second time");
    w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[a].Robots.Any(x=>x.Id==r.Id),"Return plan runs only in the next delivery phase");
}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Head);
    for(int i=0;i<12;i++)Add(w,b,RobotParts.Legs);
    w.Robots.DirectDelivery.Reserve(w,a,b,r.Id,r.Parts,null,0);w.Campaign.FinishDomestic(w);
    Check(w.Robots.DirectDelivery.Orders.Single().Status=="空き枠待機" && w.Robots.DirectDelivery.Failures.Count==0,"Full hangar waits instead of becoming a shipping failure");
    w.Campaign.BeginMilitary();w.Robots.Nodes[b].Remove(w.Robots.Nodes[b].Robots[0].Id);w.Campaign.Advance(w,-1,0,enemies:false);
    Check(w.Robots.DirectDelivery.Orders.Count==1 && w.Robots.Nodes[a].Count==1,"Capacity opening during military does not retry delivery");
    w.Campaign.FinishDomestic(w);
    Check(w.Robots.DirectDelivery.Orders.Count==0 && w.Robots.Nodes[a].Count==0,"Waiting cargo retries during next delivery phase");
}
{
    var(w,a,b,c)=Fixture();w.HumanPlayers.Add(0);w.SetHumanPlayer(1,true);w.SelectTurnRuler(0);
    var r=Add(w,a,RobotParts.Head);w.Robots.DirectDelivery.Reserve(w,a,b,r.Id,r.Parts,null,0);int turn=w.Population.Turn;
    Check(!w.Campaign.FinishDomestic(w) && w.TurnPlayer==1 && w.Campaign.Phase==CampaignPhase.Domestic && w.Robots.Nodes[a].Count==1,"First human submits domestic without delivering");
    Check(w.Campaign.FinishDomestic(w) && w.TurnPlayer==0 && w.Robots.Nodes[b].Count==1,"Last human triggers one shared delivery");
    w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0);
    Check(w.Campaign.Phase==CampaignPhase.Military && w.Campaign.AwaitingPlayers && w.Population.Turn==turn && w.TurnPlayer==1,"Each human gets military decisions before world advances");
    w.Campaign.Advance(w,-1,0);
    Check(w.Population.Turn==turn+1 && !w.Campaign.AwaitingPlayers && w.Campaign.Phase==CampaignPhase.Domestic,"Last human completes one shared global turn");
}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Complete);
    w.Robots.DirectDelivery.Reserve(w,a,b,r.Id,RobotParts.Head,null,0);w.Nodes.All[b].Owner=1;
    w.Robots.DirectDelivery.ReconcileDestinations(w);
    Check(w.Robots.DirectDelivery.Orders.Count==0 && w.Robots.DirectDelivery.FailedPart(a,r.Id,RobotParts.Head) && w.Robots.DirectDelivery.LockedParts(a,r.Id)==0,"Enemy destination becomes informational failure and releases all reservation locks");
    Check(w.Robots.Nodes[a].Robots.Single()==r,"Failure preserves cargo");
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(saved.Robots.DirectDelivery.FailedPart(a,r.Id,RobotParts.Head),"Failure labels survive save/load");
    w.Nodes.All[b].Owner=0;w.Robots.DirectDelivery.Reserve(w,a,b,r.Id,RobotParts.Head,null,0);
    Check(!w.Robots.DirectDelivery.FailedPart(a,r.Id,RobotParts.Head),"New reservation replaces old failure label");
    var legacy=AutoSaveStore.Capture(w);
    foreach(var o in legacy.Objects)if(o.Fields!=null){o.Fields.Remove("<Phase>k__BackingField");o.Fields.Remove("_domesticSubmitted");o.Fields.Remove("<Failures>k__BackingField");}
    saved=AutoSaveStore.Restore<WorldSetup>(legacy);
    Check(saved.Campaign.Phase==CampaignPhase.Domestic && saved.Robots.DirectDelivery.Failures.Count==0 && saved.Campaign.FinishDomestic(saved),"Legacy saves default to domestic phase with no failure labels");
}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Complete);
    var game=(Game1)RuntimeHelpers.GetUninitializedObject(typeof(Game1));const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    void Set(string n,object v)=>typeof(Game1).GetField(n,flags)!.SetValue(game,v);
    object Call(string n,params object[] values)=>typeof(Game1).GetMethod(n,flags)!.Invoke(game,values)!;
    Set("_setup",w);Set("_populationNode",a);Set("_pointer",new Point(85,861));
    Check(!(bool)Call("PhasePrimaryActionAllowed",2) && (bool)Call("PhasePrimaryActionAllowed",3),"Domestic disables war and enables delivery");
    Call("OpenWarPlanning");Check(!(bool)typeof(Game1).GetField("_movementOpen",flags)!.GetValue(game)!,"War planning cannot open through another entry point during domestic");
    w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();
    Check((bool)Call("PhasePrimaryActionAllowed",2) && !(bool)Call("PhasePrimaryActionAllowed",3) && !(bool)Call("PhasePrimaryActionAllowed",4),"Military enables war and disables both delivery controls");
    Check(!(bool)Call("CanMoveHangarPart",r,RobotParts.Head),"Military forbids part drag and assembly");
    Call("OpenDirectMap");Call("OpenDirectPair",a,b);Call("OpenNodeTransport");
    Check(!(bool)typeof(Game1).GetField("_directMap",flags)!.GetValue(game)! && !(bool)typeof(Game1).GetField("_nodeTransportOpen",flags)!.GetValue(game)!,"Alternate delivery entry points are blocked during military");
    Call("NodeAssemblyLocalInput",new MouseState(),true,false);
    Check(w.Robots.Nodes[a].Robots.Single()==r,"Military assembly button cannot mutate stock");
}
Console.WriteLine("PASS: domestic/delivery/military order, same-turn attack, no double transport, multiplayer, failure labels, legacy saves and UI operation gates.");

using FolderVerse;
using System.Text.Json;
NoRepeatChecks.Run();
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var defaultTraining=new RobotTraining();
Check(defaultTraining.AssemblyPolicy==3 && defaultTraining.DeploymentPolicy==3 && defaultTraining.WorkerPolicy==3 && defaultTraining.RequiredNative==1,"New countries delegate assembly and deployment decisions locally by default");
var strictTraining=AutoSaveStore.Restore<RobotTraining>(AutoSaveStore.Capture(new RobotTraining{AssemblyPolicy=0,DeploymentPolicy=0}));
Check(strictTraining.AssemblyPolicy==0 && strictTraining.DeploymentPolicy==0,"Explicit training restrictions in saved data are retained");
var legacyTraining=AutoSaveStore.Capture(new RobotTraining{AssemblyPolicy=0,DeploymentPolicy=0});
foreach(var item in legacyTraining.Objects)item.Fields?.Remove("<WorkerPolicy>k__BackingField");
var restoredLegacy=AutoSaveStore.Restore<RobotTraining>(legacyTraining);
Check(restoredLegacy.WorkerPolicy==3 && restoredLegacy.AssemblyPolicy==0 && restoredLegacy.DeploymentPolicy==0,"Older saves get only the new worker setting default, preserving existing preferences");
Check(AutoSaveStore.Restore<RobotTraining>(AutoSaveStore.Capture(new RobotTraining{WorkerPolicy=1})).WorkerPolicy==1,"Explicit worker threshold survives save/restore");
Check(IndustryCycle.Sectors.Sum(s=>s.Percent)==100,"Wheel probability");
Check(IndustryCycle.Sectors.Sum(s=>s.Percent*s.Turns)==1200,"Expected four industry turns in thirds");
Check(IndustryCycle.Sectors.Where(s=>s.Turns==0).Sum(s=>s.Percent)==4,"Fumble 4 percent");
Check(IndustryCycle.Sectors.Where(s=>s.Turns==120).Sum(s=>s.Percent)==1,"Jackpot 1 percent");
Check(IndustryTime.Format(1)=="1/3" && IndustryTime.Format(2)=="2/3" && IndustryTime.Format(3)=="1" && IndustryTime.Format(26)=="8+2/3","Fraction formatting preserves exact thirds");
for(int seed=0;seed<1000;seed++){
    var wheel=IndustryCycle.CreateWheel(seed,1);
    Check(wheel.Sum(s=>s.Percent)==100 && wheel.Sum(s=>s.Percent*s.Turns)==1200 && wheel.Any(s=>s.Turns%3!=0),"Generated wheels have exact expectation four and fractional outcomes");
}
Check(Enumerable.Range(0,100).Select(IndustryCycle.SectorAt).GroupBy(i=>i).All(g=>g.Count()==IndustryCycle.Sectors[g.Key].Percent),"Every ticket matches visible sector area");
var robots=new RobotWorld();robots.Initialize(2,2);robots.Workshops[0].AutoAssemblyDisabled=true;
var regular=robots.Create(0,RobotParts.Head) with {DamagedParts=RobotParts.Head};
var cheap=robots.Create(0,RobotParts.Body|RobotParts.Legs) with {CheapParts=RobotParts.Body|RobotParts.Legs};
var mixed=regular.Merge(cheap,0);Check(mixed.CanFight && mixed.NativeParts(0)==2 && mixed.Origin(RobotParts.Body)==0 && mixed.Affiliation(RobotParts.Body)==0,"Healthy cheap parts retain country affiliation; damaged regular parts do not count");
Check(mixed.CombatParts(0)==2,"Healthy cheap native parts count toward combat equipment");
Check(!(mixed with {DamagedParts=RobotParts.Complete}).CanFight,"All damaged cannot fight");
var allCheap=mixed with {CheapParts=RobotParts.Complete,DamagedParts=0};
Check(allCheap.CanFight && allCheap.NativeParts(0)==3 && new RobotTraining().Eligible(allCheap),"All three healthy native cheap parts may deploy even under the default policy");
var cheapSortie=robots.Create(0,RobotParts.Complete) with {CheapParts=RobotParts.Complete};robots.Nodes[0].Add(cheapSortie);
Check(robots.CanReserveSortie(0,cheapSortie) && robots.PrepareSortieReservation(0,new[]{cheapSortie.Id}),"The actual sortie reservation accepts a healthy all-cheap native complete robot");robots.Nodes[0].Remove(cheapSortie.Id);
var foreignCheap=new Robot(10000,0,RobotParts.Complete){HeadOrigin=0,BodyOrigin=1,LegsOrigin=1,DamagedParts=RobotParts.Head,CheapParts=RobotParts.Body|RobotParts.Legs};
Check(!foreignCheap.CanFight && !new RobotTraining{DeploymentPolicy=1}.Eligible(foreignCheap),"Damaged native identity cannot borrow affiliation from foreign cheap parts");
Check(new RobotTraining().Eligible(mixed) && !new RobotTraining{DeploymentPolicy=0}.Eligible(mixed),"Default delegates locally while explicit pure deployment remains restrictive");
robots.Nodes[0].Add(mixed);robots.Split(0,mixed.Id,RobotParts.Body);var body=robots.Nodes[0].Robots.Single(r=>r.Parts==RobotParts.Body);
Check(body.IsCheap(RobotParts.Body) && body.CheapParts==RobotParts.Body,"Split preserves only matching cheap flags");
var rest=robots.Nodes[0].Robots.Single(r=>r.Id!=body.Id);robots.MovePart(0,body.Id,RobotParts.Body,rest.Id,0);
var recombined=robots.Nodes[0].Robots.Single();Check(recombined.CheapParts==cheap.CheapParts && recombined.DamagedParts==regular.DamagedParts,"Part movement preserves both flags independently");
Check(robots.Nodes.SelectMany(s=>s.Robots).All(r=>(r.CheapParts&~r.Parts)==0),"No absent-part cheap flags after split and movement");
var copy=JsonSerializer.Deserialize<Robot>(JsonSerializer.Serialize(recombined))!;Check(copy.CheapParts==recombined.CheapParts && copy.Origin(RobotParts.Head)==0,"Serialization retains cheap country and damage");
robots.Workshops[0].AutoAssemblyDisabled=false;robots.Transport.SetPlan(0,0,RobotParts.Body,new[]{new TransportWeight(1,360)});robots.Advance((a,b)=>true,n=>0,123,industry:false);
Check(robots.IndustryTurn==0 && robots.Nodes[1].Robots.Single().IsCheap(RobotParts.Body),"Transport does not advance industry and preserves cheap flag");
var factory=robots.Workshops[1];factory.GeneratePlans(123);factory.AutoAssemblyDisabled=true;
for(int i=0;i<3;i++)Check(factory.PlanAt(i+4).Cheap && factory.PlanAt(i+4).Turns<factory.PlanAt(i).Turns,"Cheap manufacture is strictly faster");
factory.SelectPlan(4);factory.SetRest(1,2);int period=factory.ProductionPeriod;
for(int i=0;i<period;i++)robots.AdvanceIndustry(n=>0);
Check(robots.Nodes[1].Robots.Any(r=>r.IsCheap(factory.Product)),"Cheap production creates real flagged parts");
int stock=robots.Nodes[1].Count;robots.AdvanceIndustry(n=>0);robots.AdvanceIndustry(n=>0);Check(robots.Nodes[1].Count==stock,"Rest consumes industry turns");
factory.SelectPlan(3);stock=robots.Nodes[1].Count;for(int i=0;i<40;i++)robots.AdvanceIndustry(n=>0);Check(robots.Nodes[1].Count==stock,"Stop plan prevents cheap overproduction");
var timelineWorld=new RobotWorld();timelineWorld.Initialize(1,1);
var timeline=timelineWorld.Workshops[0];timeline.Configure(RobotParts.Head,2);timeline.SetRest(3,2);timeline.AutoAssemblyDisabled=true;
Check(timeline.CycleTurns==12 && timeline.CycleProgress==0 && timeline.TurnsUntilNextProduction==2,"Timeline starts at zero with all repeats and whole-turn rest included");
for(int tick=1;tick<=12;tick++){
    timelineWorld.AdvanceIndustry(n=>0);
    Check(timeline.CycleProgress==(tick==12?0:tick),"Caret follows repeat boundaries and rest, then wraps");
    Check(timelineWorld.Nodes[0].Count==Math.Min(tick/2,3),"Each completion marker produces one robot, rest produces none");
    if(tick==6)Check(timeline.RestRemaining==6 && timeline.TurnsUntilNextProduction==8,"Rest begins at final completion and next completion includes rest");
}
timelineWorld.AdvanceIndustry(n=>0);timeline.SetRest(2,3);
Check(timeline.CycleProgress==1 && timeline.CycleTurns==13,"Changing repeat/rest preserves current robot progress and recomputes timeline");
timeline.SetReservedSlots(12);timelineWorld.AdvanceIndustry(n=>0);
Check(timeline.Waiting && timeline.CycleProgress==2 && timeline.TurnsUntilNextProduction==0,"Capacity wait holds caret at completion without claiming another work turn");
timelineWorld.AdvanceIndustry(n=>0);Check(timeline.CycleProgress==2,"Capacity wait keeps caret stationary");
timeline.SetReservedSlots(0);timelineWorld.AdvanceIndustry(n=>0);
Check(!timeline.Waiting && timeline.CycleProgress==2 && timeline.ProducedSinceRest==1,"Releasing capacity produces at the same completion boundary");
var repeatWorld=new RobotWorld();repeatWorld.Initialize(1,1);
var repeatLane=repeatWorld.Workshops[0];repeatLane.Configure(RobotParts.Head,4);repeatLane.SetRest(9,1);repeatLane.AutoAssemblyDisabled=true;
// Leave room for all nine products without changing the production rules.
for(int tick=1;tick<=39;tick++){
    repeatWorld.AdvanceIndustry(n=>0);
    Check(repeatLane.LaneProgress==(tick==39?0:tick>=36?tick-32:tick%4),"One-robot lane uses modulo across nine repeats, then enters whole-turn rest");
    if(tick==10)Check(repeatLane.LaneProgress==2 && repeatLane.ProducedSinceRest==2,"Ten production turns place caret at second tick of four-turn lane");
    if(tick==36)Check(repeatLane.RestRemaining==3 && repeatLane.CycleProgress==36,"Ninth completion starts three-unit rest at the end of the production stretch");
}
Check(timeline.LaneProgress==0,"Successful intermediate completion wraps caret to start");
RobotWorld ProductionWorld(params RobotParts[] products){
    var result=new RobotWorld();result.Initialize(1,1);var shop=result.Workshops[0];
    for(int seed=0;shop.ProductionLines.Length!=3;seed++)shop.GenerateLines(seed);
    for(int lane=0;lane<3;lane++){
        shop.ProductionLines[lane].Configure(lane<products.Length?products[lane]:RobotParts.Head,4);
        if(lane>=products.Length)shop.ProductionLines[lane].SelectPlan(3);
    }
    return result;
}
var shipping=ProductionWorld(RobotParts.Head,RobotParts.Body|RobotParts.Legs);shipping.Workshops[0].SetReservedSlots(11);
for(int tick=0;tick<4;tick++)shipping.AdvanceIndustry(n=>0);
Check(shipping.Nodes[0].Count==1 && shipping.Nodes[0].Robots.Single().Parts==RobotParts.Complete,"Sequential delivery completes a robot with only one available slot");
var noLookahead=ProductionWorld(RobotParts.Head,RobotParts.Body,RobotParts.Legs);noLookahead.Workshops[0].SetReservedSlots(11);
for(int tick=0;tick<6;tick++)noLookahead.AdvanceIndustry(n=>0);
var held=noLookahead.Workshops[0].ProductionLines[1];
Check(noLookahead.Nodes[0].Robots.Single().Parts==RobotParts.Head && held.WaitingProductionTurns==3 && held.LaneProgress==4,"No lookahead or partial merge: lower lane stores first and later lanes wait at completion for three ticks");
Check(AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(noLookahead)).Workshops[0].ProductionLines[1].WaitingProductionTurns==3,"Waiting duration survives save/restore");
var legacyWait=AutoSaveStore.Capture(noLookahead);
legacyWait.Version=1;
foreach(var item in legacyWait.Objects)item.Fields?.Remove("<WaitingProductionTurns>k__BackingField");
var upgradedWait=AutoSaveStore.Restore<RobotWorld>(legacyWait);
Check(upgradedWait.Workshops[0].ProductionLines[1].WaitingProductionTurns==0,"Old saves start counting waits from zero");
Check(upgradedWait.IndustryTurn==18 && upgradedWait.Workshops[0].ProductionLines[1].ProductionPeriod==12 && upgradedWait.Workshops[0].ProductionLines[1].ProductionAge==12 && upgradedWait.Workshops[0].RestTurns==noLookahead.Workshops[0].RestTurns,"Version-one saves migrate elapsed time and progress to thirds without scaling integer factory settings");
Check(AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(upgradedWait)).IndustryTurn==18,"Saving an upgraded game does not multiply its clock a second time");
var oldRecipe=AutoSaveStore.Capture(new ProductionPlan(RobotParts.Complete,4));oldRecipe.Version=1;
Check(AutoSaveStore.Restore<ProductionPlan>(oldRecipe).Turns==12,"Legacy four-turn recipe still takes four displayed turns");
var oldSector=AutoSaveStore.Capture(new IndustrySector(8,10));oldSector.Version=1;
Check(AutoSaveStore.Restore<IndustrySector>(oldSector).Turns==24,"Pending legacy roulette sectors keep their displayed duration");
noLookahead.Workshops[0].SetReservedSlots(10);noLookahead.AdvanceIndustry(n=>0);
Check(noLookahead.Nodes[0].Robots.Single().Parts==RobotParts.Complete && held.WaitingProductionTurns==0,"New space permits body delivery, then legs merge with both stored parts in the same tick");
var disabled=ProductionWorld(RobotParts.Head,RobotParts.Body|RobotParts.Legs);disabled.Workshops[0].SetReservedSlots(11);disabled.Workshops[0].AutoAssemblyDisabled=true;
for(int tick=0;tick<4;tick++)disabled.AdvanceIndustry(n=>0);
Check(disabled.Workshops[0].ProductionLines[1].WaitingProductionTurns==1 && disabled.Nodes[0].Robots.Single().Parts==RobotParts.Head,"Auto assembly opt-out prevents direct delivery merges");
var bulk=ProductionWorld(RobotParts.Head,RobotParts.Body|RobotParts.Legs);
for(int tick=0;tick<47;tick++)bulk.AdvanceIndustry(n=>0);
Check(bulk.Nodes[0].Count==11 && bulk.Nodes[0].Robots.All(r=>r.Parts==RobotParts.Complete) && bulk.Workshops[0].ProductionLines.Take(2).All(l=>l.ProductionAge==3),"A 47-turn batch follows individual sequential deliveries without skipping progress");
var lockedProduct=ProductionWorld(RobotParts.Body|RobotParts.Legs);lockedProduct.Workshops[0].SetReservedSlots(11);
lockedProduct.Nodes[0].Add(lockedProduct.Create(0,RobotParts.Head) with {Disposal=true});
for(int tick=0;tick<4;tick++)lockedProduct.AdvanceIndustry(n=>0);
Check(lockedProduct.Workshops[0].Waiting && lockedProduct.Nodes[0].Robots.Single().Parts==RobotParts.Head,"Reserved parts are excluded from direct production assembly");
var w=new WorldSetup();w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Campaign.Advance(w,-1,0);
Check(w.Industry.Pending && w.Robots.IndustryTurn==0,"Global combat queues industry without implicit production");
foreach(int wanted in new[]{0,3,120,26})
{
    w.Industry.Reset();w.Industry.Queue(w.Population.Turn);
    int seed=Enumerable.Range(0,100000).First(s=>IndustryCycle.CreateWheel(s,w.Population.Turn)[IndustryCycle.SectorAt(new SeedRandom(s^w.Population.Turn*7919^0x45123).Next(100),IndustryCycle.CreateWheel(s,w.Population.Turn))].Turns==wanted);
    w.Industry.Roll(seed);int before=w.Robots.IndustryTurn;int turn=w.Population.Turn;
    if(wanted>0){
        Check(w.Industry.Advance(w,1) && w.Industry.AppliedUnits==1 && w.Robots.IndustryTurn==before+1,"Manufacturing advances one simulation unit");
        var saved=AutoSaveStore.Restore<IndustryCycle>(AutoSaveStore.Capture(w.Industry));
        Check(saved.AppliedUnits==1,"Partial industry progress survives serialization");
    }
    Check(w.Industry.Apply(w) && !w.Industry.Apply(w),"Exactly once application");
    Check(w.Robots.IndustryTurn==before+wanted && w.Population.Turn==turn,"Independent industrial clock, including zero and forty");w.Robots.Check();
}
Console.WriteLine("PASS: wheel probabilities, expectation, flags, eligibility, split/move/transport/serialization, cheap plans/rest/stop, independent clocks and exactly-once 0/1/40 industry.");
PremiumChecks.Run();

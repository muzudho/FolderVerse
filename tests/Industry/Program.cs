using FolderVerse;
using System.Text.Json;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Check(IndustryCycle.Sectors.Sum(s=>s.Percent)==100,"Wheel probability");
Check(IndustryCycle.Sectors.Sum(s=>s.Percent*s.Turns)==400,"Expected four industry turns");
Check(IndustryCycle.Sectors.Where(s=>s.Turns==0).Sum(s=>s.Percent)==4,"Fumble 4 percent");
Check(IndustryCycle.Sectors.Where(s=>s.Turns==40).Sum(s=>s.Percent)==1,"Jackpot 1 percent");
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
Check(new RobotTraining{DeploymentPolicy=1}.Eligible(mixed) && !new RobotTraining().Eligible(mixed),"Mixed deployment policy respected");
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
var w=new WorldSetup();w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Campaign.Advance(w,-1,0);
Check(w.Industry.Pending && w.Robots.IndustryTurn==0,"Global combat queues industry without implicit production");
foreach(int wanted in new[]{0,1,40})
{
    w.Industry.Reset();w.Industry.Queue(w.Population.Turn);
    int seed=Enumerable.Range(0,100000).First(s=>IndustryCycle.CreateWheel(s,w.Population.Turn)[IndustryCycle.SectorAt(new SeedRandom(s^w.Population.Turn*7919^0x45123).Next(100),IndustryCycle.CreateWheel(s,w.Population.Turn))].Turns==wanted);
    w.Industry.Roll(seed);int before=w.Robots.IndustryTurn;int turn=w.Population.Turn;
    Check(w.Industry.Apply(w) && !w.Industry.Apply(w),"Exactly once application");
    Check(w.Robots.IndustryTurn==before+wanted && w.Population.Turn==turn,"Independent industrial clock, including zero and forty");w.Robots.Check();
}
Console.WriteLine("PASS: wheel probabilities, expectation, flags, eligibility, split/move/transport/serialization, cheap plans/rest/stop, independent clocks and exactly-once 0/1/40 industry.");

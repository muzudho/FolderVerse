using FolderVerse;
using System.Text.Json;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var signatures=new HashSet<string>();
for(int seed=0;seed<1000;seed++)
{
    var wheel=IndustryCycle.CreateWheel(seed,seed%31+1);signatures.Add(JsonSerializer.Serialize(wheel));
    Check(wheel.All(s=>s.Percent>0) && wheel.Sum(s=>s.Percent)==100,"Valid dynamic areas");
    Check(wheel.Sum(s=>s.Percent*s.Turns)==400,"Every wheel retains expectation four");
    Check(wheel.Where(s=>s.Turns==0).Sum(s=>s.Percent)==4,"Every wheel retains fumble four percent");
    Check(wheel.Where(s=>s.Turns==wheel.Max(x=>x.Turns)).Sum(s=>s.Percent)==1,"Jackpot retains one percent");
    Check(Enumerable.Range(0,100).GroupBy(t=>IndustryCycle.SectorAt(t,wheel)).All(g=>g.Count()==wheel[g.Key].Percent),"Area and draw match");
    Check(wheel.SequenceEqual(IndustryCycle.CreateWheel(seed,seed%31+1)),"Reproducible wheel");
}
Check(signatures.Count>900,"Dynamic number sets and weights vary");
var w=new RobotWorld();w.Initialize(1,2);var a=w.Create(0,RobotParts.Complete) with {CheapParts=RobotParts.Head,DamagedParts=RobotParts.Head};
var b=w.Create(1,RobotParts.Complete);w.Nodes[0].Add(a);w.Nodes[0].Add(b);
for(int i=2;i<12;i++)w.Nodes[0].Add(w.Create(0,RobotParts.Head));
var ids=w.Nodes[0].Robots.Select(r=>r.Id).ToArray();
long destination=w.MovePart(0,a.Id,RobotParts.Head,b.Id);
var source=w.Nodes[0].Robots.Single(r=>r.Id==a.Id);var target=w.Nodes[0].Robots.Single(r=>r.Id==b.Id);
Check(destination==b.Id && w.Nodes[0].Count==12 && w.Nodes[0].Robots.Select(r=>r.Id).SequenceEqual(ids),"Full hangar swap retains identities and capacity");
Check(source.Origin(RobotParts.Head)==1 && !source.IsCheap(RobotParts.Head) && !source.IsDamaged(RobotParts.Head),"Source receives target part");
Check(target.Origin(RobotParts.Head)==0 && target.IsCheap(RobotParts.Head) && target.IsDamaged(RobotParts.Head),"Target receives all physical part attributes");
Check(source.Origin(RobotParts.Body)==0 && target.Origin(RobotParts.Body)==1,"Unrelated parts unchanged");
var before=JsonSerializer.Serialize(w.Nodes[0].Robots);w.QueueSplit(0,a.Id,RobotParts.Head);
try{w.MovePart(0,a.Id,RobotParts.Head,b.Id);throw new Exception("Reserved swap accepted");}catch(InvalidOperationException){}
Check(JsonSerializer.Serialize(w.Nodes[0].Robots)==before,"Rejected swap is atomic");w.PendingEdits.Clear();
w.MovePart(0,a.Id,RobotParts.Head,b.Id);Check(w.Nodes[0].Robots.Single(r=>r.Id==a.Id).IsCheap(RobotParts.Head),"Second swap restores original cheap part");w.Check();
Console.WriteLine("PASS: 1000 balanced dynamic wheels, exact area probabilities, deterministic variation, full-hangar swaps, independent attributes, identities and reserved atomic rejection.");

using FolderVerse;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
var w=new RobotWorld();w.Initialize(4,2);for(int i=0;i<6;i++)w.Nodes[0].Add(w.Create(0,RobotParts.Complete));
var f=w.Workshops[0];f.SelectPlan(3);f.SetRest(2,7);f.SetReservedSlots(5);f.AutoAssemblyDisabled=true;
w.Transport.SetPlan(0,0,RobotParts.Complete,new[]{new TransportWeight(1,270)});
w.AdjustNode(0,0,(a,b)=>true);Check(f.SelectedPlan==3 && f.RestEvery==2 && f.RestTurns==7 && f.ReservedSlots==5 && f.AutoAssemblyDisabled && w.Transport.Plan(0,0,RobotParts.Complete).Single().Degrees==270,"Manual preserves settings and transport");
f.Automation=NodeAutomation.Gentle;
for(int turn=0;turn<25;turn++){
 var before=w.Transport.Plan(0,0,RobotParts.Complete).ToDictionary(x=>x.Target,x=>x.Degrees);
 w.AdjustNode(0,0,(a,b)=>true,nodeOwner:n=>n==3?1:0);
 var after=w.Transport.Plan(0,0,RobotParts.Complete).ToDictionary(x=>x.Target,x=>x.Degrees);
 foreach(int target in before.Keys.Union(after.Keys))Check(Math.Abs(after.GetValueOrDefault(target)-before.GetValueOrDefault(target))<=18,"Gentle pie moves at most five percentage points");
 Check(Math.Abs(after.Values.Sum()-before.Values.Sum())<=18,"Stay sector also gentle");
 Check(!after.ContainsKey(3),"Never chooses a foreign node");
}
Check(w.Transport.Plan(0,0,RobotParts.Complete).Sum(x=>x.Degrees)==180,"Gentle converges over multiple turns");
f.Automation=NodeAutomation.Auto;f.SelectPlan(3);w.AdjustNode(0,0,(a,b)=>true,nodeOwner:n=>0);Check(f.Manufacturing && !f.AutoAssemblyDisabled && f.ReservedSlots==2,"Auto sets manufacturing and assembly");
var mixed=w.Create(0,RobotParts.Complete).Materialize() with{BodyOrigin=1,LegsOrigin=1};foreach(var r in w.Nodes[0].Robots.ToArray())w.Nodes[0].Remove(r.Id);w.Nodes[0].Add(mixed);w.Nodes[0].Add(mixed with{Id=100});
w.AdjustNode(0,0,(a,b)=>true,nodeOwner:n=>0);Check(w.Transport.Plan(0,0,RobotParts.Complete).Count==0,"Pure training blocks mixed transport");w.Training[0].DeploymentPolicy=3;w.AdjustNode(0,0,(a,b)=>true,nodeOwner:n=>0);Check(w.Transport.Plan(0,0,RobotParts.Complete).Sum(x=>x.Degrees)>0,"Local deployment admits mixed soldiers");
Console.WriteLine("PASS: manual preservation, gentle convergence and bounds, auto factory, nation filter and training.");
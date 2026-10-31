using FolderVerse;
public static class Fixture
{
 public static readonly int[] Options={0,1,2,4,5,6,7,8,9,10};
 public static (WorldSetup World,int Node) Create(int seed=123){
  var w=new WorldSetup();w.SetParticipantCounts(1,2);
  do{w.SetWorld(seed++);}while(w.Width!=1 || w.Height!=1 || w.Depth!=1 || !WorldTerrain.HasSeaAndLand(w.Cells,w.WorldSeed));
  w.SetCast(456);w.SetPlacement(789);w.SelectPlayer(0);
  int node=w.Nodes.All.First(n=>n.Terrain!=TravelTerrain.Sea).Id;w.Nodes.All[node].Owner=0;
  var f=w.Robots.Workshops[node];f.LimitLines(3);for(int n=0;f.BaseLineCount!=1;n++)f.GenerateLines(n);
  foreach(var line in f.ProductionLines)line.SelectPlan(3);
  f.AutoAssemblyDisabled=true;f.Automation=NodeAutomation.Manual;f.SetReservedSlots(0);
  foreach(var r in w.Robots.Nodes[node].Robots.ToArray())w.Robots.Nodes[node].Remove(r.Id);
  return(w,node);
 }
 public static RobotWorkshop Appoint(WorldSetup w,int node,Robot r){w.Robots.Nodes[node].Add(r);if(!w.Robots.ToggleReservation(node,r.Id,HangarReservation.Worker))throw new Exception("Worker appointment failed");return w.Robots.Workshops[node].ProductionLines.Last();}
 public static ProductionPlan[] Catalogue(RobotWorkshop line)=>Options.Select(line.PlanAt).ToArray();
 public static void Same(ProductionPlan plan,Robot robot){
  if(plan.Product!=robot.Parts || plan.EffectiveCheapParts!=robot.CheapParts || plan.IntegratedParts!=robot.IntegratedParts || new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Any(p=>(robot.Parts&p)!=0 && robot.Origin(p)!=plan.Preview(robot.Owner).Origin(p)))throw new Exception("Actual product differs from selected preview");
 }
}

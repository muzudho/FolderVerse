namespace FolderVerse;
using System;using System.Collections.Generic;using System.Linq;
internal static class ComputerIntakePlanning {
 internal static void Plan(WorldSetup w,bool automaticAll){
  var r=w.Robots;var forecast=new Dictionary<int,List<Robot>>();long serial=0;
  bool Fits(int target,Robot product)=>r.IntakeForecastFits(target,product,forecast.GetValueOrDefault(target)??Enumerable.Empty<Robot>());
  foreach(var n in w.Nodes.All.Where(n=>n.Functional && n.Owner>=0 && (automaticAll || !w.ManualRulers().Contains(n.Owner)) && w.Relations.CanAct(n.Owner) && n.Purpose!=NodePurpose.Relay && !w.Campaign.RobotBattles.LockedNode(n.Id) && r.Workshops[n.Id].Automation!=NodeAutomation.Manual)){
   if(w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==n.Id||o.Target==n.Id))continue;
   foreach(var line in r.Workshops[n.Id].ProductionLines.Where(l=>l.Manufacturing && !l.Paused && !l.CycleFinished && l.IntakeFailure=="入荷先の格納枠が不足しています")){
    var product=line.CurrentProductionPlan.Preview(n.Owner);int current=line.IntakeTarget??n.Id;if(Fits(current,product))continue;
    var target=w.Routes.Neighbors(n).Select(t=>w.Nodes.At(t.Target,t.Entry)).DistinctBy(t=>t.Id)
     .Where(t=>t.Id!=current && t.Functional && t.Owner==n.Owner && !w.Campaign.RobotBattles.LockedNode(t.Id) && !w.Campaign.RobotBattles.LockedEdge(n.Id,t.Id) && !w.Campaign.FunctionTransfers.Orders.Any(o=>o.Source==t.Id||o.Target==t.Id))
     .OrderByDescending(t=>r.Nodes[t.Id].StorageLimit-r.Nodes[t.Id].Count-r.Workshops[t.Id].ReservedSlots).ThenBy(t=>t.Id).FirstOrDefault(t=>Fits(t.Id,product));
    if(target==null)continue;r.SetIntakeTarget(line,target.Id);if(!forecast.TryGetValue(target.Id,out var booked))forecast[target.Id]=booked=new();booked.Add(product with {Id=long.MaxValue-serial++});
   }
  }
 }
}
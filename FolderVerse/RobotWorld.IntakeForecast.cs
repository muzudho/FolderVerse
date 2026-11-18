namespace FolderVerse;
using System;using System.Collections.Generic;using System.Linq;
public sealed partial class RobotWorld {
 internal bool IntakeForecastFits(int target,Robot product,IEnumerable<Robot> forecast){
  var store=Nodes[target].PlanningCopy();bool Locked(Robot robot)=>IntakeReceiverLocked(target,robot);
  bool Add(Robot p)=>PlaceIntake(store,p,Workshops[target].ReservedSlots,!Workshops[target].AutoAssemblyDisabled,Locked);
  foreach(var pending in ProductionIntakes.Where(x=>(x.Target??x.Source)==target))if(!Add(pending.Product))return false;
  foreach(var planned in forecast)if(!Add(planned))return false;
  return Add(product);
 }
}

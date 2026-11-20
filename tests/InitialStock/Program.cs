using FolderVerse;using System.Text.Json;using System.Diagnostics;
int checks=0;void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
foreach(int seed in new[]{0,1,104729}){
 var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,19);w.SetWorld(seed);w.SetCast(8);w.SetTerritoryRights(2);var planner=new ComputerLandPlanner(w);foreach(int ruler in w.LandOrder())w.ClaimLand(ruler,planner.Select(ruler,null).Cells);w.CompleteLand(3,0,0,new(0,0,17));
 var fixedRobots=w.Robots.Nodes.SelectMany(s=>s.Robots).ToArray();var pockets=w.Robots.Retinues.SelectMany(s=>s.Robots).ToArray();var limits=w.Robots.Nodes.Select(s=>s.WarehouseLimit).ToArray();
 foreach(var (rp,mp,uniform) in new[]{(0,0,false),(100,0,false),(0,100,false),(55,25,false),(55,25,true),(25,25,false)}){
  var options=new InitialStockSettings(rp,mp,71,uniform);w.RebuildInitialStock(options);var report=w.InitialStock!;
  Check(report.RobotUnits+report.RawUnits+report.EmptyUnits==report.CapacityUnits,"global conservation");
  Check(report.CapacityUnits==w.Nodes.All.Where(n=>n.Functional&&n.Owner>=0).Sum(n=>w.Robots.Nodes[n.Id].TotalCapacity*3),"denominator excludes pockets");
  Check(report.RobotUnits==w.Robots.Nodes.SelectMany(s=>s.Robots).Sum(r=>SalvagePlan.PartCount(r.Parts)),"actual robot thirds");
  Check(report.RawUnits==w.Robots.Nodes.SelectMany(s=>s.MaplePackages).Sum(p=>p.Sections),"actual raw thirds");
  Check(w.Robots.Nodes.SelectMany(s=>s.Robots).Where(r=>r.FactoryWorker||r.WarehouseKeeper).SequenceEqual(fixedRobots),"fixed robots unchanged");Check(w.Robots.Retinues.SelectMany(s=>s.Robots).SequenceEqual(pockets),"pockets unchanged");
  Check(w.Robots.Nodes.Select(s=>s.WarehouseLimit).SequenceEqual(limits)&&w.Campaign.FunctionTransfers.Orders.Count==0,"no initial relocation");
  foreach(var site in report.Sites){var store=w.Robots.Nodes[site.Node];Check(store.Count<=store.TotalCapacity,"physical fit");Check(site.RobotUnits+site.RawUnits<=site.CapacityUnits&&site.RobotUnits>=site.FixedRobotUnits,"noncrossing stacked graph");Check(store.Robots.All(r=>r.Owner==w.Nodes.All[site.Node].Owner&&r.ValidIntegration),"nationality and integration");}
  if(rp==100)Check(report.EmptyUnits==0&&report.RawUnits==0&&w.Robots.Nodes.SelectMany(s=>s.Robots).All(r=>r.Parts==RobotParts.Complete),"100 percent is full complete robots");
  if(rp==0&&mp==100)Check(report.EmptyUnits==0&&report.RobotUnits==report.FixedRobotUnits,"raw maximum respects fixed robots");
  var physicalIds=w.Robots.Nodes.Concat(w.Robots.Retinues).SelectMany(s=>s.Robots.Select(r=>r.Id).Concat(s.MaplePackages.Select(p=>p.Id))).Concat(w.Salvage.Items.Select(i=>i.Robot.Id)).ToArray();Check(physicalIds.Distinct().Count()==physicalIds.Length,"no ID collision with salvage");
  var robots=w.Robots.Nodes.SelectMany(s=>s.Robots).ToArray();var raw=w.Robots.Nodes.SelectMany(s=>s.MaplePackages).ToArray();w.RebuildInitialStock(options);Check(w.Robots.Nodes.SelectMany(s=>s.Robots).SequenceEqual(robots)&&w.Robots.Nodes.SelectMany(s=>s.MaplePackages).SequenceEqual(raw),"repeat preview is deterministic including IDs");
  var copy=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(copy.InitialStock!.CapacityUnits==report.CapacityUnits&&copy.Robots.Nodes.SelectMany(s=>s.Robots).SequenceEqual(robots),"save restore");
 }
 w.RebuildInitialStock(new(55,25,71));var old=w.InitialStock!.Sites;w.RebuildInitialStock(new(55,25,987));Check(old.Sum(s=>s.RobotUnits)==w.InitialStock!.RobotUnits&&old.Sum(s=>s.RawUnits)==w.InitialStock.RawUnits,"distribution conserves ratios");Check(!old.SequenceEqual(w.InitialStock.Sites),"random distribution actually changes sites");
 Console.WriteLine($"PASS seed {seed}: fixed={w.InitialStock.FixedRobotUnits}, total={w.InitialStock.CapacityUnits}, rows={old.Length}");
}
Console.WriteLine($"CHECKS {checks}");

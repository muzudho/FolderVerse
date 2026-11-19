using FolderVerse;using System.Reflection;
var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(0,2);w.SetWorld(104729);w.SetCast(1);w.SetTerritoryRights(2);foreach(int owner in w.LandOrder())w.ClaimLand(owner,new ComputerLandPlanner(w).Select(owner,null).Cells);w.CompleteLand(3,25,25);
var a=w.Nodes.All.First(n=>n.Functional&&w.Routes.Neighbors(n).Length>0);var rr=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(rr.Target,rr.Entry);a.Owner=b.Owner=0;a.Purpose=b.Purpose=NodePurpose.General;
w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);var r=w.Robots;foreach(int id in new[]{a.Id,b.Id})typeof(RobotStore).GetMethod("SetCapacity",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(r.Nodes[id],new object[]{5});typeof(RobotWorkshop).GetMethod("RemoveProductionLines",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(r.Workshops[a.Id],null);r.Workshops[a.Id].LimitLines(1);var worker=r.Create(0,RobotParts.Complete);r.Nodes[a.Id].Add(worker);
void Check(bool c,string m){if(!c)throw new Exception(m);Console.WriteLine("PASS "+m);}
Check(r.ToggleReservation(a.Id,worker.Id,HangarReservation.Worker),"appoint real manufacturer");var line=r.Workshops[a.Id].ProductionLines.Single();var product=r.Create(0,RobotParts.Head);r.ProductionIntakes.Add(new(a.Id,null,product,line));
Check(r.ToggleReservation(a.Id,worker.Id,HangarReservation.Worker),"release worker");Check(!r.Workshops[a.Id].ProductionLines.Any(),"worker lane removed");Check(r.ProductionIntakes.Count==1,"finished product survives release");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;var pending=r.ProductionIntakes.Single();r.SetIntakeTarget(pending.Line,b.Id);Check(r.ProductionIntakes.Single().Target==b.Id && r.ProductionIntakes.Single().Status=="入荷予約","detached saved lane retarget updates completed product");
w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));r=w.Robots;Check(r.ProductionIntakes.Single().Target==b.Id,"retarget survives second save");foreach(int mode in new[]{0,1,2}){
 var x=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));var e=new RobotEncounter{Kind=mode==0?BattleKind.Node:BattleKind.Edge,Source=mode==2?b.Id:a.Id,Target=mode==2?a.Id:b.Id};x.Campaign.RobotBattles.Encounters.Add(e);
 Check(mode==0?x.Campaign.RobotBattles.LockedNode(b.Id):x.Campaign.RobotBattles.LockedEdge(a.Id,b.Id),"lock precondition "+mode);
 Check(!x.Robots.PartPlanning.Preview(x).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"blocked forecast "+mode);
 
 e.Finished=true;Check(x.Robots.PartPlanning.Preview(x).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"unlocked forecast "+mode);
 typeof(RobotWorld).GetMethod("ResolveProductionIntakes",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(x.Robots,new object[]{x});Check(x.Robots.ProductionIntakes.Count==0&&x.Robots.Nodes[b.Id].Robots.Count(z=>z.Id==product.Id)==1,"unlocked arrives once "+mode);
}
var ownership=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));ownership.Relations.Reset();
Check(ownership.Robots.PartPlanning.Preview(ownership).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"friendly ownership forecast starts visible");
ownership.Nodes.All[b.Id].Owner=1;
Check(!ownership.Robots.PartPlanning.Preview(ownership).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"ownership change refreshes cached forecast without delivery");
Check(ownership.Relations.SetSuperior(1,0),"alliance hierarchy setup");
Check(ownership.Robots.PartPlanning.Preview(ownership).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"alliance change refreshes cached forecast without delivery");
Check(ownership.Relations.SetSuperior(1,-1),"alliance hierarchy release");
Check(!ownership.Robots.PartPlanning.Preview(ownership).Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"alliance release removes cached arrival without delivery");
Check(ownership.Robots.ProductionIntakes.Count==1 && !ownership.Robots.Nodes[b.Id].Robots.Any(z=>z.Id==product.Id),"forecast ownership changes do not deliver or consume pending product");



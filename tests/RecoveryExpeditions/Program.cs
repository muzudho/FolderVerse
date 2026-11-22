using FolderVerse;using System.Reflection;
AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY",Environment.GetEnvironmentVariable("GAME_RUNTIME"));
int checks=0;
void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
WorldSetup Fixture(){var w=new WorldSetup{EquipmentMode=RobotEquipmentMode.Materials};w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.Relations.Reset();w.SelectPlayer(0);w.Relations.Leader=0;w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);w.Salvage.Reset();w.Salvage.EnableExpeditions(w);foreach(var n in w.Nodes.All)n.Owner=0;var home=w.Nodes.All.First(n=>n.Functional);w.ConquerorLocations[0]=home.Cell;w.ConquerorPoints[0]=home.Center;return w;}
int Home(WorldSetup w)=>w.Nodes.Current(0)!.Id;
Robot Worker(WorldSetup w,int node,bool pocket=false,bool damaged=false){var r=w.Robots.Create(0,RobotParts.Complete) with{DamagedParts=damaged?RobotParts.Complete:0};(pocket?w.Robots.Retinues[0]:w.Robots.Nodes[node]).Add(r);return r;}
SalvageItem Goods(WorldSetup w,int node,RobotParts parts=RobotParts.Head){var r=w.Robots.Create(1,parts) with{HeadMaterial=RobotMaterial.Maple,BodyMaterial=RobotMaterial.Tin,LegsMaterial=RobotMaterial.Silicone};var item=new SalvageItem(1000+w.Salvage.Items.Count,node,r);w.Salvage.Items.Add(item);return item;}
void Step(WorldSetup w,int turn)=>w.Salvage.Advance(w,turn);
{
 var w=Fixture();int h=Home(w);var worker=Worker(w,h,damaged:true);var item=Goods(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});
 Check(w.Salvage.LoanCount(0)==2&&w.Robots.Retinues[0].ReservedReturns==2,"node worker reserves own fallback plus goods pocket slots");Check(w.Robots.Nodes[h].Robots.Single().SalvageReserved&&!w.Robots.CanShip(h,w.Robots.Nodes[h].Robots.Single()),"pending worker cannot ship");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));m=w.Salvage.Missions.Single();Check(m.Pending&&w.Salvage.LoanCount(0)==2,"pending loan and worker survive save");Check(w.Salvage.CancelExpedition(w,m.Id,0)&&w.Robots.Retinues[0].ReservedReturns==0&&!w.Robots.Nodes[h].Robots.Single().SalvageReserved,"cancel restores worker and all loans");
 m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Check(!m.Pending&&w.Robots.Nodes[h].Robots.Count==0,"departure frees source hangar");Step(w,1);Check(m.Cargo.Count==1&&m.Returning&&m.LoanCount==2,"recover named target then return");Step(w,2);Check(m.Finished&&w.Robots.Nodes[h].Robots.Count==2&&w.Robots.Retinues[0].ReservedReturns==0,"worker and cargo return atomically to source");Check(w.Robots.Nodes[h].Robots.Single(r=>r.Id==worker.Id).DamagedParts==RobotParts.Complete,"damaged complete worker performs same job");w.Robots.Check();
}
{
 var w=Fixture();int h=Home(w);var worker=Worker(w,h,true);var item=Goods(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,true,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});Check(m.LoanCount==2&&w.Robots.Retinues[0].Count==1&&w.Robots.Retinues[0].ReservedReturns==1,"pocket pending own slot not double counted");w.Salvage.CommitExpeditions(w);Check(w.Robots.Retinues[0].Count==0&&m.LoanCount==2,"departed pocket worker has fallback reserved");Step(w,1);Step(w,2);Check(m.Finished&&w.Robots.Retinues[0].Robots.Count==2,"pocket source returns to pocket");
}
{
 var w=Fixture();int h=Home(w);var item=Goods(w,h);var a=Worker(w,h);var b=Worker(w,h);var x=w.Salvage.ReserveExpedition(w,0,h,false,a.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});var y=w.Salvage.ReserveExpedition(w,0,h,false,b.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});Check(w.Salvage.LoanCount(0)==4,"insurance teams independently reserve four slots");w.Salvage.CommitExpeditions(w);Step(w,1);Check(x.Cargo.Count==1&&y.Cargo.Count==0&&y.History.Any(s=>s.Contains("回収済み")),"insurance does not duplicate physical goods");Check(y.LoanCount==1&&w.Salvage.LoanCount(0)==3,"abandoned target unlocks only corresponding cargo loan");Step(w,2);Check(w.Robots.Nodes[h].Robots.Count==3&&w.Salvage.LoanCount(0)==0,"both workers return and all locks release");
}
{
 var w=Fixture();int h=Home(w);var r=w.Robots.Create(1,RobotParts.Complete) with{IntegratedParts=RobotParts.Head|RobotParts.Body,HeadMaterial=RobotMaterial.Maple,BodyMaterial=RobotMaterial.Maple,LegsMaterial=RobotMaterial.Tin};w.Salvage.Items.Add(new(1000,h,r));var f=new RecoveryFilter{Anything=false,Shapes=new(){RecoveryShape.Head},Materials=new(){RobotMaterial.Maple}};Check(w.Salvage.RecoveryChoices(w,h,f).Length==0,"integrated head-body is not a single head");f.Shapes=new(){RecoveryShape.HeadBody};var c=w.Salvage.RecoveryChoices(w,h,f);Check(c.Length==1&&c[0].Target.Parts==(RobotParts.Head|RobotParts.Body),"integrated unit is selected as a unit");var worker=Worker(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,c.Select(c=>c.Target));w.Salvage.CommitExpeditions(w);Step(w,1);Check(m.Cargo.Single().IntegratedParts==(RobotParts.Head|RobotParts.Body)&&w.Salvage.Items.Single().Robot.Parts==RobotParts.Legs,"mixed package only removes matching detachable unit");f.Shapes.Clear();Check(f.Error.Length>0,"empty shape selection requires input");f.Shapes.Add(RecoveryShape.Head);f.Materials.Clear();Check(f.Error.Length>0,"empty material selection requires input");
}
{
 var w=Fixture();int h=Home(w);var a=Goods(w,h);w.Salvage.Items[0]=a with{Robot=a.Robot with{DamagedParts=RobotParts.Head}};var f=new RecoveryFilter{Anything=false,Shapes=new(){RecoveryShape.Head},Materials=new(){RobotMaterial.Maple}};Check(w.Salvage.RecoveryChoices(w,h,f).Length==0,"all-good rejects damaged part");f.AllowDamaged=true;Check(w.Salvage.RecoveryChoices(w,h,f).Length==1,"damage allowed includes damaged");w.Salvage.Items[0]=a;Check(w.Salvage.RecoveryChoices(w,h,f).Length==1,"damage allowed also includes good");
}
{
 var w=Fixture();int h=Home(w);for(int i=0;i<7;i++){var worker=Worker(w,h);var item=Goods(w,h);if(i<6)w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});else{bool blocked=false;try{w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});}catch(InvalidOperationException){blocked=true;}Check(blocked,"seventh team rejected by shared twelve slots");}}Check(w.Salvage.LoanCount(0)==12&&!w.Robots.Retinues[0].HasSpace,"six teams consume shared pocket completely");
}
{
 var w=Fixture();int h=Home(w);var worker=Worker(w,h);var item=Goods(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Step(w,1);for(int i=0;i<12;i++)Worker(w,h);Step(w,2);Check(m.Finished&&w.Robots.Retinues[0].Robots.Count==2&&w.Robots.Nodes[h].Count==12,"full source falls back to reserved pocket without overflow");
}
{
 var w=Fixture();int h=Home(w);int t=w.Nodes.All.Where(n=>n.Id!=h&&w.Salvage.RecoveryPath(w,h,n.Id).Length>=3).OrderBy(n=>w.Salvage.RecoveryPath(w,h,n.Id).Length).First().Id;var worker=Worker(w,h);var item=Goods(w,t);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Check(w.Salvage.ReserveRecoveryReturn(m.Id,0)&&m.Targets.Count==1,"return instruction remains a reservation until commit");w.Salvage.CommitExpeditions(w);Check(m.Returning&&m.Targets.Count==0&&m.LoanCount==1,"committed return releases uncollected loans");Step(w,1);Check(m.Finished&&w.Salvage.Items.Count==1,"return never resumes collecting");
}
{
 var w=Fixture();var snap=AutoSaveStore.Capture(w);foreach(var o in snap.Objects.Where(o=>o.Fields!=null)){o.Fields.Remove("<Missions>k__BackingField");o.Fields.Remove("ExpeditionsEnabled");o.Fields.Remove("<SalvageReserved>k__BackingField");}var old=AutoSaveStore.Restore<WorldSetup>(snap);Check(old.Salvage.Missions.Count==0&&!old.Salvage.ExpeditionsEnabled,"older save missing new fields restores safely");old.Salvage.Agents.Add(new(){Owner=0,Node=Home(old),Cargo=new(){old.Robots.Create(0,RobotParts.Body)}});old.Salvage.EnableExpeditions(old);Check(old.Salvage.Agents.Count==0&&old.Salvage.Items.Count==1,"upgrade preserves old agent cargo in local basket");
}
{
 var w=Fixture();int h=Home(w);var near=w.Nodes.All.Where(n=>n.Id!=h).Select(n=>new{n.Id,Path=w.Salvage.RecoveryPath(w,h,n.Id)}).Where(p=>p.Path.Length==2).First().Id;var far=w.Nodes.All.Select(n=>new{n.Id,Path=w.Salvage.RecoveryPath(w,h,n.Id)}).Where(p=>p.Path.Length==3).First().Id;var a=Goods(w,near);var b=Goods(w,far);w.Campaign.RobotBattles.Encounters.Add(new(){Kind=BattleKind.Node,Target=near});var f=new RecoveryFilter{IncludeUnreachable=true};var choices=w.Salvage.RecoveryChoices(w,h,f);Check(choices.First().Target.Item==a.Id&&!choices.First().Reachable,"nearest hypothetical target remains first even if blocked");f.IncludeUnreachable=false;Check(w.Salvage.RecoveryChoices(w,h,f).All(c=>c.Target.Item!=a.Id),"reachable-only hides blocked targets");var worker=Worker(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(a.Id,RobotParts.Head),new RecoveryTarget(b.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Step(w,1);Check(m.History.Any(x=>x.Contains("経路がありません"))&&m.LoanCount==2,"unreachable nearest abandoned then only unused loan released");
}
{
 var w=Fixture();int h=Home(w);int near=w.Nodes.All.First(n=>w.Salvage.RecoveryPath(w,h,n.Id).Length==2).Id;var item=Goods(w,near);var worker=Worker(w,h);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Step(w,1);w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));m=w.Salvage.Missions.Single();Check(m.Cargo.Count==1&&m.LoanCount==2&&m.Worker.Id==worker.Id,"travel cargo worker and loans survive active save");w.Campaign.RobotBattles.Encounters.Add(new(){Kind=BattleKind.Node,Target=h});Step(w,2);Check(!m.Finished&&m.Status.Contains("経路待ち")&&m.LoanCount==2,"unreachable home and pocket keep worker cargo and locks");w.Campaign.RobotBattles.Encounters.Clear();Step(w,3);Check(m.Finished&&w.Robots.Retinues[0].Robots.Count==2,"restored route returns to pocket without restarting collection");
}
{
 var w=Fixture();int h=Home(w);int near=w.Nodes.All.First(n=>w.Salvage.RecoveryPath(w,h,n.Id).Length==2).Id;var worker=Worker(w,h,true);var item=Goods(w,near);var m=w.Salvage.ReserveExpedition(w,0,h,true,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Step(w,1);int destination=w.Nodes.All.First(n=>n.Functional&&w.Salvage.RecoveryPath(w,near,n.Id).Length>=4).Id;w.ConquerorLocations[0]=w.Nodes.All[destination].Cell;w.ConquerorPoints[0]=w.Nodes.All[destination].Center;foreach(var n in w.Nodes.All)n.Owner=1;Check(w.Salvage.RecoveryPath(w,near,destination).Length>0,"diplomatic passage through other countries is allowed");w.Relations.SetSuperior(0,1);for(int t=2;t<40&&!m.Finished;t++)Step(w,t);Check(m.Finished&&m.Node==destination&&w.Robots.Retinues[0].Robots.Count==2,"pocket return follows moving subordinate queen");
}
{
 var w=Fixture();int h=Home(w);int near=w.Nodes.All.First(n=>w.Salvage.RecoveryPath(w,h,n.Id).Length==2).Id;var worker=Worker(w,h);var item=Goods(w,near);var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(item.Id,RobotParts.Head)});w.Salvage.CommitExpeditions(w);Step(w,1);w.Relations.Powered[0]=false;w.Nodes.All[h].Owner=1;Step(w,2);Check(m.Finished&&w.Robots.Nodes[h].Robots.Any(r=>r.Id==worker.Id&&r.Owner==0)&&w.Salvage.LoanCount(0)==0,"eliminated queen worker unloads as foreign robot in host country");
}
{
 var w=Fixture();int h=Home(w);var worker=Worker(w,h);var raw=new MaplePackage(888888,2){Material=RawMaterial.Scrap,ScrapLots=new[]{new ScrapLot(2,h,0)}};var scrap=w.Robots.Create(1,RobotParts.Body) with{RecoveredRaw=raw};w.Salvage.Items.Add(new(1000,h,scrap));var m=w.Salvage.ReserveExpedition(w,0,h,false,worker.Id,new[]{new RecoveryTarget(1000,RobotParts.Body)});w.Salvage.CommitExpeditions(w);Step(w,1);Step(w,2);Check(m.Finished&&w.Robots.Nodes[h].MaplePackages.Sum(p=>p.Units)==2&&w.Robots.Nodes[h].Robots.Count==1,"scrap unload preserves exact compressed quantity");
 var manufacturing=w.Robots.Create(1,RobotParts.Head) with{HeadWork=new(1,4,1)};var dismantling=w.Robots.Create(1,RobotParts.Legs) with{DismantlingAge=1};w.Salvage.Items.Add(new(2001,h,manufacturing));w.Salvage.Items.Add(new(2002,h,dismantling));var f=new RecoveryFilter{Anything=false,Shapes=new(){RecoveryShape.Manufacturing,RecoveryShape.Dismantling},AllowDamaged=true};var c=w.Salvage.RecoveryChoices(w,h,f);Check(c.Length==2&&c.Any(c=>c.Robot.IsWorkpiece)&&c.Any(c=>c.Robot.IsDismantling),"manufacturing and dismantling WIP are separate searchable kinds");
}
{
 var w=Fixture();int h=Home(w);var item=Goods(w,h);w.Salvage.Items[0]=item with{Robot=item.Robot with{DamagedParts=RobotParts.Head}};var f=new RecoveryFilter();Check(w.Salvage.RecoveryChoices(w,h,f).Length==0,"anything still respects all-good condition");f.AllowDamaged=true;Check(w.Salvage.RecoveryChoices(w,h,f).Length==1,"anything damaged-allowed includes damaged goods");
}
if(args.Length>0){
 var w=AutoSaveStore.Read<WorldSetup>(args[0]);w.Robots.Check();var missions=w.Salvage.Missions;Check(w.Salvage.ExpeditionsEnabled&&missions.Count>0,"campaign CPU uses real recovery workers");Check(missions.Any(m=>m.Finished),"campaign CPU completes recovery and return");foreach(int owner in Enumerable.Range(0,w.ActiveCount)){Check(w.Salvage.LoanCount(owner)<=12,"campaign pocket loan cap country "+owner);foreach(var m in missions.Where(m=>m.Owner==owner&&!m.Finished))Check(m.LoanCount==1+m.Cargo.Count+m.Targets.Count,"campaign active loan matches worker cargo and remaining targets");}var away=missions.Where(m=>!m.Finished&&!m.Pending).SelectMany(m=>m.Cargo.Append(m.Worker));var physical=w.Robots.Nodes.Concat(w.Robots.Retinues).SelectMany(s=>s.Robots).Concat(away).Concat(w.Salvage.Items.Select(i=>i.Robot)).ToArray();Check(physical.Select(r=>r.Id).Distinct().Count()==physical.Length,"campaign physical robots have no duplicate store mission or basket IDs");Console.WriteLine("CPU missions="+missions.Count+" finished="+missions.Count(m=>m.Finished)+" active="+missions.Count(m=>!m.Finished));
}

{
 var w=Fixture();int source=Home(w);int goal=w.Nodes.All.Where(n=>n.Functional&&n.Id!=source).Select(n=>new{n.Id,Path=w.Salvage.RecoveryPath(w,source,n.Id)}).Where(n=>n.Path.Length>0).OrderBy(n=>n.Path.Length).First().Id;
 var worker=Worker(w,source);var goods=Goods(w,source);var m=w.Salvage.ReserveExpedition(w,0,source,false,worker.Id,new[]{new RecoveryTarget(goods.Id,goods.Robot.Parts)},goal);
 Check(m.Source==source&&m.DeliveryNode==goal,"source and receiving hangar are independent");
 w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));m=w.Salvage.Missions.Single();Check(m.DeliveryNode==goal,"independent receiving destination survives save");
 w.Salvage.CommitExpeditions(w);for(int t=1;t<100&&!m.Finished;t++)Step(w,t);
 Check(m.Finished&&w.Robots.Nodes[goal].Robots.Count==2&&w.Robots.Nodes[source].Robots.Count==0&&w.Salvage.LoanCount(0)==0,"worker and recovered cargo finish at receiving hangar rather than source");
}
{
 var w=Fixture();int source=Home(w);int goal=w.Nodes.All.Where(n=>n.Functional&&n.Id!=source).First().Id;
 var worker=Worker(w,source);var goods=Goods(w,source);var m=w.Salvage.ReserveExpedition(w,0,source,false,worker.Id,new[]{new RecoveryTarget(goods.Id,goods.Robot.Parts)},goal);
 w.Nodes.All[goal].Owner=1;w.Salvage.CommitExpeditions(w);for(int t=1;t<100&&!m.Finished;t++)Step(w,t);
 Check(m.Finished&&m.PocketFallback&&w.Robots.Retinues[0].Robots.Count==2&&w.Robots.Nodes[goal].Robots.Count==0,"lost receiving hangar falls back to pocket without delivering into hostile storage");
}
{
 var w=Fixture();int source=Home(w);int goal=w.Nodes.All.First(n=>n.Functional&&n.Id!=source).Id;var goods=Goods(w,source);
 w.Campaign.RobotBattles.Encounters.Add(new(){Kind=BattleKind.Node,Target=goal});
 Check(w.Salvage.RecoveryChoices(w,source,new RecoveryFilter()).Any(c=>c.Target.Item==goods.Id)&&w.Salvage.RecoveryChoices(w,source,new RecoveryFilter(),goal).Length==0,"reachable-only checks delivery leg as well as collection leg");
 Check(w.Salvage.RecoveryChoices(w,source,new RecoveryFilter{IncludeUnreachable=true},goal).Any(c=>!c.Reachable),"include unreachable keeps goods with blocked delivery leg visible");
}
{
 var w=Fixture();int source=Home(w);int goal=w.Nodes.All.Where(n=>n.Functional&&n.Id!=source).Select(n=>new{n.Id,Path=w.Salvage.RecoveryPath(w,source,n.Id)}).Where(n=>n.Path.Length>0).OrderBy(n=>n.Path.Length).First().Id;
 var worker=Worker(w,source,true);var goods=Goods(w,source);var m=w.Salvage.ReserveExpedition(w,0,source,true,worker.Id,new[]{new RecoveryTarget(goods.Id,goods.Robot.Parts)},goal);
 w.Salvage.CommitExpeditions(w);for(int t=1;t<100&&!m.Finished;t++)Step(w,t);
 Check(m.Finished&&!m.PocketFallback&&w.Robots.Nodes[goal].Robots.Count==2&&w.Robots.Retinues[0].Robots.Count==0,"pocket worker can deliver to a different receiving hangar");
}
{
 var w=Fixture();int source=Home(w);int goal=w.Nodes.All.First(n=>n.Functional&&n.Id!=source).Id;var worker=Worker(w,source);var goods=Goods(w,source);
 w.Salvage.ReserveExpedition(w,0,source,false,worker.Id,new[]{new RecoveryTarget(goods.Id,goods.Robot.Parts)},goal);
 var snap=AutoSaveStore.Capture(w);foreach(var obj in snap.Objects.Where(o=>o.Fields!=null))obj.Fields.Remove("DeliveryNode");w=AutoSaveStore.Restore<WorldSetup>(snap);var m=w.Salvage.Missions.Single();
 Check(m.DeliveryNode==null,"save without delivery destination retains legacy default");
 w.Salvage.CommitExpeditions(w);for(int t=1;t<100&&!m.Finished;t++)Step(w,t);
 Check(m.Finished&&w.Robots.Nodes[source].Robots.Count==2&&w.Robots.Nodes[goal].Robots.Count==0,"legacy mission still returns to original source");
}
Console.WriteLine("PASS checks="+checks);
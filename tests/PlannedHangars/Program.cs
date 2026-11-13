using FolderVerse;

if(args.Contains("--ui")){using var game=new PlannedHangarsUi();game.Run();return;}
if(args.Contains("--menu-ui")){using var game=new SystemMenuHitsUi();game.Run();return;}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static (WorldSetup W,int A,int B,int C) Fixture()
{
    var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
    foreach(var s in w.Robots.Workshops)s.AutoAssemblyDisabled=true;
    var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(route.Target,route.Entry);
    int c=w.Routes.Neighbors(b).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).First(n=>n!=a.Id);
    a.Owner=b.Owner=0;w.Nodes.All[c].Owner=1;w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    return(w,a.Id,b.Id,c);
}
static Robot Add(WorldSetup w,int n,RobotParts p){var r=w.Robots.Create(0,p);w.Robots.Nodes[n].Add(r);return r;}
foreach(bool cancel in new[]{false,true}){
    var(w,a,b,c)=Fixture();var head=Add(w,a,RobotParts.Head);var rest=Add(w,b,RobotParts.Body|RobotParts.Legs);
    w.Campaign.FunctionTransfers.Reserve(w,a,b);var plans=w.Robots.PartPlanning;
    Check(plans.Preview(w).Nodes[b].Count==2 && w.Robots.Nodes[a].Count==1,"Transfer preview is separate from actual stock");
    plans.Reserve(w,b,b,head.Id,RobotParts.Head,rest.Id,0);
    Check(plans.Preview(w).Nodes[b].Robots.Single().Parts==RobotParts.Complete && w.Robots.Nodes[b].Robots.Single().Parts!=RobotParts.Complete,"Assembly is only a reservation");
    Check(plans.Undo(0) && plans.Preview(w).Nodes[b].Count==2,"Undo restores projected inputs");
    plans.Reserve(w,b,b,head.Id,RobotParts.Head,rest.Id,0);w.Campaign.PlanDeliverySortie(w,new(0,c,1,b,false,false,new[]{rest.Id}));
    if(cancel)w.Campaign.FunctionTransfers.Cancel(a);
    Check(plans.Preview(w).Nodes[b].Robots.Single().Parts==RobotParts.Complete,"Cancelled transfer still forecasts equivalent adjacent intake");
    w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[a].Count==0 && w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete && w.Campaign.PlannedOrders.Count==1,"Transfer kept or cancelled: recipe executes at requested destination");
    if(cancel)Check(w.Robots.Transport.LastTransfers.Any(s=>s.Source==a && s.Target==b && s.Parts==RobotParts.Head),"Fallback transport has playback shipment");
}
{
    var(w,a,b,c)=Fixture();var r=Add(w,a,RobotParts.Complete);for(int i=0;i<12;i++)Add(w,b,RobotParts.Head);
    w.Campaign.FunctionTransfers.Reserve(w,a,b);var plans=w.Robots.PartPlanning;
    plans.Reserve(w,b,b,r.Id,RobotParts.Head,null,0);w.Campaign.FunctionTransfers.Cancel(a);w.Campaign.FinishDomestic(w);
    Check(w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete && w.Robots.Nodes[b].Count==12 && plans.LastResults.Single().Status.Contains("格納枠"),"Impossible fallback is atomic, preserves inputs and explains capacity failure");
}
{
    var(w,a,b,c)=Fixture();w.Nodes.All[b].Owner=1;var robot=Add(w,a,RobotParts.Complete);var plans=w.Robots.PartPlanning;
    long id=plans.Reserve(w,a,a,robot.Id,RobotParts.Head,null,0);plans.Reserve(w,a,a,robot.Id,RobotParts.Body,id,0);plans.Reserve(w,a,a,robot.Id,RobotParts.Legs,id,0);
    w.Campaign.PlanDeliverySortie(w,new(0,b,1,a,false,false,new[]{id}));
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));w.Robots.ConfigureFactories(w);w.Campaign.FinishDomestic(w);
    Check(w.Campaign.LastCancelledWars.Count==0 && w.Campaign.PlannedOrders.Single().RobotIds!.Single()>0,"Chained split/assembly dependencies and provisional IDs survive save and bind on arrival");
}
{
    var(w,a,b,c)=Fixture();w.Nodes.All[b].Owner=1;var robot=Add(w,a,RobotParts.Complete);var plans=w.Robots.PartPlanning;
    long id=plans.Reserve(w,a,a,robot.Id,RobotParts.Head,null,0);plans.Reserve(w,a,a,robot.Id,RobotParts.Body,id,0);plans.Reserve(w,a,a,robot.Id,RobotParts.Legs,id,0);
    w.Campaign.PlanDeliverySortie(w,new(0,b,1,a,false,false,new[]{id}));plans.Undo(0);w.Campaign.FinishDomestic(w);
    Check(w.Campaign.PlannedOrders.Count==0 && w.Campaign.LastCancelledWars.Count==1,"Undoing a required assembly step cancels the entire dependent war");
}
foreach(bool adjacent in new[]{false,true}){
    var(w,a,b,c)=Fixture();int destination=adjacent?b:a,target=adjacent?c:b;if(!adjacent)w.Nodes.All[b].Owner=1;
    var line=w.Robots.Workshops[a];line.Configure(RobotParts.Complete,1);line.SetDoNotRepeat(true);line.IntakeTarget=adjacent?b:null;
    w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
    Check(w.Robots.Nodes[a].Count==0 && w.Robots.Nodes[destination].Count==0 && w.Robots.ProductionIntakes.Count==1,"Manufacture creates pending intake, not actual hangar stock");
    var arrival=w.Robots.PartPlanning.Preview(w).Nodes[destination].Robots.Single();w.Campaign.PlanDeliverySortie(w,new(0,target,1,destination,false,false,new[]{arrival.Id}));
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));w.Robots.ConfigureFactories(w);
    int turn=w.Population.Turn;w.Campaign.FinishDomestic(w);w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false,allPlanned:true);
    Check(w.Robots.ProductionIntakes.Count==0 && w.Campaign.LastCancelledWars.Count==0 && w.Population.Turn==turn+1 && w.Campaign.RobotBattles.LastMarches.Any(m=>m.Source==destination),"Local/adjacent manufactured intake can sortie at the immediately following delivery without an extra turn");
}
{
    var(w,a,b,c)=Fixture();var line=w.Robots.Workshops[a];line.Configure(RobotParts.Complete,1);line.SetDoNotRepeat(true);line.IntakeTarget=b;w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
    for(int i=0;i<12;i++)Add(w,b,RobotParts.Head);w.Campaign.FinishDomestic(w);
    Check(w.Robots.ProductionIntakes.Single().Status.Contains("格納枠") && w.Robots.Nodes[b].Count==12,"Unavailable intake remains queued with explicit reason, without losing its product");
}
Console.WriteLine("PASS: forecast-only operations, undo, function-transfer fallback/capacity failure, chained IDs, dependent war cancellation, manufactured local/adjacent intake, immediate-next sortie and save/resume.");
foreach(bool full in new[]{false,true}){
    var(w,a,b,c)=Fixture();var head=Add(w,b,RobotParts.Head);
    w.Robots.Workshops[b].AutoAssemblyDisabled=false;
    for(int i=1;i<(full?12:11);i++)Add(w,b,RobotParts.Complete);
    var line=w.Robots.Workshops[a];line.Configure(RobotParts.Body|RobotParts.Legs,1);line.SetDoNotRepeat(true);line.IntakeTarget=b;
    w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
    Check(w.Robots.ProductionIntakes.Count==1,"Factory rejected compatible empty parts");
    if(!full)w.Robots.Workshops[b].AutoAssemblyDisabled=true;
    var forecast=w.Robots.PartPlanning.Preview(w).Nodes[b];
    if(full)Check(forecast.Count==12 && forecast.Robots.Single(r=>r.Id==head.Id).Parts==RobotParts.Complete,"Full hangar forecast did not fit the incoming parts");
    else Check(forecast.Count==12 && forecast.Robots.Any(r=>r.Id==w.Robots.ProductionIntakes.Single().Product.Id),"Free-slot booking must preserve its own product ID");
    if(full){
        line.Configure(RobotParts.Body,1);line.IntakeTarget=b;
        w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);
        Check(w.Robots.ProductionIntakes.Count==1 && line.Waiting,"A second line double-booked the same empty part");
    }
    w.Campaign.FinishDomestic(w);
    Check(w.Robots.ProductionIntakes.Count==0 && w.Robots.Nodes[b].Robots.Single(r=>r.Id==head.Id).Parts==(full?RobotParts.Complete:RobotParts.Head),"Delivery disagreed with forecast");
}
Console.WriteLine("PASS: whole-slot priority, full-hangar partial intake and prevention of double-booking.");
{
    var(w,a,b,c)=Fixture();w.Nodes.All[b].Owner=1;var line=w.Robots.Workshops[a];line.AutoAssemblyDisabled=false;
    foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}){line.Configure(part,1);line.SetDoNotRepeat(true);w.Robots.AdvanceIndustry(n=>w.Nodes.All[n].Owner);}
    var expected=w.Campaign.DeliverySortieRobots(w,0,a).Single();Check(expected.Parts==RobotParts.Complete && w.Robots.Nodes[a].Count==0,"Automatic assembly is visible in intake forecast without changing stock");
    w.Campaign.PlanDeliverySortie(w,new(0,b,1,a,false,false,new[]{expected.Id}));
    Check(w.Robots.PartPlanning.Orders.Count==2,"Selecting automatic forecast records its exact assembly recipe");w.Campaign.FinishDomestic(w);
    Check(w.Campaign.LastCancelledWars.Count==0 && w.Campaign.PlannedOrders.Count==1 && w.Robots.Nodes[a].Robots.Single().Parts==RobotParts.Complete,"Automatic intake recipe executes before reserved sortie");
    Console.WriteLine("PASS: automatic assembly forecast preserves immediate-next-turn sortie without requiring extra manual assembly.");
}

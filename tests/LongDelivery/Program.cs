using FolderVerse;
using System.Reflection;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
static void Tick(WorldSetup w)=>typeof(RobotLongDelivery).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(w.Robots.LongDelivery,new object[]{w});
static WorldSetup Fixture(){var w=new WorldSetup();w.SetParticipantCounts(1,1);w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);foreach(var node in w.Nodes.All)node.Owner=0;return w;}
static int Target(WorldSetup w)=>w.Nodes.All.OrderByDescending(n=>RobotLongDelivery.Path(w,0,n.Id,0).Length).First().Id;
{
    var w=Fixture();int target=Target(w);var path=RobotLongDelivery.Path(w,0,target,0);Check(path.Length>2,"Fixture is a multi-hop route");
    var robot=w.Robots.Create(0,RobotParts.Complete) with {CheapParts=RobotParts.Head,PremiumParts=RobotParts.Body};w.Robots.Nodes[0].Add(robot);
    var order=w.Robots.LongDelivery.Reserve(w,0,target,robot.Id,RobotParts.Head,null,0);
    Check(w.Robots.Nodes[0].Count==1 && w.Robots.Nodes[target].Count==0,"Reservation does not move the actual cargo");
    var held=w.Robots.Nodes[0].Robots.Single();Check(held.Stationed && !held.CanFight && !w.Robots.CanEditReservedPart(0,held,0),"Reserved cargo cannot fight or be edited");
    var middle=w.Robots.Nodes[path[1]];for(int i=0;i<middle.TotalCapacity;i++)middle.Add(w.Robots.Create(0,RobotParts.Legs));Tick(w);
    Check(!order.Started && order.Node==0 && order.Status.Contains("満杯"),"Full intermediate hangar leaves cargo intact at the current node");middle.Remove(middle.Robots.First().Id);Tick(w);
    Check(order.Started && order.Node==path[1] && w.Robots.Nodes[0].Robots.Single().Parts==(RobotParts.Body|RobotParts.Legs),"Exactly one hop moves only the selected part and leaves the rest");
    Check(middle.Robots.Single(r=>r.Id==order.CargoId).IsCheap(RobotParts.Head),"Part quality survives transshipment");
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));order=w.Robots.LongDelivery.Orders.Single();
    for(int i=2;i<path.Length;i++)Tick(w);
    Check(w.Robots.LongDelivery.Orders.Count==0 && w.Robots.Nodes[target].Robots.Single().Parts==RobotParts.Head && w.Robots.Nodes[target].Robots.Single().LongDeliveryNumber==null,"Saved relay delivers in one global hop per tick and releases the cargo");w.Robots.Check();
}
{
    var w=Fixture();int target=Target(w);var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[0].Add(robot);
    var order=w.Robots.LongDelivery.Reserve(w,0,target,robot.Id,RobotParts.Complete,null,0);Tick(w);int current=order.Node;
    w.Nodes.All[target].Owner=1;Tick(w);
    Check(w.Robots.LongDelivery.Orders.Count==0 && w.Robots.Nodes[current].Robots.Single().Id==robot.Id && w.Robots.Nodes[current].Robots.Single().LongDeliveryNumber==null,"Lost route cancels without deleting cargo at its intermediate node");
    Check(w.Robots.LongDelivery.Results.Last().Status.Contains("つながらなく"),"Cancellation reports why the route failed");
}
{
    var w=Fixture();int target=Target(w);var robot=w.Robots.Create(0,RobotParts.Head);var receiver=w.Robots.Create(0,RobotParts.Legs);w.Robots.Nodes[0].Add(robot);w.Robots.Nodes[target].Add(receiver);
    w.Robots.LongDelivery.Reserve(w,0,target,robot.Id,RobotParts.Head,receiver.Id,0);
    for(int i=1;i<RobotLongDelivery.Path(w,0,target,0).Length;i++)Tick(w);
    Check(w.Robots.Nodes[target].Robots.Single().Parts==(RobotParts.Head|RobotParts.Legs),"Final delivery joins the selected robot without changing unrelated parts");
    var legacy=AutoSaveStore.Capture(w);foreach(var obj in legacy.Objects)if(obj.Fields!=null){obj.Fields.Remove("<LongDelivery>k__BackingField");obj.Fields.Remove("<LongDeliveryNumber>k__BackingField");}
    Check(AutoSaveStore.Restore<WorldSetup>(legacy).Robots.LongDelivery.Orders.Count==0,"Old saves restore without long-distance reservations");
}
{
    var w=Fixture();int target=Target(w);int adjacent=RobotLongDelivery.Path(w,0,target,0)[^2];
    var head=w.Robots.Create(0,RobotParts.Head);var body=w.Robots.Create(0,RobotParts.Body);w.Robots.Nodes[adjacent].Add(head);w.Robots.Nodes[0].Add(body);
    w.Robots.LongDelivery.Reserve(w,adjacent,target,head.Id,RobotParts.Head,null,0,0);w.Robots.LongDelivery.Reserve(w,0,target,body.Id,RobotParts.Body,null,0,0);Tick(w);
    w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));for(int step=0;step<w.Nodes.All.Length && w.Robots.LongDelivery.Orders.Count>0;step++)Tick(w);
    Check(w.Robots.Nodes[target].Robots.Single().Parts==(RobotParts.Head|RobotParts.Body),"A later parcel joins the same destination slot after saving and reloading");
    w=Fixture();target=Target(w);for(int n=0;n<2;n++){var part=w.Robots.Create(0,RobotParts.Head);w.Robots.Nodes[0].Add(part);w.Robots.LongDelivery.Reserve(w,0,target,part.Id,RobotParts.Head,null,0,n);}
    var old=AutoSaveStore.Capture(w);foreach(var obj in old.Objects)if(obj.Fields!=null){obj.Fields.Remove("<TargetSlot>k__BackingField");obj.Fields.Remove("_slotArrivals");}
    w=AutoSaveStore.Restore<WorldSetup>(old);for(int step=0;step<w.Nodes.All.Length && w.Robots.LongDelivery.Orders.Count>0;step++)Tick(w);
    Check(w.Robots.Nodes[target].Count==2,"Legacy shipments to empty slots remain independent");
}
{
    var battle=new BattleState(1,new()){Kind=BattleKind.Node,Defender=1,FlagStanding=false};
    var robot=new Robot(99,0,RobotParts.Complete);battle.Units.Add(new(){Id=99,Owner=0,Robot=robot});battle.Events.Add(new(1,BattlePhase.Flag,99,"旗を倒した"));
    var lessons=new BattleLessons();lessons.ObserveEmptyFlag(battle);Check(lessons.Pending.Single().Owner==0,"Only the attacker learns the undefended-flag lesson");lessons.Research();Check((lessons.Owned[1]&BattleLesson.EmptyHome)==0,"Absent defender never receives the lesson");
    battle.Units.Add(new(){Id=100,Owner=1,Robot=new(100,1,RobotParts.Complete),Fallen=true});var defended=new BattleLessons();defended.ObserveEmptyFlag(battle);Check(defended.Pending.Count==0,"Defeating guards is not an absent-guard lesson");
    var w=Fixture();w.Lessons.Owned[0]|=BattleLesson.EmptyHome;w.Robots.Nodes[0].Add(w.Robots.Create(0,RobotParts.Complete));w.Robots.Nodes[0].Add(w.Robots.Create(0,RobotParts.Complete));
    Check(BattleLessons.ReserveHomeGuards(w,0,0)==1 && w.Robots.Nodes[0].Robots.Count(r=>r.DefenseOnly)==1 && BattleLessons.ReserveHomeGuards(w,0,0)==0,"Learned domestic policy reserves exactly one guard and is idempotent");
    Check(w.Campaign.RobotBattles.SelectableRobots(w,0,0,false).Length==1,"Only the unreserved soldier may sortie");
}
{
    var relay=Fixture();int far=Target(relay);var complete=relay.Robots.Create(0,RobotParts.Complete);relay.Robots.Nodes[0].Add(complete);
    foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})relay.Robots.LongDelivery.Reserve(relay,0,far,complete.Id,part,null,0);
    Check(relay.Robots.LongDelivery.Orders.Count==3,"Disjoint parts of one robot may have independent reservations");
    int hops=RobotLongDelivery.Path(relay,0,far,0).Length-1;
    for(int i=0;i<hops;i++)Tick(relay);
    Check(relay.Robots.LongDelivery.Orders.Count==0 && relay.Robots.Nodes[far].Robots.Count==1 && relay.Robots.Nodes[far].Robots.Single().Parts==RobotParts.Complete,"Three parcels to the same destination slot assemble into one complete robot");
    int hazards=0,icePoints=0,iceWalls=0;
    for(int seed=0;seed<12;seed++){
        var w=Fixture();w.SetWorld(seed);w.SetPlacement(3);
        Check(w.Cells.All(c=>w.Nodes.InCell(c.Id).Any()),"Every region keeps at least one location after whirlpool generation");
        Check(w.Routes.ComponentsAfterRepair==1,"Whirlpools preserve global reachability");
        foreach(var cell in w.Cells){
            foreach(var edge in w.Routes.Segments(cell.Id)) { Check(!w.Routes.PackIceAt(cell.Id,edge.A) && !w.Routes.PackIceAt(cell.Id,edge.B),"Routes never enter pack ice"); }
            foreach(var edge in w.Routes.Segments(cell.Id))Check(!w.Routes.WhirlpoolAt(cell.Id,edge.A) && !w.Routes.WhirlpoolAt(cell.Id,edge.B),"No route crosses a whirlpool");
            for(int y=0;y<10;y++)for(int x=0;x<10;x++)if(w.Routes.WhirlpoolAt(cell.Id,new(x,y)))Check(w.Nodes.At(cell.Id,new(x,y))==null && !w.Routes.Walkable(cell.Id,new(x,y)),"A hazardous sea point has neither a location nor a passage");
        }
        if(w.Routes.Whirlpools.Length>0 && hazards==0){
            var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.Routes.Whirlpools.SequenceEqual(w.Routes.Whirlpools),"Whirlpool placement survives saves");
            var legacy=AutoSaveStore.Capture(w);foreach(var obj in legacy.Objects)if(obj.Fields!=null)obj.Fields.Remove("_whirlpools");Check(AutoSaveStore.Restore<WorldSetup>(legacy).Routes.Whirlpools.Length==0,"Older saves retain their original terrain without new whirlpools");
        }
        foreach(var cell in w.Cells)for(int y=0;y<10;y++)for(int x=0;x<10;x++)if(w.Routes.PackIceAt(cell.Id,new(x,y))){icePoints++;Check(w.IsPolarFace(cell.Face) && !w.Routes.Walkable(cell.Id,new(x,y)) && w.Nodes.At(cell.Id,new(x,y))==null,"Ice exists only on polar faces and has no location");}
        iceWalls+=w.Routes.IceWalls.Length;
        Check(w.Routes.Whirlpools.All(h=>!w.IsPolarFace(w.Cells[h.Cell].Face)),"Ordinary seas have whirlpools and polar seas have ice");
        var connectivity=typeof(TerrainRoutes).GetMethod("IceNetworkConnected",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Check((bool)connectivity.Invoke(w.Routes,null)!,"Actual post-ice navigable network is connected");
        if(seed==0){var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));Check(restored.Routes.IceWalls.SequenceEqual(w.Routes.IceWalls),"Ice walls survive saving");var old=AutoSaveStore.Capture(w);foreach(var obj in old.Objects)if(obj.Fields!=null){obj.Fields.Remove("_packIce");obj.Fields.Remove("_iceWalls");}Check(AutoSaveStore.Restore<WorldSetup>(old).Routes.IceWalls.Length==0,"Old saves do not acquire ice walls");}
        if(w.Routes.Whirlpools.Length>0){
            w.SetTerritoryRights(3);int offset=0;foreach(int ruler in w.LandOrder()){var claim=Enumerable.Range(offset,w.TerritoryRights[ruler]).ToArray();w.ClaimLand(ruler,claim);offset+=claim.Length;}
            w.CompleteLand(4,25);var nearby=w.Nodes.All.Where(w.Routes.NearWhirlpool).ToArray();Check(nearby.Length>0,"Whirlpools have neighboring sea locations");
            foreach(var node in nearby){Check(w.Robots.Nodes[node.Id].TotalCapacity==1,"Hazard-adjacent sea locations start with one storage slot");Check(w.Robots.Workshops[node.Id].ProductionLines.Length==0,"Hazard-adjacent sea locations have no initial manufacturing lane");}
            var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));restored.Robots.ConfigureFactories(restored);Check(nearby.All(n=>restored.Robots.Nodes[n.Id].TotalCapacity==1 && restored.Robots.Workshops[n.Id].ProductionLines.Length==0),"Loading does not regenerate missing hazard factories");
        }
        hazards+=w.Routes.Whirlpools.Length;
    }
    Console.WriteLine($"PASS: {hazards} sea whirlpools across twelve worlds; every region has a location, every region remains reachable and no route or location enters a whirlpool.");
    Console.WriteLine($"Polar ice: {icePoints} blocked samples, {iceWalls} closed crossings across twelve worlds.");Check(icePoints>0 && iceWalls>0,"Polar ice actually narrows navigation");
    Check(hazards>0,"New worlds actually contain sea whirlpools");
}
Console.WriteLine("PASS: multi-hop relay, capacity waits, exact parts, quality, save/legacy, route cancellation, recipient assembly and attacker-only empty-home lesson with automatic defense reservation.");

{var w=new WorldSetup();w.PrepareTutorialStepThree(1,2,3);Check(w.Routes.IceWalls.Length==0 && w.Routes.Whirlpools.Length==0,"Tutorial maps keep their original terrain");}

using FolderVerse;

static class DeliverySortiesChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static (WorldSetup W,int A,int B,int C) Fixture()
    {
        var w=new WorldSetup();w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.Relations.Reset();w.SelectPlayer(0);w.HumanPlayers.Add(0);
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.Robots.ConfigureFactories(w);
        foreach(var s in w.Robots.Workshops)s.AutoAssemblyDisabled=true;
        w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
        var a=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);
        var r=w.Routes.Neighbors(a)[0];var b=w.Nodes.At(r.Target,r.Entry);
        int c=w.Routes.Neighbors(b).Select(x=>w.Nodes.At(x.Target,x.Entry).Id).First(n=>n!=a.Id);
        a.Owner=b.Owner=0;w.Nodes.All[c].Owner=1;return(w,a.Id,b.Id,c);
    }
    static Robot Add(WorldSetup w,int n,RobotParts p){var r=w.Robots.Create(0,p);w.Robots.Nodes[n].Add(r);return r;}
    static long Incoming(WorldSetup w,int a,int b)
    {
        var r=Add(w,a,RobotParts.Complete);w.Robots.DirectDelivery.ReserveParts(w,a,b,r.Id,r.Parts,null,0);
        var preview=w.Campaign.DeliverySortieRobots(w,0,b).Single();
        Check(preview.Id<0 && preview.Parts==RobotParts.Complete,"Projected split/merge robot has stable provisional identity");
        Check(w.Robots.Nodes[a].Robots.Single().Id==r.Id && w.Robots.Nodes[b].Count==0,"Preview never changes real stock");return preview.Id;
    }
    static void Plan(WorldSetup w,int b,int c,params long[] ids)=>w.Campaign.PlanDeliverySortie(w,new(0,c,ids.Length,b,false,false,ids));
    public static void Run()
    {
        {
            var(w,a,b,c)=Fixture();long id=Incoming(w,a,b);Plan(w,b,c,id);
            Check(w.Campaign.PlannedOrders.Single().DeliveryMembers!.Single().DeliveryNumbers.Length==3,"All three incoming parts are dependencies");
            w=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));w.Robots.ConfigureFactories(w);
            Check(w.Campaign.DeliverySortieRobots(w,0,b).Single().Id==id,"Arrival forecast survives save/load");
            int turn=w.Population.Turn;w.Campaign.FinishDomestic(w);var arrived=w.Robots.Nodes[b].Robots.Single();
            Check(w.Campaign.PlannedOrders.Count==1,"Expected arrival reservation: "+string.Join(";",w.Campaign.LastCancelledWars.Select(x=>x.Reason))+" receipts="+string.Join(",",w.Robots.DirectDelivery.CompletedArrivals.Select(x=>$"{x.Key}:{x.Value}")));
            Check(w.Campaign.PlannedOrders.Single().RobotIds!.Single()==arrived.Id && arrived.Id>0,"Arrival IDs bind to actual completed robot");
            w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false,allPlanned:true);
            Check(w.Population.Turn==turn+1 && w.Campaign.LastCancelledWars.Count==0 && w.Campaign.RobotBattles.LastMarches.Any(x=>x.Source==b && x.Target==c),"Forecast robot sorties in the same turn");
        }
        {
            var(w,a,b,c)=Fixture();long id=Incoming(w,a,b);var teammate=Add(w,b,RobotParts.Complete);Plan(w,b,c,id,teammate.Id);
            int d=w.Routes.Neighbors(w.Nodes.All[c]).Select(x=>w.Nodes.At(x.Target,x.Entry).Id).First(n=>n!=b);w.Nodes.All[d].Owner=0;
            var support=Add(w,d,RobotParts.Complete);Plan(w,d,c,support.Id);
            w.Robots.DirectDelivery.Cancel(w.Robots.DirectDelivery.Orders.First().Number);w.Campaign.FinishDomestic(w);
            Check(w.Campaign.PlannedOrders.Count==0 && w.Campaign.LastCancelledWars.Count==1,"One missing member cancels every source of the coordinated war");
            Check(w.Campaign.LastCancelledWars[0].Reason.Contains("直送便"),"Cancellation explains missing delivery");
            w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false,allPlanned:true);
            Check(w.Campaign.RobotBattles.LastMarches.Count==0 && w.Robots.Nodes[d].Robots.Any(r=>r.Id==support.Id),"No substitute or surviving teammate departs");
            var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
            Check(saved.Campaign.LastCancelledWars.Single().Turn==w.Population.Turn-1,"Previous global turn's cancellation reason survives save/load");
            w.Campaign.FinishDomestic(w);Check(w.Campaign.LastCancelledWars.Count==0,"History expires after the next planning turn");
        }
        {
            var(w,a,b,c)=Fixture();long id=Incoming(w,a,b);Plan(w,b,c,id);
            for(int i=0;i<12;i++)Add(w,b,RobotParts.Legs);
            w.Campaign.FinishDomestic(w);
            Check(w.Campaign.PlannedOrders.Count==0 && w.Campaign.LastCancelledWars.Count==1 && w.Robots.DirectDelivery.Orders.Count>0,"Capacity-blocked delivery cancels war while delivery waits");
        }
        {
            var(w,a,b,c)=Fixture();var head=Add(w,b,RobotParts.Head);var rest=Add(w,a,RobotParts.Body|RobotParts.Legs);
            w.Robots.DirectDelivery.ReserveParts(w,a,b,rest.Id,rest.Parts,head.Id,0);Plan(w,b,c,head.Id);
            w.Campaign.FinishDomestic(w);
            Check(w.Campaign.PlannedOrders.Single().RobotIds!.Single()==head.Id && w.Robots.Nodes[b].Robots.Single().Parts==RobotParts.Complete,"Existing arrival anchor is not locked out by war reservation");
        }
        {
            var(w,a,b,c)=Fixture();var r=Add(w,b,RobotParts.Complete);Plan(w,b,c,r.Id);w.Robots.Nodes[b].Remove(r.Id);
            w.Campaign.FinishDomestic(w);Check(w.Campaign.PlannedOrders.Count==0 && w.Campaign.LastCancelledWars.Single().Reason.Contains("完成体"),"Missing existing member cancels entire planned sortie");
        }
        {
            var(w,a,b,c)=Fixture();var first=Add(w,b,RobotParts.Complete);var second=Add(w,b,RobotParts.Complete);var separate=Add(w,b,RobotParts.Complete);
            w.Nodes.All[a].Owner=1;Plan(w,b,c,first.Id,second.Id);Plan(w,b,a,separate.Id);
            Check(w.Robots.ToggleReservation(b,first.Id,HangarReservation.Defense),"Selected member can change role explicitly");
            w.Campaign.FinishDomestic(w);
            Check(w.Campaign.PlannedOrders.Count==1 && w.Campaign.PlannedOrders.Single().TargetNode==a && w.Campaign.LastCancelledWars.Single().Target==c,"Member cancellation cancels its whole war, preserving another target");
        }
        {
            var(w,a,b,c)=Fixture();long id=Incoming(w,a,b);var ready=Add(w,b,RobotParts.Complete);Plan(w,b,c,id,ready.Id);
            w.Campaign.FinishDomestic(w);long arrival=w.Campaign.PlannedOrders.Single().RobotIds!.First(x=>x!=ready.Id);
            w.Robots.Nodes[b].Remove(arrival);w.Robots.RevalidateWorkReservations();
            w.Campaign.BeginMilitary();w.Campaign.Advance(w,-1,0,enemies:false,allPlanned:true);
            Check(w.Campaign.LastCancelledWars.Count==1 && w.Campaign.RobotBattles.LastMarches.Count==0,"Revalidation cannot silently shrink a resolved arrival lineup");
        }
        Console.WriteLine("PASS: delivery forecast, identity binding, same-turn sortie, atomic coordinated cancellation, capacity waiting and persisted previous-turn reasons.");
    }
}

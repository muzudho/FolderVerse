using FolderVerse;
using System.Reflection;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static WorldSetup Fixture(){
    var w=new WorldSetup();w.SetWorld(1);w.SetCast(2);w.SetPlacement(3);w.SelectPlayer(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);w.HumanPlayers.Clear();w.HumanPlayers.Add(0);
    foreach(var n in w.Nodes.All)n.Owner=-1;
    w.Nodes.All[0].Owner=0;
    var seen=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);int count=0;
    while(queue.TryDequeue(out int id) && count<10)foreach(var route in w.Routes.Neighbors(w.Nodes.All[id])){
        int next=w.Nodes.At(route.Target,route.Entry).Id;if(!seen.Add(next))continue;
        w.Nodes.All[next].Owner=1;count++;queue.Enqueue(next);if(count==10)break;
    }
    w.Nodes.Recount();return w;
}
{
    var w=Fixture();var c=new Captive(1,0,0);w.Relations.Pending.Add(c);
    Check(w.Relations.SetMercyOffer(w,c,8),"Loser may offer eight of ten owned nodes");
    int demand=w.Relations.ComputerMercyDemand(w,c);w.Relations.SetMercyOffer(w,c,2);
    Check(w.Relations.ComputerMercyDemand(w,c)==demand,"The CPU demand cannot inspect the hidden offer");
    w.Relations.SetMercyOffer(w,c,8);
    var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(saved.Relations.MercyOffers.Single().Nodes.Length==8,"The secret offer survives saving");
    var result=saved.Relations.ResolveMercy(saved,c,3);
    Check(result.Success && result.Nodes.Length==8 && saved.Relations.OwnedMercyNodes(saved,c)==2 && saved.Relations.Powered[1],"Demand three transfers the actual offered eight and spares the loser");
    var connected=new HashSet<int>{0};var q=new Queue<int>();q.Enqueue(0);
    while(q.TryDequeue(out int id))foreach(var route in saved.Routes.Neighbors(saved.Nodes.All[id])){
        int next=saved.Nodes.At(route.Target,route.Entry).Id;if(saved.Nodes.All[next].Owner==0 && connected.Add(next))q.Enqueue(next);
    }
    Check(result.Nodes.All(connected.Contains),"A contiguous surrender cluster touches the captor's territory");
}
{
    var w=Fixture();var c=new Captive(1,0,0);w.Relations.Pending.Add(c);w.Relations.SetMercyOffer(w,c,8);
    var result=w.Relations.ResolveMercy(w,c,9);
    Check(!result.Success && !w.Relations.Powered[1] && !w.Nodes.All.Any(n=>n.Owner==1) && w.Nodes.All.Count(n=>n.Owner==0)==1,"Failed negotiation removes the battery without gifting territory");
}
{
    var w=Fixture();foreach(var n in w.Nodes.All)n.Owner=-1;w.Nodes.All[0].Owner=0;
    foreach(int id in new[]{1,2,3,4})w.Nodes.All[id].Owner=1;w.Nodes.All[1].FunctionHost=2;w.Nodes.All[1].FunctionUnits=0;w.Nodes.All[2].FunctionUnits=2;
    var c=new Captive(1,0,0);w.Relations.Pending.Add(c);
    Check(w.Relations.OwnedMercyNodes(w,c)==3 && w.Relations.MercyAmounts(w,c).SequenceEqual(new[]{0,1,2,3}) && !w.Relations.SetMercyOffer(w,c,4),"Transferred groups count as one functional location");
    w.Relations.SetMercyOffer(w,c,3);var result=w.Relations.ResolveMercy(w,c,3);
    Check(result.Offer==3 && result.Nodes.Length==4 && w.Nodes.All[1].Owner==0 && w.Nodes.All[2].Owner==0,"The complete function group changes ownership together");
}
{
    var w=Fixture();foreach(var n in w.Nodes.All)n.Owner=-1;w.Nodes.All[0].Owner=0;w.Nodes.All[1].Owner=w.Nodes.All[2].Owner=1;
    var c=new Captive(1,0,0);w.Relations.Pending.Add(c);
    Check(!w.Relations.CanMercy(w,c) && !w.Relations.Choose(w,c,DefeatedChoice.Release) && w.Relations.Pending.Contains(c),"Below three functional locations mercy is unavailable without silently executing another choice");
    w.Nodes.All[3].Owner=1;
    Check(w.Relations.CanMercy(w,c) && w.Relations.SetMercyOffer(w,c,1) && w.Relations.SetMercyOffer(w,c,2) && w.Relations.SetMercyOffer(w,c,3),"Three locations permit offering one, two or all three");
}
{
    var w=Fixture();w.Relations.Capture(w,0,1,0);
    Check(w.Relations.Pending.Any(c=>c.Ruler==0) && w.Relations.MercyOffers.Count==0 && w.Relations.Powered[0],"A human captive awaits their own hidden offer input");
    w=Fixture();w.HumanPlayers.Clear();w.Relations.Leader=-1;
    var keyword=ConquerorCatalog.Keywords.First(k=>k.Personality==3);w.Looks[1]=new(keyword.BaseId,keyword.VariantId);
    w.Relations.Capture(w,1,0,0);
    Check(w.Relations.MercyResults.Count==1 && w.Relations.MercyResults[0].Success && w.Relations.MercyResults[0].Offer>0,"CPU-versus-CPU mercy actually transfers land");
}
{
    var w=Fixture();
    void Remove(int ruler){var c=new Captive(ruler,0,0);w.Relations.Pending.Add(c);w.Relations.Choose(w,c,DefeatedChoice.RemoveBattery);}
    Remove(4);Remove(2);
    Check(w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==4).Rank>w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==2).Rank,"Earlier battery removal ranks lower, even in the same turn");
    w.Relations.RestoreBattery(w,4);
    Check(w.Relations.EliminationOrders[4]==0 && w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==4).Rank<w.Relations.ProvisionalRanking(w).Single(r=>r.Ruler==2).Rank,"System-menu revival restores active ranking");
    Remove(4);Check(w.Relations.EliminationOrders[4]>w.Relations.EliminationOrders[2],"A revived conqueror receives a new elimination order");
    var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(restored.Relations.ProvisionalRanking(restored).Single(r=>r.Ruler==4).Rank<restored.Relations.ProvisionalRanking(restored).Single(r=>r.Ruler==2).Rank,"Elimination ranking survives saving");
    var legacy=AutoSaveStore.Capture(w);
    foreach(var item in legacy.Objects)if(item.Fields!=null)foreach(string field in new[]{"<MercyOffers>k__BackingField","<MercyResults>k__BackingField","<EliminationOrders>k__BackingField","_nextEliminationOrder"})item.Fields.Remove(field);
    AutoSaveStore.Restore<WorldSetup>(legacy);
}
{
    var w=Fixture();w.HumanPlayers.Clear();
    w.ConquerorLocations[0]=w.Nodes.All[0].Cell;w.ConquerorPoints[0]=w.Nodes.All[0].Center;
    for(int i=0;i<6;i++)w.Robots.Nodes[0].Add(w.Robots.Create(0,RobotParts.Complete));
    var clone=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    foreach(var n in w.Nodes.All)n.Population.People[0]=0;
    foreach(var n in clone.Nodes.All)n.Population.People[0]=100000000;
    March[] Orders(WorldSetup world){March[] result=Array.Empty<March>();world.Campaign.Advance(world,-1,0,automaticAll:true,deferResolution:orders=>{result=orders.ToArray();return true;});return result;}
    Check(Orders(w).SequenceEqual(Orders(clone)),"Changing only population cannot alter robot AI attack orders");
}
Console.WriteLine("PASS: negotiated surrender counts and connected groups, hidden offers, CPU/human capture paths, failure, elimination order, revival, legacy saves and population-independent robot attack decisions.");

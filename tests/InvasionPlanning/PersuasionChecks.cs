using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;

static class PersuasionChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static (WorldSetup World,Node Home,int[] Targets) Fixture()
    {
        var w=new WorldSetup();w.SetWorld(2);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
        w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);
        var home=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().Count()>=2);
        var targets=w.Routes.Neighbors(home).Select(r=>w.Nodes.At(r.Target,r.Entry).Id).Distinct().Take(2).ToArray();
        home.Owner=0;foreach(int id in targets)w.Nodes.All[id].Owner=1;
        w.ConquerorLocations[0]=home.Cell;w.ConquerorPoints[0]=home.Center;
        w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=100};
        return(w,home,targets);
    }
    static void Persuade(BattleState battle)=>typeof(BattleTurnResolver).GetMethod("Persuade",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{battle});
    public static void Run()
    {
        {
            var(w,home,targets)=Fixture();var a=w.Robots.Create(0,RobotParts.Complete);var b=w.Robots.Create(0,RobotParts.Complete);
            w.Robots.Nodes[home.Id].Add(a);w.Robots.Nodes[home.Id].Add(b);
            w.Campaign.Plan(new(0,targets[0],1,home.Id,false,false,new[]{a.Id}));
            w.Campaign.Plan(new(0,targets[1],1,home.Id,false,false,new[]{b.Id}));
            Check(w.Campaign.PlannedOrders.Count==2,"Different robots in one hangar retain different destinations");
            var save=AutoSaveStore.Restore<ConquestCampaign>(AutoSaveStore.Capture(w.Campaign));
            Check(save.PlannedOrders.Count==2,"Split reservations survive autosave");
            bool rejected=false;try{w.Campaign.Resolve(w,new[]{new March(0,targets[0],1,home.Id,false,false,new[]{a.Id}),new March(0,targets[1],1,home.Id,false,false,new[]{a.Id})});}catch(ArgumentException){rejected=true;}
            Check(rejected && w.Robots.Nodes[home.Id].Count==2 && w.Population.Turn==0,"Duplicate robot destinations fail before any mutation");
            w.Campaign.Resolve(w,w.Campaign.PlannedOrders.ToArray());
            Check(w.Campaign.RobotBattles.LastMarches.Count==2 && w.Robots.Nodes[home.Id].Count==0,"Both destinations deploy from one hangar");
        }
        {
            var(w,home,targets)=Fixture();var a=w.Robots.Create(0,RobotParts.Complete);var b=w.Robots.Create(0,RobotParts.Complete);
            w.Campaign.Plan(new(0,targets[0],2,home.Id,false,false,new[]{a.Id,b.Id}));
            w.Campaign.Plan(new(0,targets[1],1,home.Id,false,false,new[]{a.Id}));
            Check(w.Campaign.PlannedOrders.Count==2 && w.Campaign.PlannedOrders.Single(o=>o.TargetNode==targets[0]).RobotIds.SequenceEqual(new[]{b.Id}),"Reassigning one robot preserves its former companions");
            w.Campaign.Reset();w.Campaign.Plan(new(0,targets[0],1,home.Id,true,true,new[]{a.Id}));w.Campaign.Plan(new(0,targets[1],1,home.Id,true,true,new[]{b.Id}));
            Check(w.Campaign.PlannedOrders.Count==2 && w.Campaign.PlannedOrders.Count(o=>o.Queen)==1,"Pocket robots split while their queen retains one destination");
        }
        {
            var battle=new BattleState(123,new(){PersuasionPercent=100}){GlobalTurn=7};battle.Armies.Add(new(){Owner=0});battle.Armies.Add(new(){Owner=1});
            var original=new Robot(11,1,RobotParts.Complete){DamagedParts=RobotParts.Body,CheapParts=RobotParts.Legs};
            battle.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(2,2)});
            var unit=new BattleUnit{Id=11,Owner=1,Robot=original,Position=new(3,2)};battle.Units.Add(unit);
            battle.Capture(BattlePhase.Persuade);Persuade(battle);battle.Capture(BattlePhase.Converted);
            Check(unit.Owner==0 && unit.Robot.Origin(RobotParts.Head)==0 && unit.Robot.Origin(RobotParts.Legs)==0 && unit.Robot.Origin(RobotParts.Body)==1,"Battle conversion changes intact part colors and preserves damaged origins");
            Check(unit.Robot.DamagedParts==RobotParts.Body && unit.Robot.CheapParts==RobotParts.Legs,"Persuasion does not repair damage or remove cheap stripes");
            Check(battle.Frames[0].Units.Single(u=>u.Id==11).Robot.Origin(RobotParts.Head)==1 && battle.Frames[1].Units.Single(u=>u.Id==11).Robot.Origin(RobotParts.Head)==0,"Replay snapshots change color at success, never before");
            var queen=new BattleUnit{Id=-3,Owner=2,Role=BattleRole.Queen,Position=new(3,3)};battle.Units.Add(queen);battle.Armies.Add(new(){Owner=2});
            Persuade(battle);Check(unit.Owner==0,"A second queen cannot reconvert the same robot in this global turn");
            var restored=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(battle));
            Check(restored.Units.Single(u=>u.Id==11).Robot.LastBattlePersuasionTurn==7,"Battle success cap survives save and continuation");
        }
        {
            var battle=new BattleState(9,new(){PersuasionPercent=0}){GlobalTurn=2};battle.Armies.Add(new(){Owner=0});battle.Armies.Add(new(){Owner=1});
            battle.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(2,2)});battle.Units.Add(new(){Id=10,Owner=1,Position=new(3,2),Robot=new(10,1,RobotParts.Complete)});
            Persuade(battle);Persuade(battle);Check(battle.Persuasions.Count==2 && battle.Persuasions.All(p=>!p.Success),"Failed combat persuasion can be retried without a limit");
        }
        {
            var battle=new BattleState(5,new(){PersuasionPercent=100}){GlobalTurn=3};battle.Armies.Add(new(){Owner=0});
            battle.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(2,2)});var unit=new BattleUnit{Id=12,Owner=0,Position=new(3,2),Robot=new(12,0,RobotParts.Complete){HeadOrigin=1,BodyOrigin=1}};battle.Units.Add(unit);
            Persuade(battle);Check(unit.Robot.Origin(RobotParts.Head)==0 && unit.Robot.Origin(RobotParts.Body)==1,"Own mixed robot converts one foreign part per success");
            Persuade(battle);Check(unit.Robot.Origin(RobotParts.Body)==1,"Own mixed robot also respects the success cap");
            battle.GlobalTurn++;Persuade(battle);Check(unit.Robot.Origin(RobotParts.Body)==0,"Next global turn permits another part conversion");
        }
        {
            var(w,home,targets)=Fixture();var enemy=w.Robots.Create(1,RobotParts.Complete) with {DamagedParts=RobotParts.Body};var mixed=w.Robots.Create(0,RobotParts.Complete) with {HeadOrigin=1};
            w.Robots.Nodes[home.Id].Add(enemy);w.Robots.Nodes[home.Id].Add(mixed);
            var events=w.Robots.PersuadeAtHome(w).Where(e=>e.Ruler==0).ToList();
            Check(events.Count==2 && events[0].Before.Owner==1 && events.All(e=>e.Success),"Domestic persuasion tries foreign robots before own mixed robots");
            Check(events.All(e=>new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Count(p=>e.Before.Origin(p)!=e.After.Origin(p))==1),"Each domestic success changes exactly one part");
            Check(events[0].After.Owner==0 && events[0].After.Origin(RobotParts.Body)==1 && events[0].After.IsDamaged(RobotParts.Body),"Domestic success recruits without repairing damage");
            Check(w.Robots.PersuadeAtHome(w).Count==0,"Opening the inspector again never rerolls domestic persuasion");
            var robots=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(w.Robots));Check(robots.DomesticPersuasionTurns[0]==0 && robots.Nodes[home.Id].Robots.All(r=>r.LastDomesticPersuasionTurn==0),"Domestic attempt limits and colors survive save");
            var game=(Game1)RuntimeHelpers.GetUninitializedObject(typeof(Game1));typeof(Game1).GetField("_homePersuasion",Flags)!.SetValue(game,events);typeof(Game1).GetField("_homePersuasionAge",Flags)!.SetValue(game,5d);
            string Message()=>(string)typeof(Game1).GetProperty("HomePersuasionMessage",Flags)!.GetValue(game)!;
            Check(Message()=="やったあ！　こんにちわです～！","Foreign-robot success message has first priority");events.RemoveAt(0);Check(Message()=="これからも、よろしくお願いします～！","Own-part success message has second priority");
            var doc=AutoSaveStore.Capture(robots);foreach(var entry in doc.Objects)if(entry.Fields!=null){entry.Fields.Remove("<LastBattlePersuasionTurn>k__BackingField");entry.Fields.Remove("<LastDomesticPersuasionTurn>k__BackingField");entry.Fields.Remove("<DomesticPersuasionTurns>k__BackingField");}
            var old=AutoSaveStore.Restore<RobotWorld>(doc);Check(old.DomesticPersuasionTurns.Count==0 && old.Nodes[home.Id].Robots.All(r=>r.LastDomesticPersuasionTurn==-1 && r.LastBattlePersuasionTurn==-1),"Old saves receive safe persuasion defaults");
        }
        {
            var(w,home,targets)=Fixture();var campaign=w.Campaign.RobotBattles;var original=w.Robots.Create(1,RobotParts.Complete);w.Robots.Nodes[targets[0]].Add(original);
            typeof(RobotCampaign).GetMethod("Depart",Flags)!.Invoke(campaign,new object[]{w,targets[0],original,false});
            var converted=original.Persuaded(0,RobotParts.Complete,true);var unit=new BattleUnit{Id=original.Id,Owner=0,Robot=converted};
            var battle=new BattleState(2){Winner=-1};battle.Armies.Add(new(){Owner=0,HomeNode=home.Id});
            typeof(RobotCampaign).GetMethod("ReturnUnits",Flags)!.Invoke(campaign,new object[]{w,new RobotEncounter(),battle,new[]{unit}});
            Check(w.Robots.Nodes[home.Id].Robots.Single().Owner==0 && w.Robots.Nodes[targets[0]].Count==0,"A recruited robot returns to its new country's home instead of the enemy store");
            var second=w.Robots.Create(1,RobotParts.Complete);w.Robots.Nodes[targets[0]].Add(second);
            typeof(RobotCampaign).GetMethod("Depart",Flags)!.Invoke(campaign,new object[]{w,targets[0],second,false});
            while(w.Robots.Nodes[home.Id].HasSpace)w.Robots.Nodes[home.Id].Add(w.Robots.Create(0,RobotParts.Complete));
            var next=new BattleUnit{Id=second.Id,Owner=0,Robot=second.Persuaded(0,RobotParts.Complete,true)};
            typeof(RobotCampaign).GetMethod("ReturnUnits",Flags)!.Invoke(campaign,new object[]{w,new RobotEncounter(),battle,new[]{next}});
            Check(campaign.Reserves.Any(r=>r.Node==home.Id && r.Robot.Id==second.Id) && w.Robots.Nodes[targets[0]].ReservedReturns==0,"A full friendly hangar keeps recruited robots in reserve without restoring enemy ownership");
        }
        Console.WriteLine("PASS: split destinations, atomic duplicate rejection, recruitment colors, replay snapshots, persuasion limits, domestic priority and save compatibility");
    }
}

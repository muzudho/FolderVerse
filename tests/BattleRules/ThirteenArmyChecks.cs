using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;

static class ThirteenArmyChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static BattleState Create(BattleTerrain terrain,int count=13)
    {
        var stock=new RobotWorld();stock.Initialize(1,20);
        var battle=new BattleState(73,new(){NoWillTurns=100,FlagDefenseTurns=500,PersuasionPercent=0}){Kind=BattleKind.Node,Defender=0};
        battle.BuildTerrain(terrain);
        for(int owner=0;owner<count;owner++)battle.AddArmy(owner,0,Enumerable.Range(0,8).Select(_=>stock.Create(owner,RobotParts.Complete)),preferredSide:owner==0?-1:(owner-1)/3);
        battle.ChooseLayout();battle.Deploy();battle.BuildFortifications(12);return battle;
    }
    static int Distance(BattleState b,Point from,Point? destination=null)
    {
        var queue=new Queue<(Point Point,int Steps)>();var seen=new HashSet<Point>{from};queue.Enqueue((from,0));
        while(queue.TryDequeue(out var item)){
            if(item.Point==(destination??b.Flag))return item.Steps;
            foreach(BattleFacing face in Enum.GetValues<BattleFacing>()){
                var next=item.Point+BattleState.Forward(face);
                if(b.Walkable(next) && seen.Add(next))queue.Enqueue((next,item.Steps+1));
            }
        }
        return -1;
    }
    public static void Run()
    {
        foreach(var terrain in Enum.GetValues<BattleTerrain>()){
            var b=Create(terrain);b.Check();
            Check(b.EntryCapacity==13 && b.Armies.Count==13 && b.Armies.Count(a=>a.Side==4)==1,"13 armies fit on one node battlefield");
            Check(Enumerable.Range(0,4).All(side=>b.Armies.Count(a=>a.Side==side)==3),"Every edge has three distinct army entries");
            Check(b.Units.Count==39 && b.Armies.All(a=>a.Waiting.Count==5),"Three initial soldiers per army; surplus soldiers remain queued");
            if(terrain==BattleTerrain.Mountain){
                Check(Enumerable.Range(3,7).Count(x=>b.Walkable(new(x,3)))==1,"The northern mountain ridge keeps exactly one narrow pass");
            }
            foreach(var army in b.Armies.Where(a=>a.Side<4)){
                var distances=b.Units.Where(u=>u.Owner==army.Owner).Select(u=>Distance(b,u.Position)).Order().ToArray();
                Check(distances.SequenceEqual(new[]{6,7,7}),$"Fair initial flag distances survive {terrain} terrain and defenses: {string.Join(',',distances)}");
                var entries=b.ArmyEntrance(army).ToArray();
                Check(entries.Length==1,"Every outer army has one entry tile");
                var retreat=b.Units.Where(u=>u.Owner==army.Owner).Select(u=>Distance(b,u.Position,entries[0])+1).Order().ToArray();
                Check(retreat.SequenceEqual(new[]{5,6,7}),$"Fair physical retreat distances survive {terrain} terrain and defenses: {string.Join(',',retreat)}");
            }
            bool rejected=false;try{b.AddArmy(13,0,Array.Empty<Robot>());}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"A fourteenth army is rejected before changing the board");
            var army0=b.Armies.First(a=>a.Side==0 && a.EntrySlot==0);
            foreach(var unit in b.Units.Where(u=>u.Owner==army0.Owner))unit.Fallen=true;
            Check(b.Reinforce() && b.Units.Count(u=>u.Active && u.Owner==army0.Owner)==1 && b.ArmyEntrance(army0).Contains(b.Units.Last().Position),"A vacated small-boat/mountain entry admits its own queued soldier");
            var copy=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));copy.Check();
            Check(copy.Armies.Select(a=>(a.Side,a.EntrySlot)).SequenceEqual(b.Armies.Select(a=>(a.Side,a.EntrySlot))),"Distinct army entries survive autosaving");
            for(int turn=0;turn<35 && !b.Finished;turn++)BattleTurnResolver.Advance(b);
            b.Check();
            var retreatBoard=Create(terrain);var retreaters=retreatBoard.Units.Where(u=>u.Owner!=0).ToArray();
            foreach(var army in retreatBoard.Armies.Where(a=>a.Owner!=0)){
                army.Withdrawing=true;
                while(army.Waiting.TryDequeue(out var waiting)){waiting.Retreated=true;retreatBoard.Units.Add(waiting);}
            }
            for(int step=0;step<7 && !retreatBoard.Finished;step++){
                var hold=retreatBoard.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));
                BattleTurnResolver.Advance(retreatBoard,hold);
            }
            Check(retreaters.All(u=>u.Retreated && retreatBoard.HomeExit(u.Position,retreatBoard.Army(u.Owner))),"All twelve armies physically retreat through their own one-cell exits in at most seven steps");
        }
        {
            var b=Create(BattleTerrain.Field);
            foreach(var u in b.Units.Where(u=>u.Owner==0))u.Fallen=true;
            b.Reinforce();var unit=b.Units.Last();
            Check(unit.Owner==0 && unit.Position==b.Flag,"The defending army reinforces at the flag");
            unit.Position=b.Flag+new Point(0,-1);unit.Retreating=true;
            var orders=b.Units.Where(u=>u.Active).ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing));
            BattleTurnResolver.Advance(b,orders);
            Check(unit.Retreated && unit.Position==b.Flag,"The defending army retreats through the flag");
        }
        {
            var stock=new RobotWorld();stock.Initialize(1,20);
            var b=new BattleState(1){Kind=BattleKind.Node,Defender=0};b.BuildTerrain(BattleTerrain.Mountain);
            b.AddArmy(0,0,Enumerable.Range(0,8).Select(_=>stock.Create(0,RobotParts.Complete)));
            b.AddArmy(1,0,Enumerable.Range(0,8).Select(_=>stock.Create(1,RobotParts.Complete)),preferredSide:0);
            b.ChooseLayout();b.Deploy();var before=b.Units.Select(u=>(u.Id,u.Position)).ToArray();
            b.AddArmy(2,0,Enumerable.Range(0,8).Select(_=>stock.Create(2,RobotParts.Complete)),preferredSide:0);
            b.Reinforce();b.Check();
            Check(before.All(item=>b.Units.Single(u=>u.Id==item.Id).Position==item.Position),"A late third army never teleports or discards the existing combatants");
            Check(b.Armies.Count==3 && b.Army(2).EntrySlot!=b.Army(1).EntrySlot,"A late army receives a distinct entrance on the same side");
        }
        {
            var old=new BattleState(1){BoardSize=10};for(int i=0;i<4;i++)old.AddArmy(i,0,Array.Empty<Robot>());
            bool rejected=false;try{old.AddArmy(4,0,Array.Empty<Robot>());}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Legacy 10x10 battle capacity is unchanged");
            var save=AutoSaveStore.Capture(old);foreach(var item in save.Objects)item.Fields?.Remove("EntrySlot");
            var restored=AutoSaveStore.Restore<BattleState>(save);restored.Check();Check(restored.Armies.All(a=>a.EntrySlot==1),"Legacy armies default to the middle entry");
        }
        {
            var world=new WorldSetup();world.SetWorld(1);world.SetCast(2);world.SetPlacement(3);world.SelectPlayer(0);
            var armies=Enumerable.Range(0,13).Select(owner=>(Owner:owner,Home:owner,Robots:Enumerable.Range(0,8).Select(_=>world.Robots.Create(owner,RobotParts.Complete)).ToList(),Queen:false)).ToArray();
            var campaign=world.Campaign.RobotBattles;
            var e=(RobotEncounter)typeof(RobotCampaign).GetMethod("CreateEncounter",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(campaign,new object[]{world,BattleKind.Node,1,0,0,armies,null!})!;
            Check(e.Battles.Count==1 && e.Battles[0].Armies.Count==13 && !e.Battles[0].SplitBoard,"The campaign keeps a thirteen-country encounter on one board");
            typeof(RobotCampaign).GetMethod("AddReinforcement",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(campaign,new object[]{world,e,13,13,new List<Robot>(),true});
            typeof(RobotCampaign).GetMethod("PrepareSplitBoards",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{e});
            Check(e.Battles.Count==2 && e.Battles[0].Armies.Count==13,"A fourteenth country creates another board without shrinking the full board");
            foreach(var state in e.Battles)state.Check();
        }
        Console.WriteLine("PASS: 13 armies, three entries per edge, fair 6/7/7 flag distances in all terrain, queued reinforcements, central flag exit, saves and unsplit campaign encounter.");
    }
}

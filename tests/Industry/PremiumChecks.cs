using FolderVerse;
using System.Text.Json;

static class PremiumChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var normal=new Robot(1,0,RobotParts.Complete);
        var premium=normal with {PremiumParts=RobotParts.Head};
        Check(premium.CombatParts(0)==normal.CombatParts(0)+1 && premium.CombatRifles(0)==normal.CombatRifles(0)+1,"One premium part adds one use to shields, daggers and rifles");
        Check((premium with {PremiumParts=RobotParts.Complete}).CombatParts(0)==premium.CombatParts(0),"Premium bonus applies once per robot");
        Check((premium with {DamagedParts=RobotParts.Head}).PremiumBonus==0,"Damaged premium parts cannot provide their bonus");
        var head=premium.Slice(RobotParts.Head);
        var assembled=normal.Slice(RobotParts.Body|RobotParts.Legs).Merge(head,0);
        Check(assembled.PremiumParts==RobotParts.Head && assembled.CombatParts(0)==4,"Splitting and mixed assembly preserve premium quality");
        var replaced=assembled.Merge(head with {PremiumParts=0,CheapParts=RobotParts.Head},0);
        Check(replaced.PremiumParts==0 && replaced.IsCheap(RobotParts.Head) && replaced.PremiumBonus==0,"Replacing premium with cheap removes its bonus");
        var board=new BattleState(1);board.AddArmy(0,0,new[]{premium});
        var unit=board.Armies.Single().Waiting.Single();
        Check(unit.Daggers==4 && unit.Shields==4 && unit.Rifles==5,"Battle deployment uses premium ammunition");
        int rare=0;RobotWorkshop selected=null!;
        for(int seed=0;seed<1000;seed++){
            var line=new RobotWorkshop();line.GeneratePlans(seed);
            if(line.PlanCount==11)continue;
            rare++;selected=line;
            var plan=line.PlanAt(11);
            Check(plan.PremiumParts==plan.Product && plan.EffectiveCheapParts==0 && plan.Turns==line.RegularCompleteProductionTurns*2,
                "Rare catalogue offers one premium single part at twice regular complete cost");
            var same=new RobotWorkshop();same.GeneratePlans(seed);
            Check(same.PlanAt(11)==plan,"Premium availability and recipe are seed-stable");
        }
        Check(rare is >=60 and <=140,"Premium catalogue is rare across 1000 seeds");
        var robots=new RobotWorld();robots.Initialize(2,1);
        selected.AutoAssemblyDisabled=true;selected.SetReservedSlots(0);selected.SelectPlan(11);robots.Workshops[0]=selected;
        for(int i=1;i<selected.ProductionPeriod;i++)robots.AdvanceIndustry(_=>0);
        Check(robots.Nodes[0].Count==0,"Premium production waits for the full rare-part cost");
        var loaded=AutoSaveStore.Restore<RobotWorld>(AutoSaveStore.Capture(robots));loaded.AdvanceIndustry(_=>0);
        Check(loaded.Nodes[0].Robots.Single().PremiumParts!=0,"Saving in progress preserves the expensive premium recipe");
        robots.Initialize(2,1);robots.Nodes[0].Add(robots.Create(0,RobotParts.Complete) with {PremiumParts=RobotParts.Head});
        var rule=robots.Transport.AddInstruction(0,0,1);
        robots.Transport.ConfigurePartChoices(0,0,rule.Number,1,1,false,1);robots.Transport.SetInstructionRate(0,0,rule.Number,360);
        robots.Transport.ResolvePlans(robots,(_,_)=>true,1);
        Check(robots.Nodes[1].Count==0,"A regular-only instruction leaves premium parts in place");
        robots.Transport.ConfigurePartChoices(0,0,rule.Number,1,1<<10,false,1);robots.Transport.SetInstructionRate(0,0,rule.Number,360);
        robots.Transport.ResolvePlans(robots,(_,_)=>true,1);
        Check(robots.Nodes[1].Robots.Single().IsPremium(RobotParts.Head) && robots.Nodes[0].Robots.Single().PremiumParts==0,
            "Premium transport selection preserves quality on both sides of a split");
        var legacy=AutoSaveStore.Capture(normal);
        foreach(var item in legacy.Objects)item.Fields?.Remove("<PremiumParts>k__BackingField");
        Check(AutoSaveStore.Restore<Robot>(legacy).PremiumParts==0,"Legacy robots remain regular");
        var world=new WorldSetup();world.SetWorld(1);world.SetCast(2);world.SetPlacement(3);world.SelectPlayer(0);
        world.SetCountryAppearance(0,4,4,1);
        Check(world.CountrySecondaryColor(0)!=4,"The secondary color cannot equal the primary color");
        world.SetCountryEmblem(0,13);
        var saved=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(world));
        Check(saved.CountryEmblem(0)==13 && saved.CountrySecondaryColor(0)!=saved.ColorIndices[0]%WorldSetup.Colors.Length,"Tulip emblem and contrasting second color survive saving");
        var directory=Path.Combine(AppContext.BaseDirectory,"statistics-test-"+Guid.NewGuid().ToString("N"));
        var statistics=new StatisticsLog(directory,1,3);
        Check(statistics.Write(world) && !statistics.Write(world),"Statistics writes once per global turn");
        for(int i=0;i<5;i++){world.Population.Advance(world);statistics.Write(world);}
        var files=Directory.GetFiles(directory,"statistics-*.jsonl");
        Check(files.Length==3,"Statistics rotation bounds the number of files");
        foreach(var file in files)foreach(var row in File.ReadLines(file)){
            using var json=JsonDocument.Parse(row);
            Check(json.RootElement.GetProperty("countries").GetArrayLength()==world.ActiveCount,"Each statistics record contains one country aggregate per participant");
        }
        world.SetWorld(5);world.SetCast(6);world.SetPlacement(7);world.SelectPlayer(0);
        Check(statistics.Write(world),"Statistics handles a new game reusing the world object");
        Console.WriteLine($"PASS: premium rarity {rare}/1000, combat, split/merge, production, transport, legacy saves, emblems, colors and bounded statistics rotation.");
    }
}

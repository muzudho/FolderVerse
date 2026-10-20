using FolderVerse;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Runtime.CompilerServices;

if(args.Contains("--ui")){using var game=new InvasionUiCheck();game.Run();return;}
PersuasionChecks.Run();

static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
static (WorldSetup World,Node Home,Node Target) Fixture()
{
    var w=new WorldSetup();w.SetWorld(2);w.SetCast(123);w.SetPlacement(456);w.SelectPlayer(0);
    w.Robots.Initialize(w.Nodes.All.Length,w.ActiveCount);
    var home=w.Nodes.All.First(n=>w.Routes.Neighbors(n).Length>0);var route=w.Routes.Neighbors(home)[0];var target=w.Nodes.At(route.Target,route.Entry);
    home.Owner=0;target.Owner=1;
    w.ConquerorLocations[0]=home.Cell;w.ConquerorPoints[0]=home.Center;
    w.ConquerorLocations[1]=target.Cell;w.ConquerorPoints[1]=target.Center;
    w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    return(w,home,target);
}
foreach(bool queen in new[]{false,true})
{
    var(w,home,target)=Fixture();var hangar=Enumerable.Range(0,3).Select(_=>w.Robots.Create(0,RobotParts.Complete)).ToArray();
    foreach(var r in hangar)w.Robots.Nodes[home.Id].Add(r);
    var pocket=Enumerable.Range(0,3).Select(_=>w.Robots.Create(0,RobotParts.Complete)).ToArray();foreach(var r in pocket)w.Robots.Retinues[0].Add(r);
    w.Robots.Nodes[target.Id].Add(w.Robots.Create(1,RobotParts.Complete));
    w.Campaign.Plan(new(0,target.Id,1,home.Id,false,false,new[]{hangar[2].Id}));
    w.Campaign.Plan(new(0,target.Id,1,home.Id,queen,true,new[]{pocket[1].Id}));
    Check(w.Campaign.PlannedOrders.Count==2,"Hangar and battery orders must remain independent");
    w.Campaign.Resolve(w,w.Campaign.PlannedOrders.ToArray());
    var units=w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Where(u=>u.Owner==0).ToArray();
    Check(units.Where(u=>u.Robot!=null).Select(u=>u.Id).Order().SequenceEqual(new[]{hangar[2].Id,pocket[1].Id}.Order()),"Only chosen IDs deploy from the correct stores");
    Check(units.Any(u=>u.Role==BattleRole.Queen)==queen,"Queen participation independent of pocket robot selection");
    Check(w.Robots.Nodes[home.Id].Count==2 && w.Robots.Retinues[0].Count==2,"Unselected robots stay in stores");
    Check(w.Campaign.RobotBattles.LastQueenMarches.Count==(queen?1:0),"Queen participation also records movement");
    w.Campaign.RobotBattles.Check(w);
    w.Campaign.CancelReservation(0,home.Id,true,target.Id);Check(w.Campaign.PlannedOrders.Count==1 && !w.Campaign.PlannedOrders[0].PocketOnly,"Battery cancellation preserves hangar");
}
{
    var(w,home,target)=Fixture();w.Robots.Nodes[target.Id].Add(w.Robots.Create(1,RobotParts.Complete));
    w.Campaign.Resolve(w,new[]{new March(0,target.Id,0,home.Id,true,true,Array.Empty<long>())});
    Check(w.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles).SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Any(u=>u.Owner==0 && u.Role==BattleRole.Queen),"Queen alone can deploy");
}
{
    var(w,home,target)=Fixture();var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[home.Id].Add(robot);
    foreach(var ids in new[]{new[]{robot.Id,robot.Id},new[]{long.MaxValue}})
    {
        bool rejected=false;try{w.Campaign.Resolve(w,new[]{new March(0,target.Id,ids.Length,home.Id,false,false,ids)});}catch(ArgumentException){rejected=true;}
        Check(rejected && w.Robots.Nodes[home.Id].Count==1 && w.Population.Turn==0,"Invalid IDs fail before mutation");
    }
    w.Campaign.Plan(new(0,target.Id,1,home.Id,false,false,new[]{robot.Id}));
    var save=AutoSaveStore.Capture(w);var loaded=AutoSaveStore.Restore<WorldSetup>(save);
    Check(loaded.Campaign.PlannedOrders.Single().RobotIds.SequenceEqual(new[]{robot.Id}),"Selected IDs survive autosave");
    foreach(var entry in save.Objects)if(entry.Fields!=null){entry.Fields.Remove("<RobotIds>k__BackingField");entry.Fields.Remove("<Persuasions>k__BackingField");}
    loaded=AutoSaveStore.Restore<WorldSetup>(save);
    Check(loaded.Campaign.PlannedOrders.Single().RobotIds==null,"Old saves without selected IDs remain readable");
}
// Exercise the selection dialog without creating a GPU/window.
{
    var(w,home,target)=Fixture();var r=w.Robots.Create(0,RobotParts.Complete);w.Robots.Retinues[0].Add(r);
    var g=(Game1)RuntimeHelpers.GetUninitializedObject(typeof(Game1));const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    FieldInfo Field(string n)=>typeof(Game1).GetField(n,flags)!;
    object Call(string n,params object[] args)=>typeof(Game1).GetMethod(n,flags)!.Invoke(g,args)!;
    Field("_setup").SetValue(g,w);Field("_invasionSources").SetValue(g,Activator.CreateInstance(Field("_invasionSources").FieldType));Field("_invasionDraft").SetValue(g,new HashSet<long>());
    Call("BuildInvasionSources",target.Id);
    var rows=(System.Collections.IList)Field("_invasionSources").GetValue(g)!;int battery=Enumerable.Range(0,rows.Count).First(i=>(bool)rows[i]!.GetType().GetField("Queen")!.GetValue(rows[i])!);
    Call("OpenInvasionPicker",battery);Field("_pointer").SetValue(g,new Point(400,350));Call("InvasionPickerInput",true,false);
    Field("_pointer").SetValue(g,new Point(450,730));Call("InvasionPickerInput",true,false);
    Field("_pointer").SetValue(g,new Point(1400,820));Call("InvasionPickerInput",true,false);
    var row=rows[battery]!;Check(((HashSet<long>)row.GetType().GetField("Robots")!.GetValue(row)!).SetEquals(new[]{r.Id}) && (bool)row.GetType().GetField("Selected")!.GetValue(row)!,"Picker applies robot and independent queen choice");
    Call("OpenInvasionPicker",battery);Field("_pointer").SetValue(g,new Point(400,350));Call("InvasionPickerInput",true,false);Call("InvasionPickerInput",false,true);
    Check(((HashSet<long>)row.GetType().GetField("Robots")!.GetValue(row)!).Contains(r.Id),"Escape discards draft edits and closes dialog");
    Field("_selectedMoveNode").SetValue(g,target.Id);
    var partial=w.Robots.Create(0,RobotParts.Head);w.Robots.Retinues[0].Add(partial);
    Check(((Robot[])Call("InvasionPreviewRobots",row)).Select(x=>x.Id).SequenceEqual(new[]{r.Id}),"Inline preview only includes complete robots");
    var selected=(HashSet<long>)row.GetType().GetField("Robots")!.GetValue(row)!;
    Call("InvasionClick",new Point(110,345+battery*77));Check(!selected.Contains(r.Id),"Inline robot click deselects without a modal");
    Call("InvasionClick",new Point(110,345+battery*77));Check(selected.Contains(r.Id),"Inline robot click selects without a modal");
    w.Campaign.Plan(new(0,home.Id,1,home.Id,false,true,new[]{r.Id}));
    Check((bool)Call("ReservedElsewhere",row,r),"Other-target pocket reservation is marked");
    w.Campaign.Plan(new(0,target.Id,1,home.Id,false,true,new[]{r.Id}));
    Check(!(bool)Call("ReservedElsewhere",row,r),"Current target is not marked as another reservation");
    var net=new CubeNet();Field("_net").SetValue(g,net);
    Field("_movementOpen").SetValue(g,true);Field("_warPlanning").SetValue(g,true);
    net.MovementTargets.Add(target.Id);net.MovementTargets.Add(home.Id);net.SelectedTarget=target.Id;
    net.AttackTargetMarkers=true;net.RevealMovementTargets=true;
    Call("InvasionClick",new Point(700,935));
    Check(w.Campaign.PlannedOrders.Single().TargetNode==target.Id,"Confirm saves the selected reservation");
    Check((bool)Field("_movementOpen").GetValue(g)! && (bool)Field("_warPlanning").GetValue(g)! && net.AttackTargetMarkers && net.RevealMovementTargets,"Confirm keeps war mode and target markers active");
    Check((int)Field("_selectedMoveNode").GetValue(g)! ==-1 && net.SelectedTarget==-1 && rows.Count==0 && (int)Field("_invasionPicker").GetValue(g)! ==-1,"Confirm resets target, source rows and picker");
    Call("SelectMoveNode",home.Id);
    Check((int)Field("_selectedMoveNode").GetValue(g)! ==home.Id && w.Campaign.PlannedOrders.Single().TargetNode==target.Id,"Next target can be selected while the saved reservation remains");
    Call("CloseNodeInspector");Call("UpdateDomesticSlide",1f);
    Check(!(bool)Field("_movementOpen").GetValue(g)! && !net.AttackTargetMarkers && net.MovementTargets.Count==0,"Closing the inspector exits war mode");
    Check(w.Campaign.PlannedOrders.Single().TargetNode==target.Id,"Closing preserves confirmed reservations");
}
// Distance uses taxicab geometry; successful conversion has separate before/after frames.
{
    var b=new BattleState(123,new(){PersuasionPercent=100,PersuasionRadius=4});b.Armies.Add(new(){Owner=0});b.Armies.Add(new(){Owner=1});
    b.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(1,1)});
    b.Units.Add(new(){Id=10,Owner=1,Position=new(3,3)});b.Units.Add(new(){Id=11,Owner=1,Position=new(4,3)});
    typeof(BattleTurnResolver).GetMethod("Persuade",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{b});
    Check(b.Units.Single(u=>u.Id==10).Owner==0 && b.Units.Single(u=>u.Id==11).Owner==1,"Four taxicab squares in range, five out of range");
    Check(b.Persuasions.Count==1 && b.Persuasions[0].Success && b.Persuasions[0].Queen==-1 && b.Persuasions[0].PreviousOwner==1,"Replay records source, target, success and former owner");
}
{
    var b=new BattleState(11,new(){PersuasionPercent=100,NoWillTurns=1000});b.Armies.Add(new(){Owner=0});b.Armies.Add(new(){Owner=1,Side=2});
    b.Units.Add(new(){Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(2,2),Daggers=0,Rifles=0});b.Units.Add(new(){Id=12,Owner=1,Position=new(4,2)});
    BattleTurnResolver.Advance(b,b.Units.ToDictionary(u=>u.Id,u=>new BattleAction(Point.Zero,u.Facing)));
    Check(b.Frames.Count(f=>f.Phase==BattlePhase.Persuade)==2 && b.Frames.Count(f=>f.Phase==BattlePhase.Converted)==4,"Readable persuasion and successful-conversion hold durations");
    Check(b.Frames.First(f=>f.Phase==BattlePhase.Persuade).Units.Single(u=>u.Id==12).Owner==1 && b.Frames.First(f=>f.Phase==BattlePhase.Converted).Units.Single(u=>u.Id==12).Owner==0,"Replay shows former affiliation before success, new affiliation after success");
}
Console.WriteLine("PASS: selected IDs, independent pocket/queen, solo queen, invalid selection atomicity, autosave compatibility, dialog apply/cancel and persuasion distance/events");

// A reservation alone is not an achievement, and defensive participation does not count.
{
    var w=new WorldSetup();w.PrepareTutorialStepOne(123,456,789);w.CompleteLand(90);w.SelectPlayer(0);w.HumanPlayers.Add(0);
    var home=w.Nodes.Current(0);var route=w.Routes.Neighbors(home).First();var target=w.Nodes.At(route.Target,route.Entry);target.Owner=1;
    foreach(var old in w.Robots.Nodes[home.Id].Robots.ToArray())w.Robots.Nodes[home.Id].Remove(old.Id);var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Nodes[home.Id].Add(robot);
    w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1,NoWillTurns=1000,PersuasionPercent=0};
    Check(!TutorialAchievements.Attacked(w),"Fresh tutorial has no attack achievement");
    var g=(Game1)RuntimeHelpers.GetUninitializedObject(typeof(Game1));const BindingFlags f=BindingFlags.NonPublic|BindingFlags.Instance;
    typeof(Game1).GetField("_setup",f)!.SetValue(g,w);
    Check(!(bool)typeof(Game1).GetProperty("NodeTransportAllowed",f)!.GetValue(g)!,"Step one hides and disables transport planning entry");
    bool Gate()=>(bool)typeof(Game1).GetMethod("ShowTutorialAttackGuideIfRequired",f)!.Invoke(g,null)!;
    Check(Gate(),"Unachieved tutorial with no reservation shows guide and blocks end turn");
    w.Campaign.Plan(new(0,target.Id,1,home.Id,false,false,new[]{robot.Id}));
    Check(!TutorialAchievements.Attacked(w),"Planning alone does not earn a medal");
    Check(!Gate(),"An attack reservation permits tutorial end turn");
    w.Campaign.Resolve(w,w.Campaign.PlannedOrders.ToArray());
    Check(TutorialAchievements.Attacked(w),"Actual attack battle qualifies for the tutorial medal");
    var medalPath=Path.Combine(AppContext.BaseDirectory,"intro-medals-"+Guid.NewGuid()+".json");
    try{
        var medals=new TutorialAchievements(medalPath);
        void Set(string n,object value)=>typeof(Game1).GetField(n,f)!.SetValue(g,value);
        object Get(string n)=>typeof(Game1).GetField(n,f)!.GetValue(g)!;
        void Call(string n,params object[] a)=>typeof(Game1).GetMethod(n,f)!.Invoke(g,a);
        Set("_autoSaveEnabled",true);Set("_tutorialAchievements",medals);Set("_robotScenes",w.Campaign.RobotBattles.Scenes.ToArray());
        Set("_battleWave",w.Campaign.RobotBattles.Scenes.FindIndex(b=>TutorialAchievements.AttackedScene(w,b)));
        Call("QueueTutorialAttackAchievement");Check(!medals.Has(TutorialAchievements.FirstAttack),"No medal during transport or before transformation");
        Set("_battleIntroAge",6.49d);Call("RobotBattleInput",.005d,false,false);Check(!medals.Has(TutorialAchievements.FirstAttack),"No medal while intro remains active");
        Call("RobotBattleInput",.02d,false,false);Check(medals.Has(TutorialAchievements.FirstAttack) && (double)Get("_tutorialAchievementAge")==0,"Intro completion persists medal and starts cut-in");
        Call("RobotBattleInput",.5d,true,true);Check((double)Get("_tutorialAchievementAge")==.5 && (double)Get("_battleAge")==0,"Click/Enter do not skip toast or advance hidden combat");
        Call("RobotBattleInput",3d,false,false);Check((double)Get("_tutorialAchievementAge")==-1,"Cut-in disappears after 3.5 seconds without input");
        Call("QueueTutorialAttackAchievement");Check(!(bool)Get("_tutorialAttackPending"),"Unlocked medal is not announced again");
    }finally{File.Delete(medalPath);}

    void SetGuide(string n,object value)=>typeof(Game1).GetField(n,f)!.SetValue(g,value);
    object GetGuide(string n)=>typeof(Game1).GetField(n,f)!.GetValue(g)!;
    object CallGuide(string n,params object[] a)=>typeof(Game1).GetMethod(n,f)!.Invoke(g,a)!;
    SetGuide("_screen",Enum.Parse(typeof(Game1).GetField("_screen",f)!.FieldType,"WorldStatus"));
    SetGuide("_tutorialAttackAchieved",false);SetGuide("_tutorialAchievements",null!);SetGuide("_lastCutInPhase","内政フェーズ");SetGuide("_cutInStarted",100d);
    CallGuide("UpdateTutorialStartGuide",101d);Check(!(bool)GetGuide("_tutorialIntroShown"),"Wait until domestic phase cut-in finishes before automatic tutorial guide");
    CallGuide("UpdateTutorialStartGuide",102d);Check((bool)GetGuide("_tutorialAttackGuide") && (int)GetGuide("_tutorialGuideMessage")==0,"Automatic start guide tells user to press war reservation");
    SetGuide("_tutorialAttackGuide",false);CallGuide("UpdateTutorialStartGuide",110d);Check(!(bool)GetGuide("_tutorialAttackGuide"),"Automatic start guide appears only once per run");
    SetGuide("_tutorialAchievementAge",-1d);SetGuide("_pointer",new Point(1500,40));CallGuide("TutorialMessageInput",true,false);Check((bool)GetGuide("_tutorialMessagesOpen"),"Review button opens message list");
    SetGuide("_pointer",new Point(450,550));CallGuide("TutorialMessageInput",true,false);Check((int)GetGuide("_tutorialMessageSelection")==2,"Review list selects achievement message without awarding it");
    CallGuide("TutorialMessageInput",false,true);Check(!(bool)GetGuide("_tutorialMessagesOpen") && !(bool)GetGuide("_tutorialAttackAchieved"),"Closing reader does not grant achievement");
    typeof(Game1).GetField("_tutorialAttackAchieved",f)!.SetValue(g,true);w.Campaign.Reset();Check(!Gate(),"Completed step permits peaceful end turns");
    w.Campaign.RobotBattles.LastMarches.Clear();Check(!TutorialAchievements.Attacked(w),"Defending an existing scene does not count as attack");
}
Console.WriteLine("PASS: tutorial medals require an actual attacking battle, not a reservation or defense.");

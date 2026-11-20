using FolderVerse;
using Microsoft.Xna.Framework;
if(args.Contains("--ui")){using var game=new TacticPreview();game.Run();return;}
int checks=0;
void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
foreach(var terrain in new[]{BattleTerrain.Field,BattleTerrain.Fort,BattleTerrain.Mountain,BattleTerrain.Sea})
foreach(int troops in new[]{1,12})foreach(int count in new[]{0,4,8,12}){
    var b=new BattleState(42){Kind=BattleKind.Node,Defender=0,EquipmentMode=RobotEquipmentMode.Materials};b.BuildTerrain(terrain);
    var stock=new RobotWorld();stock.Initialize(1,2);
    b.AddArmy(0,0,Enumerable.Range(0,troops).Select(_=>stock.Create(0,RobotParts.Complete)));
    b.AddArmy(1,0,Enumerable.Range(0,troops).Select(_=>stock.Create(1,RobotParts.Complete)));
    b.ChooseLayout();b.Deploy();b.BuildSiteFortifications(count);b.Check();
    Check(b.Fortifications.Count==(count==0?4:count),$"{terrain}/{troops}/{count}: expected exact cover, got {b.Fortifications.Count}");
    Check(b.Fortifications.All(f=>f.Position!=b.Flag&&!b.Units.Any(u=>u.Active&&u.Position==f.Position)),"Flag and deployed units stay clear");
    foreach(var f in b.Fortifications){
        Check(!b.Walkable(f.Position),"Cover blocks walking");
        if(count==0)foreach(var face in Enum.GetValues<BattleFacing>())Check(!b.BlocksShot(f.Position,face,1)&&b.BlocksShot(f.Position,face,2),"Trees pass adjacent shots from both sides only");
        else Check(f.Kind==FortificationKind.Wall&&!b.BlocksShot(f.Position,f.Facing,1)&&b.BlocksShot(f.Position,(BattleFacing)(((int)f.Facing+2)%4),1),"Wall fires outwards only");
    }
    var seen=new HashSet<Point>{b.Flag};var q=new Queue<Point>();q.Enqueue(b.Flag);
    while(q.TryDequeue(out var p))foreach(var face in Enum.GetValues<BattleFacing>()){var next=p+BattleState.Forward(face);if(b.Walkable(next)&&seen.Add(next))q.Enqueue(next);}
    Check(seen.Count==Enumerable.Range(0,b.BoardSize).Sum(y=>Enumerable.Range(0,b.BoardSize).Count(x=>b.Walkable(new(x,y)))),"All walkable terrain remains connected");
    var saved=AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(b));
    Check(saved.Fortifications.SequenceEqual(b.Fortifications)&&saved.WallShotDirections.OrderBy(x=>x.Key.X).ThenBy(x=>x.Key.Y).SequenceEqual(b.WallShotDirections.OrderBy(x=>x.Key.X).ThenBy(x=>x.Key.Y)),"Cover and shot directions survive save");
    b.BuildSiteFortifications(12);Check(b.Fortifications.SequenceEqual(saved.Fortifications),"Reinforcements do not add walls");
}
BattleState Duel(bool guard){
    var b=new BattleState(1){Kind=BattleKind.Node,Defender=0};b.Armies.Add(new(){Owner=0,Side=4});b.Armies.Add(new(){Owner=1,Side=0});
    b.Units.Add(new(){Id=1,Owner=0,Position=new(6,5),Daggers=1,Rifles=0,Shields=2,Robot=new Robot(1,0,RobotParts.Complete){DefenseOnly=guard}});
    b.Units.Add(new(){Id=2,Owner=1,Position=new(6,3),Daggers=1,Rifles=0,Shields=2});
    b.Units.Add(new(){Id=3,Owner=1,Position=new(8,6),Daggers=1,Rifles=0,Shields=2});return b;
}
foreach(var terrain in new[]{BattleTerrain.Field,BattleTerrain.Mountain,BattleTerrain.Sea})foreach(int count in new[]{0,4,8,12}){
    var crowded=new BattleState(8){Kind=BattleKind.Node,Defender=0,EquipmentMode=RobotEquipmentMode.Materials};crowded.BuildTerrain(terrain);
    var stock=new RobotWorld();stock.Initialize(1,13);
    for(int owner=0;owner<13;owner++)crowded.AddArmy(owner,0,Enumerable.Range(0,12).Select(_=>stock.Create(owner,RobotParts.Complete)));
    crowded.ChooseLayout();crowded.Deploy();crowded.BuildSiteFortifications(count);crowded.Check();
    Check(crowded.Fortifications.Count==(count==0?4:count),$"13-army {terrain}/{count}: got {crowded.Fortifications.Count}");
    Check(crowded.Armies.Where(a=>a.Side<4).SelectMany(a=>crowded.ApproachPath(a).Concat(crowded.RetreatPath(a))).All(crowded.Walkable),"13-army entry and retreat paths stay open");
}
var unlearned=Duel(true);var u=unlearned.Units[0];var bad=BattleAi.Choose(unlearned,u);
Check(BattleAi.Distance(u.Position+bad.Step,unlearned.Flag)>BattleAi.Distance(u.Position,unlearned.Flag),"Unlearned guard still pursues away from threatened flag");
BattleTurnResolver.Advance(unlearned);Check((u.WitnessedLessons&BattleLesson.DefenseEndurance)!=0,"Bad guard action witnesses lesson17");
var lessons=new BattleLessons();lessons.BringHome(u);Check(lessons.Research().Any(a=>a.Lesson==BattleLesson.DefenseEndurance),"Surviving witness brings home and researches lesson17");
Check((AutoSaveStore.Restore<BattleLessons>(AutoSaveStore.Capture(lessons)).Owned[0]&BattleLesson.DefenseEndurance)!=0,"Lesson17 survives save");
u.Fallen=true;var lost=new BattleLessons();lost.BringHome(u);Check(!lost.Pending.Any(),"Fallen witness cannot bring home lesson17");
var learned=Duel(true);learned.Lessons[0]=BattleLesson.DefenseEndurance;var good=BattleAi.Choose(learned,learned.Units[0]);
Check(BattleAi.Distance(learned.Units[0].Position+good.Step,learned.Flag)<BattleAi.Distance(learned.Units[0].Position,learned.Flag),"Learned guard returns to threatened flag");
var free=Duel(false);var before=BattleAi.Choose(free,free.Units[0]);free.Lessons[0]=BattleLesson.DefenseEndurance;Check(BattleAi.Choose(free,free.Units[0])==before,"Lesson17 does not alter ordinary troops");
var cover=Duel(true);cover.Units.RemoveRange(1,2);var defender=cover.Units[0];defender.Position=new(3,6);defender.Daggers=0;defender.Rifles=3;
cover.Units.Add(new(){Id=4,Owner=1,Position=new(6,0),Rifles=3,Daggers=0});cover.Walls.Add(new(6,4));cover.WallShotDirections[new(6,4)]=6;cover.Fortifications.Add(new(new(6,4),FortificationKind.Wall,0,BattleFacing.North));cover.Lessons[0]=BattleLesson.DefenseEndurance;
for(int i=0;i<6;i++)defender.Position+=BattleAi.Choose(cover,defender).Step;
Check(defender.Position==new Point(6,5),"Learned guard seeks inner wall firing position");
Check(BattleAi.Choose(cover,defender).Weapon==BattleWeapon.Rifle,"Learned guard fires outwards while protected");
Console.WriteLine($"PASS {checks}: land/sea cover, saves, unlearned failure, lesson acquisition, endurance and ordinary troops.");

var amb=Duel(true);amb.Units.RemoveRange(1,2);var guard=amb.Units[0];guard.Position=new(6,5);amb.AmbushEnabled[0]=true;amb.Walls.Add(new(6,4));amb.Fortifications.Add(new(new(6,4),FortificationKind.Tree,0,BattleFacing.North));amb.Units.Add(new(){Id=9,Owner=1,Position=new(6,2),Facing=BattleFacing.South,Daggers=1});
Check(BattleTactics.AmbushAction(amb,guard)?.Step==Point.Zero,"Ambush waits behind cover without a lesson");amb.Turn=3;Check(BattleTactics.AmbushAction(amb,guard)==null,"Waiting has a bounded cooldown");amb.Turn=0;amb.Units[1].Position=new(7,5);amb.Units[1].Facing=BattleFacing.North;amb.Flag=new(4,6);Check(BattleTactics.AmbushAction(amb,guard)?.Weapon==BattleWeapon.Dagger,"Ambush cuts an exposed flank");amb.AmbushEnabled[0]=false;Check(BattleTactics.AmbushAction(amb,guard)==null,"Character without tactic does not ambush");amb.AmbushEnabled[0]=true;guard.Robot=guard.Robot with{DefenseOnly=false};Check(BattleTactics.AmbushAction(amb,guard)==null,"Ordinary troops are unchanged");guard.Robot=guard.Robot with{DefenseOnly=true};Check(AutoSaveStore.Restore<BattleState>(AutoSaveStore.Capture(amb)).AmbushEnabled[0],"Tactic availability survives save");Check(Enumerable.Range(0,60).Count(BattleTactics.UsesAmbush)==30,"Sixty characters have two distinct tactic policies");Console.WriteLine("PASS 7 ambush checks");


var old=AutoSaveStore.Capture(amb);foreach(var entry in old.Objects)entry.Fields?.Remove("AmbushEnabled");Check(!AutoSaveStore.Restore<BattleState>(old).AmbushEnabled.Any(x=>x),"Old battle saves default to no tactic");
amb.Units[1].Position=new(8,5);guard.Rifles=3;guard.Daggers=0;Check(BattleTactics.AmbushAction(amb,guard)?.Weapon==BattleWeapon.Rifle,"Rifle ambush uses clear side shot");amb.Walls.Add(new(7,5));Check(BattleTactics.AmbushAction(amb,guard)==null,"Blocked rifle line cannot ambush");amb.Walls.Remove(new(7,5));amb.Units[1].Position=amb.Flag+new Point(1,0);Check(BattleTactics.AmbushAction(amb,guard)==null,"Threatened flag takes priority");amb.Units[1].Position=new(8,5);var chosen=BattleTactics.AmbushAction(amb,guard)!;BattleTactics.Observe(amb,amb.Units.ToArray(),new Dictionary<long,BattleAction>{{guard.Id,chosen},{9,new(Point.Zero,BattleFacing.North)}});Check(amb.Events.Count(e=>e.Detail==BattleTactics.Ambush)==1,"Automatic tactical action records one activation");Console.WriteLine("PASS 5 additional tactic checks");

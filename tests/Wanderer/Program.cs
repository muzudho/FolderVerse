using FolderVerse;
using Microsoft.Xna.Framework;
static void Check(bool value,string message){if(!value)throw new Exception(message);}
WorldSetup World(){var w=new WorldSetup();w.PrepareTutorialStepTwo(123,456,789);w.SelectPlayer(0);w.HumanPlayers.Add(0);return w;}
var w=World();
Check(w.Width==1 && w.Height==2 && w.Depth==1 && w.ActiveCount==7,"Step two shape and cast");
Check(w.TerritoryCounts[0]==0 && Enumerable.Range(1,6).All(r=>w.TerritoryCounts[r]==1) && w.Owners.Count(o=>o<0)==4,"Zero-cell start, six one-cell nations and neutral remainder");
Check(w.Robots.Retinues[0].Count==0,"Empty starting pocket");
Check(Enumerable.Range(1,6).All(r=>w.Robots.Training[r].RequiredNative==1 && w.Robots.Training[r].DeploymentPolicy==1),"Step two CPUs assemble and deploy mixed robots");
Check(w.Robots.Training[0].RequiredNative==3 && w.Robots.Training[0].DeploymentPolicy==0,"Player training remains unchanged");
foreach(var n in w.Nodes.All){var stock=w.Robots.Nodes[n.Id].Robots;Check(stock.Count==(n.Owner<0?1:4),"Initial stock count");Check(stock.Count(r=>r.Owner==0 && r.NativeParts(0)==3)==1,"One regular player robot per node");Check(n.Owner<0 || stock.Count(r=>r.Owner==n.Owner && r.CheapParts==RobotParts.Complete)==3,"Three cheap national robots");}
int home=w.Nodes.Current(0).Id;
var target=w.Routes.NodeChoices(0).Select(r=>w.Nodes.At(r.Target,r.Entry)).First(n=>n.Id!=home);
target.Owner=2;w.Nodes.Recount();
var pocket=w.Robots.Create(0,RobotParts.Complete);w.Robots.Retinues[0].Add(pocket);
var before=w.Nodes.All.Select(n=>n.Owner).ToArray();
w.Campaign.Resolve(w,new[]{new March(0,target.Id,0,home,true,true,Peaceful:true)});
Check(w.Nodes.Current(0).Id==target.Id && w.TutorialMoved,"Peaceful foreign visit actually moves");
Check(before.SequenceEqual(w.Nodes.All.Select(n=>n.Owner)) && w.Campaign.RobotBattles.Encounters.Count==0,"Visit neither captures territory nor starts a battle");
Check(w.Robots.Retinues[0].Robots.Any(r=>r.Id==pocket.Id),"Pocket travels with queen");
var events=w.Robots.PersuadeAtHome(w);Check(w.Robots.DomesticPersuasionTurns.ContainsKey(0),"Persuasion available abroad");
foreach(var policy in Enum.GetValues<WandererPolicy>())
{
 w=World();home=w.Nodes.Current(0).Id;
 var source=w.Routes.Neighbors(w.Nodes.All[home]).Select(r=>w.Nodes.At(r.Target,r.Entry)).First(n=>n.Id!=home);source.Owner=2;w.Nodes.Recount();
 w.SetWandererPolicy(0,policy);var robot=w.Robots.Create(0,RobotParts.Complete);w.Robots.Retinues[0].Add(robot);
 w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1};
 w.Campaign.Resolve(w,new[]{new March(2,home,0,source.Id,false)});
 var encounter=w.Campaign.RobotBattles.Encounters.First(e=>e.Target==home && e.Kind==BattleKind.Node);
 var state=encounter.Battles.First(b=>b.Armies.Any(a=>a.Owner==0));var army=state.Army(0);
 Check(army.Wanderer && army.WandererPolicy!=WandererPolicy.Random,"Bystander policy resolves once at encounter entry");
 Check(state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Any(u=>u.Owner==0 && u.Role==BattleRole.Queen),"Resident queen forced to participate");
 Check(state.Units.Concat(state.Armies.SelectMany(a=>a.Waiting)).Any(u=>u.Id==robot.Id),"Eligible pocket robot forced to participate");
 if(policy==WandererPolicy.Retreat){Check(army.Withdrawing,"Immediate retreat policy");Check(w.Nodes.Current(0).Id!=home,"Immediate withdrawal leaves even a continuing battle");Check(w.Robots.Retinues[0].Robots.Any(r=>r.Id==robot.Id),"Immediate withdrawal returns pocket robots exactly once");}
 var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
 Check(restored.TutorialStep==2 && restored.ActiveCount==7 && restored.WandererPolicyFor(0)==policy,"Preset and policy survive save");
 Check(restored.Campaign.RobotBattles.Encounters.SelectMany(e=>e.Battles).SelectMany(b=>b.Armies).Any(a=>a.Wanderer && a.WandererPolicy==army.WandererPolicy),"Resolved battle policy survives save");
 var resolvedPolicy=army.WandererPolicy;
 for(int turn=0;turn<30 && !encounter.Finished;turn++)
 {
  w.Campaign.Resolve(w,Array.Empty<March>());
  Check(army.WandererPolicy==resolvedPolicy,"A continuing battle never rerolls its bystander policy");
  Check(encounter.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Count(u=>u.Id==-1)<=1,"No duplicate or returning retired queen");
 }
 if(policy==WandererPolicy.Retreat && encounter.Finished){Check(w.Nodes.Current(0).Id!=home,"Retreat leaves the battlefield node");Check(!w.Relations.Pending.Any(c=>c.Ruler==0),"Escaped queen is not captured by battlefield winner");}
}
// Earlier saves lack the newly introduced fields and must still load.
var legacyWorld=World();legacyWorld.Campaign.Plan(new(0,legacyWorld.Routes.NodeChoices(0).Select(r=>legacyWorld.Nodes.At(r.Target,r.Entry).Id).First(),0));
var legacy=AutoSaveStore.Capture(legacyWorld);
var newFields=new HashSet<string>{"WandererPolicies","_presetCastCount","TutorialPocketed","TutorialMoved","TutorialPocketNoticeShown","TutorialMoveNoticeShown","<Peaceful>k__BackingField","Wanderer","WandererPolicy"};
foreach(var entry in legacy.Objects)if(entry.Fields!=null)foreach(var field in newFields)entry.Fields.Remove(field);
var migrated=AutoSaveStore.Restore<WorldSetup>(legacy);Check(migrated.WandererPolicyFor(0)==WandererPolicy.Retreat,"Earlier saves get safe policy defaults");
w=World();w.Relations.Released[0]=true;Check(w.Relations.CanAct(0),"Released powered conquerors can act");w.Robots.PersuadeAtHome(w);Check(w.Robots.DomesticPersuasionTurns.ContainsKey(0),"Released conqueror still receives domestic persuasion");
w.Relations.Powered[0]=false;Check(!w.Relations.CanAct(0),"Battery removal still prevents action");
// Several landless residents exceed one board's entrances and must join split boards.
w=World();home=w.Nodes.Current(0).Id;var invadingSource=w.Routes.Neighbors(w.Nodes.All[home]).Select(r=>w.Nodes.At(r.Target,r.Entry)).First(n=>n.Id!=home);
foreach(var n in w.Nodes.All)n.Owner=1;invadingSource.Owner=2;
foreach(int guest in new[]{0,3,4,5,6})
{
 w.ConquerorLocations[guest]=w.Nodes.All[home].Cell;w.ConquerorPoints[guest]=w.Nodes.All[home].Center;w.SetWandererPolicy(guest,WandererPolicy.Win);
 if(!w.HumanPlayers.Contains(guest))w.HumanPlayers.Add(guest);w.Robots.Retinues[guest].Add(w.Robots.Create(guest,RobotParts.Complete));
}
w.Nodes.Recount();w.Campaign.RobotBattles.Rules=new(){SegmentTurns=1};w.Campaign.Resolve(w,new[]{new March(2,home,0,invadingSource.Id,false)});
var crowded=w.Campaign.RobotBattles.Encounters.First(e=>e.Target==home);
Check(crowded.Battles.Count>1 && crowded.Battles.All(b=>b.SplitBoard),"Crowded bystanders use split boards without partial flag wins");
Check(crowded.Battles.SelectMany(b=>b.Units.Concat(b.Armies.SelectMany(a=>a.Waiting))).Count(u=>u.Role==BattleRole.Queen)==5,"All five resident queens participate once");
for(int turn=0;turn<70 && !crowded.Finished;turn++){w.Campaign.Resolve(w,Array.Empty<March>());w.Campaign.RobotBattles.Check(w);}
// Persuasion policy withdraws all units when an armed enemy closes on the queen.
var battle=new BattleState(10){Kind=BattleKind.Edge};battle.AddArmy(0,0,Array.Empty<Robot>(),true);battle.AddArmy(1,1,Array.Empty<Robot>(),false);
battle.Army(0).Wanderer=true;battle.Army(0).WandererPolicy=WandererPolicy.Persuade;
battle.Army(0).Waiting.Clear();var queen=new BattleUnit{Id=-1,Owner=0,Role=BattleRole.Queen,Position=new(4,4),Daggers=0,Rifles=0};battle.Units.Add(queen);battle.Units.Add(new(){Id=10,Owner=1,Position=new(4,6)});
BattleAi.Choose(battle,queen);Check(battle.Army(0).Withdrawing && queen.Retreating,"Danger triggers retreat");
Console.WriteLine("PASS: tutorial preset, peaceful foreign visits, pocket travel, domestic persuasion abroad, all four bystander policies and save persistence.");

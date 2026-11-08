using FolderVerse;
if(args.Contains("--ui")){using var game=new CountrySettingsUi();game.Run();return;}
static void Check(bool value,string message){if(!value)throw new Exception(message);}
var w=new WorldSetup();w.SetParticipantCounts(1,59);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);w.HumanPlayers.Add(0);w.SelectTurnRuler(0);
int active=w.ActiveCount,total=w.ParticipantCount;
for(int seed=0;seed<100;seed++){
    var cast=new WorldSetup();cast.SetParticipantCounts(0,60);cast.SetCast(seed);
    var pairs=new HashSet<(int,int)>();
    for(int i=0;i<60;i++){
        int a=cast.ColorIndices[i]%WorldSetup.Colors.Length,b=cast.CountrySecondaryColor(i);
        Check(a!=b && pairs.Add((Math.Min(a,b),Math.Max(a,b))),"No identical or reversed two-color pairs across sixty countries");
        Check(cast.CountryPattern(i)>0 && cast.CountryEmblem(i)>0,"Large casts use both patterns and emblems");
    }
    cast.SetParticipantCounts(0,7);cast.SetCast(seed);
    Check(Enumerable.Range(0,7).All(i=>cast.CountryPattern(i)==0 && cast.CountryEmblem(i)==0),"Seven-country casts remain plain after reroll");
}

Check(Enumerable.Range(0,60).Select(i=>(w.OwnerColor(i),w.CountryPattern(i))).Distinct().Count()==60,"60 default country identities are unique");
w.Relations.SetSuperior(1,0);w.SetHumanPlayer(1,true);
Check(w.ControllerOf(0)==0 && w.ControllerOf(1)==1 && !w.ControlledParty(0).Contains(1),"Human subordinate keeps a separate controller");
w.Campaign.UseRobotCombat=false;int turn=w.Population.Turn;w.Campaign.Advance(w,-1,0,enemies:true);
Check(w.PlayerSlot==1 && w.Population.Turn==turn,"Human subordinate receives its own domestic phase");
w.Campaign.Advance(w,-1,0);
Check(w.Population.Turn==turn+1,"Human boss and subordinate finish one shared world turn");
w.SetHumanPlayer(1,false);Check(w.ControllerOf(1)==0,"CPU subordinate inherits the human boss");
w.SetHumanPlayer(0,false);Check(w.ComputerOnly && w.ManualRulers().Length==0,"Last human can become CPU without legacy fallback");
w.SetHumanPlayer(2,true);Check(!w.ComputerOnly && w.ControllerOf(2)==2,"CPU-only world can become manual again");
Check(w.ActiveCount==active && w.ParticipantCount==total,"Control changes preserve the cast and world");
w.SetHumanPlayer(0,true);w.SelectTurnRuler(0);turn=w.Population.Turn;w.Campaign.Advance(w,-1,0);var route=w.Routes.NodeChoices(0).First();w.Campaign.Plan(new(0,w.Nodes.At(route.Target,route.Entry).Id,0,Peaceful:true));w.SetHumanPlayer(0,false);Check(w.Campaign.PlannedOrders.Count==1,"CPU switch retains an existing march");w.SelectTurnRuler(2);w.Campaign.Advance(w,-1,0);
Check(w.Population.Turn==turn+1 && !w.Campaign.AwaitingPlayers,"Removing a submitted human cannot leave the round waiting forever");
Check(w.Campaign.PlannedOrders.Count==0,"Switched CPU orders resolve rather than remaining stale");
var original=w.OwnerColor(2);var robot=w.Robots.Create(2,RobotParts.Complete);
w.SetCountryAppearance(2,6,12,3);Check(w.OwnerColor(2)==WorldSetup.Colors[6] && w.CountryMapColor(2,0,1)==WorldSetup.Colors[12] && robot.Owner==2,"Appearance changes preserve robot identity");
w.ToggleComputerOperations(58);
var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
Check(restored.ShowComputerOperations(58) && !restored.ShowComputerOperations(2),"CPU display preference survives save/load per ruler");
Check(restored.IsHumanPlayer(2) && restored.CountryPattern(2)==3 && restored.CountrySecondaryColor(2)==12,"Appearance and controls survive save/load");
var legacy=AutoSaveStore.Capture(w);foreach(var entry in legacy.Objects)if(entry.Fields!=null){entry.Fields.Remove("_countryPatterns");entry.Fields.Remove("_countrySecondaryColors");entry.Fields.Remove("_showComputerOperations");}
var old=AutoSaveStore.Restore<WorldSetup>(legacy);Check(old.CountryPattern(2)==0 && old.OwnerColor(2)==w.OwnerColor(2),"Old saves retain their colors and receive safe defaults");
Check(!old.ShowComputerOperations(58),"Old saves default CPU display to off");
string Mask(int pattern,int rotation)=>string.Concat(Enumerable.Range(0,100).Select(i=>{int x=i%10,y=i/10;for(int r=0;r<rotation;r++)(x,y)=(9-y,x);return WorldSetup.CountryPatternSecondary(pattern,x,y)?'1':'0';}));
for(int a=0;a<WorldSetup.CountryPatternNames.Length;a++)for(int b=0;b<a;b++)for(int r=0;r<4;r++)Check(Mask(a,0)!=Mask(b,r),"No rotated duplicate pattern options");
Console.WriteLine("PASS: 60 identities, human subordinate turns, CPU transitions, stable ownership, save roundtrip/legacy and rotated-pattern deduplication.");

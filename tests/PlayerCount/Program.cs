using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Check(Game1.EstimatedPlayTime(1)=="20分","1 person estimate");
Check(Game1.EstimatedPlayTime(10)=="3時間20分","10 person estimate");
Check(Game1.EstimatedPlayTime(20)=="6時間40分","20 person estimate");
foreach(int count in new[]{1,2,10,20})
{
    var w=new WorldSetup();w.SetPlayerCount(count);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);
    Check(w.ActiveCount>=count && w.Looks.Length==WorldSetup.CastCount,"Available conquerors");
    for(int r=0;r<w.ActiveCount;r++)_ = w.OwnerColor(r);
    var layout=CharacterSelectionLayout.Create(w.TerritoryCounts,w.ActiveCount);
    Check(layout.Length==w.ActiveCount+1 && layout.All(t=>t.Bounds.Width>0 && t.Bounds.Height>0),"20-person selection layout");
    w.HumanPlayers.AddRange(Enumerable.Range(0,count).Reverse());w.SelectTurnRuler(count-1);
    var order=w.ManualRulers();Check(order.SequenceEqual(w.HumanPlayers),"Selection order retained");
    int before=w.Population.Turn;
    foreach(int r in order)
    {
        Check(w.PlayerSlot==r,"Correct next human");
        w.Campaign.Advance(w,-1,0);
        if(r!=order[^1])Check(w.Population.Turn==before && w.Campaign.AwaitingPlayers,"No resolution before everyone finishes");
    }
    Check(w.Population.Turn==before+1 && !w.Campaign.AwaitingPlayers,"Exactly one resolution after all humans");
    w.SetTerritoryRights(789);Check(w.TerritoryRights.Sum()==w.Cells.Length && w.TerritoryRights.Take(w.ActiveCount).All(n=>n>0),"Land rights preserve every participant");
    int cell=0;foreach(int r in w.LandOrder()){var claim=Enumerable.Range(cell,w.TerritoryRights[r]).ToArray();cell+=claim.Length;w.ClaimLand(r,claim);}
    w.CompleteLand(123);Check(w.Capitals.Length==w.ActiveCount && w.Capitals.All(c=>c>=0),"All participants start at a valid node");
}
Console.WriteLine("PASS: 1/2/10/20 participants, estimates, cast, layout, selection order, simultaneous orders and land setup.");

foreach(var (humans,computers) in new[]{(0,60),(60,0),(1,59),(59,1),(1,2),(2,0),(1,0),(0,1),(30,30)})
{
    var w=new WorldSetup();w.SetParticipantCounts(humans,computers);w.SetWorld(0);w.SetCast(123);w.SetTerritoryRights(456);
    Check(w.ActiveCount==humans+computers,"Exact participant count");
    Check(w.ComputerOnly==(humans==0),"Computer-only mode");
    Check(w.Looks.Length==humans+computers && w.Portraits.Length==humans+computers && w.Looks.Distinct().Count()==humans+computers,"Unique expanded cast");
    for(int r=0;r<w.ActiveCount;r++)_ = w.OwnerColor(r);
    var layout=CharacterSelectionLayout.Create(w.TerritoryCounts,w.ActiveCount);
    Check(layout.Length==w.ActiveCount+1 && layout.All(t=>t.Bounds.Width>0 && t.Bounds.Height>0),"Expanded selection layout");
    var overview=CharacterSelectionLayout.CreateOverview(w.TerritoryCounts.Take(w.ActiveCount).ToArray());
    Check(overview.Length==w.ActiveCount && overview.All(t=>t.Bounds.Width>0 && t.Bounds.Height>0),"Expanded overview layout");
    int cell=0;foreach(int r in w.LandOrder()){var claim=Enumerable.Range(cell,w.TerritoryRights[r]).ToArray();cell+=claim.Length;w.ClaimLand(r,claim);}
    w.CompleteLand(789);w.HumanPlayers.AddRange(Enumerable.Range(0,humans));w.SelectPlayer(0);
    Check(w.ManualRulers().Length==humans,"Only humans receive manual controls");
    Check(w.Nodes.All.All(n=>n.Owner<humans+computers),"No extra computer owners");
    var restored=AutoSaveStore.Restore<WorldSetup>(AutoSaveStore.Capture(w));
    Check(restored.ComputerOnly==w.ComputerOnly && restored.ParticipantCount==w.ParticipantCount,"Participant save round trip");
    w.Campaign.UseRobotCombat=false;
    foreach(int r in humans==0?new[]{0}:w.ManualRulers()){w.SelectPlayer(r);w.Campaign.Advance(w,-1,0,automaticAll:humans==0);}
    Check(w.Population.Turn==1,"Expanded game advances one complete turn");
}
foreach(var (humans,computers) in new[]{(0,0),(-1,1),(1,-1),(1,60),(61,0),(int.MaxValue,int.MaxValue)})
{
    bool rejected=false;try{new WorldSetup().SetParticipantCounts(humans,computers);}catch(ArgumentOutOfRangeException){rejected=true;}
    Check(rejected,"Invalid counts rejected");
}
Console.WriteLine("PASS: 0/60, 60/0, mixed, single participant, exact cast, colors, land, layouts, controls, save and turn progression.");

{
    var robots=new RobotWorld();robots.Initialize(1,60);
    var robot=robots.Create(59,RobotParts.Complete);
    var battle=new BattleState(123,new BattleRules());battle.AddArmy(59,0,new[]{robot});battle.Check();
    Check(battle.Armies.Single().Owner==59,"Expanded owners accepted in robot combat");
}

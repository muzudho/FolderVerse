using FolderVerse;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Check(Game1.EstimatedPlayTime(1)=="20分","1 person estimate");
Check(Game1.EstimatedPlayTime(10)=="3時間20分","10 person estimate");
Check(Game1.EstimatedPlayTime(20)=="6時間40分","20 person estimate");
foreach(int count in new[]{1,2,10,20})
{
    var w=new WorldSetup();w.SetPlayerCount(count);w.SetWorld(0);w.SetCast(123);w.SetPlacement(456);
    Check(w.ActiveCount>=count && w.Looks.Length==20,"Available conquerors");
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

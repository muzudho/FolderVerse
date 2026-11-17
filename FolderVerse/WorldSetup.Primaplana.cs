namespace FolderVerse;

using System.Linq;

public sealed partial class WorldSetup
{
    public void PreparePrimaplana()
    {
        EquipmentMode=RobotEquipmentMode.Materials;
        SetParticipantCounts(1,2);
        const int seed=123;
        ApplyWorldGeography(seed,3,1,1,CreateCells(3,1,1,plane:true),0);
        TutorialStep=4;
        PolarAxis=1;
        CityNames=WorldNames.Cities(Cells,seed,Terrain);
        SetCast(seed);
        var characters=Enumerable.Range(0,ConquerorCatalog.BaseCount)
            .Select(id=>new ConquerorLook(id,0)).Where(ConquerorRelations.AcceptsRecruitment).Take(3).ToArray();
        for(int ruler=0;ruler<ActiveCount;ruler++){
            Looks[ruler]=characters[ruler];
            Portraits[ruler]=Looks[ruler].BaseId;
        }
        ConquerorFullNames=WorldNames.Conquerors(Looks,seed);
        ConquerorNames=WorldNames.ShortNames(ConquerorFullNames);
        SetTerritoryRights(seed);
        for(int ruler=0;ruler<ActiveCount;ruler++)ClaimLand(ruler,new[]{ruler});
        CompleteLand(seed,robotScatterPercent:0,rawStockPercent:0);
        foreach(var node in Nodes.All){
            Robots.Workshops[node.Id].RemoveProductionLines();
            var store=Robots.Nodes[node.Id];
            store.Replace(Enumerable.Empty<Robot>());
            if(node.Owner==0)while(store.HasSpace)store.Add(Robots.Create(0,RobotParts.Complete));
        }
        var pocket=Robots.Retinues[0];
        while(pocket.HasSpace)pocket.Add(Robots.Create(0,RobotParts.Complete));
        SelectPlayer(0);
        HumanPlayers.Add(0);
        SelectTurnRuler(0);
    }
}

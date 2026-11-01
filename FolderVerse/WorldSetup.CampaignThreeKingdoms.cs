namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed partial class WorldSetup
{
    public void PrepareCampaignThreeKingdoms(int worldSeed,int castSeed,int placementSeed,int fortuneSeed,params int[] characters)
    {
        if(worldSeed<0 || castSeed<0 || placementSeed<0 || fortuneSeed<0)throw new ArgumentOutOfRangeException(nameof(worldSeed));
        if(characters==null || characters.Length!=3 || characters.Any(c=>c<0 || c>=ConquerorCatalog.BaseCount))throw new ArgumentException("Three valid characters are required",nameof(characters));
        SetParticipantCounts(1,2);
        do{SetWorld(worldSeed);worldSeed=worldSeed==int.MaxValue?0:worldSeed+1;}while(Width!=2 || Height!=2 || Depth!=2);
        SetCast(castSeed);
        for(int i=0;i<3;i++){Looks[i]=new(characters[i],0);Portraits[i]=characters[i];}
        ConquerorFullNames=WorldNames.Conquerors(Looks,castSeed);ConquerorNames=WorldNames.ShortNames(ConquerorFullNames);
        SetTerritoryRights(placementSeed);
        for(int i=0;i<3;i++)TerritoryRights[i]=8;
        TerritoryCounts=(int[])TerritoryRights.Clone();
        var faces=Cells.Select(c=>c.Face).Distinct().OrderBy(f=>f).ToArray();
        bool Adjacent(int a,int b)=>Cells.Where(c=>c.Face==a).Any(c=>c.Neighbors.Any(n=>Cells[n].Face==b));
        var partitions=new List<int[][]>();
        void Pair(int[] remaining,List<int[]> pairs){
            if(remaining.Length==0){partitions.Add(pairs.ToArray());return;}
            foreach(int other in remaining.Skip(1).Where(f=>Adjacent(remaining[0],f))){
                pairs.Add(new[]{remaining[0],other});Pair(remaining.Where(f=>f!=remaining[0] && f!=other).ToArray(),pairs);pairs.RemoveAt(pairs.Count-1);
            }
        }
        Pair(faces,new());
        var random=new SeedRandom(placementSeed);var chosen=partitions[random.Next(partitions.Count)];random.Shuffle(chosen);
        for(int ruler=0;ruler<3;ruler++)ClaimLand(ruler,Cells.Where(c=>chosen[ruler].Contains(c.Face)).Select(c=>c.Id).ToArray());
        CompleteLand(fortuneSeed);SelectPlayer(0);HumanPlayers.Add(0);SelectTurnRuler(0);
    }
}

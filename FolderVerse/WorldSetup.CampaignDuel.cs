namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed partial class WorldSetup
{
    // Campaign definitions use the normal terrain, economy, robot and save systems.
    public void PrepareCampaignDuel(int worldSeed,int castSeed,int placementSeed,int fortuneSeed,int humanCharacter,int computerCharacter)
    {
        if(worldSeed<0 || castSeed<0 || placementSeed<0 || fortuneSeed<0)throw new ArgumentOutOfRangeException(nameof(worldSeed));
        if(humanCharacter<0 || humanCharacter>=ConquerorCatalog.BaseCount || computerCharacter<0 || computerCharacter>=ConquerorCatalog.BaseCount)throw new ArgumentOutOfRangeException(nameof(humanCharacter));
        SetParticipantCounts(1,1);
        do{SetWorld(worldSeed);worldSeed=worldSeed==int.MaxValue?0:worldSeed+1;}while(Width!=1 || Height!=1 || Depth!=1);
        SetCast(castSeed);
        Looks[0]=new(humanCharacter,0);Looks[1]=new(computerCharacter,0);
        Portraits[0]=humanCharacter;Portraits[1]=computerCharacter;
        ConquerorFullNames=WorldNames.Conquerors(Looks,castSeed);ConquerorNames=WorldNames.ShortNames(ConquerorFullNames);
        SetTerritoryRights(placementSeed);TerritoryRights[0]=TerritoryRights[1]=3;TerritoryCounts=(int[])TerritoryRights.Clone();
        bool Connected(int[] ids){var seen=new HashSet<int>{ids[0]};var queue=new Queue<int>();queue.Enqueue(ids[0]);while(queue.TryDequeue(out int id))foreach(int next in Cells[id].Neighbors)if(ids.Contains(next) && seen.Add(next))queue.Enqueue(next);return seen.Count==ids.Length;}
        var triples=(from a in Enumerable.Range(0,6) from b in Enumerable.Range(a+1,5-a) from c in Enumerable.Range(b+1,5-b) select new[]{a,b,c})
            .Where(ids=>Connected(ids) && Connected(Enumerable.Range(0,6).Except(ids).ToArray())).ToArray();
        var first=triples[new SeedRandom(placementSeed).Next(triples.Length)];
        ClaimLand(0,first);ClaimLand(1,Enumerable.Range(0,6).Except(first).ToArray());
        CompleteLand(fortuneSeed);SelectPlayer(0);HumanPlayers.Add(0);SelectTurnRuler(0);
    }
}

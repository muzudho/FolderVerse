namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
public sealed partial class WorldSetup
{
    public int[] TerritoryRights {get;private set;}=new int[CastCount];
    public bool LandPending {get;private set;}
    public int FortuneSeed {get;private set;}
    public void SetTerritoryRights(int seed)
    {
        Salvage.Reset();
        if(seed<0)throw new ArgumentOutOfRangeException(nameof(seed));
        PlacementSeed=seed;PlayerSlot=-1;Relations.Reset();
        var random=new SeedRandom(seed);TerritoryRights=new int[CastCount];
        for(int i=0;i<ActiveCount;i++)TerritoryRights[i]=1;
        var order=Enumerable.Range(0,ActiveCount).OrderBy(_=>random.Next(1000000)).ToArray();
        // Rank-weighted rights preserve both large nations and one-cell hard starts.
        var tickets=order.SelectMany((r,rank)=>Enumerable.Repeat(r,rank==0 && ActiveCount>1?0:(rank+1)*(rank+1))).ToArray();
        for(int i=ActiveCount;i<Cells.Length;i++)TerritoryRights[tickets[random.Next(tickets.Length)]]++;
        TerritoryCounts=(int[])TerritoryRights.Clone();
        Owners=Enumerable.Repeat(-1,Cells.Length).ToArray();Capitals=Array.Empty<int>();ConquerorLocations=Array.Empty<int>();
        // Terrain and connections exist before any country's economy or robots.
        Population.Initialize(this);Routes.Initialize(this);Nodes.Initialize(this);Robots.Initialize(0,ActiveCount);
        Routes.RefreshDisplay();TerritoryCounts=(int[])TerritoryRights.Clone();LandPending=true;
    }
    public int[] LandOrder()=>Enumerable.Range(0,ActiveCount).OrderByDescending(i=>TerritoryRights[i]).ThenBy(i=>unchecked((uint)(i*73856093^PlacementSeed))).ToArray();
    public void ClaimLand(int ruler,int[] cells)
    {
        if(!LandPending || ruler<0 || ruler>=ActiveCount || cells.Length!=TerritoryRights[ruler] || cells.Distinct().Count()!=cells.Length || cells.Any(c=>c<0 || c>=Cells.Length || Owners[c]>=0) || Owners.Contains(ruler))throw new ArgumentException("Invalid land claim");
        foreach(int cell in cells)Owners[cell]=ruler;
    }
    public void CompleteLand(int fortuneSeed,int robotScatterPercent=25)
    {
        if(!LandPending || Owners.Any(o=>o<0))throw new InvalidOperationException("Land claims are incomplete");
        if(fortuneSeed<0)throw new ArgumentOutOfRangeException(nameof(fortuneSeed));
        if(robotScatterPercent<0 || robotScatterPercent>100)throw new ArgumentOutOfRangeException(nameof(robotScatterPercent));
        FortuneSeed=fortuneSeed;
        TerritoryCounts=Enumerable.Range(0,CastCount).Select(o=>Owners.Count(v=>v==o)).ToArray();
        Capitals=Enumerable.Range(0,ActiveCount).Select(o=>Array.IndexOf(Owners,o)).ToArray();ConquerorLocations=(int[])Capitals.Clone();
        Population.Initialize(this);Routes.Initialize(this);ConquerorPoints=ConquerorLocations.Select(Routes.Start).ToArray();Nodes.Initialize(this);
        Robots.Initialize(Nodes.All.Length,ActiveCount);
        if(TutorialStep==0)InitializeTerrainHangars();
        Robots.ConfigureFactories(this);var random=new SeedRandom(fortuneSeed);
        if(TutorialStep==0)Robots.InitializeMapleSupply(this);
        int scatterRemainder=50;
        foreach(var node in Nodes.All.Where(n=>n.Owner>=0))
        {
            var workshop=Robots.Workshops[node.Id];
            bool hasFactory=TutorialStep!=1 && !Routes.NearWhirlpool(node);
            if(hasFactory)workshop.GenerateLines(fortuneSeed^node.Id*7919,EquipmentMode);else workshop.RemoveProductionLines();
            if(random.Next(5)==0)workshop.ConfigureDisposal();
            if(TutorialStep==1){
                for(int i=0;i<4;i++)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,RobotParts.Complete,RobotRole.Soldier));
            }else{
                int count=1+random.Next(8);
                var store=Robots.Nodes[node.Id];
                int workers=TutorialStep==0?Math.Min(workshop.BaseLineCount,store.TotalCapacity):0;
                bool harvest=TutorialStep==0 && hasFactory && Robots.MapleDeposit(node.Id)>0 && workshop.LineLimit>0 && store.TotalCapacity>1;
                if(harvest)workers=Math.Min(Math.Max(2,workers),Math.Min(workshop.LineLimit,store.TotalCapacity-1));
                if(TutorialStep==0)workshop.RemoveProductionLines();
                int capacity=Math.Max(0,store.TotalCapacity-workers-(harvest?1:0));
                int scaled=Math.Min(count,capacity)*(TutorialStep==0?robotScatterPercent:100)+scatterRemainder;
                int placed=scaled/100;
                scatterRemainder=scaled%100;
                for(int i=0;i<count;i++){
                    var parts=(RobotParts)(1+random.Next(7));
                    if(i<placed)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,parts,i==0?RobotRole.Captain:RobotRole.Soldier));
                }
                for(int i=0;i<workers;i++){
                    bool collects=harvest && i==workers-1;
                    var worker=Robots.Create(node.Owner,RobotParts.Complete) with {FactoryWorker=true,HarvestAssignment=collects};
                    store.Add(worker);
                }
                if(workers>0){
                    Robots.SyncWorkers(node.Id);
                    if(harvest)
                        foreach(var lane in workshop.ProductionLines.Where(l=>l.HarvestOnly))lane.SelectMapleHarvest(1);
                }
            }
            foreach(var parts in Enumerable.Range(1,7).Select(i=>(RobotParts)i))
            {
                var targets=Routes.Neighbors(node).Select(r=>Nodes.At(r.Target,r.Entry).Id).Where(id=>id!=node.Id).Distinct().ToArray();
                if(targets.Length==0)continue;
                int total=random.Next(361);var weights=new System.Collections.Generic.List<TransportWeight>();
                foreach(int target in targets){int amount=random.Next(total+1);if(amount>0)weights.Add(new(target,amount));total-=amount;}
                Robots.Transport.SetPlan(node.Id,node.Owner,parts,weights,false);
            }
        }
        if(TutorialStep==0)
            for(int ruler=0;ruler<ActiveCount;ruler++)
                for(int i=0;i<2;i++)Robots.Retinues[ruler].Add(Robots.Create(ruler,RobotParts.Complete));
        for(int ruler=0;ruler<ActiveCount;ruler++)ConquerorPoints[ruler]=Nodes.Current(ruler).Center;
        Routes.RefreshDisplay();Campaign.Reset();LandPending=false;
        PlaceInitialTin(fortuneSeed);
        Salvage.Initialize(this);
    }

    private void PlaceInitialTin(int seed)
    {
        if(EquipmentMode!=RobotEquipmentMode.Materials)return;
        var random=new SeedRandom(seed^0x2A19E3);
        var used=new System.Collections.Generic.HashSet<int>();
        foreach(var part in RobotMaterials.Parts)
        {
            // One of each part at most, on separate sites, independent of the world generation RNG.
            var candidates=Nodes.All.Where(n=>n.Owner>=0 && n.Functional && !used.Contains(n.Id))
                .Select(n=>(n.Id,Robot:Robots.Nodes[n.Id].Robots.FirstOrDefault(r=>r.Parts==part)))
                .Where(candidate=>candidate.Robot!=null).ToArray();
            if(candidates.Length==0)continue;
            var (node,robot)=candidates[random.Next(candidates.Length)];
            used.Add(node);
            Robots.Nodes[node].Replace(Robots.Nodes[node].Robots.Select(r=>r.Id!=robot.Id?r:part switch
            {
                RobotParts.Head=>r with {HeadMaterial=RobotMaterial.Tin},
                RobotParts.Body=>r with {BodyMaterial=RobotMaterial.Tin},
                _=>r with {LegsMaterial=RobotMaterial.Tin}
            }).ToArray());
        }
    }

    internal int TerrainArea(int cell,TravelTerrain terrain)
    {
        int area=0;
        for(int y=0;y<10;y++)
        for(int x=0;x<10;x++)
            if(Routes.Terrain(cell,new Point(x,y))==terrain)area++;
        return area;
    }

    private void InitializeTerrainHangars()
    {
        foreach(var region in Nodes.All.GroupBy(node=>(node.Cell,node.Terrain))){
            var (cell,terrain)=region.Key;
            int area=TerrainArea(cell,terrain);
            double mean=terrain==TravelTerrain.Land
                ? area<=50 ? 1+area/10.0 : 6+(area-50)*.08
                : 2+Math.Max(0,area-50)*.04;
            foreach(var node in region){
                var random=new SeedRandom(unchecked(WorldSeed^(node.Id+1)*7919));
                int center=(int)mean;
                if(random.Unit()<mean-center)center++;
                int spread=Math.Min(terrain==TravelTerrain.Land?2:1,Math.Min(center-1,RobotStore.Capacity-center));
                int capacity=center+random.Next(2*spread+1)-spread;
                if(Routes.NearWhirlpool(node))capacity=1;
                Robots.Nodes[node.Id].SetCapacity(capacity);
                Robots.Workshops[node.Id].SetStorageSlotLimit(capacity);
            }
        }
    }
}


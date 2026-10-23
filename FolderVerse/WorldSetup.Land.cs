namespace FolderVerse;
using System;
using System.Linq;
public sealed partial class WorldSetup
{
    public int[] TerritoryRights {get;private set;}=new int[20];
    public bool LandPending {get;private set;}
    public int FortuneSeed {get;private set;}
    public void SetTerritoryRights(int seed)
    {
        if(seed<0)throw new ArgumentOutOfRangeException(nameof(seed));
        PlacementSeed=seed;PlayerSlot=-1;Relations.Reset();
        var random=new SeedRandom(seed);TerritoryRights=new int[CastCount];
        for(int i=0;i<ActiveCount;i++)TerritoryRights[i]=1;
        var order=Enumerable.Range(0,ActiveCount).OrderBy(_=>random.Next(1000000)).ToArray();
        // Rank-weighted rights preserve both large nations and one-cell hard starts.
        var tickets=order.SelectMany((r,rank)=>Enumerable.Repeat(r,rank==0?0:(rank+1)*(rank+1))).ToArray();
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
    public void CompleteLand(int fortuneSeed)
    {
        if(!LandPending || Owners.Any(o=>o<0))throw new InvalidOperationException("Land claims are incomplete");
        if(fortuneSeed<0)throw new ArgumentOutOfRangeException(nameof(fortuneSeed));
        FortuneSeed=fortuneSeed;
        TerritoryCounts=Enumerable.Range(0,CastCount).Select(o=>Owners.Count(v=>v==o)).ToArray();
        Capitals=Enumerable.Range(0,ActiveCount).Select(o=>Array.IndexOf(Owners,o)).ToArray();ConquerorLocations=(int[])Capitals.Clone();
        Population.Initialize(this);Routes.Initialize(this);ConquerorPoints=ConquerorLocations.Select(Routes.Start).ToArray();Nodes.Initialize(this);
        Robots.Initialize(Nodes.All.Length,ActiveCount);Robots.ConfigureFactories(this);var random=new SeedRandom(fortuneSeed);
        foreach(var node in Nodes.All.Where(n=>n.Owner>=0))
        {
            if(TutorialStep==1)Robots.Workshops[node.Id].RemoveProductionLines();else Robots.Workshops[node.Id].GenerateLines(fortuneSeed^node.Id*7919);
            if(random.Next(5)==0)Robots.Workshops[node.Id].ConfigureDisposal();
            if(TutorialStep==1){
                for(int i=0;i<4;i++)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,RobotParts.Complete,RobotRole.Soldier));
            }else{
                int count=1+random.Next(8);
                for(int i=0;i<count;i++)Robots.Nodes[node.Id].Add(Robots.Create(node.Owner,(RobotParts)(1+random.Next(7)),i==0?RobotRole.Captain:RobotRole.Soldier));
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
        for(int ruler=0;ruler<ActiveCount;ruler++)ConquerorPoints[ruler]=Nodes.Current(ruler).Center;
        Routes.RefreshDisplay();Campaign.Reset();LandPending=false;
    }
}


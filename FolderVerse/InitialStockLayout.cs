namespace FolderVerse;
using System;
using System.Linq;

public sealed record InitialStockSettings(int RobotPercent=25,int RawPercent=25,int DistributionSeed=0,bool Uniform=false);
public sealed record InitialStockSite(int Node,int CapacityUnits,int FixedRobotUnits,int RobotUnits,int RawUnits);
public sealed record InitialStockLayout(InitialStockSettings Settings,InitialStockSite[] Sites)
{
    public int Version=>1;
    public int CapacityUnits=>Sites.Sum(s=>s.CapacityUnits);
    public int FixedRobotUnits=>Sites.Sum(s=>s.FixedRobotUnits);
    public int RobotUnits=>Sites.Sum(s=>s.RobotUnits);
    public int RawUnits=>Sites.Sum(s=>s.RawUnits);
    public int EmptyUnits=>CapacityUnits-RobotUnits-RawUnits;
}

public sealed partial class WorldSetup
{
    public InitialStockLayout InitialStock {get;private set;}

    // Called only while constructing the starting world or its isolated preview.
    public void RebuildInitialStock(InitialStockSettings settings)
    {
        if(Population.Turn!=0)throw new InvalidOperationException("Initial stock can only be configured before the first day advances.");
        if(settings.RobotPercent<0 || settings.RobotPercent>100 || settings.RawPercent<0 || settings.RawPercent>100)
            throw new ArgumentOutOfRangeException(nameof(settings));
        var sites=Nodes.All.Where(n=>n.Owner>=0 && n.Functional)
            .OrderBy(n=>Robots.Nodes[n.Id].TotalCapacity).ThenBy(n=>n.Id).ToArray();
        foreach(var node in sites){
            var store=Robots.Nodes[node.Id];store.SetMaplePackages(Array.Empty<MaplePackage>());
            store.Replace(store.Robots.Where(r=>r.WarehouseKeeper || r.FactoryWorker).ToArray());
        }
        Robots.ResetInitialStockIds();
        var capacities=sites.Select(n=>Robots.Nodes[n.Id].TotalCapacity*3).ToArray();
        var fixedUnits=sites.Select(n=>Robots.Nodes[n.Id].Robots.Sum(r=>SalvagePlan.PartCount(r.Parts))).ToArray();
        int total=capacities.Sum(),fixedTotal=fixedUnits.Sum();
        int robotTotal=Math.Clamp((int)Math.Round(total*settings.RobotPercent/100d),fixedTotal,total);
        int rawTotal=Robots.MapleSupplyEnabled?Math.Min(total-robotTotal,(int)Math.Round(total*settings.RawPercent/100d)):0;
        var random=new SeedRandom(settings.DistributionSeed^0x53484152);
        float[] Weights(){
            float phase=random.Unit()*MathF.Tau,phase2=random.Unit()*MathF.Tau;
            return sites.Select((_,i)=>settings.Uniform?1f:MathF.Exp(1.7f*MathF.Sin(i/(float)Math.Max(1,sites.Length-1)*MathF.Tau+phase)+.8f*MathF.Sin(i/(float)Math.Max(1,sites.Length-1)*MathF.Tau*3+phase2))).ToArray();
        }
        var free=capacities.Zip(fixedUnits,(a,b)=>a-b).ToArray();
        var robotAllocation=AllocateStockUnits(robotTotal-fixedTotal,free,Weights());
        var rawAllocation=AllocateStockUnits(rawTotal,free.Zip(robotAllocation,(a,b)=>a-b).ToArray(),Weights());
        var materials=RobotWorld.RawMaterials.Where(Robots.RawRequired).ToArray();
        var rows=new InitialStockSite[sites.Length];
        for(int i=0;i<sites.Length;i++){
            var node=sites[i];var store=Robots.Nodes[node.Id];
            int units=robotAllocation[i],index=0;
            while(units>0){
                int amount=Math.Min(3,units);var parts=amount==3?RobotParts.Complete:amount==2?RobotParts.Head|RobotParts.Body:RobotParts.Head;
                var robot=Robots.Create(node.Owner,parts,index++==0?RobotRole.Captain:RobotRole.Soldier).Materialize();
                if(amount==3 && random.Next(100)<8)robot=robot with{IntegratedParts=random.Next(2)==0?RobotParts.Head|RobotParts.Body:RobotParts.Body|RobotParts.Legs};
                if(random.Next(100)<5){var present=RobotMaterials.Parts.Where(p=>(parts&p)!=0).ToArray();robot=robot with{DamagedParts=present[random.Next(present.Length)]};}
                store.Add(robot);units-=amount;
            }
            for(int remaining=rawAllocation[i];remaining>0;){
                int amount=Math.Min(3,remaining);
                Robots.AddStarterRaw(store,new MaplePackage(0,amount*RobotWorld.MaplePackageSize){Material=materials[random.Next(materials.Length)]});
                remaining-=amount;
            }
            rows[i]=new(node.Id,capacities[i],fixedUnits[i],fixedUnits[i]+robotAllocation[i],rawAllocation[i]);
        }
        InitialStock=new(settings,rows);
        PlaceInitialTin(FortuneSeed);
        Salvage.Initialize(this);
    }

    private static int[] AllocateStockUnits(int total,int[] capacities,float[] weights)
    {
        total=Math.Clamp(total,0,capacities.Sum());var result=new int[capacities.Length];
        if(total==0)return result;
        double low=0,high=1;
        double Sum(double level)=>capacities.Select((c,i)=>Math.Min(c,level*c*weights[i])).Sum();
        while(Sum(high)<total)high*=2;
        for(int k=0;k<60;k++){double mid=(low+high)/2;if(Sum(mid)<total)low=mid;else high=mid;}
        var desired=capacities.Select((c,i)=>Math.Min(c,high*c*weights[i])).ToArray();
        for(int i=0;i<result.Length;i++)result[i]=(int)Math.Floor(desired[i]);
        int remaining=total-result.Sum();
        foreach(int i in Enumerable.Range(0,result.Length).OrderByDescending(i=>desired[i]-result[i]).ThenBy(i=>i))
            if(remaining>0 && result[i]<capacities[i]){result[i]++;remaining--;}
        if(remaining!=0)throw new InvalidOperationException("Initial stock allocation did not conserve capacity.");
        return result;
    }
}

public sealed partial class RobotWorld
{
    internal void ResetInitialStockIds()
    {
        var stores=Nodes.Concat(Retinues);
        long robot=stores.SelectMany(s=>s.Robots).Select(r=>r.Id).DefaultIfEmpty(0).Max();
        long raw=stores.SelectMany(s=>s.MaplePackages).Select(p=>p.Id).DefaultIfEmpty(0).Max();
        _nextId=Math.Max(robot,raw)+1;
    }
}

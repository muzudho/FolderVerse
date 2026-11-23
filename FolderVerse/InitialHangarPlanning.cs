namespace FolderVerse;
using System;using System.Linq;

// Geography-only values: independent of owners, fortune and initial inventory.
internal static class InitialHangarPlanning
{
    internal static void Assign(WorldSetup world,Node[] nodes)
    {
        foreach(var region in nodes.GroupBy(n=>n.Cell)){
            foreach(var n in region){n.InitialEmptyGoal=0;n.InitialHangarLimit=30;}
            foreach(var terrain in new[]{TravelTerrain.Land,TravelTerrain.Sea,TravelTerrain.Mountain}){
                int area=world.TerrainArea(region.Key,terrain);
                if(area==0)continue;
                var eligible=region.Where(n=>!world.Routes.NearWhirlpool(n) && (n.Terrain==terrain || n.IsHarbor && terrain==TravelTerrain.Sea)).OrderBy(n=>n.Id).ToArray();
                if(eligible.Length==0)continue;
                // A full land region has twelve free hangars; sea and mountain retain four.
                int budget=terrain==TravelTerrain.Land?(int)Math.Round(area*12/100d):4;
                int Weight(Node n)=>n.IsHarbor?1:2;
                int sum=eligible.Sum(Weight),used=0;
                foreach(var n in eligible){int share=budget*Weight(n)/sum;n.InitialEmptyGoal+=share;used+=share;}
                foreach(var n in eligible.OrderByDescending(n=>budget*Weight(n)%sum).ThenBy(n=>n.Id).Take(budget-used))n.InitialEmptyGoal++;
            }
            foreach(var n in region){
                if(world.Routes.NearWhirlpool(n)){n.InitialEmptyGoal=1;n.InitialHangarLimit=1;}
                else n.InitialHangarLimit=Math.Clamp(n.InitialEmptyGoal+1+(n.InitialEmptyGoal+2)/3+3,1,30);
            }
        }
    }

    internal static Robot CreateKeeper(WorldSetup world,int owner)
    {
        var robot=world.Robots.Create(owner,RobotParts.Complete) with {WarehouseKeeper=true};
        return world.EquipmentMode==RobotEquipmentMode.Materials
            ?robot with {HeadMaterial=RobotMaterial.Paper,BodyMaterial=RobotMaterial.Paper,LegsMaterial=RobotMaterial.Paper}:robot;
    }

    internal static void FitKeepers(WorldSetup world,Node node)
    {
        var store=world.Robots.Nodes[node.Id];
        var retained=store.Robots.Where(r=>!r.WarehouseKeeper).ToArray();
        store.SetMaplePackages(Array.Empty<MaplePackage>());
        // Replace atomically after selecting a feasible combination.
        int workers=retained.Length,maximum=store.WarehouseLimit??30;
        int bestError=int.MaxValue,bestCount=0,bestSum=0;
        int multiplier=node.Purpose==NodePurpose.Relay?2:1;
        for(int count=0;count<=29;count++)for(int sum=count;sum<=count*3;sum++){
            int capacity=Math.Min(maximum,1+count+multiplier*sum);
            if(capacity<count+workers)continue;
            int error=Math.Abs(capacity-count-workers-node.InitialEmptyGoal);
            if(error<bestError || error==bestError&&count<bestCount){bestError=error;bestCount=count;bestSum=sum;}
        }
        var keepers=new System.Collections.Generic.List<Robot>();int remaining=bestSum;
        for(int i=0;i<bestCount;i++){
            int group=Math.Min(3,remaining-(bestCount-i-1));remaining-=group;
            var r=CreateKeeper(world,node.Owner) with{HeadOrigin=node.Owner,BodyOrigin=group>=2?node.Owner:-2,LegsOrigin=group>=3?node.Owner:-2};
            keepers.Add(r);
        }
        store.Replace(retained.Concat(keepers));
    }
}

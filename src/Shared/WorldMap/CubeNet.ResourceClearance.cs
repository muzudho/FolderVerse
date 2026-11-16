namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal (Vector3 Position,float Radius)[] ResourceClearance(WorldSetup world,int cell)
    {
        if(!ShowRoutes || DirectoryOnly || world.TutorialStep!=0 || world.EquipmentMode!=RobotEquipmentMode.Materials)
            return Array.Empty<(Vector3,float)>();
        var obstacles=new List<(Vector3,float)>();
        foreach(var site in DisplayedResourceSites(world,cell))
            obstacles.Add((site.Position,site.Size*1.15f));
        foreach(var node in world.Nodes.InCell(cell)){
            if(!NodeVisible(world,node))continue;
            int loads=node.Functional && node.Purpose!=NodePurpose.Relay?world.Robots.MapleDeposit(node.Id):0;
            bool planted=world.Robots.MaplePlantingCount(node.Id)>0;
            if(loads<=0 && !planted)continue;
            if(!_grovePoints.TryGetValue(node,out var point)){
                point=MapleGrovePoint(world,node);
                _grovePoints.Add(node,point);
            }
            obstacles.Add((world.Routes.Position(cell,point),loads>=4 || planted?.15f:loads>1?.12f:.07f));
        }
        return obstacles.ToArray();
    }

    internal Vector3 SiteDisplayPosition(WorldSetup world,Node node,(Vector3 Position,float Radius)[] obstacles)
    {
        var center=world.Routes.Position(node.Cell,node.Center);
        if(node.IsHarbor || obstacles.Length==0)return center;
        float radius=HasFacilityPlatform(node)?.25f:.14f;
        float Clearance(Vector3 point)
        {
            float clearance=float.PositiveInfinity;
            foreach(var obstacle in obstacles)
                clearance=Math.Min(clearance,Vector3.Distance(point,obstacle.Position)-obstacle.Radius-radius);
            return clearance;
        }
        float bestClearance=Clearance(center);
        if(bestClearance>=0)return center;
        var best=center;
        float bestDistance=float.PositiveInfinity;
        foreach(var tile in node.Points){
            var candidate=world.Routes.Position(node.Cell,tile);
            float distance=Vector3.DistanceSquared(candidate,center);
            if(distance>.16f)continue;
            float clearance=Clearance(candidate);
            if(clearance>=0){
                if(bestClearance>=0 && distance>=bestDistance)continue;
            }else if(clearance<=bestClearance)continue;
            best=candidate;
            bestClearance=clearance;
            bestDistance=distance;
        }
        return best;
    }

    internal static Vector2 ResourceSafeRobotSlot(SurfaceCell cell,Vector3 center,float spacing,float clearance,
        (Vector3 Position,float Radius)[] obstacles,ref int nextSlot)
    {
        var offset=RobotSpiralSlot(nextSlot++,spacing);
        if(obstacles.Length==0)return offset;
        for(int attempt=0;attempt<96;attempt++){
            var point=center+cell.U*offset.X+cell.V*offset.Y;
            bool free=true;
            foreach(var obstacle in obstacles){
                var delta=point-obstacle.Position;
                float radius=obstacle.Radius+clearance;
                if(Vector2.DistanceSquared(Vector2.Zero,new(Vector3.Dot(delta,cell.U),Vector3.Dot(delta,cell.V)))>=radius*radius)continue;
                free=false;
                break;
            }
            if(free)return offset;
            offset=RobotSpiralSlot(nextSlot++,spacing);
        }
        return offset;
    }
}

namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static bool TryHarborFrame(WorldSetup world,Node node,out Vector3 shore,out Vector3 seaward)
    {
        var cell=world.Cells[node.Cell];
        var land=world.Routes.Position(node.Cell,node.Center);
        foreach(var link in world.Routes.HarborConnections(node.Cell,node.Center)){
            bool fromLand=link.Cell==node.Cell && link.Point==node.Center;
            int seaCell=fromLand?link.Target:link.Cell;
            var seaPoint=fromLand?link.Other:link.Point;
            if(world.Routes.Terrain(seaCell,seaPoint)!=TravelTerrain.Sea)continue;
            // At a cube edge the sea point can lie on the next face. Use the
            // shared edge to orient the pier on the land face.
            var direction=(world.Cells[seaCell].Normal==cell.Normal?world.Routes.Position(seaCell,seaPoint):link.Position)-land;
            direction-=Vector3.Dot(direction,cell.Normal)*cell.Normal;
            if(direction.LengthSquared()<.000001f)continue;
            shore=link.Position;
            seaward=Vector3.Normalize(direction);
            return true;
        }
        shore=land;seaward=Vector3.Zero;
        return false;
    }

    private static void DrawHarborPier(UiPainter ui,Func<float,float,float,Vector2> point,Func<Color,Color> tint)
    {
        var orange=new Color(235,112,42);
        var white=new Color(254,242,222);
        var navy=new Color(42,65,94);
        Box(-.43f,-.68f,.43f,.7f,0,.24f,white);
        Box(-.43f,.7f,1.00f,1.35f,0,.24f,white);
        Box(-.47f,-.68f,.47f,.71f,.24f,.37f,orange);
        Box(-.47f,.71f,1.04f,1.39f,.24f,.37f,orange);
        Box(-.34f,-.68f,.34f,.82f,.37f,.40f,white);
        Box(-.34f,.82f,.91f,1.25f,.37f,.40f,white);
        Box(-.28f,1.05f,-.05f,1.27f,.40f,.63f,navy);
        Box(.62f,1.05f,.85f,1.27f,.40f,.63f,navy);

        void Box(float x,float y,float right,float front,float baseZ,float topZ,Color color)
        {
            var a=point(x,y,topZ);var b=point(right,y,topZ);
            var c=point(right,front,topZ);var d=point(x,front,topZ);
            var center=(a+b+c+d)/4;
            Side(a,b,point(x,y,baseZ),point(right,y,baseZ),.82f);
            Side(b,c,point(right,y,baseZ),point(right,front,baseZ),.68f);
            Side(c,d,point(right,front,baseZ),point(x,front,baseZ),.82f);
            Side(d,a,point(x,front,baseZ),point(x,y,baseZ),.68f);
            FillProjectedFace(ui,new[]{a,b,c,d},tint(color));
            void Side(Vector2 p,Vector2 q,Vector2 r,Vector2 s,float shade)
            {
                if(Cross(q-p,center-p)*Cross(q-p,r-p)<0)
                    FillProjectedFace(ui,new[]{p,q,s,r},tint((color*shade) with {A=255}));
            }
        }
    }
}

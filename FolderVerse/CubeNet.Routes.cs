namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public bool ShowFlags {get;set;}=true;
    public bool ShowBatteries {get;set;}=true;
    public bool ShowRoutes {get;set;}
    private void DrawCellRoutes(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2> project)
    {
        if(!ShowRoutes)return;
        var starts=world.ConquerorLocations.Select((c,r)=>(Cell:c,Ruler:r)).Where(p=>p.Cell==cell).Select(p=>world.ConquerorPoints[p.Ruler]).Append(world.Routes.Start(cell)).Distinct();
        var drawn=new System.Collections.Generic.HashSet<(Point,Point)>();
        foreach(var start in starts)for(int d=0;d<4;d++)
        {
            var route=world.Routes.Find(cell,start,d);if(route==null)continue;
            for(int i=1;i<route.Path.Length;i++)
            {
                var a=route.Path[i-1];var b=route.Path[i];if(drawn.Contains((a,b)) || drawn.Contains((b,a)))continue;drawn.Add((a,b));
                var pa=project(world.Routes.Position(cell,a));var pb=project(world.Routes.Position(cell,b));var delta=pb-pa;
                var color=world.Routes.Land(cell,a)==world.Routes.Land(cell,b)?world.Routes.Land(cell,a)?new Color(255,231,150):new Color(130,239,255):new Color(255,150,60);
                ui.Tile((pa+pb)/2,new Vector2(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),color);
            }
            var exit=project(world.Routes.Position(cell,route.Exit));
            var incoming=world.Routes.Position(route.Target,route.Entry);var outgoing=world.Routes.Position(cell,route.Exit);
            var edge=project((incoming+outgoing)/2);var line=edge-exit;
            bool landing=world.Routes.Land(cell,route.Exit)!=world.Routes.Land(route.Target,route.Entry);
            ui.Tile((exit+edge)/2,new Vector2(line.Length()+1,2),MathF.Atan2(line.Y,line.X),landing?new Color(255,150,60):new Color(170,240,255));
            ui.Tile(exit,new Vector2(landing?5:3),0,landing?new Color(255,150,60):Color.White);
        }
    }
}

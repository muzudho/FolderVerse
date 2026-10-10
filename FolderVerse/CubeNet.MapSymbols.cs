namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public static readonly Color EquatorColor=new(240,102,112);
    // Use coordinate adjacency rather than Y=0: odd-height worlds offset the latitude origin.
    public static IEnumerable<(SurfaceCell Cell,Vector3 A,Vector3 B)> EquatorEdges(WorldSetup world)
    {
        var latitude=world.Cells.ToDictionary(c=>c.Id,c=>WorldCoordinates.At(world,c).Y);
        foreach(var cell in world.Cells)
        {
            if(latitude[cell.Id] is not (-1 or 0))continue;
            foreach(int id in cell.Neighbors)
            {
                if(latitude[id]!=(latitude[cell.Id]==0?-1:0))continue;
                var shared=cell.Corners.Where(p=>Array.Exists(world.Cells[id].Corners,q=>q==p)).ToArray();
                if(shared.Length==2)yield return (cell,shared[0],shared[1]);
            }
        }
    }
    private void DrawMapSymbols(UiPainter ui,WorldSetup world,
        Func<(SurfaceCell Cell,Vector3 A,Vector3 B),(Vector2 A,Vector2 B)?> project)
    {
        if(!ShowMapSymbols)return;
        foreach(var edge in EquatorEdges(world))
        {
            var projected=project(edge);if(projected is not { } line)continue;
            var delta=line.B-line.A;if(delta.LengthSquared()<.01f)continue;
            ui.Tile((line.A+line.B)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),EquatorColor);
        }
    }
}

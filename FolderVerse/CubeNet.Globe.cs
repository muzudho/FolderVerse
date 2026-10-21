namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public void DrawGlobeLayers(UiPainter ui,WorldSetup setup,WorldPreview globe,Rectangle panel,float yaw,float pitch,float pulse)
    {
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result);return result;}
            DrawCellRoutes(ui,setup,cell.Id,Project,pulse);
        }
        DrawMapSymbols(ui,setup,p=>
        {
            if(!globe.ProjectVisible(p.Cell.Center,p.Cell.Normal,yaw,pitch,panel,out _))return null;
            globe.ProjectVisible(p.A,p.Cell.Normal,yaw,pitch,panel,out var a);
            globe.ProjectVisible(p.B,p.Cell.Normal,yaw,pitch,panel,out var b);
            return (a,b);
        });
    }
}

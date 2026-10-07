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
        if(!ShowBatteries)return;
        for(int ruler=0;ruler<setup.ActiveCount;ruler++)
        {
            if(!setup.Relations.Powered[ruler])continue;
            var cell=setup.Cells[setup.ConquerorLocations[ruler]];
            if(!globe.ProjectVisible(setup.Routes.Position(cell.Id,setup.ConquerorPoints[ruler]),cell.Normal,yaw,pitch,panel,out var at))continue;
            bool own=ruler==setup.PlayerSlot;int width=own?25:12,height=own?16:8;
            int x=(int)at.X-width/2,y=(int)at.Y-height/2;
            var color=own?Color.Lerp(new Color(255,240,119),Color.White,(MathF.Sin(pulse*4)+1)/2):setup.OwnerColor(ruler);
            ui.Box(new(x-2,y-2,width+6,height+4),new Color(12,27,36));
            ui.Box(new(x,y,width,height),color);ui.Box(new(x+width,y+height/4,3,height/2),color);
            if(own)for(int i=0;i<3;i++)ui.Box(new(x+3+i*7,y+3,5,10),new Color(35,112,70));
        }
    }
}

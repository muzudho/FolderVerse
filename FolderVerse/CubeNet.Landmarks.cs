namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public void DrawLandmarks(UiPainter ui,WorldSetup world,int cell,Func<Vector3,Vector2?> project,float time,bool respectVisibility=true)
    {
        foreach(var node in world.Nodes.InCell(cell)){
            if(respectVisibility && !NodeVisible(world,node))continue;
            var mark=NodeLandmarks.At(world,node.Id);var center=project(mark.Position);var origin=project(world.Routes.Position(cell,node.Center));
            var unit=project(mark.Position+world.Cells[cell].U*.1f);
            if(!center.HasValue || !origin.HasValue || !unit.HasValue)continue;
            float size=MathHelper.Clamp(Vector2.Distance(center.Value,unit.Value)*.55f,3.5f,13);
            if(EmphasizedNode(node.Id))size=Math.Max(size,13);
            var delta=center.Value-origin.Value;
            ui.Tile((center.Value+origin.Value)/2,new(delta.Length(),1),MathF.Atan2(delta.Y,delta.X),NodeTint(node.Id,new Color(221,219,182,120)));
            for(int i=0;i<mark.Objects.Length;i++)LandmarkArt.Draw(ui,mark.Objects[i],center.Value+new Vector2((i-(mark.Objects.Length-1)/2f)*size*1.9f,0),size,time,DimNode(node.Id)?FocusBrightness:1);
        }
    }
}

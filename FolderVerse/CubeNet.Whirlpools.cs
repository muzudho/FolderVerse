namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private readonly List<(Vector2 Center,float Radius)> _whirlpoolScreens=new();
    private void DrawWhirlpools(UiPainter ui,WorldSetup world,Rectangle panel)
    {
        _whirlpoolScreens.Clear();if(world.Owners.Length!=world.Cells.Length)return;DrawPackIce(ui,world,panel);
        foreach(var whirl in world.Routes.Whirlpools){
            var cell=world.Cells[whirl.Cell];
            SeaWhirlpoolArt.Draw(ui,p=>AnimatedSurfacePosition(cell.Id,cell.Origin+cell.U*((whirl.Center.X+.5f+p.X*whirl.Radius)/10)+cell.V*((whirl.Center.Y+.5f+p.Y*whirl.Radius)/10),world,panel));
            var center=AnimatedSurfacePosition(cell.Id,world.Routes.WhirlpoolCenter(whirl),world,panel);
            float radius=Vector2.Distance(center,AnimatedSurfacePosition(cell.Id,world.Routes.WhirlpoolCenter(whirl)+cell.U*whirl.Radius/10,world,panel));
            _whirlpoolScreens.Add((center,radius));
        }
    }
    private Rectangle AvoidWhirlpool(Rectangle card)
    {
        foreach(var whirl in _whirlpoolScreens){
            var delta=card.Center.ToVector2()-whirl.Center;float minimum=whirl.Radius+MathF.Sqrt(card.Width*card.Width+card.Height*card.Height)/2+3;
            if(delta.LengthSquared()>=minimum*minimum)continue;
            var direction=delta.LengthSquared()<.01f?Vector2.UnitY:Vector2.Normalize(delta);
            var target=whirl.Center+direction*minimum;card.Offset((int)(target.X-card.Center.X),(int)(target.Y-card.Center.Y));
        }
        return card;
    }
}

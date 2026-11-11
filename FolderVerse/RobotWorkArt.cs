namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class RobotWorkArt
{
    public static void DrawWindupKey(UiPainter ui, Rectangle robot, float opacity = 1, float progress = 1)
    {
        if (progress <= 0 || opacity <= 0)
            return;

        int scale = Math.Max(1, robot.Width / 32);
        int x = robot.Right - 10 * scale;
        int y = robot.Y + robot.Height / 11 -
            (int)((1 - MathHelper.SmoothStep(0, 1, progress)) * 4 * scale);
        var shadow = new Color(47, 54, 64) * opacity;
        var metal = new Color(169, 178, 186) * opacity;
        var shine = new Color(231, 235, 237) * opacity;
        ui.Box(new(x - 2 * scale, y + 2 * scale, 10 * scale, 3 * scale), shadow);
        ui.Box(new(x - 2 * scale, y + 2 * scale, 10 * scale, scale), metal);
        for (int loop = 0; loop < 2; loop++)
        {
            int top = y + loop * 4 * scale;
            ui.Box(new(x + scale, top, 5 * scale, scale), shine);
            ui.Box(new(x, top + scale, scale, 2 * scale), metal);
            ui.Box(new(x + 6 * scale, top + scale, scale, 2 * scale), metal);
            ui.Box(new(x + scale, top + 3 * scale, 5 * scale, scale), shadow);
        }
    }

    public static void DrawFlag(UiPainter ui,Rectangle bounds,Color color,float wave=0,float opacity=1)
    {
        float scale=bounds.Height/40f;var pole=new Vector2(bounds.X+5*scale,bounds.Bottom-2*scale);
        float angle=wave*.32f;var top=pole+Vector2.Transform(new Vector2(0,-35*scale),Matrix.CreateRotationZ(angle));
        ui.Tile((pole+top)/2,new Vector2(3*scale,35*scale),angle,new Color(224,226,211)*opacity);
        for(int layer=0;layer<2;layer++)for(int strip=0;strip<16;strip++){
            float bend=MathF.Sin(strip*.35f+wave*2)*Math.Abs(wave)*2*scale;
            var at=top+Vector2.Transform(new Vector2((4+strip)*scale,(8+bend/scale)*scale),Matrix.CreateRotationZ(angle));
            ui.Tile(at,new Vector2(1.5f*scale+(layer==0?2:0),13*scale+(layer==0?2:0)),angle,(layer==0?new Color(15,25,33):color)*opacity);
        }
    }
}

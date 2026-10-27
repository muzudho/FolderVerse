namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class RobotWorkArt
{
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
    public static void DrawShoes(UiPainter ui,Rectangle robot,Color color,float opacity=1,float progress=1)
    {
        if(progress<=0)return;
        int height=Math.Max(3,(int)(robot.Height*.13f)),width=Math.Max(4,(int)(robot.Width*.31f));
        int rise=(int)((1-MathHelper.SmoothStep(0,1,Math.Clamp(progress,0,1)))*robot.Height*.20f);
        foreach(float side in new[]{1/6f,7/12f}){
            var shoe=new Rectangle(robot.X+(int)(robot.Width*side)-1,robot.Bottom-height+rise,width,height);
            var edge=shoe;edge.Inflate(1,1);ui.Box(edge,new Color(15,25,33)*opacity);ui.Box(shoe,color*opacity);
            ui.Box(new(shoe.X,shoe.Bottom-2,shoe.Width,2),Color.Lerp(color,Color.White,.5f)*opacity);
        }
    }
}

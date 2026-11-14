namespace FolderVerseMuseum;
using System;
using Microsoft.Xna.Framework;

internal static class RobotPartSilhouette
{
    public static Rectangle Row(Rectangle shape,int y,bool cheap,bool premium=false)
    {
        if(!cheap && !premium)return new(shape.X,y,shape.Width,1);
        float t=(y-shape.Y+.5f)/Math.Max(1,shape.Height);
        int neck=premium?(int)(shape.Width*.22f*MathF.Pow(MathF.Sin(MathF.PI*t),2)):(int)(shape.Width*.12f);
        float radius=Math.Min(shape.Width,shape.Height)*.24f;
        float edge=Math.Max(0,radius-Math.Min(y-shape.Top+.5f,shape.Bottom-y-.5f));
        int corner=(int)MathF.Ceiling(radius-MathF.Sqrt(Math.Max(0,radius*radius-edge*edge)));
        int inset=Math.Min((shape.Width-1)/2,neck+corner);
        return new(shape.X+inset,y,Math.Max(1,shape.Width-inset*2),1);
    }
}

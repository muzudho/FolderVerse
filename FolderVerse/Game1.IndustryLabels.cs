namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private (string Text,Rectangle Bounds,float Scale,bool Backplate)[] IndustryLabelLayout(Vector2 center,float radius,float angle)
    {
        var wheel=_setup.Industry.Wheel;
        var result=new (string Text,Rectangle Bounds,float Scale,bool Backplate)[wheel.Count];
        var middles=new float[wheel.Count];float percent=0;
        for(int i=0;i<wheel.Count;i++){middles[i]=angle+(percent+wheel[i].Percent*.5f)/100*MathHelper.TwoPi;percent+=wheel[i].Percent;}
        var placed=new System.Collections.Generic.List<Rectangle>();
        // Reserve the large sectors first, then stagger the tiny sectors along
        // their own radial direction. Compute hidden labels too, to avoid jumps.
        foreach(int i in Enumerable.Range(0,wheel.Count).OrderByDescending(i=>wheel[i].Percent)){
            string text=IndustryTime.Format(wheel[i].Turns).Replace("+"," ");
            bool tiny=wheel[i].Percent<=2;
            var direction=new Vector2(MathF.Cos(middles[i]),MathF.Sin(middles[i]));
            Rectangle best=default;float bestScale=.65f,bestScore=float.MaxValue;
            foreach(float scale in new[]{.65f,.58f,.50f,.44f}){
                var size=IndustryDurationSize(wheel[i].Turns,scale);
                int width=(int)MathF.Ceiling(size.X)+8,height=(int)MathF.Ceiling(size.Y)+4;
                foreach(float distance in Enumerable.Range(0,21).Select(n=>.16f+n*.035f).OrderBy(d=>MathF.Abs(d-(tiny?.84f:.70f)))){
                    var at=center+direction*(radius*distance);
                    var bounds=new Rectangle((int)MathF.Round(at.X-width/2f),(int)MathF.Round(at.Y-height/2f),width,height);
                    bool inside=new[]{new Vector2(bounds.Left,bounds.Top),new Vector2(bounds.Right,bounds.Top),new Vector2(bounds.Left,bounds.Bottom),new Vector2(bounds.Right,bounds.Bottom)}.All(p=>Vector2.DistanceSquared(p,center)<(radius-6)*(radius-6));
                    if(!inside)continue;
                    var padded=bounds;padded.Inflate(4,4);
                    int overlaps=placed.Sum(p=>{var intersection=Rectangle.Intersect(padded,p);return intersection.Width*intersection.Height;});
                    float score=overlaps*1000+( .65f-scale)*100+MathF.Abs(distance-(tiny?.84f:.70f));
                    if(score<bestScore){bestScore=score;best=bounds;bestScale=scale;}
                }
                if(bestScore<1)break;
            }
            result[i]=(text,best,bestScale,tiny && wheel[i].Turns>0);placed.Add(best);
        }
        return result;
    }
}

namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private (string Text,Rectangle Bounds,float Scale,bool Backplate)[] IndustryLabelLayout(Vector2 center,float radius,float angle)
    {
        var wheel=_setup.Industry.Wheel;
        var middles=new float[wheel.Count];float percent=0;
        for(int i=0;i<wheel.Count;i++){middles[i]=(percent+wheel[i].Percent*.5f)/100*MathHelper.TwoPi;percent+=wheel[i].Percent;}
        // Reserve rotation-invariant clearance around every upright label. Retry the
        // entire layout at a smaller size instead of squeezing only the last label.
        for(float maximum=.65f;maximum>.015f;maximum*=.9f){
            var result=new (string Text,Rectangle Bounds,float Scale,bool Backplate)[wheel.Count];
            var placed=new List<(Vector2 Center,float Radius)>();bool complete=true;
            foreach(int i in Enumerable.Range(0,wheel.Count).OrderByDescending(i=>IndustryDurationSize(wheel[i].Turns,maximum).LengthSquared())){
                bool tiny=wheel[i].Percent<=2,found=false;
                var direction=new Vector2(MathF.Cos(middles[i]),MathF.Sin(middles[i]));
                foreach(float scale in new[]{maximum,maximum*.9f,maximum*.8f}){
                    var size=IndustryDurationSize(wheel[i].Turns,scale);
                    int width=(int)MathF.Ceiling(size.X)+8,height=(int)MathF.Ceiling(size.Y)+4;
                    float disc=MathF.Sqrt(width*width+height*height)/2+5;
                    foreach(float distance in Enumerable.Range(0,37).Select(n=>n*.025f).OrderBy(d=>MathF.Abs(d-(tiny?.84f:.70f)))){
                        var at=center+direction*(radius*distance);
                        var fixedCenter=new Vector2(MathF.Round(at.X),MathF.Round(at.Y));
                        if(Vector2.Distance(fixedCenter,center)+disc>=radius-6 || placed.Any(p=>Vector2.Distance(fixedCenter,p.Center)<p.Radius+disc))continue;
                        placed.Add((fixedCenter,disc));var offset=fixedCenter-center;
                        var rotated=center+new Vector2(offset.X*MathF.Cos(angle)-offset.Y*MathF.Sin(angle),offset.X*MathF.Sin(angle)+offset.Y*MathF.Cos(angle));
                        var bounds=new Rectangle((int)MathF.Round(rotated.X)-width/2,(int)MathF.Round(rotated.Y)-height/2,width,height);
                        result[i]=(IndustryTime.Format(wheel[i].Turns).Replace("+"," "),bounds,scale,tiny && wheel[i].Turns>0);
                        found=true;break;
                    }
                    if(found)break;
                }
                if(!found){complete=false;break;}
            }
            if(complete)return result;
        }
        throw new InvalidOperationException("Industry wheel labels cannot fit the available radius.");
    }
}

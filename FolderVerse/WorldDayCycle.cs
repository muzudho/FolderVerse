namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// A scenic clock, not a simulation of sunlight or character ageing.
public static class WorldDayCycle
{
    public static int RotationDirection(WorldSetup w)=>(w.WorldSeed&1)==0?1:-1;
    public static int Perimeter(WorldSetup w){var s=w.Geographic(new Vector3(w.Width,w.Height,w.Depth));return 2*(int)(Math.Abs(s.X)+Math.Abs(s.Z));}
    public static float DawnPhase(WorldSetup w,int ruler){
        int cell=ruler>=0&&ruler<w.ConquerorLocations.Length?w.ConquerorLocations[ruler]:-1;
        return cell>=0&&cell<w.Cells.Length?Phase(w,w.Cells[cell]):0;
    }
    public static float Phase(WorldSetup w,SurfaceCell cell)=>Wrap(RotationDirection(w)*(WorldCoordinates.At(w,cell).X/(float)Perimeter(w)+.5f));
    public static float Wrap(float value)=>value-MathF.Floor(value);
    public static float PhaseAt(WorldSetup w,Vector3 point){
        var p=w.Geographic(point);var size=w.Geographic(new Vector3(w.Width,w.Height,w.Depth));
        float width=Math.Abs(size.X),depth=Math.Abs(size.Z),x=p.X,z=p.Z;
        float reach=Math.Max(Math.Abs(x)/(width*.5f),Math.Abs(z)/(depth*.5f));
        if(reach<.0001f)return 0;
        x/=reach;z/=reach;
        float longitude=Math.Abs(x)/(width*.5f)>=Math.Abs(z)/(depth*.5f)-.0001f
            ?x>0?depth-(z+depth/2)-.5f:depth+width+(z+depth/2)-.5f
            :z<0?depth+width-(x+width/2)-.5f:2*depth+width+(x+width/2)-.5f;
        longitude=Wrap((longitude+width)/(2*(width+depth))+.5f);
        return Wrap(RotationDirection(w)*longitude);
    }
    public static int[] Order(WorldSetup w,IEnumerable<int> rulers)=>rulers.OrderBy(r=>DawnPhase(w,r))
        .ThenBy(r=>(r-(w.Population.Turn%Math.Max(1,w.ActiveCount))+w.ActiveCount)%Math.Max(1,w.ActiveCount)).ToArray();
    public static float Hour(WorldSetup w,SurfaceCell cell,float dawn)=>Wrap(dawn-Phase(w,cell)+8/24f)*24;
    public static float Daylight(float hour){
        if(hour>=10&&hour<=18)return 1;
        if(hour>6&&hour<10)return MathHelper.SmoothStep(0,1,(hour-6)/4);
        if(hour>18&&hour<22)return MathHelper.SmoothStep(1,0,(hour-18)/4);
        return 0;
    }
    public static Color Tint(Color original,float hour){
        float day=Daylight(hour);
        var night=original.ToVector3()*new Vector3(.20f,.25f,.40f);
        var color=Vector3.Lerp(night,original.ToVector3(),day);
        float twilight=4*day*(1-day)*.26f;
        color=Vector3.Lerp(color,hour<12?new Vector3(.40f,.24f,.55f):new Vector3(.75f,.28f,.18f),twilight);
        return new Color(color);
    }
}

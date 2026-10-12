namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public static class Arcball
{
    public static Vector3 Project(Point point,Rectangle area)
    {
        float radius=Math.Max(1,Math.Min(area.Width,area.Height)*.5f);
        float x=(point.X-area.Center.X)/radius,y=(area.Center.Y-point.Y)/radius;
        float length=x*x+y*y;
        return length<=1?new Vector3(x,y,MathF.Sqrt(1-length)):Vector3.Normalize(new Vector3(x,y,0));
    }
    public static Quaternion Drag(Quaternion orientation,Point from,Point to,Rectangle area)
    {
        var a=Project(from,area);var b=Project(to,area);
        float dot=Math.Clamp(Vector3.Dot(a,b),-1,1);
        var axis=Vector3.Cross(a,b);
        if(axis.LengthSquared()<1e-10f){
            if(dot>0)return orientation;
            axis=Vector3.Cross(a,MathF.Abs(a.X)<.9f?Vector3.Right:Vector3.Up);
        }
        var delta=Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis),MathF.Acos(dot));
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(orientation)*Matrix.CreateFromQuaternion(delta)));
    }
}

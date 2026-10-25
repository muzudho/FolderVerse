namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class ToyOrientationRenderer
{
    public static Color AxisColor(int axis)=>axis switch{0=>new(244,91,91),1=>new(89,216,126),2=>new(95,156,255),_=>throw new ArgumentOutOfRangeException(nameof(axis))};
    public static Vector3 AxisDirection(int axis)=>axis switch{0=>Vector3.UnitX,1=>Vector3.UnitY,2=>Vector3.UnitZ,_=>throw new ArgumentOutOfRangeException(nameof(axis))};
    public static Vector3 ScrewCenter(int axis)=>-AxisDirection(axis)*2.3f;
    public Vector2 ProjectScrew(int axis,Rectangle bounds)
    {
        var p=new Viewport(bounds).Project(ScrewCenter(axis),_effect.Projection,_effect.View,Matrix.Identity);
        return new(p.X,p.Y);
    }
    private static VertexPositionColorNormal[] BuildAxisScrews()
    {
        var mesh=new List<VertexPositionColorNormal>();
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color tint){mesh.Add(new(a,tint,normal));mesh.Add(new(b,tint,normal));mesh.Add(new(c,tint,normal));mesh.Add(new(a,tint,normal));mesh.Add(new(c,tint,normal));mesh.Add(new(d,tint,normal));}
        for(int axis=0;axis<3;axis++){
            var outward=-AxisDirection(axis);var u=axis==1?Vector3.UnitX:Vector3.UnitY;var v=Vector3.Cross(outward,u);
            var center=ScrewCenter(axis);var color=AxisColor(axis);var dark=new Color(24,36,44);
            Vector3 P(float x,float y,float z)=>center+u*x+v*y+outward*z;
            Vector3 Ring(float angle,float radius,float z)=>P(MathF.Cos(angle)*radius,MathF.Sin(angle)*radius,z);
            void Cylinder(float radius,float from,float to,Color tint){
                for(int i=0;i<24;i++){float a=i*MathHelper.TwoPi/24,b=(i+1)*MathHelper.TwoPi/24;var normal=u*MathF.Cos((a+b)/2)+v*MathF.Sin((a+b)/2);
                    Quad(Ring(a,radius,from),Ring(b,radius,from),Ring(b,radius,to),Ring(a,radius,to),normal,tint);
                    Quad(P(0,0,to),Ring(a,radius,to),Ring(b,radius,to),P(0,0,to),outward,tint);
                    Quad(P(0,0,from),Ring(b,radius,from),Ring(a,radius,from),P(0,0,from),-outward,tint);
                }
            }
            // Short threaded stubs stop well outside the robot; no axis crosses the body.
            Cylinder(.085f,-.42f,-.08f,Color.Lerp(color,Color.White,.25f));
            for(int i=0;i<90;i++){float a=i*MathHelper.TwoPi/30,b=(i+1)*MathHelper.TwoPi/30;float z=-.42f+i*.34f/90;
                var p=Ring(a,.091f,z);var q=Ring(b,.091f,z+.34f/90);var normal=u*MathF.Cos(a)+v*MathF.Sin(a);
                Quad(p,q,q+outward*.015f,p+outward*.015f,normal,dark);
            }
            Cylinder(.225f,-.08f,.04f,color);
            // A cross recess on both faces stays legible from either side of the axis.
            foreach(float z in new[]{-.083f,.043f}){
                var normal=z>0?outward:-outward;
                Quad(P(-.15f,-.025f,z),P(.15f,-.025f,z),P(.15f,.025f,z),P(-.15f,.025f,z),normal,dark);
                Quad(P(-.025f,-.15f,z),P(.025f,-.15f,z),P(.025f,.15f,z),P(-.025f,.15f,z),normal,dark);
            }
        }
        return mesh.ToArray();
    }
}

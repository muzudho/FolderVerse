namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static readonly Vector2[] YachtHull =
        { new(-.105f,-.046f),new(.038f,-.046f),new(.115f,0),new(.038f,.046f),new(-.105f,.046f) };

    private static void DrawScenicVehicle(UiPainter ui, SurfaceCell cell, ScenicRoutes.Vehicle vehicle,
        Func<Vector3,Vector2> project, bool globe, Func<Color,Color> tint)
    {
        var forward = vehicle.Forward;
        var across = Vector3.Cross(cell.Normal,forward);
        var anchor = project(vehicle.At);
        var heading = project(vehicle.At+forward*.1f)-anchor;
        if (heading.LengthSquared() < .01f) return;
        var flatForward = Vector2.Normalize(heading);
        var flatAcross = new Vector2(-flatForward.Y,flatForward.X);
        float flatScale = MathHelper.Clamp(heading.Length()*10,65,115);
        Vector2 Project(Vector2 point, float height) => globe
            ? project(vehicle.At+forward*point.X+across*point.Y+cell.Normal*height)
            : anchor+(flatForward*point.X+flatAcross*point.Y)*flatScale;

        void Face(Color color, Vector2 a, Vector2 b, Vector2 c, float height)
        {
            Span<Vector2> points = stackalloc Vector2[3] { Project(a,height), Project(b,height), Project(c,height) };
            FillProjectedFace(ui,points,tint(color));
        }
        void Solid(ReadOnlySpan<Vector2> shape, float bottom, float top, Color color, Color side)
        {
            Span<Vector2> upper = stackalloc Vector2[shape.Length];
            Span<Vector2> lower = stackalloc Vector2[shape.Length];
            for (int i=0;i<shape.Length;i++)
            {
                upper[i]=Project(shape[i],top);
                lower[i]=Project(shape[i],bottom);
            }
            if (globe)
            {
                float winding = Cross(upper[1]-upper[0],upper[2]-upper[1]);
                Span<Vector2> wall = stackalloc Vector2[4];
                for (int i=0;i<shape.Length;i++)
                {
                    int next=(i+1)%shape.Length;
                    if (Cross(upper[next]-upper[i],lower[i]-upper[i])*winding>=0) continue;
                    wall[0]=upper[i]; wall[1]=upper[next]; wall[2]=lower[next]; wall[3]=lower[i];
                    FillProjectedFace(ui,wall,tint(i%2==0 ? side : Color.Lerp(side,color,.25f)));
                }
            }
            FillProjectedFace(ui,upper,tint(color));
        }
        void Box(float rear,float front,float left,float right,float bottom,float top,Color color,Color side)
        {
            Span<Vector2> shape=stackalloc Vector2[4] { new(rear,left),new(front,left),new(front,right),new(rear,right) };
            Solid(shape,bottom,top,color,side);
        }

        if (vehicle.Sea)
        {
            Solid(YachtHull,.008f,.041f,new(190,77,56),new(94,43,45));
            Box(-.086f,.044f,-.03f,.03f,.041f,.047f,new(239,190,124),new(190,120,67));
            if (globe)
            {
                Span<Vector2> sail=stackalloc Vector2[3]
                {
                    Project(new(.038f,0),.21f), Project(new(.038f,0),.054f), Project(new(-.09f,0),.054f)
                };
                FillProjectedFace(ui,sail,tint(new Color(255,249,218)));
                Span<Vector2> jib=stackalloc Vector2[3]
                {
                    Project(new(.042f,0),.16f), Project(new(.052f,0),.054f), Project(new(.104f,0),.054f)
                };
                FillProjectedFace(ui,jib,tint(new Color(179,218,222)));
            }
            else
            {
                Face(new(255,249,218),new(.059f,-.007f),new(-.088f,-.033f),new(-.051f,.021f),0);
                Face(new(179,218,222),new(.085f,.008f),new(-.052f,.038f),new(-.037f,.005f),0);
            }
        }
        else
        {
            var rubber = new Color(37,43,45);
            Box(-.102f,-.057f,-.072f,.072f,.008f,.049f,rubber,rubber);
            Box(.055f,.1f,-.072f,.072f,.008f,.049f,rubber,rubber);
            Box(-.116f,.117f,-.052f,.052f,.031f,.076f,new(181,133,63),new(111,75,40));
            Box(-.066f,.044f,-.041f,.041f,.076f,.117f,new(222,182,99),new(71,112,118));
            Box(.044f,.063f,-.036f,.036f,.075f,.088f,new(114,169,174),new(61,98,108));
            Box(.098f,.116f,-.043f,.043f,.067f,.078f,new(252,232,164),new(143,121,69));
        }
    }
}

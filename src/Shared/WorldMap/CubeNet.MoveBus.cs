namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal static void DrawMoveBusModel(UiPainter ui,Func<float,float,float,Vector2> project,Color livery)
    {
        var rubber=new Color(37,37,41);
        var teal=Color.Lerp(new Color(42,124,116),livery,.30f);
        var ivory=new Color(250,230,194);
        var amber=new Color(236,156,42);
        var glass=new Color(57,78,87);
        float visibleSide=Visible(project(-1,-.5f,0),project(1,-.5f,0),project(1,-.5f,1))?-1:1;

        foreach(float axle in new[]{-.65f,.65f})
        {
            Wheel(axle,-visibleSide*.59f);
            Wheel(axle,visibleSide*.59f);
        }
        Box(-1.12f,1.12f,.51f,.18f,.39f,amber);
        Box(-1.04f,1.04f,.55f,.31f,.72f,teal);
        Box(-.99f,1f,.54f,.72f,1.26f,ivory);
        Box(-.87f,.87f,.48f,1.26f,1.34f,ivory);
        for(int i=0;i<3;i++)
        {
            float x=-.88f+i*.61f;
            float y=visibleSide*.546f;
            Face(glass,project(x,y,.82f),project(x+.48f,y,.82f),project(x+.48f,y,1.17f),project(x,y,1.17f));
        }
        Face(glass,project(1.006f,-.43f,.81f),project(1.006f,.43f,.81f),project(1.006f,.43f,1.17f),project(1.006f,-.43f,1.17f));
        Face(glass,project(-.996f,.43f,.81f),project(-.996f,-.43f,.81f),project(-.996f,-.43f,1.17f),project(-.996f,.43f,1.17f));
        foreach(float y in new[]{-.37f,.37f})
        {
            Face(ivory,project(1.046f,y-.09f,.44f),project(1.046f,y+.09f,.44f),project(1.046f,y+.09f,.59f),project(1.046f,y-.09f,.59f));
            Face(new Color(216,82,49),project(-1.046f,y+.07f,.44f),project(-1.046f,y-.07f,.44f),project(-1.046f,y-.07f,.57f),project(-1.046f,y+.07f,.57f));
        }

        void Wheel(float x,float y)
        {
            Span<Vector2> tire=stackalloc Vector2[12];
            Span<Vector2> hub=stackalloc Vector2[12];
            for(int i=0;i<tire.Length;i++)
            {
                float angle=MathHelper.TwoPi*i/tire.Length;
                float dx=MathF.Cos(angle),dz=MathF.Sin(angle);
                tire[i]=project(x+.25f*dx,y,.25f+.25f*dz);
                hub[i]=project(x+.13f*dx,y,.25f+.13f*dz);
            }
            FillProjectedFace(ui,tire,rubber);
            FillProjectedFace(ui,hub,ivory);
        }
        void Box(float rear,float front,float width,float bottom,float top,Color color)
        {
            var a=project(rear,-width,bottom);var b=project(front,-width,bottom);
            var c=project(front,width,bottom);var d=project(rear,width,bottom);
            var e=project(rear,-width,top);var f=project(front,-width,top);
            var g=project(front,width,top);var h=project(rear,width,top);
            Face(color*.86f,a,b,f,e);Face(color*.71f,b,c,g,f);
            Face(color*.86f,c,d,h,g);Face(color*.71f,d,a,e,h);
            FillProjectedFace(ui,stackalloc Vector2[]{e,f,g,h},color);
        }
        void Face(Color color,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            if(Visible(a,b,c))FillProjectedFace(ui,stackalloc Vector2[]{a,b,c,d},color);
        }
        static bool Visible(Vector2 a,Vector2 b,Vector2 c)=>Cross(b-a,c-b)<0;
    }
}

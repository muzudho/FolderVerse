namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public static class CountryEmblemArt
{
    private static readonly Lazy<Rectangle[][]> Rows=new(()=>Enumerable.Range(0,WorldSetup.CountryEmblemNames.Length).Select(Build).ToArray());
    internal static IReadOnlyList<Rectangle> Tiles(int emblem)=>emblem>0 && emblem<Rows.Value.Length?Rows.Value[emblem]:Array.Empty<Rectangle>();
    private static bool Disc(Vector2 p,Vector2 center,float radius)=>Vector2.DistanceSquared(p,center)<=radius*radius;
    private static bool Line(Vector2 p,Vector2 a,Vector2 b,float width)
    {
        var d=b-a;float t=Math.Clamp(Vector2.Dot(p-a,d)/d.LengthSquared(),0,1);
        return Vector2.DistanceSquared(p,a+d*t)<=width*width;
    }
    private static bool Polygon(Vector2 p,params Vector2[] vertices)
    {
        bool inside=false;
        for(int i=0,j=vertices.Length-1;i<vertices.Length;j=i++){
            var a=vertices[i];var b=vertices[j];
            if((a.Y>p.Y)!=(b.Y>p.Y) && p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
        }
        return inside;
    }
    private static readonly Vector2[] HeartOutline=Enumerable.Range(0,96).Select(i=>{
        float a=i*MathHelper.TwoPi/96,s=MathF.Sin(a);
        float y=13*MathF.Cos(a)-5*MathF.Cos(2*a)-2*MathF.Cos(3*a)-MathF.Cos(4*a);
        return new Vector2(.86f*s*s*s,-(y+2.5f)/18);
    }).ToArray();
    private static bool Heart(Vector2 p)=>Polygon(p,HeartOutline);
    private static bool Cat(Vector2 p)
    {
        bool head=p.X*p.X/(.72f*.72f)+(p.Y+.02f)*(p.Y+.02f)/(.60f*.60f)<=1 ||
            Polygon(p,new(-.68f,-.12f),new(-.72f,-.88f),new(-.17f,-.52f)) ||
            Polygon(p,new(.17f,-.52f),new(.72f,-.88f),new(.68f,-.12f));
        // Holes in the silhouette keep the button eyes visible in either ink colour.
        if(Disc(p,new(-.27f,-.13f),.085f) || Disc(p,new(.27f,-.13f),.085f))return false;
        return head;
    }
    private static bool Dolphin(Vector2 p)
    {
        bool body=Polygon(p,new(-.86f,.44f),new(-.62f,.02f),new(-.45f,-.36f),new(-.12f,-.61f),new(.24f,-.60f),
            new(.47f,-.43f),new(.62f,-.17f),new(.89f,-.03f),new(.65f,.08f),new(.40f,-.03f),new(.13f,-.17f),new(-.14f,-.06f),new(-.40f,.20f),new(-.61f,.50f));
        bool fins=Polygon(p,new(-.28f,-.48f),new(-.38f,-.88f),new(.05f,-.58f)) ||
            Polygon(p,new(.01f,-.21f),new(.29f,.24f),new(.29f,-.10f)) ||
            Polygon(p,new(-.62f,.35f),new(-.92f,.29f),new(-.86f,.68f),new(-.56f,.49f),new(-.31f,.67f),new(-.37f,.34f));
        return (body || fins) && !Disc(p,new(.40f,-.32f),.065f);
    }
    private static bool Gem(Vector2 p)=>Polygon(p,new(-.53f,-.65f),new(.53f,-.65f),new(.85f,-.16f),new(0,.88f),new(-.85f,-.16f)) &&
        !Line(p,new(-.73f,-.15f),new(.73f,-.15f),.045f) &&
        !Line(p,new(-.22f,-.56f),new(0,-.16f),.04f) &&
        !Line(p,new(.22f,-.56f),new(0,-.16f),.04f) &&
        !Line(p,new(0,-.08f),new(0,.63f),.04f);
    private static bool Snowflake(Vector2 p)
    {
        for(int arm=0;arm<6;arm++){
            float angle=arm*MathF.PI/3;var along=new Vector2(MathF.Cos(angle),MathF.Sin(angle));var cross=new Vector2(-along.Y,along.X);
            if(Line(p,Vector2.Zero,along*.85f,.065f) || Line(p,along*.48f,along*.69f+cross*.20f,.055f) ||
                Line(p,along*.48f,along*.69f-cross*.20f,.055f))return true;
        }
        return false;
    }
    private static bool Shape(int kind,Vector2 p)
    {
        switch(kind){
            case 1:return Cat(p);
            case 2:return Dolphin(p);
            case 3:return (p.X+.22f)*(p.X+.22f)/(.35f*.35f)+(p.Y-.48f)*(p.Y-.48f)/(.24f*.24f)<=1 ||
                p.X>=.02f && p.X<=.19f && p.Y>=-.85f && p.Y<=.48f;
            case 4:return Heart(new(p.X,-p.Y-.1f)) || Polygon(p,new(-.12f,.25f),new(.12f,.25f),new(.25f,.83f),new(-.25f,.83f));
            case 5:return Heart(p);
            case 6:return Math.Abs(p.X)/.72f+Math.Abs(p.Y)/.9f<=1;
            case 7:return Disc(p,new(0,-.38f),.36f) || Disc(p,new(-.35f,.08f),.36f) || Disc(p,new(.35f,.08f),.36f) || Line(p,new(0,.2f),new(0,.82f),.1f);
            case 8:return Polygon(p,new(-.55f,.6f),new(-.64f,.13f),new(-.32f,-.35f),new(-.24f,.06f),new(.18f,-.91f),
                new(.48f,-.35f),new(.37f,-.05f),new(.66f,-.35f),new(.6f,.53f),new(.28f,.83f),new(-.26f,.83f));
            case 9:return Disc(p,new(0,.28f),.53f) || Polygon(p,new(-.47f,.02f),new(0,-.88f),new(.47f,.02f));
            case 10:return Polygon(p,new(.05f,-.9f),new(.62f,-.9f),new(.12f,-.12f),new(.57f,-.12f),new(-.40f,.94f),new(-.08f,.18f),new(-.55f,.18f));
            case 11:
                var previous=Vector2.Zero;
                for(int i=1;i<=64;i++){
                    float a=i*MathF.PI*3.6f/64,r=i*.85f/64;var next=new Vector2(MathF.Cos(a),MathF.Sin(a))*r;
                    if(Line(p,previous,next,.075f))return true;previous=next;
                }
                return false;
            case 12:return Snowflake(p);
            case 13:return Polygon(p,new(-.6f,-.80f),new(-.26f,-.54f),new(0,-.88f),new(.26f,-.54f),new(.6f,-.80f),
                new(.53f,-.12f),new(.20f,.13f),new(-.20f,.13f),new(-.53f,-.12f)) ||
                Line(p,new(0,.07f),new(0,.9f),.08f) ||
                Polygon(p,new(0,.67f),new(-.53f,.30f),new(-.30f,.75f),new(0,.83f)) ||
                Polygon(p,new(0,.58f),new(.53f,.15f),new(.34f,.67f),new(0,.78f));
            case 14:return Gem(p);
            case 15:
                if(p.Y>.50f)return false;
                float radius=new Vector2(p.X,(p.Y-.50f)*.75f).Length();
                return Math.Abs(radius-.84f)<.065f || Math.Abs(radius-.53f)<.065f || Math.Abs(radius-.22f)<.065f;
            default:return false;
        }
    }
    private static Rectangle[] Build(int kind)
    {
        var result=new List<Rectangle>();
        for(int y=0;y<32;y++){
            int start=-1;
            for(int x=0;x<=32;x++){
                bool filled=x<32 && Shape(kind,new((x+.5f)/16-1,(y+.5f)/16-1));
                if(filled && start<0)start=x;
                if(!filled && start>=0){result.Add(new(start,y,x-start,1));start=-1;}
            }
        }
        return result.ToArray();
    }
    public static void Draw(UiPainter ui,Rectangle area,int emblem,Color ink,float opacity=1)
    {
        if(emblem<=0 || emblem>=Rows.Value.Length || area.Width<1 || area.Height<1)return;
        var spans=Rows.Value[emblem].Select(r=>new Rectangle(area.X+r.X*area.Width/32,area.Y+r.Y*area.Height/32,
            (r.Right*area.Width/32-r.X*area.Width/32),Math.Max(1,(r.Bottom*area.Height/32-r.Y*area.Height/32)))).Where(r=>r.Width>0).ToArray();
        foreach(var span in spans){var edge=span;edge.Inflate(1,1);ui.Box(edge,new Color(12,23,31)*opacity);}
        foreach(var span in spans)ui.Box(span,ink*opacity);
    }
}

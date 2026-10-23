namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

// Small map pictograms share the existing flat, outlined map rendering.
public static class LandmarkArt
{
    public static void Draw(UiPainter ui,LandmarkKind kind,Vector2 at,float size,float time=0)
    {
        var ink=new Color(15,31,39);var cream=new Color(247,224,174);var wood=new Color(166,111,63);var green=new Color(78,158,95);var red=new Color(230,100,83);
        Vector2 P(float x,float y)=>at+new Vector2(x,y)*size;
        void Bar(float x,float y,float w,float h,Color color,float angle=0)=>ui.Tile(P(x,y),new Vector2(w,h)*size,angle,color);
        void Line(float x,float y,float xx,float yy,float width,Color color){var a=P(x,y);var b=P(xx,yy);var d=b-a;ui.Tile((a+b)/2,new(d.Length(),width*size),MathF.Atan2(d.Y,d.X),color);}
        void Box(float x,float y,float w,float h,Color color){Bar(x,y,w+.12f,h+.12f,ink);Bar(x,y,w,h,color);}
        void Roof(float x,float y,float width,Color color){Line(x-width/2,y,x,y-width*.45f,.2f,ink);Line(x,y-width*.45f,x+width/2,y,.2f,ink);Line(x-width/2,y,x,y-width*.45f,.11f,color);Line(x,y-width*.45f,x+width/2,y,.11f,color);}
        void Tree(float x,float y,float scale){Box(x,y+.25f*scale,.15f*scale,.7f*scale,wood);for(int i=0;i<3;i++){float yy=y-i*.22f*scale;Line(x-.4f*scale,yy,x,yy-.5f*scale,.28f*scale,green);Line(x,yy-.5f*scale,x+.4f*scale,yy,.28f*scale,green);}}
        Bar(0,.65f,1.65f,.13f,ink);
        switch(kind){
        case LandmarkKind.House:
            Box(0,.15f,1.15f,.9f,cream);Roof(0,-.3f,1.5f,red);Box(0,.35f,.25f,.5f,wood);Bar(-.35f,0,.2f,.22f,new Color(111,194,224));break;
        case LandmarkKind.Windmill:
            Box(0,.12f,.62f,1.05f,cream);Roof(0,-.4f,.85f,red);
            float spin=time*.6f;for(int i=0;i<4;i++){float a=spin+i*MathHelper.PiOver2;var d=new Vector2(MathF.Cos(a),MathF.Sin(a));Line(0,-.45f,d.X*.85f,-.45f+d.Y*.85f,.13f,ink);Bar(d.X*.57f,-.45f+d.Y*.57f,.55f,.15f,cream,a);}Box(0,-.45f,.17f,.17f,wood);break;
        case LandmarkKind.Well:
            Box(0,.35f,1.05f,.45f,new Color(160,176,183));Bar(0,.12f,.8f,.17f,new Color(61,126,159));Line(-.55f,.4f,-.55f,-.7f,.12f,wood);Line(.55f,.4f,.55f,-.7f,.12f,wood);Roof(0,-.65f,1.55f,red);Line(0,-.6f,0,.1f,.05f,cream);break;
        case LandmarkKind.Tree:Tree(0,0,1.15f);break;
        case LandmarkKind.Grove:Tree(-.5f,.15f,.7f);Tree(.5f,.15f,.7f);Tree(0,-.15f,.9f);break;
        case LandmarkKind.Rock:case LandmarkKind.Reef:
            if(kind==LandmarkKind.Reef){Bar(0,.48f,1.8f,.12f,new Color(136,220,240));Bar(.3f,.7f,1.5f,.08f,new Color(136,220,240));}
            Bar(-.27f,.18f,.75f,.65f,new Color(136,150,161),-.3f);Bar(.3f,.28f,.6f,.45f,new Color(181,191,196),.3f);Line(-.55f,-.05f,-.2f,-.25f,.1f,cream);break;
        case LandmarkKind.Flowers:
            Bar(0,.37f,1.6f,.4f,green);for(int i=0;i<6;i++){float x=(i%3-1)*.5f,y=i/3*.38f-.2f;Line(x,y+.35f,x,y,.05f,green);var color=i%2==0?new Color(255,201,90):new Color(239,141,180);Bar(x,y,.34f,.12f,color);Bar(x,y,.12f,.34f,color);Bar(x,y,.08f,.08f,cream);}break;
        case LandmarkKind.Lighthouse:
            Box(0,0,.6f,1.2f,cream);Bar(0,.15f,.6f,.2f,red);Box(0,-.65f,.85f,.3f,new Color(255,220,106));Roof(0,-.85f,1,red);Line(-.4f,-.65f,-.9f,-.8f,.06f,cream);Line(.4f,-.65f,.9f,-.8f,.06f,cream);break;
        case LandmarkKind.Pier:
            Box(0,.05f,1.5f,.55f,wood);for(int i=0;i<5;i++)Line(-.6f+i*.3f,-.18f,-.6f+i*.3f,.3f,.04f,cream);Line(-.6f,-.35f,-.6f,.7f,.13f,wood);Line(.6f,-.35f,.6f,.7f,.13f,wood);break;
        case LandmarkKind.Wreck:
            Line(-.8f,.1f,-.3f,.55f,.25f,wood);Line(-.3f,.55f,.65f,.35f,.25f,wood);Line(.65f,.35f,.85f,-.1f,.25f,wood);Line(-.1f,.3f,.1f,-.8f,.12f,cream);Line(.1f,-.8f,.55f,-.4f,.14f,cream);Line(-.4f,-.2f,.2f,.1f,.08f,wood);break;
        case LandmarkKind.Buoy:
            Box(0,.38f,1,.22f,red);Box(0,.02f,.38f,.5f,red);Bar(0,-.02f,.38f,.13f,cream);Line(0,-.22f,0,-.8f,.08f,ink);Bar(.22f,-.7f,.4f,.23f,new Color(255,211,91));break;
        case LandmarkKind.Gulls:
            for(int i=0;i<3;i++){float x=(i-1)*.6f,y=i%2==0?0:-.5f;float flap=.12f*MathF.Sin(time*2+i);Line(x-.3f,y-.16f-flap,x,y,.1f,cream);Line(x,y,x+.3f,y-.16f-flap,.1f,cream);}break;
        case LandmarkKind.RookTower:
            Box(0,.1f,.8f,1.1f,new Color(182,191,195));Box(0,.58f,1.2f,.2f,cream);Box(0,-.5f,1.1f,.3f,cream);
            for(int i=-1;i<=1;i++)Box(i*.4f,-.78f,.24f,.3f,cream);Box(0,.35f,.23f,.55f,ink);break;
        case LandmarkKind.Castle:
            Box(0,.3f,1.3f,.65f,new Color(176,183,199));foreach(float x in new[]{-.6f,.6f}){Box(x,-.05f,.42f,1.2f,cream);Roof(x,-.65f,.7f,new Color(110,133,194));}Box(0,.4f,.3f,.45f,wood);Box(0,-.1f,.6f,.45f,cream);break;
        case LandmarkKind.JapaneseCastle:
            Box(0,.53f,1.45f,.26f,new Color(135,150,162));for(int i=0;i<3;i++){float y=.27f-i*.45f,w=1.3f-i*.3f;Box(0,y,w,.35f,cream);Roof(0,y-.2f,w+.35f,new Color(69,113,132));Line(-(w+.35f)/2,y-.2f,-(w+.5f)/2,y-.33f,.1f,new Color(69,113,132));Line((w+.35f)/2,y-.2f,(w+.5f)/2,y-.33f,.1f,new Color(69,113,132));Bar(0,y,.12f,.18f,ink);}break;
        case LandmarkKind.Arch:
            Box(-.55f,.1f,.35f,1.05f,cream);Box(.55f,.1f,.35f,1.05f,cream);Box(0,-.6f,1.55f,.35f,cream);Bar(0,-.63f,1.65f,.1f,new Color(186,170,139));
            for(int i=0;i<16;i++){float a=MathHelper.Pi+i*MathHelper.Pi/16,b=MathHelper.Pi+(i+1)*MathHelper.Pi/16;Line(MathF.Cos(a)*.4f,MathF.Sin(a)*.4f-.03f,MathF.Cos(b)*.4f,MathF.Sin(b)*.4f-.03f,.2f,cream);}break;
        }
    }
}

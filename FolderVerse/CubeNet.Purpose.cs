namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static void DrawPurposeBuilding(UiPainter ui,NodePurpose purpose,Func<float,float,float,Vector2> point,Func<Color,Color> tint)
    {
        var cream=new Color(255,218,151);
        void Face(Color color,params Vector2[] corners)=>FillProjectedFace(ui,corners,tint(color));
        void Box(float x,float y,float w,float d,float z,float h,Color color)
        {
            var a=point(x,y,z+h);var b=point(x+w,y,z+h);
            var c=point(x+w,y+d,z+h);var e=point(x,y+d,z+h);
            var bottom=point(x+w/2,y+d/2,z);
            Side(a,b,point(x,y,z),point(x+w,y,z),.68f);
            Side(b,c,point(x+w,y,z),point(x+w,y+d,z),.82f);
            Side(c,e,point(x+w,y+d,z),point(x,y+d,z),.68f);
            Side(e,a,point(x,y+d,z),point(x,y,z),.82f);
            Face(color,a,b,c,e);
            void Side(Vector2 p,Vector2 q,Vector2 r,Vector2 s,float shade)
            {
                if(Cross(q-p,bottom-p)*Cross(q-p,r-p)<0)
                    Face((color*shade) with { A=255 },p,q,s,r);
            }
        }
        switch(purpose)
        {
            case NodePurpose.Harvest:
                Box(-.75f,-.68f,1.5f,1.08f,0,.65f,new Color(117,139,58));
                Box(-.83f,-.73f,1.66f,1.18f,.65f,.16f,new Color(169,178,84));
                for(int i=0;i<3;i++)Box(-.64f+i*.44f,.32f,.32f,.55f,0,.28f,cream);
                break;
            case NodePurpose.Manufacturing:
                Box(-.78f,-.7f,1.56f,1.4f,0,.58f,new Color(218,96,52));
                Box(-.75f,-.65f,.62f,1.3f,.58f,.17f,cream);
                Box(-.06f,-.65f,.65f,1.3f,.58f,.3f,cream);
                Box(.48f,-.62f,.29f,.29f,.58f,.75f,new Color(232,109,63));
                break;
            case NodePurpose.Relay:
                var teal=new Color(64,166,165);
                Box(-.8f,-.65f,1.6f,1.4f,0,.16f,teal);
                for(int i=0;i<3;i++)Box(-.65f+i*.46f,.22f,.37f,.45f,.16f,.37f,cream);
                Box(-.7f,-.54f,.22f,.25f,.16f,.58f,teal);
                Box(.48f,-.54f,.22f,.25f,.16f,.58f,teal);
                Box(-.8f,-.65f,1.6f,.68f,.74f,.14f,teal);
                break;
            case NodePurpose.Defense:
                var blue=new Color(91,133,214);
                Box(-.75f,-.28f,1.5f,.62f,0,.52f,blue);
                Box(-.87f,-.43f,.54f,.86f,0,.89f,blue);
                Box(.33f,-.43f,.54f,.86f,0,.89f,blue);
                break;
            case NodePurpose.Frontline:
                var orange=new Color(232,119,52);
                Box(-.47f,-.13f,.94f,1.04f,0,.18f,orange);
                Box(-.79f,-.59f,.37f,.69f,0,.93f,orange);
                Box(.42f,-.59f,.37f,.69f,0,.93f,orange);
                Box(-.79f,-.59f,1.58f,.69f,.78f,.19f,orange);
                Face(cream,point(-.11f,.27f,.19f),point(.11f,.27f,.19f),point(.11f,.56f,.19f),point(-.11f,.56f,.19f));
                Face(cream,point(-.29f,.56f,.19f),point(.29f,.56f,.19f),point(0,.83f,.19f));
                break;
        }
    }
}

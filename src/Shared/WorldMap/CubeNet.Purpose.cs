namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal static void DrawPurposeIcon(UiPainter ui,NodePurpose? purpose,Vector2 at,bool sea,bool enabled)
    {
        Color Tint(Color color)=>enabled?color:Color.Lerp(color,new Color(25,34,40),.7f);
        float scale=purpose is NodePurpose.Defense or NodePurpose.Frontline?.8f:1f;
        Vector2 Project(float x,float y,float z)=>at+new Vector2(x*18+y*8,y*9-x*4-z*18)*scale;
        if(purpose==null)DrawHarborPier(ui,Project,Tint);
        else if(purpose==NodePurpose.General){
            var top=Tint(sea?new Color(164,226,242):new Color(255,232,153));
            Disc(ui,at+new Vector2(0,3),12,Tint(sea?new Color(52,110,135):new Color(152,111,53)));
            Disc(ui,at,12,top);
        }
        else DrawPurposeBuilding(ui,purpose.Value,sea,Project,Tint);
    }

    private static Vector3? PurposeFlagAnchor(Node node)=>!node.Functional || node.IsHarbor ? null : node.Purpose switch {
        NodePurpose.Defense=>new Vector3(-.75f,0,1.11f*1.35f),
        NodePurpose.Frontline=>new Vector3(0,-.12f,1.48f*1.35f),
        _=>null
    };

    private static void DrawPurposeBuilding(UiPainter ui,NodePurpose purpose,bool sea,Func<float,float,float,Vector2> point,Func<Color,Color> tint)
    {
        float heightScale=purpose is NodePurpose.Defense or NodePurpose.Frontline?1.35f:1f;
        Vector2 Project(float x,float y,float z)=>point(x,y,z*heightScale);
        var cream=new Color(255,218,151);
        void Face(Color color,params Vector2[] corners)=>FillProjectedFace(ui,corners,tint(color));
        Color Shade(Color color,float strength)=>(color*strength) with { A=255 };
        // Extrude a convex front profile along the ground plane.
        void Extrude(Vector2[] profile,float back,float front,Color color)
        {
            var a=Array.ConvertAll(profile,p=>Project(p.X,back,p.Y));
            var b=Array.ConvertAll(profile,p=>Project(p.X,front,p.Y));
            float winding=Cross(b[1]-b[0],b[2]-b[1]);
            for(int i=0;i<b.Length;i++){
                int j=(i+1)%b.Length;
                if(Cross(b[j]-b[i],a[i]-b[i])*winding<0)
                    Face(Shade(color,i%2==0?.84f:1f),b[i],b[j],a[j],a[i]);
            }
            if(winding<0)Face(Shade(color,.72f),b);
            else if(winding>0)Face(Shade(color,.72f),a);
        }
        void Roof(float left,float right,float ridge,float back,float front,float eave,float peak,Color color)
            =>Extrude(new[]{new Vector2(left,eave),new Vector2(right,eave),new Vector2(ridge,peak)},back,front,color);
        void Log(float x,float front,float z,float radius,Color wood)
        {
            var profile=new Vector2[8];
            for(int i=0;i<profile.Length;i++){
                float angle=i*MathHelper.TwoPi/profile.Length;
                profile[i]=new(x+MathF.Cos(angle)*radius,z+MathF.Sin(angle)*radius);
            }
            Extrude(profile,front-.42f,front,wood);
        }
        void Box(float x,float y,float w,float d,float z,float h,Color color)
        {
            var a=Project(x,y,z+h);var b=Project(x+w,y,z+h);
            var c=Project(x+w,y+d,z+h);var e=Project(x,y+d,z+h);
            var roofCenter=Project(x+w/2,y+d/2,z+h);
            Side(a,b,Project(x,y,z),Project(x+w,y,z),.68f);
            Side(b,c,Project(x+w,y,z),Project(x+w,y+d,z),.82f);
            Side(c,e,Project(x+w,y+d,z),Project(x,y+d,z),.68f);
            Side(e,a,Project(x,y+d,z),Project(x,y,z),.82f);
            Face(color,a,b,c,e);
            void Side(Vector2 p,Vector2 q,Vector2 r,Vector2 s,float shade)
            {
                // Compare with the roof interior, which stays on the same side of its edge at any height.
                if(Cross(q-p,roofCenter-p)*Cross(q-p,r-p)<0)
                    Face((color*shade) with { A=255 },p,q,s,r);
            }
        }
        switch(purpose)
        {
            case NodePurpose.Harvest:
                var olive=new Color(117,139,58);
                Box(-.79f,-.68f,1.58f,1.4f,0,.09f,olive);
                Box(-.73f,-.65f,1.46f,.17f,.09f,.49f,olive);
                Box(-.73f,-.48f,.18f,.95f,.09f,.49f,olive);
                Box(.55f,-.48f,.18f,.95f,.09f,.49f,olive);
                Roof(-.84f,.84f,0,-.73f,.55f,.58f,.96f,new(169,178,84));
                Log(-.20f,.78f,.26f,.17f,cream);
                Log(.16f,.78f,.26f,.17f,cream);
                Log(-.02f,.73f,.53f,.17f,new(249,192,96));
                break;
            case NodePurpose.Manufacturing:
                var brick=new Color(218,96,52);
                Box(-.83f,-.72f,1.66f,1.44f,0,.09f,Shade(brick,.77f));
                Box(-.78f,-.68f,1.56f,1.36f,.09f,.48f,brick);
                for(int i=0;i<3;i++)Box(-.58f+i*.42f,.68f,.19f,.025f,.26f,.17f,cream);
                Roof(-.82f,0,-.04f,-.71f,.72f,.57f,.86f,cream);
                Roof(0,.82f,.78f,-.71f,.72f,.57f,.86f,cream);
                Box(.43f,-.56f,.25f,.25f,.74f,.43f,brick);
                Box(.39f,-.60f,.33f,.33f,1.17f,.10f,new(232,109,63));
                Box(.46f,-.53f,.19f,.19f,1.27f,.008f,Shade(brick,.45f));
                break;
            case NodePurpose.Relay:
                var teal=new Color(64,166,165);
                Box(-.8f,-.65f,1.6f,1.4f,0,.16f,new(173,150,109));
                foreach(float x in new[]{-.7f,.54f})Box(x,-.54f,.16f,.17f,.16f,.55f,teal);
                for(int i=0;i<3;i++){
                    float x=-.58f+i*.40f;
                    Box(x,.12f,.32f,.40f,.16f,.33f,new(218,150,48));
                    Box(x+.04f,.52f,.24f,.02f,.20f,.24f,cream);
                }
                foreach(float x in new[]{-.7f,.54f})Box(x,.39f,.16f,.17f,.16f,.55f,teal);
                Roof(-.84f,.84f,0,-.65f,.62f,.71f,.97f,teal);
                break;
            case NodePurpose.Defense:
                var coping=new Color(106,128,159);
                var limestone=new Color(246,231,195);
                foreach(float x in new[]{-.92f,.25f})
                {
                    if(sea)
                        for(int i=0;i<3;i++)Box(x+i*.23f,-.49f,.22f,.98f,0,.19f,new(61,83,111));
                    Box(x,-.31f,.67f,.62f,.15f,.46f,limestone);
                    for(int i=0;i<3;i++)Box(x+i*.23f,-.32f,.14f,.64f,.61f,.12f,coping);
                }
                foreach(float x in new[]{-.96f,.54f})
                {
                    Box(x,-.43f,.42f,.86f,.16f,.67f,limestone);
                    Box(x-.025f,-.46f,.47f,.92f,.83f,.13f,coping);
                    Roof(x-.025f,x+.445f,x+.21f,-.45f,.45f,.96f,1.11f,coping);
                }
                if(!sea)
                    Box(-.26f,-.30f,.52f,.60f,.57f,.16f,limestone);
                break;
            case NodePurpose.Frontline:
                var orange=new Color(237,125,48);
                var plaster=new Color(250,234,204);
                var hull=new Color(75,108,133);
                Box(-.53f,-.53f,1.06f,sea?1.59f:1.36f,0,.14f,sea?hull:cream);
                Box(-.50f,-.50f,.23f,.73f,.14f,.79f,plaster);
                Box(.27f,-.50f,.23f,.73f,.14f,.79f,plaster);
                Box(-.27f,-.50f,.54f,.17f,.14f,.54f,Shade(plaster,.49f));
                Box(-.50f,-.50f,1,.73f,.68f,.51f,plaster);
                Roof(-.59f,.59f,0,-.57f,.30f,1.19f,1.48f,orange);
                Box(-.32f,.22f,.64f,.17f,.66f,.10f,orange);
                for(int i=0;i<2;i++)Box(-.23f+i*.32f,.23f,.13f,.015f,.87f,.17f,Shade(plaster,.44f));
                if(sea)
                {
                    for(int i=0;i<3;i++)Box(-.49f,.36f+i*.23f,.98f,.20f,.14f,.035f,cream);
                    Box(.61f,.31f,.35f,.66f,.02f,.13f,orange);
                    Box(.66f,.41f,.25f,.32f,.15f,.12f,plaster);
                    Face(orange,Project(.61f,.97f,.15f),Project(.96f,.97f,.15f),Project(.785f,1.21f,.15f));
                }
                else
                    Face(cream,Project(-.28f,.23f,.15f),Project(.28f,.23f,.15f),Project(.28f,.86f,.02f),Project(-.28f,.86f,.02f));
                break;
        }
    }
}

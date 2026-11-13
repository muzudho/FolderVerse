namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static void DrawPurposeBuilding(UiPainter ui,NodePurpose purpose,Func<float,float,float,Vector2> point,Func<Color,Color> tint)
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
        void Battlements(float x,float y,float width,float depth,float height,Color color)
        {
            float tooth=width/3;
            Box(x,y,tooth,depth,height,.14f,color);
            Box(x+2*tooth,y,tooth,depth,height,.14f,color);
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
                var blue=new Color(74,109,181);
                var coping=new Color(143,177,232);
                var limestone=new Color(243,244,231);
                Box(-.88f,-.46f,1.76f,.92f,0,.12f,new(46,65,108));
                Box(-.75f,-.30f,1.5f,.65f,.12f,.46f,limestone);
                Box(-.43f,-.32f,.86f,.69f,.55f,.07f,coping);
                for(int i=0;i<3;i++)Box(-.35f+i*.28f,.15f,.15f,.22f,.62f,.13f,coping);
                foreach(float x in new[]{-.87f,.33f}){
                    Box(x,-.43f,.54f,.86f,0,.13f,Shade(blue,.78f));
                    Box(x+.04f,-.38f,.46f,.76f,.13f,.62f,limestone);
                    Box(x,-.43f,.54f,.86f,.75f,.1f,coping);
                    Battlements(x,-.43f,.54f,.20f,.85f,coping);
                    Battlements(x,.23f,.54f,.20f,.85f,coping);
                    Box(x+.22f,.38f,.10f,.02f,.43f,.21f,Shade(blue,.43f));
                }
                break;
            case NodePurpose.Frontline:
                var orange=new Color(232,119,52);
                var plaster=new Color(250,240,217);
                Box(-.80f,-.62f,1.60f,.74f,0,.14f,new(134,66,43));
                float RampHeight(float y)=>MathHelper.Lerp(.15f,.025f,(y-.06f)/.85f);
                Vector2 Ramp(float x,float y,float lift=0)=>Project(x,y,RampHeight(y)+lift);
                Face(Shade(orange,.7f),Project(-.47f,.06f,0),Project(-.47f,.91f,0),Ramp(-.47f,.91f),Ramp(-.47f,.06f));
                Face(Shade(orange,.8f),Project(.47f,.06f,0),Project(.47f,.91f,0),Ramp(.47f,.91f),Ramp(.47f,.06f));
                Face(new Color(249,158,86),Ramp(-.47f,.06f),Ramp(.47f,.06f),Ramp(.47f,.91f),Ramp(-.47f,.91f));
                Box(-.79f,-.59f,.37f,.69f,.14f,.79f,plaster);
                Box(.42f,-.59f,.37f,.69f,.14f,.79f,plaster);
                Box(-.79f,-.59f,1.58f,.69f,.78f,.15f,plaster);
                Box(-.81f,-.61f,1.62f,.73f,.93f,.04f,orange);
                Battlements(-.79f,-.59f,.37f,.69f,.97f,orange);
                Battlements(.42f,-.59f,.37f,.69f,.97f,orange);
                Extrude(new[]{new Vector2(-.42f,.54f),new Vector2(-.20f,.78f),new Vector2(-.42f,.78f)},-.59f,.10f,plaster);
                Extrude(new[]{new Vector2(.42f,.54f),new Vector2(.42f,.78f),new Vector2(.20f,.78f)},-.59f,.10f,plaster);
                Face(Color.White,Ramp(-.12f,.24f,.01f),Ramp(.12f,.24f,.01f),Ramp(.12f,.55f,.01f),Ramp(-.12f,.55f,.01f));
                Face(Color.White,Ramp(-.34f,.55f,.01f),Ramp(.34f,.55f,.01f),Ramp(0,.83f,.01f));
                break;
        }
    }
}

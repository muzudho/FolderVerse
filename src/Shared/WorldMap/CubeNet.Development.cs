namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    private static readonly Dictionary<(NodePurpose Purpose,bool Sea,int Grade),(Vector3[] Points,Color Color)[]> DevelopedFacilityMeshes=new();
    // Shared polygon model: identical geometry is projected into the globe and the flat map.
    private static void DrawDevelopedFacility(UiPainter ui,NodePurpose purpose,bool sea,int grade,
        Func<float,float,float,Vector2> project,Func<Color,Color> tint)
    {
        grade=Math.Clamp(grade,0,purpose==NodePurpose.Relay?5:2);
        if(DevelopedFacilityMeshes.TryGetValue((purpose,sea,grade),out var cached)){
            DrawDevelopedMesh(ui,cached,project,tint);return;
        }
        var faces=new List<(Vector3[] Points,Color Color)>();
        var wall=new Color(248,228,190);var roof=new Color(40,135,139);
        var edge=new Color(231,140,57);var glass=new Color(37,70,84);var wood=new Color(179,121,65);
        void Face(Color c,params Vector3[] p)=>faces.Add((p,c));
        void Box(float x,float y,float z,float w,float d,float h,Color c){
            var a=new Vector3(x,y,z);var b=new Vector3(x+w,y,z);var e=new Vector3(x,y+d,z);var f=new Vector3(x+w,y+d,z);
            var up=new Vector3(0,0,h);
            Face(c,a,e,f,b);Face(c,a+up,b+up,f+up,e+up);
            Face(c,a,b,b+up,a+up);Face(c,b,f,f+up,b+up);Face(c,f,e,e+up,f+up);Face(c,e,a,a+up,e+up);
        }
        void Roof(float x,float y,float z,float w,float d,Color c){
            float peak=z+.25f;var a=new Vector3(x,y,z);var b=new Vector3(x+w,y,z);
            var e=new Vector3(x,y+d,z);var f=new Vector3(x+w,y+d,z);var t=new Vector3(x+w/2,y,peak);var u=new Vector3(x+w/2,y+d,peak);
            Face(c,a,t,u,e);Face(c,t,b,f,u);Face(wall,a,b,t);Face(wall,f,e,u);
            Box(x-.02f,y-.02f,z-.025f,w+.04f,.04f,.045f,edge);
            Box(x-.02f,y+d-.02f,z-.025f,w+.04f,.04f,.045f,edge);
            Box(x+w/2-.025f,y,peak,.05f,d,.035f,edge);
            for(int i=1;i<5;i++){
                float yy=y+d*i/5;Face(c*.8f,new(x,yy,z+.004f),new(x+w/2,yy,peak+.004f),new(x+w/2,yy+.012f,peak+.004f),new(x,yy+.012f,z+.004f));
                Face(c*.8f,new(x+w/2,yy,peak+.004f),new(x+w,yy,z+.004f),new(x+w,yy+.012f,z+.004f),new(x+w/2,yy+.012f,peak+.004f));
            }
        }
        void Building(float x,float y,float w,float d,int floors,bool open=false,Color? top=null){
            const float baseZ=.10f;float h=.40f*floors;
            if(open){
                Box(x,y,baseZ,w,.08f,h,wall);
                foreach(float xx in new[]{x,x+w-.075f})foreach(float yy in new[]{y,y+d-.075f})Box(xx,yy,baseZ,.075f,.075f,h,wall);
                Box(x-.01f,y+d-.08f,baseZ+h-.08f,w+.02f,.08f,.065f,wood);
                foreach(float xx in new[]{x,x+w-.075f})Box(xx-.012f,y+d-.09f,baseZ,.10f,.10f,.09f,new(146,147,135));
            }else{
                Box(x,y,baseZ,w,d,h,wall);
                foreach(float xx in new[]{x-.004f,x+w-.035f})Box(xx,y-.006f,baseZ,.04f,d+.012f,h,edge);
                Box(x+w*.36f,y+d+.006f,baseZ,w*.28f,.015f,.29f,wood);
                for(int plank=1;plank<4;plank++)Box(x+w*(.36f+.28f*plank/4),y+d+.024f,baseZ,.008f,.008f,.29f,edge);
                Box(x+w*.58f,y+d+.034f,.23f,.025f,.025f,.025f,new(238,196,86));
                for(int level=0;level<floors;level++){
                    float z=baseZ+.18f+level*.40f;
                    foreach(float xx in new[]{x+w*.12f,x+w*.72f}){
                        Box(xx-.015f,y+d+.008f,z-.015f,w*.16f+.03f,.015f,.18f,Color.White);
                        Box(xx,y+d+.025f,z,w*.16f,.015f,.15f,glass);
                        Box(xx+w*.075f,y+d+.042f,z,.012f,.008f,.15f,wall);
                        Box(xx,y+d+.042f,z+.07f,w*.16f,.008f,.012f,wall);
                        Box(xx-.02f,y+d+.012f,z-.035f,w*.16f+.04f,.06f,.025f,edge);
                    }
                    Box(x+w+.009f,y+d*.32f,z,.015f,d*.25f,.15f,glass);
                    if(level>0)Box(x-.025f,y-.025f,baseZ+level*.40f,w+.05f,d+.05f,.045f,edge);
                }
            }
            if(purpose==NodePurpose.Relay)Box(x-.06f,y-.06f,baseZ+h,w+.12f,d+.12f,.07f,new(76,102,132));
            else if(purpose==NodePurpose.Manufacturing){
                for(int tooth=0;tooth<3;tooth++){
                    float xx=x-.06f+tooth*(w+.12f)/3,ww=(w+.12f)/3,z=baseZ+h;
                    Face(roof,new(xx,y-.06f,z),new(xx+ww,y-.06f,z+.24f),new(xx+ww,y+d+.06f,z+.24f),new(xx,y+d+.06f,z));
                    Face(glass,new(xx+ww,y-.06f,z),new(xx+ww,y+d+.06f,z),new(xx+ww,y+d+.06f,z+.24f),new(xx+ww,y-.06f,z+.24f));
                    Face(wall,new(xx,y-.06f,z),new(xx+ww,y-.06f,z),new(xx+ww,y-.06f,z+.24f));
                    Face(wall,new(xx,y+d+.06f,z),new(xx+ww,y+d+.06f,z+.24f),new(xx+ww,y+d+.06f,z));
                }
            }else Roof(x-.06f,y-.06f,baseZ+h,w+.12f,d+.12f,top??roof);
        }
        void Crate(float x,float y){
            Box(x,y,.10f,.23f,.25f,.20f,wood);Box(x+.035f,y-.007f,.105f,.025f,.265f,.20f,edge);Box(x+.17f,y-.007f,.105f,.025f,.265f,.20f,edge);
            Box(x-.006f,y-.008f,.14f,.242f,.018f,.018f,new(220,177,105));Box(x-.006f,y-.008f,.23f,.242f,.018f,.018f,new(220,177,105));
        }
        float width=purpose==NodePurpose.Relay?1.4f+grade*.18f:1.45f+grade*.23f;
        Box(-width/2,-.65f,0,width,1.45f,.10f,sea?new(58,96,119):new(199,184,148));
        // Seams in the toy paving and floating pontoons remain visible at medium zoom.
        for(int i=1;i<6;i++)Box(-width/2,.80f-i*.24f,.102f,width,.012f,.008f,new(230,219,192));
        if(sea)foreach(float xx in new[]{-width/2,width/2-.12f})Box(xx,-.62f,-.10f,.12f,1.38f,.11f,new(48,75,93));
        if(purpose==NodePurpose.FinalDisposal){Building(-.36f,-.50f,.72f,.55f,1,false,new Color(65,109,110));for(int i=0;i<=grade;i++){Box(-width/2+i*.45f,.17f,.10f,.38f,.45f,.23f,new Color(100,119,135));Box(-width/2+i*.45f,.17f,.33f,.38f,.45f,.035f,new Color(232,184,77));}Box(.32f,-.40f,.10f,.12f,.12f,.60f,new Color(232,184,77));}else if(purpose==NodePurpose.Relay){
            int bays=1+Math.Min(grade,3);float bw=width/bays;
            for(int i=0;i<bays;i++)Building(-width/2+i*bw,-.57f,bw-.035f,.68f,1,true);
            if(grade>=4)Building(width/2-.44f,.12f,.42f,.51f,1);
            if(grade>=5)Building(-width/2,.12f,.42f,.51f,1);
            for(int i=0;i<=Math.Min(grade,3);i++)Crate(-width/2+.10f+i*.30f,.19f);
            for(int i=0;i<3;i++)Box(-.25f+i*.15f,.49f,.10f,.12f,.14f,.09f,new(221,174,99));
        }else if(purpose==NodePurpose.Defense){
            int towers=2+grade;float step=(width-.32f)/(towers-1);
            Box(-width/2,-.20f,.10f,width,.33f,.43f,wall);
            for(int i=0;i<towers;i++){
                float x=-width/2+i*step;Box(x,-.30f,.10f,.32f,.52f,.62f,wall);Box(x-.025f,-.325f,.72f,.37f,.57f,.065f,new(86,123,153));
                foreach(float dx in new[]{0f,.23f})foreach(float yy in new[]{-.30f,.15f})Box(x+dx,yy,.78f,.09f,.08f,.13f,new(86,123,153));
                Box(x+.11f,.226f,.39f,.065f,.012f,.18f,glass);
            }
            Box(-.14f,.142f,.10f,.28f,.015f,.36f,wood);
            var gate=new List<Vector3>{new(-.14f,.164f,.10f),new(.14f,.164f,.10f),new(.14f,.164f,.38f)};
            for(int i=0;i<=8;i++){float angle=i*MathHelper.Pi/8;gate.Add(new(.14f*MathF.Cos(angle),.164f,.38f+.14f*MathF.Sin(angle)));}
            Face(wood,gate.AsEnumerable().Reverse().ToArray());
            for(int i=0;i<3;i++)Box(-.12f+i*.11f,.17f,.11f,.018f,.015f,.33f,edge);
        }else if(purpose==NodePurpose.Frontline){
            Building(-.38f,-.46f,.76f,.77f,1+grade,false,edge);
            Box(-.22f,.32f,.10f,.44f,.29f,.06f,wall);
        }else if(purpose==NodePurpose.General){
            Building(-width/2+.06f,-.43f,.60f,.73f,1+(grade==2?1:0));
            Building(.05f,-.37f,.51f,.65f,1,false,edge);
            if(grade>0)Building(-.35f,.34f,.55f,.31f,1);
        }else{
            int bays=1+grade;float bw=width/bays;
            for(int i=0;i<bays;i++)Building(-width/2+i*bw,-.48f,bw-.025f,.78f,1,purpose==NodePurpose.Harvest);
            if(purpose==NodePurpose.Manufacturing){
                Box(width/2-.24f,-.40f,.6f,.16f,.16f,.42f,edge);Box(width/2-.265f,-.425f,1.02f,.21f,.21f,.06f,wall);
            }else for(int i=0;i<3;i++)Box(-.32f+i*.20f,.38f,.10f,.16f,.30f,.13f,new(224,175,95));
        }
        var mesh=faces.ToArray();DevelopedFacilityMeshes[(purpose,sea,grade)]=mesh;
        DrawDevelopedMesh(ui,mesh,project,tint);
    }
    private static void DrawDevelopedMesh(UiPainter ui,(Vector3[] Points,Color Color)[] faces,
        Func<float,float,float,Vector2> project,Func<Color,Color> tint)
    {
        var origin=project(0,0,0);var px=project(1,0,0)-origin;var py=project(0,1,0)-origin;var pz=project(0,0,1)-origin;
        var camera=Vector3.Cross(new(px.X,py.X,pz.X),new(px.Y,py.Y,pz.Y));if(camera.Z<0)camera=-camera;
        foreach(var face in faces.Where(f=>Vector3.Dot(Vector3.Cross(f.Points[1]-f.Points[0],f.Points[2]-f.Points[0]),camera)>0)
            .OrderBy(f=>Vector3.Dot(f.Points.Aggregate(Vector3.Zero,(sum,p)=>sum+p)/f.Points.Length,camera))){
            var normal=Vector3.Normalize(Vector3.Cross(face.Points[1]-face.Points[0],face.Points[2]-face.Points[0]));
            float light=.72f+.28f*Math.Max(0,Vector3.Dot(normal,Vector3.Normalize(new Vector3(-.4f,-.6f,1))));
            FillProjectedFace(ui,face.Points.Select(p=>project(p.X,p.Y,p.Z)).ToArray(),tint((face.Color*light) with {A=255}));
        }
    }
}

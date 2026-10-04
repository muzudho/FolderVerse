namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// A physical orientation reference: bowl +X, chopsticks -X, head +Y, feet -Y,
// belly +Z and back -Z. Rotate the complete toy with the same matrix as the globe.
public sealed class ToyOrientationRenderer:IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly VertexBuffer _mesh;
    private readonly int _primitives;
    public ToyOrientationRenderer(GraphicsDevice device)
    {
        _device=device;_effect=new BasicEffect(device){VertexColorEnabled=true};
        _effect.EnableDefaultLighting();_effect.AmbientLightColor=new Vector3(.48f);
        _effect.SpecularColor=new Vector3(.18f);_effect.SpecularPower=18;
        var mesh=new List<VertexPositionColorNormal>();
        var ivory=new Color(230,218,183);var steel=new Color(83,112,122);
        var teal=new Color(41,146,159);var gold=new Color(241,174,65);
        var dark=new Color(28,46,59);var orange=new Color(230,100,55);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color color)
        {
            mesh.Add(new(a,color,normal));mesh.Add(new(b,color,normal));mesh.Add(new(c,color,normal));
            mesh.Add(new(a,color,normal));mesh.Add(new(c,color,normal));mesh.Add(new(d,color,normal));
        }
        void Box(Vector3 center,Vector3 size,Color color)
        {
            var lo=center-size/2;var hi=center+size/2;
            Quad(new(lo.X,lo.Y,hi.Z),new(hi.X,lo.Y,hi.Z),new(hi.X,hi.Y,hi.Z),new(lo.X,hi.Y,hi.Z),Vector3.Backward,color);
            Quad(new(hi.X,lo.Y,lo.Z),new(lo.X,lo.Y,lo.Z),new(lo.X,hi.Y,lo.Z),new(hi.X,hi.Y,lo.Z),Vector3.Forward,color);
            Quad(new(hi.X,lo.Y,hi.Z),new(hi.X,lo.Y,lo.Z),new(hi.X,hi.Y,lo.Z),new(hi.X,hi.Y,hi.Z),Vector3.Right,color);
            Quad(new(lo.X,lo.Y,lo.Z),new(lo.X,lo.Y,hi.Z),new(lo.X,hi.Y,hi.Z),new(lo.X,hi.Y,lo.Z),Vector3.Left,color);
            Quad(new(lo.X,hi.Y,hi.Z),new(hi.X,hi.Y,hi.Z),new(hi.X,hi.Y,lo.Z),new(lo.X,hi.Y,lo.Z),Vector3.Up,color);
            Quad(new(lo.X,lo.Y,lo.Z),new(hi.X,lo.Y,lo.Z),new(hi.X,lo.Y,hi.Z),new(lo.X,lo.Y,hi.Z),Vector3.Down,color);
        }
        void Sphere(Vector3 center,float radius,Color color)
        {
            Vector3 At(float latitude,float longitude)=>new(MathF.Cos(latitude)*MathF.Cos(longitude),MathF.Sin(latitude),MathF.Cos(latitude)*MathF.Sin(longitude));
            for(int row=0;row<8;row++)for(int col=0;col<16;col++)
            {
                float a=-MathHelper.PiOver2+row*MathHelper.Pi/8,b=a+MathHelper.Pi/8,c=col*MathHelper.TwoPi/16,d=c+MathHelper.TwoPi/16;
                var p=At(a,c);var q=At(a,d);var r=At(b,d);var s=At(b,c);
                Quad(center+p*radius,center+q*radius,center+r*radius,center+s*radius,Vector3.Normalize(p+q+r+s),color);
            }
        }
        Box(new(0,.85f,0),new(.74f,.6f,.57f),ivory);
        Box(new(0,1.17f,0),new(.82f,.1f,.64f),steel);
        Box(new(0,1.32f,0),new(.045f,.25f,.045f),steel);Sphere(new(0,1.47f,0),.075f,orange);
        foreach(float x in new[]{-.19f,.19f})
        {Box(new(x,.91f,.302f),new(.19f,.19f,.04f),dark);Box(new(x-.025f,.94f,.328f),new(.045f,.045f,.025f),Color.White);}
        Box(new(0,.7f,.3f),new(.28f,.055f,.04f),orange);
        Box(new(0,.54f,0),new(.25f,.14f,.25f),steel);
        Box(new(0,.07f,0),new(.65f,.79f,.45f),ivory);
        Box(new(0,.12f,.243f),new(.48f,.52f,.045f),teal);
        for(int i=0;i<3;i++)Box(new(-.14f+i*.14f,.22f,.278f),new(.095f,.12f,.035f),i==0?gold:i==1?new Color(124,194,77):orange);
        Box(new(0,-.04f,.278f),new(.25f,.07f,.035f),dark);
        // The back has a large orange winding key instead of a face or control panel.
        Box(new(0,.12f,-.28f),new(.14f,.43f,.12f),orange);
        Box(new(0,.12f,-.35f),new(.34f,.11f,.08f),orange);
        foreach(float side in new[]{-1f,1f})
        {
            Sphere(new(side*.4f,.29f,0),.13f,steel);
            Box(new(side*.48f,.05f,0),new(.18f,.4f,.2f),teal);
            Box(new(side*.66f,-.12f,.08f),new(.37f,.17f,.2f),ivory);
            Box(new(side*.19f,-.57f,0),new(.21f,.43f,.25f),steel);
            Box(new(side*.19f,-.82f,.08f),new(.3f,.16f,.4f),teal);
        }
        // Open bowl in the +X hand. Gold outside, light inside and a thick rim.
        var bowl=new Vector3(.88f,.02f,.12f);
        for(int i=0;i<24;i++)
        {
            float a=i*MathHelper.TwoPi/24,b=(i+1)*MathHelper.TwoPi/24;
            Vector3 Rim(float angle,float radius,float y)=>bowl+new Vector3(MathF.Cos(angle)*radius,y,MathF.Sin(angle)*radius);
            var bottom=bowl+new Vector3(0,-.23f,0);
            Quad(bottom,Rim(a,.24f,0),Rim(b,.24f,0),bottom,Vector3.Normalize(new Vector3(MathF.Cos((a+b)/2),-.7f,MathF.Sin((a+b)/2))),gold);
            Quad(bowl+new Vector3(0,-.17f,0),Rim(b,.19f,0),Rim(a,.19f,0),bowl+new Vector3(0,-.17f,0),Vector3.Up,ivory);
            Quad(Rim(a,.19f,0),Rim(b,.19f,0),Rim(b,.24f,0),Rim(a,.24f,0),Vector3.Up,gold);
        }
        // Chopsticks in the -X hand are deliberately distinct from the bowl.
        foreach(float x in new[]{-.91f,-.78f})Box(new(x,.19f,.13f),new(.045f,.72f,.045f),gold);
        _mesh=new VertexBuffer(device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);_mesh.SetData(mesh.ToArray());_primitives=mesh.Count/3;
    }
    public void Draw(float yaw,float pitch)
    {
        _device.BlendState=BlendState.Opaque;_device.DepthStencilState=DepthStencilState.Default;_device.RasterizerState=RasterizerState.CullNone;
        _effect.World=WorldPreview.Orientation(yaw,pitch);
        _effect.View=Matrix.CreateLookAt(new Vector3(0,.18f,6),new Vector3(0,.18f,0),Vector3.Up);
        float aspect=_device.Viewport.AspectRatio,height=Math.Max(3.7f,3.4f/aspect);
        _effect.Projection=Matrix.CreateOrthographic(height*aspect,height,.1f,100);
        _device.SetVertexBuffer(_mesh);
        foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,_primitives);}
    }
    public void Dispose(){_mesh.Dispose();_effect.Dispose();}
}

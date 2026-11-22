namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class FallingTileRenderer:IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly List<VertexPositionColor> _solid=new(70000),_shadow=new(28000);
    private readonly List<VertexPositionColorTexture> _front=new(14000);
    private VertexPositionColor[] _colorBuffer=Array.Empty<VertexPositionColor>();
    private VertexPositionColorTexture[] _frontBuffer=Array.Empty<VertexPositionColorTexture>();
    public FallingTileRenderer(GraphicsDevice device){_device=device;_effect=new(device){VertexColorEnabled=true,Projection=Matrix.CreateOrthographicOffCenter(0,1920,1080,0,-1000,1000)};}
    private static void Quad(List<VertexPositionColor> list,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
    {list.Add(new(a,color));list.Add(new(b,color));list.Add(new(c,color));list.Add(new(a,color));list.Add(new(c,color));list.Add(new(d,color));}
    public void Draw(Texture2D image,FallingTilePattern pattern,float age)
    {
        _solid.Clear();_shadow.Clear();_front.Clear();
        foreach(var tile in pattern.Tiles){
            float elapsed=age-tile.Delay;var center=FallingTilePattern.Center(tile,age);if(center.Y>1140)continue;
            var rotation=FallingTilePattern.Rotation(tile,age);
            Vector3 P(float x,float y,float z)=>center+Vector3.Transform(new Vector3(x,y,z),rotation);
            Vector3 a=P(-15,-15,-2),b=P(15,-15,-2),c=P(15,15,-2),d=P(-15,15,-2);
            Vector3 aa=P(-15,-15,2),bb=P(15,-15,2),cc=P(15,15,2),dd=P(-15,15,2);
            if(elapsed>0){
                // Soft contact shadow on the following image; grows and fades as a tile lifts away.
                float alpha=.22f*Math.Clamp(1-elapsed/1.25f,0,1);var offset=new Vector3(7+elapsed*12,9+elapsed*16,0);
                for(int layer=3;layer>=0;layer--){float s=1+layer*.045f;Vector3 S(Vector3 p)=>center+(p-center)*s+offset;Quad(_shadow,S(a),S(b),S(c),S(d),Color.Black*(alpha/(layer+2)));}
                Quad(_solid,aa,bb,cc,dd,tile.Back);
                var edge=Color.Lerp(tile.Back,Color.Black,.35f);
                Quad(_solid,a,b,bb,aa,edge);Quad(_solid,b,c,cc,bb,edge);Quad(_solid,c,d,dd,cc,edge);Quad(_solid,d,a,aa,dd,edge);
            }
            // A flipped panel shows its colored rear instead of a mirrored screen fragment.
            if(Vector3.TransformNormal(Vector3.UnitZ,rotation).Z<=0)continue;
            float u=tile.X/64f,v=tile.Y/36f,du=1/64f,dv=1/36f;
            void V(Vector3 p,float x,float y)=>_front.Add(new(p,Color.White,new Vector2(x,y)));
            V(a,u,v);V(b,u+du,v);V(c,u+du,v+dv);V(a,u,v);V(c,u+du,v+dv);V(d,u,v+dv);
        }
        _device.BlendState=BlendState.AlphaBlend;_device.DepthStencilState=DepthStencilState.None;_device.RasterizerState=RasterizerState.CullNone;
        _effect.TextureEnabled=false;DrawColors(_shadow);DrawColors(_solid);
        _effect.TextureEnabled=true;_effect.Texture=image;_device.SamplerStates[0]=SamplerState.LinearClamp;
        if(_frontBuffer.Length<_front.Count)Array.Resize(ref _frontBuffer,_front.Count);_front.CopyTo(_frontBuffer);
        if(_front.Count>0)foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,_frontBuffer,0,_front.Count/3);}
    }
    private void DrawColors(List<VertexPositionColor> vertices){if(vertices.Count==0)return;if(_colorBuffer.Length<vertices.Count)Array.Resize(ref _colorBuffer,vertices.Count);vertices.CopyTo(_colorBuffer);foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawUserPrimitives(PrimitiveType.TriangleList,_colorBuffer,0,vertices.Count/3);}}
    public void Dispose()=>_effect.Dispose();
}
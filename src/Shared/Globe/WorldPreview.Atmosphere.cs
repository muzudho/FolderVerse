namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private BasicEffect _atmosphereEffect;
    private const float AtmosphereThickness=2; // One surface region is one world unit wide.

    private void DrawAtmosphere(float opacity)
    {
        var viewport=_device.Viewport;
        var half=new Vector3(Width,Height,Depth)*.5f;
        var points=new List<Vector2>(8);
        for(int i=0;i<8;i++)
        {
            var corner=half*new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
            var projected=viewport.Project(corner,_effect.Projection,_effect.View,_effect.World);
            points.Add(new(projected.X,projected.Y));
        }
        var ordered=points.Distinct().OrderBy(p=>p.X).ThenBy(p=>p.Y).ToArray();
        var hull=new List<Vector2>(8);
        static float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
        void Append(IEnumerable<Vector2> candidates)
        {
            int start=hull.Count;
            foreach(var point in candidates)
            {
                while(hull.Count>=start+2 && Cross(hull[^1]-hull[^2],point-hull[^1])<=0)
                    hull.RemoveAt(hull.Count-1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count-1);
        }
        if(ordered.Length<3)return;
        Append(ordered);
        Append(ordered.Reverse());
        var clip=Vector4.Transform(new Vector4(0,0,0,1),_effect.World*_effect.View*_effect.Projection);
        float thickness=AtmosphereThickness*Math.Abs(_effect.Projection.M11)*viewport.Width/(2*clip.W);
        var rim=new List<(Vector2 Point,Vector2 Normal)>();
        for(int i=0;i<hull.Count;i++)
        {
            var point=hull[i];
            var incoming=Vector2.Normalize(point-hull[(i+hull.Count-1)%hull.Count]);
            var outgoing=Vector2.Normalize(hull[(i+1)%hull.Count]-point);
            float angle=MathF.Atan2(-incoming.X,incoming.Y);
            float turn=MathF.Atan2(Cross(incoming,outgoing),Vector2.Dot(incoming,outgoing));
            for(int step=0;step<=5;step++)
            {
                float direction=angle+turn*step/5;
                rim.Add((point,new Vector2(MathF.Cos(direction),MathF.Sin(direction))));
            }
        }
        const int bands=8;
        var vertices=new List<VertexPositionColor>(rim.Count*bands*6);
        VertexPositionColor Vertex((Vector2 Point,Vector2 Normal) edge,int band)
        {
            float distance=band/(float)bands;
            float alpha=.29f*opacity*MathF.Pow(1-distance,2.4f);
            return new(new Vector3(edge.Point+edge.Normal*(thickness*distance),0),new Color(116,201,255)*alpha);
        }
        for(int band=0;band<bands;band++)
        for(int i=0;i<rim.Count;i++)
        {
            var a=rim[i];var b=rim[(i+1)%rim.Count];
            vertices.Add(Vertex(a,band));vertices.Add(Vertex(b,band));vertices.Add(Vertex(b,band+1));
            vertices.Add(Vertex(a,band));vertices.Add(Vertex(b,band+1));vertices.Add(Vertex(a,band+1));
        }
        _atmosphereEffect??=new BasicEffect(_device){VertexColorEnabled=true};
        _atmosphereEffect.Projection=Matrix.CreateOrthographicOffCenter(viewport.X,viewport.X+viewport.Width,
            viewport.Y+viewport.Height,viewport.Y,0,1);
        _device.BlendState=BlendState.AlphaBlend;
        _device.DepthStencilState=DepthStencilState.None;
        var geometry=vertices.ToArray();
        foreach(var pass in _atmosphereEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList,geometry,0,geometry.Length/3);
        }
    }
}

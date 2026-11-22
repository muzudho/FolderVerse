namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview
{
    private BasicEffect _atmosphereEffect;
    private readonly VertexPositionColor[] _atmosphereFaces=new VertexPositionColor[36];
    private const float AtmosphereThickness=1; // One surface region is one world unit wide.
    private const float InnerAtmosphereThickness=.3f;
    private readonly List<VertexPositionColor> _cloudMesh=new();
    private VertexPositionColor[] _cloudVertices=Array.Empty<VertexPositionColor>();
    private static Vector3 AtmosphereAxis(int axis)=>axis switch {0=>Vector3.UnitX,1=>Vector3.UnitY,_=>Vector3.UnitZ};

    internal void DrawAtmosphere(float seconds,float opacity=1,float cloudSpeedMultiplier=1)
    {
        using var measure=new WorldPreview.DrawCpuScope();
        _atmosphereEffect??=new BasicEffect(_device){VertexColorEnabled=true};
        _atmosphereEffect.World=_effect.World;
        _atmosphereEffect.View=_effect.View;
        _atmosphereEffect.Projection=_effect.Projection;
        _device.BlendState=BlendState.AlphaBlend;
        _device.DepthStencilState=DepthStencilState.DepthRead;
        ApplyMapClip();

        var half=new Vector3(Width,Height,Depth)*.5f;
        var viewTransform=_effect.World*_effect.View;
        DrawShell(half+new Vector3(InnerAtmosphereThickness),.20f);
        DrawAtmosphereClouds(seconds,opacity,cloudSpeedMultiplier);
        DrawShell(half+new Vector3(AtmosphereThickness),.10f);
        _device.DepthStencilState=DepthStencilState.Default;

        void DrawShell(Vector3 size,float alpha)
        {
            int count=0;
            for(int axis=0;axis<3;axis++)
            {
                var direction=AtmosphereAxis(axis);
                var u=AtmosphereAxis((axis+1)%3)*Vector3.Dot(size,AtmosphereAxis((axis+1)%3));
                var v=AtmosphereAxis((axis+2)%3)*Vector3.Dot(size,AtmosphereAxis((axis+2)%3));
                float extent=Vector3.Dot(size,direction);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    var normal=direction*sign;
                    var center=normal*extent;
                    if(Vector3.Dot(Vector3.TransformNormal(normal,viewTransform),-Vector3.Transform(center,viewTransform))<=0)continue;
                    var tint=axis switch
                    {
                        0=>new Color(128,192,230),
                        1=>new Color(177,225,250),
                        _=>new Color(151,211,244)
                    };
                    var color=tint*(alpha*opacity);
                    var a=new VertexPositionColor(center-u-v,color);
                    var b=new VertexPositionColor(center+u-v,color);
                    var c=new VertexPositionColor(center+u+v,color);
                    var d=new VertexPositionColor(center-u+v,color);
                    _atmosphereFaces[count++]=a;_atmosphereFaces[count++]=b;_atmosphereFaces[count++]=c;
                    _atmosphereFaces[count++]=a;_atmosphereFaces[count++]=c;_atmosphereFaces[count++]=d;
                }
            }
            if(count==0)return;
            foreach(var pass in _atmosphereEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserPrimitives(PrimitiveType.TriangleList,_atmosphereFaces,0,count/3);
            }
        }

    }

    private void DrawAtmosphereClouds(float seconds,float opacity,float speedMultiplier)
    {
        var size=new Vector3(Width,Height,Depth)*.5f+new Vector3(InnerAtmosphereThickness);
        var up=AtmosphereAxis(_polarAxis);
        var uAxis=AtmosphereAxis((_polarAxis+1)%3);
        var vAxis=AtmosphereAxis((_polarAxis+2)%3);
        var u=uAxis*Vector3.Dot(size,uAxis);
        var v=vAxis*Vector3.Dot(size,vAxis);
        Span<Vector3> corners=stackalloc Vector3[]{-u-v,u-v,u+v,-u+v};
        float perimeter=4*(u.Length()+v.Length());
        float height=Vector3.Dot(size,up);
        int cloudBands=Math.Clamp((int)MathF.Ceiling(height),3,6);
        int cloudsPerBand=Math.Clamp((int)MathF.Ceiling(perimeter/2.8f),4,20);
        const float speed=.24f;
        float scroll=(seconds*speed*speedMultiplier)%perimeter;
        float alpha=opacity*MathHelper.Lerp(.60f,.26f,MathHelper.Clamp((GlobeZoom-1)/2,0,1));
        var bright=new Color(240,250,255)*alpha;
        var shaded=new Color(152,198,226)*alpha;
        var viewTransform=_effect.World*_effect.View;
        var mesh=_cloudMesh;
        mesh.Clear();
        float edgeStart=0;
        for(int edge=0;edge<corners.Length;edge++)
        {
            var from=corners[edge];var to=corners[(edge+1)%corners.Length];
            float length=Vector3.Distance(from,to);
            float edgeEnd=edgeStart+length;
            var middle=(from+to)*.5f;
            var normal=Vector3.Normalize(middle);
            if(Vector3.Dot(Vector3.TransformNormal(normal,viewTransform),-Vector3.Transform(middle,viewTransform))>0)
            {
                for(int band=0;band<cloudBands;band++)
                for(int cloud=0;cloud<cloudsPerBand;cloud++)
                {
                    // Scroll in unfolded box coordinates, with staggered clouds at each latitude.
                    int pattern=cloud*7+band*11;
                    float spacing=perimeter/cloudsPerBand;
                    float stagger=(band*.618f)%1;
                    float jitter=(pattern%5-2)*.08f;
                    float center=((cloud+stagger+jitter)*spacing+scroll+perimeter)%perimeter;
                    float width=.64f+(pattern%5)*.11f;
                    float altitude=((band+.5f)/cloudBands-.5f)*height*1.65f
                        +(pattern%7-3)*.055f;
                    for(int wrap=-1;wrap<=1;wrap++)
                    {
                        float left=center+wrap*perimeter-width*.5f;
                        float a=Math.Max(left,edgeStart),b=Math.Min(left+width,edgeEnd);
                        if(b<=a)continue;
                        // Clip each strip to its current face so clouds fold over corners continuously.
                        for(int strip=0;strip<12;strip++)
                        {
                            float start=Math.Max(a,left+width*strip/12);
                            float end=Math.Min(b,left+width*(strip+1)/12);
                            if(end<=start)continue;
                            Vector3 Point(float distance,int row)
                            {
                                float t=(distance-left)/width;
                                float tip=MathF.Sqrt(Math.Max(0,1-MathF.Pow(2*t-1,2)));
                                float lobes=.72f+.22f*MathF.Cos(t*MathHelper.TwoPi*3+.6f);
                                float lift=row switch {0=>-.105f*tip,1=>0,_=>.23f*tip*lobes};
                                // Both neighboring faces use the same box vertex at the fold.
                                var surface=distance==edgeStart?from:distance==edgeEnd?to
                                    :Vector3.Lerp(from,to,(distance-edgeStart)/length);
                                return surface+up*(altitude+lift);
                            }
                            for(int row=0;row<2;row++)
                            {
                                var ink=row==0?shaded:bright;
                                var p=new VertexPositionColor(Point(start,row),ink);
                                var q=new VertexPositionColor(Point(end,row),ink);
                                var r=new VertexPositionColor(Point(end,row+1),ink);
                                var s=new VertexPositionColor(Point(start,row+1),ink);
                                mesh.Add(p);mesh.Add(q);mesh.Add(r);
                                mesh.Add(p);mesh.Add(r);mesh.Add(s);
                            }
                        }
                    }
                }
            }
            edgeStart=edgeEnd;
        }
        if(mesh.Count==0)return;
        if(_cloudVertices.Length<mesh.Count)Array.Resize(ref _cloudVertices,mesh.Count*2);
        mesh.CopyTo(_cloudVertices);
        foreach(var pass in _atmosphereEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleList,_cloudVertices,0,mesh.Count/3);
        }
    }
}

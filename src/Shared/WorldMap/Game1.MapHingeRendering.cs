namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private void DrawMapHinges(CubeNet net,Rectangle panel,Rectangle area)
    {
        float progress=MathHelper.Clamp(_mapTransitionAge/MapTransitionDuration,0,1);
        float unfold=_mapUnfold?progress:1-progress;
        float Ease(float t){t=MathHelper.Clamp(t,0,1);return t*t*(3-2*t);}
        // Derive hinges from the current net, including faces rearranged by edge clicks.
        var faces=net.Faces;
        var parent=Enumerable.Repeat(-1,6).ToArray();var traversal=new List<int>{0};
        for(int n=0;n<traversal.Count;n++)
        {
            int a=traversal[n];
            for(int b=1;b<6;b++)
            {
                if(parent[b]>=0 || Math.Abs(Vector3.Dot(CubeNet.Normals[faces[a].Face],CubeNet.Normals[faces[b].Face]))>.1f)continue;
                var delta=faces[b].Center-faces[a].Center;
                bool joined=Math.Abs(delta.Y)<.001f && Math.Abs(Math.Abs(delta.X)-(faces[a].Width+faces[b].Width)/2)<.001f ||
                    Math.Abs(delta.X)<.001f && Math.Abs(Math.Abs(delta.Y)-(faces[a].Height+faces[b].Height)/2)<.001f;
                if(joined){parent[b]=a;traversal.Add(b);}
            }
        }
        int[] order=traversal.Skip(1).Reverse().ToArray();var pose=new Matrix[6];pose[0]=Matrix.Identity;
        float Angle(int index)=>MathHelper.PiOver2*(1-Ease(unfold*6-Array.IndexOf(order,index)));
        var rootNormal=CubeNet.Normals[faces[0].Face];
        float d=Math.Abs(rootNormal.X)*_setup.Width+Math.Abs(rootNormal.Y)*_setup.Height+Math.Abs(rootNormal.Z)*_setup.Depth;
        Matrix Hinge(Vector3 pivot,Matrix rotation)=>Matrix.CreateTranslation(-pivot)*rotation*Matrix.CreateTranslation(pivot);
        foreach(int index in traversal.Skip(1))
        {
            var anchor=faces[parent[index]];var delta=faces[index].Center-anchor.Center;
            var outward=Vector2.Normalize(delta);
            var pivot=anchor.Center+new Vector2(outward.X*anchor.Width/2,outward.Y*anchor.Height/2);
            var axis=Vector3.Cross(Vector3.UnitZ,new Vector3(outward.X,-outward.Y,0));
            pose[index]=Hinge(new(pivot.X,-pivot.Y,0),Matrix.CreateFromAxisAngle(axis,Angle(index)))*pose[parent[index]];
        }
        float align=Ease(unfold*6-5);
        var rotation=Matrix.CreateFromQuaternion(Quaternion.Slerp(_mapOrientation,Quaternion.Identity,align));
        float radius=new Vector3(_setup.Width,_setup.Height,_setup.Depth).Length()*.5f+.4f;
        var fit=net.Fit(panel);
        var view=Matrix.CreateLookAt(new(0,0,radius*3.3f),Vector3.Zero,Vector3.Up);
        var projection=Matrix.CreatePerspectiveFieldOfView(2*MathF.Atan(MathF.Tan(MathHelper.ToRadians(21))/_world.GlobeZoom),area.Width/(float)area.Height,.1f,100);
        var viewport=new Viewport(area);
        Vector3 Shape(int index,Vector3 point)
        {
            var flat=faces[index].Project(point);
            return Vector3.Transform(Vector3.Transform(new Vector3(flat.X,-flat.Y,0),pose[index])+new Vector3(0,0,d/2*(1-align)),rotation);
        }
        var corners=faces.SelectMany((f,index)=>new[]{new Vector3(f.Center.X-f.Width/2,-f.Center.Y-f.Height/2,0),new Vector3(f.Center.X+f.Width/2,-f.Center.Y+f.Height/2,0),new Vector3(f.Center.X-f.Width/2,-f.Center.Y+f.Height/2,0),new Vector3(f.Center.X+f.Width/2,-f.Center.Y-f.Height/2,0)}.Select(p=>Vector3.Transform(Vector3.Transform(p,pose[index])+new Vector3(0,0,d/2*(1-align)),rotation))).ToArray();
        float extent=Math.Max(corners.Max(p=>Math.Abs(p.X))/(panel.Width*.43f),corners.Max(p=>Math.Abs(p.Y))/(panel.Height*.43f));
        float scale=extent>0?Math.Min(fit.Scale,1/extent):fit.Scale;
        Vector3 Project(int index,Vector3 point)
        {
            var shaped=Shape(index,point);
            var anchor=Vector3.Transform(_world.GlobeAnchor??Vector3.Zero,_world.ViewOrientation(_yaw,_pitch));
            var perspective=viewport.Project(shaped-new Vector3(anchor.X,anchor.Y,0)*(1-align),projection,view,Matrix.Identity);
            perspective.X+=_world.GlobePan.X*area.Width*(1-align);perspective.Y+=_world.GlobePan.Y*area.Height*(1-align);
            var flat=net.RotateScreen(faces[index].Project(point)*fit.Scale+fit.Origin,panel);
            var fitted=new Vector2(panel.Center.X+shaped.X*scale,panel.Center.Y-shaped.Y*scale);
            var screen=Vector2.Lerp(new(perspective.X,perspective.Y),fitted,Ease(unfold*6));
            screen=Vector2.Lerp(screen,flat,align);
            return new(screen,0);
        }
        var terrain=_setup.Terrain;
        GraphicsDevice.BlendState=BlendState.Opaque;GraphicsDevice.DepthStencilState=DepthStencilState.None;
        using var raster=new RasterizerState{CullMode=CullMode.None,ScissorTestEnable=true};GraphicsDevice.RasterizerState=raster;
        _mapEffect.World=Matrix.Identity;_mapEffect.View=Matrix.Identity;
        _mapEffect.Projection=Matrix.CreateOrthographicOffCenter(0,1920,1080,0,0,1);
        Vector3 FaceCenter(int index)
        {
            var normal=CubeNet.Normals[faces[index].Face];
            var cell=_setup.Cells.First(c=>c.Face==faces[index].Face);
            return normal*Vector3.Dot(cell.Center,normal);
        }
        foreach(int index in Enumerable.Range(0,6).OrderBy(i=>Shape(i,FaceCenter(i)).Z))
        {
            var vertices=new List<VertexPositionColor>();
            foreach(var cell in _setup.Cells.Where(c=>c.Face==faces[index].Face))
            {
                const int detail=10;
                for(int y=0;y<detail;y++)for(int x=0;x<detail;x++)
                {
                    var a=cell.Origin+cell.U*(x/(float)detail)+cell.V*(y/(float)detail);var b=a+cell.U/detail;var c=b+cell.V/detail;var e=a+cell.V/detail;
                    var faceNormal=Vector3.TransformNormal(Vector3.UnitZ,pose[index]*rotation);
                    var color=faceNormal.Z<0?new Color(115,119,125):net.TerrainColor(_setup,cell.Id,terrain.Elevation((a+c)/2),x,y);
                    color=net.FocusTerrain(color,Vector2.Zero,cell.Id);
                    foreach(var p in new[]{a,b,c,a,c,e})vertices.Add(new(Project(index,p),color));
                }
            }
            foreach(var pass in _mapEffect.CurrentTechnique.Passes){pass.Apply();GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,vertices.ToArray(),0,vertices.Count/3);}
        }
    }
}

namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const float MapTransitionDuration=3.6f;
    private float _mapTransitionAge=-1;
    private bool _mapUnfold;
    private Quaternion _mapOrientation;
    private BasicEffect _mapEffect;
    private SoundEffect _mapCrack;
    private int _mapSoundStep=-1;
    private bool MapTransitionActive=>_mapTransitionAge>=0;
    private void StartMapTransition()
    {
        if(MapTransitionActive)return;
        _mapUnfold=_statusGlobe;
        if(_mapUnfold)
        {
            int selected=HitGlobeCell(new Point(StatusGlobeArea.Center.X,StatusGlobeArea.Center.Y));
            if(selected<0)selected=_setup.Cells.OrderByDescending(c=>Vector3.Transform(c.Center,WorldPreview.Orientation(_yaw,_pitch)).Z).First().Id;
            _statusCell=selected;_net.SetCenter(_setup,_setup.Cells[selected].Face);_net.ResetView();
            _net.Drag(-_net.Faces[0].Project(_setup.Cells[selected].Center)*_net.Fit(NetPanel).Scale);
        }
        var face=_net.Faces[0];var normal=CubeNet.Normals[face.Face];
        var basis=new Matrix(face.Right.X,face.Right.Y,face.Right.Z,0,face.Up.X,face.Up.Y,face.Up.Z,0,normal.X,normal.Y,normal.Z,0,0,0,0,1);
        _mapOrientation=Quaternion.CreateFromRotationMatrix(basis*WorldPreview.Orientation(_yaw,_pitch));
        _mapTransitionAge=0;_mapSoundStep=-1;_dragging=_mapDragging=false;_globePressedCell=-1;
        _mapEffect??=new BasicEffect(GraphicsDevice){VertexColorEnabled=true};
        if(_mapCrack==null)
        {
            try
            {
                const int rate=22050;var bytes=new byte[rate/5*2];var random=new Random(31415);
                for(int i=0;i<bytes.Length/2;i++)
                {
                    float t=i/(float)(bytes.Length/2);short sample=(short)((random.NextDouble()*2-1)*15000*Math.Pow(1-t,4));
                    bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
                }
                _mapCrack=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            catch(NoAudioHardwareException error){RecordFailure(error,"map_audio_init");}
        }
        UpdateMapTransition(0);
    }
    private void UpdateMapTransition(float elapsed)
    {
        if(!MapTransitionActive)return;
        _mapTransitionAge+=elapsed;
        float progress=MathHelper.Clamp(_mapTransitionAge/MapTransitionDuration,0,1);
        int step=_mapUnfold?Math.Min(4,(int)(progress*6)):(int)(progress*6)-1;
        if(step>=0 && step<5 && step!=_mapSoundStep)
        {
            _mapSoundStep=step;
            try{_mapCrack?.Play(.65f,0,0);}catch(NoAudioHardwareException error){RecordFailure(error,"map_audio_play");}
        }
        if(progress<1)return;
        _statusGlobe=!_mapUnfold;_mapTransitionAge=-1;_inputOutcome="view_changed";
    }
    private void DrawMapTransition()
    {
        float progress=MathHelper.Clamp(_mapTransitionAge/MapTransitionDuration,0,1);
        float unfold=_mapUnfold?progress:1-progress;
        float Ease(float t){t=MathHelper.Clamp(t,0,1);return t*t*(3-2*t);}
        // The back face opens first, staying attached to the right face until its turn.
        int[] order={5,2,3,4,1};var pose=new Matrix[6];pose[0]=Matrix.Identity;
        float Angle(int index)=>MathHelper.PiOver2*(1-Ease(unfold*6-Array.IndexOf(order,index)));
        var faces=_net.Faces;float w=faces[0].Width,h=faces[0].Height,d=faces[1].Width;
        Matrix Hinge(Vector3 pivot,Matrix rotation)=>Matrix.CreateTranslation(-pivot)*rotation*Matrix.CreateTranslation(pivot);
        pose[1]=Hinge(new(w/2,0,0),Matrix.CreateRotationY(Angle(1)));
        pose[2]=Hinge(new(-w/2,0,0),Matrix.CreateRotationY(-Angle(2)));
        pose[3]=Hinge(new(0,h/2,0),Matrix.CreateRotationX(-Angle(3)));
        pose[4]=Hinge(new(0,-h/2,0),Matrix.CreateRotationX(Angle(4)));
        pose[5]=Hinge(new(w/2+d,0,0),Matrix.CreateRotationY(Angle(5)))*pose[1];
        float align=Ease(unfold*6-5);
        var rotation=Matrix.CreateFromQuaternion(Quaternion.Slerp(_mapOrientation,Quaternion.Identity,align));
        float radius=new Vector3(_setup.Width,_setup.Height,_setup.Depth).Length()*.5f+.4f;
        var area=StatusGlobeArea;var fit=_net.Fit(NetPanel);
        var view=Matrix.CreateLookAt(new(0,0,radius*3.3f),Vector3.Zero,Vector3.Up);
        var projection=Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(42),area.Width/(float)area.Height,.1f,100);
        var viewport=new Viewport(area);
        Vector3 Shape(int index,Vector3 point)
        {
            var flat=faces[index].Project(point);
            return Vector3.Transform(Vector3.Transform(new Vector3(flat.X,-flat.Y,0),pose[index])+new Vector3(0,0,d/2*(1-align)),rotation);
        }
        var corners=faces.SelectMany((f,index)=>new[]{new Vector3(f.Center.X-f.Width/2,-f.Center.Y-f.Height/2,0),new Vector3(f.Center.X+f.Width/2,-f.Center.Y+f.Height/2,0),new Vector3(f.Center.X-f.Width/2,-f.Center.Y+f.Height/2,0),new Vector3(f.Center.X+f.Width/2,-f.Center.Y-f.Height/2,0)}.Select(p=>Vector3.Transform(Vector3.Transform(p,pose[index])+new Vector3(0,0,d/2*(1-align)),rotation))).ToArray();
        float extent=Math.Max(corners.Max(p=>Math.Abs(p.X))/ (NetPanel.Width*.43f),corners.Max(p=>Math.Abs(p.Y))/(NetPanel.Height*.43f));
        float scale=extent>0?Math.Min(fit.Scale,1/extent):fit.Scale;
        Vector3 Project(int index,Vector3 point)
        {
            var shaped=Shape(index,point);var perspective=viewport.Project(shaped,projection,view,Matrix.Identity);
            var flat=faces[index].Project(point)*fit.Scale+fit.Origin;
            var fitted=new Vector2(NetPanel.Center.X+shaped.X*scale,NetPanel.Center.Y-shaped.Y*scale);
            var screen=Vector2.Lerp(new(perspective.X,perspective.Y),fitted,Ease(unfold*6));
            screen=Vector2.Lerp(screen,flat,align);
            return new(screen,0);
        }
        var terrain=WorldTerrain.Offset(_setup.WorldSeed);
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
                    var color=WorldTerrain.ColorAt(WorldTerrain.Elevation((a+c)/2,terrain),cell.Normal);
                    var post=_setup.Nodes.At(cell.Id,new Point(x,y));
                    if(!_net.ShowRoutes && post!=null)color=Color.Lerp(color,_setup.OwnerColor(post.Owner),post.Owner==_setup.PlayerSlot?.4f:.16f);
                    foreach(var p in new[]{a,b,c,a,c,e})vertices.Add(new(Project(index,p),color));
                }
            }
            foreach(var pass in _mapEffect.CurrentTechnique.Passes){pass.Apply();GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,vertices.ToArray(),0,vertices.Count/3);}
        }
    }
}

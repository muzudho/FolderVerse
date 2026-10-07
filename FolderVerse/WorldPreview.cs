namespace FolderVerse;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private VertexBuffer _vertices;
    private int _primitiveCount;
    private VertexBuffer _markers;
    private int _markerCount;
    private VertexBuffer _highlight;
    private int _highlightCount;
    public int HighlightedOwner { get; private set; } = -1;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }
    public int Seed { get; private set; }
    public string Kind => ((Width > 1 ? 1 : 0) + (Height > 1 ? 1 : 0) + (Depth > 1 ? 1 : 0)) switch
    {
        0 or 1 => "地棒", 2 => "地盤", _ => "地箱"
    };
    public string GlobeName=>Kind+"儀";
    public static Matrix Orientation(float yaw,float pitch)=>Matrix.CreateRotationX(pitch)*Matrix.CreateRotationY(yaw);

    public WorldPreview(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device) { VertexColorEnabled = true };
        _effect.EnableDefaultLighting();
        _effect.AmbientLightColor = new Vector3(0.48f);
        _effect.SpecularColor = new Vector3(0.12f);
    }

    public void Generate(int width, int height, int depth, int seed)
    {
        if (width < 1 || width > 5 || height < 1 || height > 5 || depth < 1 || depth > 5)
            throw new ArgumentOutOfRangeException(nameof(width));
        _highlight?.Dispose();_highlight=null;_highlightCount=0;HighlightedOwner=-1;
        Width = width; Height = height; Depth = depth; Seed = seed;
        _markers?.Dispose(); _markers = null; _markerCount = 0;
        var mesh = new List<VertexPositionColorNormal>();
        var half = new Vector3(width, height, depth) * 0.5f;
        var random = new SeedRandom(seed);
        var offset = new Vector3(random.Unit(), random.Unit(), random.Unit()) * 100;
        AddFace(mesh, new Vector3(half.X, -half.Y, -half.Z), Vector3.Up * height, Vector3.Backward * depth, Vector3.Right, height, depth, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Up * height, Vector3.Forward * depth, Vector3.Left, height, depth, offset);
        AddFace(mesh, new Vector3(-half.X, half.Y, -half.Z), Vector3.Backward * depth, Vector3.Right * width, Vector3.Up, depth, width, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Forward * depth, Vector3.Right * width, Vector3.Down, depth, width, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Right * width, Vector3.Up * height, Vector3.Backward, width, height, offset);
        AddFace(mesh, new Vector3(half.X, -half.Y, -half.Z), Vector3.Left * width, Vector3.Up * height, Vector3.Forward, width, height, offset);
        _vertices?.Dispose();
        _vertices = new VertexBuffer(_device, VertexPositionColorNormal.VertexDeclaration, mesh.Count, BufferUsage.WriteOnly);
        _vertices.SetData(mesh.ToArray());
        _primitiveCount = mesh.Count / 3;
    }

    public void ShowSetup(WorldSetup setup, bool territories)
    {
        Generate(setup.Width, setup.Height, setup.Depth, setup.WorldSeed);
        if (!territories || setup.Owners.Length == 0) return;
        var mesh = new List<VertexPositionColorNormal>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector3 normal)
        {
            mesh.Add(new(a,color,normal)); mesh.Add(new(b,color,normal)); mesh.Add(new(c,color,normal));
            mesh.Add(new(a,color,normal)); mesh.Add(new(c,color,normal)); mesh.Add(new(d,color,normal));
        }
        foreach(var cell in setup.Cells)
        {
            int owner = setup.Owners[cell.Id]; var corners = cell.Corners;
            for(int edge=0;edge<4;edge++)
            {
                var a=corners[edge]; var b=corners[(edge+1)%4];
                int neighbor=-1;
                foreach(int id in cell.Neighbors)
                {
                    var other=setup.Cells[id].Corners;
                    if(Array.Exists(other,p=>p==a) && Array.Exists(other,p=>p==b)) { neighbor=id; break; }
                }
                if(neighbor>=0 && setup.Owners[neighbor]==owner) continue;
                var inward=Vector3.Normalize(cell.Center-(a+b)*0.5f);
                var lift=cell.Normal*0.014f;
                Quad(a+inward*0.02f+lift,b+inward*0.02f+lift,b+inward*0.065f+lift,a+inward*0.065f+lift,setup.OwnerColor(owner),cell.Normal);
            }
        }
        for(int owner=0;owner<setup.Capitals.Length;owner++)
        {
            if(setup.TerritoryCounts[owner]==0)continue;
            var cell=setup.Cells[setup.Capitals[owner]]; var foot=cell.Center+cell.Normal*0.03f;
            var top=foot+cell.Normal*0.48f;
            Quad(foot-cell.U*0.013f,foot+cell.U*0.013f,top+cell.U*0.013f,top-cell.U*0.013f,new Color(255,241,198),cell.V);
            Quad(top,top+cell.U*0.27f,top+cell.U*0.27f-cell.Normal*0.18f,top-cell.Normal*0.18f,setup.OwnerColor(owner),cell.V);
            Quad(foot-cell.U*0.12f-cell.V*0.12f,foot+cell.U*0.12f-cell.V*0.12f,foot+cell.U*0.12f+cell.V*0.12f,foot-cell.U*0.12f+cell.V*0.12f,setup.OwnerColor(owner),cell.Normal);
        }
        if(mesh.Count==0)return;
        _markers=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _markers.SetData(mesh.ToArray()); _markerCount=mesh.Count/3;
    }
    public void HighlightOwner(WorldSetup setup,int owner)
    {
        if(owner==HighlightedOwner)return;
        _highlight?.Dispose();_highlight=null;_highlightCount=0;HighlightedOwner=owner;
        if(owner<0 || owner>=setup.ActiveCount || setup.Owners.Length==0)return;
        var mesh=new List<VertexPositionColorNormal>();
        foreach(var cell in setup.Cells)
        {
            if(setup.Owners[cell.Id]!=owner)continue;
            var points=cell.Corners;var lift=cell.Normal*0.008f;var color=setup.OwnerColor(owner);
            foreach(int index in new[]{0,1,2,0,2,3})mesh.Add(new(points[index]+lift,color,cell.Normal));
        }
        _highlight=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _highlight.SetData(mesh.ToArray());_highlightCount=mesh.Count/3;
    }
    private static void AddFace(List<VertexPositionColorNormal> mesh, Vector3 origin, Vector3 u, Vector3 v, Vector3 normal, int unitsU, int unitsV, Vector3 offset)
    {
        const int detail = 10;
        int countU = unitsU * detail, countV = unitsV * detail;
        for (int y = 0; y < countV; y++)
        for (int x = 0; x < countU; x++)
        {
            var a = origin + u * (x / (float)countU) + v * (y / (float)countV);
            var b = a + u / countU;
            var c = b + v / countV;
            var d = a + v / countV;
            var center = (a + c) * 0.5f;
            Color color=WorldTerrain.ColorAt(WorldTerrain.Elevation(center,offset),normal);
            if (x % detail == 0 || y % detail == 0) color = Color.Lerp(color, new Color(17, 47, 62), 0.3f);
            mesh.Add(new(a, color, normal)); mesh.Add(new(b, color, normal)); mesh.Add(new(c, color, normal));
            mesh.Add(new(a, color, normal)); mesh.Add(new(c, color, normal)); mesh.Add(new(d, color, normal));
        }
    }

    public void Draw(float yaw, float pitch,float glow=0,bool markers=true,bool globeView=false)
    {
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullNone;
        _effect.World = globeView?GlobeOrientation(yaw,pitch):Orientation(yaw,pitch);
        float radius = new Vector3(Width, Height, Depth).Length() * 0.5f + (_markers == null ? 0 : 0.4f);
        _effect.View = Matrix.CreateLookAt(new Vector3(0, 0, radius * 3.3f), Vector3.Zero, Vector3.Up);
        _effect.Projection = Matrix.CreatePerspectiveFieldOfView(globeView?GlobeFieldOfView:MathHelper.ToRadians(42), _device.Viewport.AspectRatio, 0.1f, 100);
        _device.SetVertexBuffer(_vertices);
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawPrimitives(PrimitiveType.TriangleList, 0, _primitiveCount);
        }
        if(_highlight!=null)
        {
            _device.SetVertexBuffer(_highlight);_effect.LightingEnabled=false;
            // Unlit country fill plus a subtle pulse makes the selected land stand out.
            _effect.DiffuseColor=new Vector3(0.82f+0.18f*(0.5f+0.5f*MathF.Sin(glow*3)));
            foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,_highlightCount);}
            _effect.DiffuseColor=Vector3.One;_effect.LightingEnabled=true;
        }
        if(_markers != null && markers)
        {
            _device.SetVertexBuffer(_markers); _effect.LightingEnabled=false;
            foreach(var pass in _effect.CurrentTechnique.Passes) { pass.Apply(); _device.DrawPrimitives(PrimitiveType.TriangleList,0,_markerCount); }
            _effect.LightingEnabled=true;
        }
    }
    public bool ProjectVisible(Vector3 point,Vector3 normal,float yaw,float pitch,Rectangle area,out Vector2 screen,float normalOffset=0)
    {
        var orientation=GlobeOrientation(yaw,pitch);
        float radius=new Vector3(Width,Height,Depth).Length()*.5f+(_markers==null?0:.4f);
        var camera=new Vector3(0,0,radius*3.3f);
        var transformed=Vector3.Transform(point,orientation);
        var facing=Vector3.TransformNormal(normal,orientation);
        var view=Matrix.CreateLookAt(camera,Vector3.Zero,Vector3.Up);
        var projection=Matrix.CreatePerspectiveFieldOfView(GlobeFieldOfView,area.Width/(float)area.Height,.1f,100);
        var viewport=new Viewport(area);var projected=viewport.Project(point+normal*normalOffset,projection,view,orientation);
        screen=new(projected.X,projected.Y);
        return Vector3.Dot(facing,camera-transformed)>0 && projected.Z>=0 && projected.Z<=1;
    }
    public void Dispose() { _vertices?.Dispose(); _markers?.Dispose();_highlight?.Dispose(); _effect.Dispose(); }
}

namespace FolderVerse;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class WorldPreview : IDisposable
{
    private const float RegionSeamInset=.018f;
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
        0 => "地賽", 1 => "地棒", 2 => "地盤", _ => "地箱"
    };
    public string GlobeName=>Kind+"儀";
    public Quaternion? ManualOrientation {get;set;}
    public Matrix ViewOrientation(float yaw,float pitch)=>ManualOrientation.HasValue?Matrix.CreateFromQuaternion(ManualOrientation.Value):Orientation(yaw,pitch);
    public Matrix GlobeRock {get;set;}=Matrix.Identity;
    public Matrix DisplayOrientation(float yaw,float pitch)=>ViewOrientation(yaw,pitch)*GlobeRock;
    public static Matrix Orientation(float yaw,float pitch)=>Matrix.CreateRotationX(pitch)*Matrix.CreateRotationY(yaw);
    private static readonly Quaternion[] FaceTourPoses={
        Quaternion.Identity,
        Quaternion.CreateFromAxisAngle(Vector3.Up,-MathHelper.PiOver2),
        Quaternion.CreateFromAxisAngle(Vector3.Right,MathHelper.PiOver2),
        Quaternion.CreateFromAxisAngle(Vector3.Up,MathHelper.Pi),
        Quaternion.CreateFromAxisAngle(Vector3.Up,MathHelper.PiOver2),
        Quaternion.CreateFromAxisAngle(Vector3.Right,-MathHelper.PiOver2)
    };
    public static Quaternion FaceTour(Quaternion start,double fraction)
    {
        float progress=(float)Math.Clamp(fraction,0,1)*(FaceTourPoses.Length+1);
        int segment=Math.Min((int)progress,FaceTourPoses.Length);
        var from=segment==0?start:FaceTourPoses[segment-1];
        var to=segment==FaceTourPoses.Length?start:FaceTourPoses[segment];
        return Quaternion.Slerp(from,to,MathHelper.SmoothStep(0,1,progress-segment));
    }

    public static Quaternion OpeningFaceTour(double fraction)
    {
        float progress=(float)Math.Clamp(fraction,0,1)*5;
        int step=Math.Min((int)progress,4);
        float turn=MathHelper.PiOver2*MathHelper.SmoothStep(0,1,progress-step);
        if(step==0)return Quaternion.CreateFromAxisAngle(Vector3.Right,turn);

        var pose=Matrix.CreateRotationX(MathHelper.PiOver2)*
            Matrix.CreateRotationY((step-1)*MathHelper.PiOver2+(step<4?turn:0));
        // Z is the viewing axis. Rotating around it would roll the same
        // face in place, so pitch once more to bring the last face forward.
        if(step==4)pose*=Matrix.CreateRotationX(turn);
        return Quaternion.CreateFromRotationMatrix(pose);
    }

    public WorldPreview(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device) { VertexColorEnabled = true };
        _effect.EnableDefaultLighting();
        _effect.AmbientLightColor = new Vector3(0.48f);
        _effect.SpecularColor = new Vector3(0.12f);
    }

    private int _polarAxis=1;
    public void Generate(int width, int height, int depth, int seed,int polarAxis=1,float elevationShift=0)
        =>Generate(width,height,depth,seed,polarAxis,elevationShift,null);

    internal void ShowOpeningPlanet(WorldSetup setup)
    {
        Generate(setup.Width,setup.Height,setup.Depth,setup.WorldSeed,setup.PolarAxis,setup.Terrain.ElevationShift,setup);
    }

    private void Generate(int width,int height,int depth,int seed,int polarAxis,float elevationShift,WorldSetup opening)
    {
        _polarAxis=polarAxis;
        OpeningMapleLoads=0;
        // Rods can reach nine cells and slabs seven, in any axis orientation.
        if (width < 1 || width > 9)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1 || height > 9)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (depth < 1 || depth > 9)
            throw new ArgumentOutOfRangeException(nameof(depth));
        _whirlpoolMesh?.Dispose();_whirlpoolMesh=null;
        _highlight?.Dispose();_highlight=null;_highlightCount=0;HighlightedOwner=-1;
        Width = width; Height = height; Depth = depth; Seed = seed;
        _markers?.Dispose(); _markers = null; _markerCount = 0;
        var mesh = new List<VertexPositionColorNormal>();
        var half = new Vector3(width, height, depth) * 0.5f;
        var offset = new TerrainSampler(WorldTerrain.Offset(seed),elevationShift);
        AddFace(mesh, new Vector3(half.X, -half.Y, -half.Z), Vector3.Up * height, Vector3.Backward * depth, Vector3.Right, height, depth, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Up * height, Vector3.Forward * depth, Vector3.Left, height, depth, offset);
        AddFace(mesh, new Vector3(-half.X, half.Y, -half.Z), Vector3.Backward * depth, Vector3.Right * width, Vector3.Up, depth, width, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Forward * depth, Vector3.Right * width, Vector3.Down, depth, width, offset);
        AddFace(mesh, new Vector3(-half.X, -half.Y, half.Z), Vector3.Right * width, Vector3.Up * height, Vector3.Backward, width, height, offset);
        AddFace(mesh, new Vector3(half.X, -half.Y, -half.Z), Vector3.Left * width, Vector3.Up * height, Vector3.Forward, width, height, offset);
        if(opening!=null)AddPreviewMaples(mesh,opening,offset);
        _vertices?.Dispose();
        _vertices = new VertexBuffer(_device, VertexPositionColorNormal.VertexDeclaration, mesh.Count, BufferUsage.WriteOnly);
        _vertices.SetData(mesh.ToArray());
        _primitiveCount = mesh.Count / 3;
    }

    public void ShowSetup(WorldSetup setup, bool territories)
    {
        if(CountryColors && territories)ShowLandConquest(setup,true);
        else {
            Generate(setup.Width, setup.Height, setup.Depth, setup.WorldSeed,setup.PolarAxis,setup.Terrain.ElevationShift);
            if(setup.Owners.Length==setup.Cells.Length)BuildWhirlpoolMesh(setup);
        }
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
                var lift=cell.Normal*(WorldTerrain.MaxRelief+.014f);
                Quad(a+inward*0.02f+lift,b+inward*0.02f+lift,b+inward*0.065f+lift,a+inward*0.065f+lift,setup.OwnerColor(owner),cell.Normal);
            }
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
            var points=cell.Corners;var lift=cell.Normal*(WorldTerrain.MaxRelief+.008f);var color=setup.OwnerColor(owner);
            foreach(int index in new[]{0,1,2,0,2,3})mesh.Add(new(points[index]+lift,color,cell.Normal));
        }
        // During rights selection, no land belongs to the hovered ruler yet.
        if(mesh.Count==0)return;
        _highlight=new VertexBuffer(_device,VertexPositionColorNormal.VertexDeclaration,mesh.Count,BufferUsage.WriteOnly);
        _highlight.SetData(mesh.ToArray());_highlightCount=mesh.Count/3;
    }
    private void AddFace(List<VertexPositionColorNormal> mesh, Vector3 origin, Vector3 u, Vector3 v, Vector3 normal, int unitsU, int unitsV, TerrainSampler offset,
        Func<float,int,int,Color> paint=null,float brightness=1f)
    {
        const int detail = 10;
        int countU = unitsU * detail, countV = unitsV * detail;
        var stepU=u/countU;
        var stepV=v/countV;
        var axisU=Vector3.Normalize(u);
        var axisV=Vector3.Normalize(v);
        var half=new Vector3(Width,Height,Depth)*.5f;
        var heights=new float[countU+2,countV+2];
        var reliefs=new float[countU+2,countV+2];
        var valleys=new bool[countU+2,countV+2];
        for (int y=-1;y<=countV;y++)
        for (int x=-1;x<=countU;x++)
        {
            var point=origin+stepU*(x+.5f)+stepV*(y+.5f);
            bool inside=MathF.Abs(point.X)<=half.X+.001f && MathF.Abs(point.Y)<=half.Y+.001f && MathF.Abs(point.Z)<=half.Z+.001f;
            if(!inside)continue;
            float height=offset.Elevation(point);
            bool valley=WorldTerrain.IsValley(offset,point,axisU,axisV,half,height);
            heights[x+1,y+1]=height;
            valleys[x+1,y+1]=valley;
            reliefs[x+1,y+1]=WorldTerrain.Relief(height,valley);
        }
        for (int y = 0; y < countV; y++)
        for (int x = 0; x < countU; x++)
        {
            float height=heights[x+1,y+1];
            float relief=reliefs[x+1,y+1];
            var a=origin+stepU*x+stepV*y+normal*relief;
            var b=a+stepU;var c=b+stepV;var d=a+stepV;
            Color color=paint?.Invoke(height,x,y)??WorldTerrain.ColorAt(height,normal,_polarAxis);
            if(valleys[x+1,y+1])color=WorldTerrain.ValleyColor(color);
            // Each region edge contributes half of the recessed seam.
            var left=x%detail==0?axisU*RegionSeamInset:Vector3.Zero;
            var right=x%detail==detail-1?axisU*RegionSeamInset:Vector3.Zero;
            var bottom=y%detail==0?axisV*RegionSeamInset:Vector3.Zero;
            var top=y%detail==detail-1?axisV*RegionSeamInset:Vector3.Zero;
            if(left!=Vector3.Zero || right!=Vector3.Zero || bottom!=Vector3.Zero || top!=Vector3.Zero)
            {
                var floor=normal*(-.014f-relief);
                Quad(a+floor,b+floor,c+floor,d+floor,Dim(new Color(33,61,69)),normal);
            }
            a+=left+bottom;b+=bottom-right;c-=right+top;d+=left-top;
            Quad(a,b,c,d,Dim(color),normal);
            Wall(a,b,bottom,reliefs[x+1,y],-axisV);
            Wall(b,c,right,reliefs[x+2,y+1],axisU);
            Wall(c,d,top,reliefs[x+1,y+2],axisV);
            Wall(d,a,left,reliefs[x,y+1],-axisU);

            void Wall(Vector3 start,Vector3 end,Vector3 edgeInset,float neighbor,Vector3 outward)
            {
                float drop=edgeInset!=Vector3.Zero?relief+.014f:relief-neighbor;
                if(drop<=0)return;
                var down=normal*drop;
                Quad(start,end,end-down,start-down,Dim(Color.Lerp(color,new Color(49,43,38),.28f)),outward);
            }
        }
        Color Dim(Color color) => brightness == 1f ? color : Color.Lerp(Color.Black,color,brightness);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color,Vector3 facing)
        {
            mesh.Add(new(a,color,facing));mesh.Add(new(b,color,facing));mesh.Add(new(c,color,facing));
            mesh.Add(new(a,color,facing));mesh.Add(new(c,color,facing));mesh.Add(new(d,color,facing));
        }
    }

    public void Draw(float yaw, float pitch,float glow=0,bool markers=true,bool globeView=false,Rectangle? globeFrame=null,
        IEnumerable<SurfaceCell> landSelection=null)
    {
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullNone;
        _effect.World = globeView?GlobeOrientation(yaw,pitch):ViewOrientation(yaw,pitch);
        float radius = new Vector3(Width, Height, Depth).Length() * 0.5f + (_markers == null ? 0 : 0.4f);
        _effect.View = Matrix.CreateLookAt(new Vector3(0, 0, radius * 3.3f), Vector3.Zero, Vector3.Up);
        _effect.Projection = globeView?GlobeProjection(_device.Viewport.AspectRatio):Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(42), _device.Viewport.AspectRatio, 0.1f, 100);
        if(globeView && globeFrame.HasValue){
            // Preserve the original framing while allowing geometry outside it to fill the viewport.
            var f=globeFrame.Value;var v=_device.Viewport;
            _effect.Projection=GlobeProjection(f.Width/(float)f.Height)
                *Matrix.CreateScale(f.Width/(float)v.Width,f.Height/(float)v.Height,1)
                *Matrix.CreateTranslation((f.Center.X-v.X)*2f/v.Width-1,1-(f.Center.Y-v.Y)*2f/v.Height,0);
        }
        _device.SetVertexBuffer(_vertices);
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawPrimitives(PrimitiveType.TriangleList, 0, _primitiveCount);
        }
        DrawWhirlpoolMesh();
        if(_highlight!=null)
        {
            _device.SetVertexBuffer(_highlight);_effect.LightingEnabled=false;
            // Unlit country fill plus a subtle pulse makes the selected land stand out.
            _effect.DiffuseColor=new Vector3(0.82f+0.18f*(0.5f+0.5f*MathF.Sin(glow*3)));
            foreach(var pass in _effect.CurrentTechnique.Passes){pass.Apply();_device.DrawPrimitives(PrimitiveType.TriangleList,0,_highlightCount);}
            _effect.DiffuseColor=Vector3.One;_effect.LightingEnabled=true;
        }
        if(globeView)DrawCellSelection(glow);
        if(landSelection!=null)DrawLandBacklight(landSelection,glow);
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
        var projection=GlobeProjection(area.Width/(float)area.Height);
        var viewport=new Viewport(area);var projected=viewport.Project(point+normal*normalOffset,projection,view,orientation);
        screen=new(projected.X,projected.Y);
        return Vector3.Dot(facing,camera-transformed)>0 && projected.Z>=0 && projected.Z<=1;
    }
    public void Dispose() { _whirlpoolMesh?.Dispose();_vertices?.Dispose(); _markers?.Dispose();_highlight?.Dispose(); _effect.Dispose(); }
}

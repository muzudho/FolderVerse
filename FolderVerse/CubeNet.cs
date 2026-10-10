namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record NetFace(int Face,Vector3 Right,Vector3 Up,Vector2 Center,float Width,float Height)
{
    public Vector2 Project(Vector3 point)=>Center+new Vector2(Vector3.Dot(point,Right),-Vector3.Dot(point,Up));
}
public sealed partial class CubeNet
{
    public int CenterFace { get; private set; }
    public int Rotation { get; private set; }
    public float Zoom { get; private set; }=1;
    public Vector2 Pan { get; private set; }
    public void ResetView(){Zoom=1;Pan=Vector2.Zero;}
    public void FitAll(Rectangle panel,float zoom=1)
    {
        float left=Faces.Min(f=>f.Center.X-f.Width/2),right=Faces.Max(f=>f.Center.X+f.Width/2);
        float top=Faces.Min(f=>f.Center.Y-f.Height/2),bottom=Faces.Max(f=>f.Center.Y+f.Height/2);
        ResetView();float scale=Math.Min(panel.Width/(right-left),panel.Height/(bottom-top))*.94f;
        Zoom=scale/Fit(panel).Scale*zoom;Pan=-new Vector2((left+right)/2,(top+bottom)/2)*scale*zoom;
    }
    public void Drag(Vector2 delta){Pan+=delta;}
    public void ZoomAt(Rectangle panel,Point pointer,int wheel)
    {
        var before=Fit(panel);float old=Zoom;
        Zoom=MathHelper.Clamp(Zoom*MathF.Pow(1.2f,wheel/120f),0.5f,64);
        var target=new Vector2(panel.Center.X,panel.Center.Y);
        Pan+=(target-before.Origin)*(1-Zoom/old);
    }
    public NetFace[] Faces { get; private set; }=Array.Empty<NetFace>();
    public static readonly Vector3[] Normals={Vector3.Right,Vector3.Left,Vector3.Up,Vector3.Down,Vector3.Backward,Vector3.Forward};
    public static readonly string[] FaceNames={"東面","西面","北極面","南極面","正面","背面"};
    public void Home(WorldSetup setup)
    {
        CancelAnimation();
        ResetView();
        int capitalFace=setup.Cells[setup.TerritoryCounts[setup.PlayerSlot]==0?setup.ConquerorLocations[setup.PlayerSlot]:setup.Capitals[setup.PlayerSlot]].Face;
        CenterFace=Enumerable.Range(0,6).OrderByDescending(f=>setup.Cells.Count(c=>c.Face==f && setup.Owners[c.Id]==setup.PlayerSlot)).ThenBy(f=>f==capitalFace?0:1).ThenBy(f=>f).First();
        Rotation=0;Build(setup);
    }
    public void SetCenter(WorldSetup setup,int face){CancelAnimation();CenterFace=face;Rotation=0;Build(setup);}
    public void Turn(WorldSetup setup,int direction){CancelAnimation();Rotation=(Rotation+direction+4)%4;Build(setup);}
    public void Move(WorldSetup setup,int direction)
    {
        CancelAnimation();
        var center=Faces[0];var normal=direction switch{0=>center.Up,1=>center.Right,2=>-center.Up,_=>-center.Right};
        CenterFace=Array.IndexOf(Normals,normal);Build(setup);
    }
    private void Build(WorldSetup setup)
    {
        Vector3 n=Normals[CenterFace],up=Math.Abs(n.Y)>0.5f?(n.Y>0?Vector3.Backward:Vector3.Forward):Vector3.Up;
        Vector3 right=Vector3.Cross(up,n);
        for(int i=0;i<Rotation;i++){var old=right;right=up;up=-old;}
        var dimensions=new Vector3(setup.Width,setup.Height,setup.Depth);
        float Length(Vector3 axis)=>Vector3.Dot(new Vector3(Math.Abs(axis.X),Math.Abs(axis.Y),Math.Abs(axis.Z)),dimensions);
        float w=Length(right),h=Length(up),d=Length(n);
        Faces=new[]{
            new NetFace(CenterFace,right,up,Vector2.Zero,w,h),
            new NetFace(Array.IndexOf(Normals,right),-n,up,new((w+d)/2,0),d,h),
            new NetFace(Array.IndexOf(Normals,-right),n,up,new(-(w+d)/2,0),d,h),
            new NetFace(Array.IndexOf(Normals,up),right,-n,new(0,-(h+d)/2),w,d),
            new NetFace(Array.IndexOf(Normals,-up),right,n,new(0,(h+d)/2),w,d),
            new NetFace(Array.IndexOf(Normals,-n),-right,up,new(w+d,0),w,h)
        };
    }
    public (float Scale,Vector2 Origin) Fit(Rectangle panel)
    {
        float left=Faces.Min(f=>f.Center.X-f.Width/2),right=Faces.Max(f=>f.Center.X+f.Width/2);
        float top=Faces.Min(f=>f.Center.Y-f.Height/2),bottom=Faces.Max(f=>f.Center.Y+f.Height/2);
        float scale=Math.Min(panel.Width/(2*Math.Max(Math.Abs(left),Math.Abs(right))),panel.Height/(2*Math.Max(Math.Abs(top),Math.Abs(bottom))));
        return (scale*0.86f*Zoom,new Vector2(panel.Center.X,panel.Center.Y)+Pan);
    }
    public Rectangle CellBounds(SurfaceCell cell,Rectangle panel)
    {
        var face=Faces.First(f=>f.Face==cell.Face);var fit=Fit(panel);
        var points=cell.Corners.Select(face.Project).ToArray();
        float left=points.Min(p=>p.X),top=points.Min(p=>p.Y),right=points.Max(p=>p.X),bottom=points.Max(p=>p.Y);
        int x=(int)MathF.Round(fit.Origin.X+left*fit.Scale),y=(int)MathF.Round(fit.Origin.Y+top*fit.Scale);
        return new(x,y,(int)MathF.Round(fit.Origin.X+right*fit.Scale)-x,(int)MathF.Round(fit.Origin.Y+bottom*fit.Scale)-y);
    }
    public int Hit(WorldSetup setup,Rectangle panel,Point point)
    {
        if(!panel.Contains(point))return -1;
        foreach(var cell in setup.Cells)if(CellBounds(cell,panel).Contains(point))return cell.Id;
        return -1;
    }
    public static string Seam(int a,int b)
    {
        int index=0;
        for(int i=0;i<6;i++)for(int j=i+1;j<6;j++)
        {
            if(Vector3.Dot(Normals[i],Normals[j])!=0)continue;
            if(i==Math.Min(a,b) && j==Math.Max(a,b))return ((char)('A'+index)).ToString();
            index++;
        }
        return "";
    }
    public void Draw(UiPainter ui,WorldSetup setup,Rectangle panel,int focused,float pulse)
    {
        _robotBadges.Clear();
        if(IsAnimating){DrawAnimation(ui,setup,panel,pulse);return;}
        var fit=Fit(panel);var offset=WorldTerrain.Offset(setup.WorldSeed);
        foreach(var cell in setup.Cells)
        {
            var face=Faces.First(f=>f.Face==cell.Face);var rect=CellBounds(cell,panel);
            if(!rect.Intersects(MapViewport(panel)))continue;
            const int detail=10;
            for(int y=0;y<detail;y++)for(int x=0;x<detail;x++)
            {
                var p=cell.Origin+cell.U*((x+0.5f)/detail)+cell.V*((y+0.5f)/detail);
                // Project each little terrain pixel so map orientation matches the unfolded face.
                var a=face.Project(p);int px=(int)(fit.Origin.X+a.X*fit.Scale),py=(int)(fit.Origin.Y+a.Y*fit.Scale);
                int size=(int)Math.Ceiling(fit.Scale/detail)+1;
                float elevation=WorldTerrain.Elevation(p,offset);
                Color terrain=WorldTerrain.ColorAt(elevation,cell.Normal);
                var post=setup.Nodes.At(cell.Id,new Point(x,y));
                if(!ShowRoutes && post!=null)terrain=Color.Lerp(terrain,setup.OwnerColor(post.Owner),post.Owner==setup.PlayerSlot?.4f:.16f);
                ui.Box(new(px-size/2,py-size/2,size,size),terrain);
            }
            int owner=setup.Nodes.FullOwner(cell.Id);Color color=owner<0?new Color(170,181,191):setup.OwnerColor(owner);
            int thickness=owner==setup.PlayerSlot?3:2;
            foreach(int edge in ShowRoutes?Enumerable.Empty<int>():Enumerable.Range(0,4))
            {
                var a=cell.Corners[edge];var b=cell.Corners[(edge+1)%4];
                if(!ShowRoutes)
                {
                    var ga=face.Project(a)*fit.Scale+fit.Origin;var gb=face.Project(b)*fit.Scale+fit.Origin;var gd=gb-ga;
                    ui.Tile((ga+gb)/2,new Vector2(gd.Length(),1),MathF.Atan2(gd.Y,gd.X),new Color(20,42,52));
                }
                var neighbor=cell.Neighbors.First(id=>Array.Exists(setup.Cells[id].Corners,p=>p==a) && Array.Exists(setup.Cells[id].Corners,p=>p==b));
                if(owner>=0 && setup.Nodes.FullOwner(neighbor)==owner && setup.Cells[neighbor].Face==cell.Face)continue;
                var pa=face.Project(a)*fit.Scale+fit.Origin;var pb=face.Project(b)*fit.Scale+fit.Origin;
                ui.Box(new((int)Math.Min(pa.X,pb.X)-thickness/2,(int)Math.Min(pa.Y,pb.Y)-thickness/2,Math.Max(thickness,(int)Math.Abs(pa.X-pb.X)),Math.Max(thickness,(int)Math.Abs(pa.Y-pb.Y))),color);
            }
            DrawCellRoutes(ui,setup,cell.Id,p=>face.Project(p)*fit.Scale+fit.Origin,pulse);
            if(focused==cell.Id)
            {
                ui.Box(new(rect.Left,rect.Top,rect.Width,3),Color.White);ui.Box(new(rect.Left,rect.Bottom-3,rect.Width,3),Color.White);
                ui.Box(new(rect.Left,rect.Top,3,rect.Height),Color.White);ui.Box(new(rect.Right-3,rect.Top,3,rect.Height),Color.White);
            }
        }
        DrawMapSymbols(ui,setup,p=>
        {
            var face=Faces.First(f=>f.Face==p.Cell.Face);
            return (face.Project(p.A)*fit.Scale+fit.Origin,face.Project(p.B)*fit.Scale+fit.Origin);
        });
        foreach(int ruler in Enumerable.Range(0,setup.ActiveCount).OrderBy(r=>r==setup.PlayerSlot?1:0))
        {
        if(!ShowBatteries || !setup.Relations.Powered[ruler])continue;
        var location=CellBounds(setup.Cells[setup.ConquerorLocations[ruler]],panel);
        var currentFace=Faces.First(f=>f.Face==setup.Cells[setup.ConquerorLocations[ruler]].Face);
        var position=currentFace.Project(setup.Routes.Position(setup.ConquerorLocations[ruler],setup.ConquerorPoints[ruler]))*fit.Scale+fit.Origin;
        if(ruler!=setup.PlayerSlot)
        {
            int x=(int)position.X-7,y=(int)position.Y-4;
            var tint=setup.OwnerColor(ruler);
            ui.Box(new(x-2,y-2,17,12),new Color(12,27,36));ui.Box(new(x,y,12,8),tint);ui.Box(new(x+12,y+2,2,4),tint);
            continue;
        }
        int bx=(int)position.X-13,by=(int)position.Y-8;
        var battery=Color.Lerp(new Color(255,240,119),Color.White,(MathF.Sin(pulse*4)+1)/2);
        ui.Box(new(bx-3,by-3,32,22),new Color(12,27,36));
        ui.Box(new(bx,by,25,16),battery);ui.Box(new(bx+25,by+4,4,8),battery);
        for(int i=0;i<3;i++)ui.Box(new(bx+3+i*7,by+3,5,10),new Color(35,112,70));
        }
        DrawRobotLayer(ui,setup,panel,node=>NodePosition(setup,node,panel),fit.Scale);
        DrawHoveredEdge(ui,panel);
        DrawSeamLabels(ui,panel);
    }
}

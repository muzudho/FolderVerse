namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record NetFace(int Face,Vector3 Right,Vector3 Up,Vector2 Center,float Width,float Height)
{
    public Vector2 Project(Vector3 point)=>Center+new Vector2(Vector3.Dot(point,Right),-Vector3.Dot(point,Up));
}
public sealed class CubeNet
{
    public int CenterFace { get; private set; }
    public int Rotation { get; private set; }
    public float Zoom { get; private set; }=1;
    public Vector2 Pan { get; private set; }
    public void ResetView(){Zoom=1;Pan=Vector2.Zero;}
    public void Drag(Vector2 delta){Pan+=delta;}
    public void ZoomAt(Rectangle panel,Point pointer,int wheel)
    {
        var before=Fit(panel);float old=Zoom;
        Zoom=MathHelper.Clamp(Zoom*MathF.Pow(1.2f,wheel/120f),0.5f,8);
        var target=new Vector2(pointer.X,pointer.Y);
        Pan+=(target-before.Origin)*(1-Zoom/old);
    }
    public NetFace[] Faces { get; private set; }=Array.Empty<NetFace>();
    public static readonly Vector3[] Normals={Vector3.Right,Vector3.Left,Vector3.Up,Vector3.Down,Vector3.Backward,Vector3.Forward};
    public static readonly string[] FaceNames={"東面","西面","北極面","南極面","正面","背面"};
    public void Home(WorldSetup setup)
    {
        ResetView();
        int capitalFace=setup.Cells[setup.Capitals[setup.PlayerSlot]].Face;
        CenterFace=Enumerable.Range(0,6).OrderByDescending(f=>setup.Cells.Count(c=>c.Face==f && setup.Owners[c.Id]==setup.PlayerSlot)).ThenBy(f=>f==capitalFace?0:1).ThenBy(f=>f).First();
        Rotation=0;Build(setup);
    }
    public void SetCenter(WorldSetup setup,int face){CenterFace=face;Rotation=0;Build(setup);}
    public void Turn(WorldSetup setup,int direction){Rotation=(Rotation+direction+4)%4;Build(setup);}
    public void Move(WorldSetup setup,int direction)
    {
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
        var fit=Fit(panel);var offset=WorldTerrain.Offset(setup.WorldSeed);
        foreach(var cell in setup.Cells)
        {
            var face=Faces.First(f=>f.Face==cell.Face);var rect=CellBounds(cell,panel);
            const int detail=10;
            for(int y=0;y<detail;y++)for(int x=0;x<detail;x++)
            {
                var p=cell.Origin+cell.U*((x+0.5f)/detail)+cell.V*((y+0.5f)/detail);
                // Project each little terrain pixel so map orientation matches the unfolded face.
                var a=face.Project(p);int px=(int)(fit.Origin.X+a.X*fit.Scale),py=(int)(fit.Origin.Y+a.Y*fit.Scale);
                int size=(int)Math.Ceiling(fit.Scale/detail)+1;
                float elevation=WorldTerrain.Elevation(p,offset);
                Color terrain=(cell.Face==2 || cell.Face==3) && elevation>=0.52f?new Color(245,249,255):WorldTerrain.ColorAt(elevation);
                ui.Box(new(px-size/2,py-size/2,size,size),terrain);
            }
            int owner=setup.Owners[cell.Id];Color color=setup.OwnerColor(owner);
            ui.Box(rect,color*(owner==setup.PlayerSlot?0.45f+0.10f*MathF.Sin(pulse*2):0.16f));
            // Full-cell country color under a translucent terrain overlay highlights the player's country.
            int thickness=owner==setup.PlayerSlot?3:2;
            foreach(int edge in Enumerable.Range(0,4))
            {
                var a=cell.Corners[edge];var b=cell.Corners[(edge+1)%4];
                var neighbor=cell.Neighbors.First(id=>Array.Exists(setup.Cells[id].Corners,p=>p==a) && Array.Exists(setup.Cells[id].Corners,p=>p==b));
                if(setup.Owners[neighbor]==owner && setup.Cells[neighbor].Face==cell.Face)continue;
                var pa=face.Project(a)*fit.Scale+fit.Origin;var pb=face.Project(b)*fit.Scale+fit.Origin;
                ui.Box(new((int)Math.Min(pa.X,pb.X)-thickness/2,(int)Math.Min(pa.Y,pb.Y)-thickness/2,Math.Max(thickness,(int)Math.Abs(pa.X-pb.X)),Math.Max(thickness,(int)Math.Abs(pa.Y-pb.Y))),color);
            }
            int capital=Array.IndexOf(setup.Capitals,cell.Id);
            if(capital>=0)
            {
                int cx=rect.Center.X,cy=rect.Center.Y;
                ui.Box(new(cx-1,cy-9,2,18),new(255,242,206));ui.Box(new(cx+1,cy-9,Math.Max(7,rect.Width/5),7),setup.OwnerColor(capital));
            }
            if(focused==cell.Id)
            {
                ui.Box(new(rect.Left,rect.Top,rect.Width,3),Color.White);ui.Box(new(rect.Left,rect.Bottom-3,rect.Width,3),Color.White);
                ui.Box(new(rect.Left,rect.Top,3,rect.Height),Color.White);ui.Box(new(rect.Right-3,rect.Top,3,rect.Height),Color.White);
            }
        }
        foreach(var face in Faces)
        {
            var center=face.Center*fit.Scale+fit.Origin;
            var bounds=new Rectangle((int)(center.X-face.Width*fit.Scale/2),(int)(center.Y-face.Height*fit.Scale/2),(int)(face.Width*fit.Scale),(int)(face.Height*fit.Scale));
            Vector3[] directions={face.Up,face.Right,-face.Up,-face.Right};
            for(int side=0;side<4;side++)
            {
                int other=Array.IndexOf(Normals,directions[side]);
                var neighbor=Faces.First(f=>f.Face==other);
                var expected=face.Center+(side switch {0=>new Vector2(0,-(face.Height+neighbor.Height)/2),1=>new Vector2((face.Width+neighbor.Width)/2,0),2=>new Vector2(0,(face.Height+neighbor.Height)/2),_=>new Vector2(-(face.Width+neighbor.Width)/2,0)});
                if(Vector2.DistanceSquared(expected,neighbor.Center)<0.001f)continue;
                Vector2 at=center+(side switch{0=>new(0,-bounds.Height/2f),1=>new(bounds.Width/2f,0),2=>new(0,bounds.Height/2f),_=>new(-bounds.Width/2f,0)});
                at+=side switch {0=>new Vector2(0,-38),1=>new Vector2(38,0),2=>new Vector2(0,38),_=>new Vector2(-38,0)};
                ui.Text(Seam(face.Face,other),at-new Vector2(6,11),0.35f,new(255,230,158));
            }
        }
        // Coordinates sit beyond exposed edges, never on top of terrain.
        foreach(var cell in setup.Cells)
        {
            var rect=CellBounds(cell,panel);var face=Faces.First(f=>f.Face==cell.Face);
            foreach(int edge in Enumerable.Range(0,4))
            {
                var a=cell.Corners[edge];var b=cell.Corners[(edge+1)%4];
                int other=cell.Neighbors.First(id=>Array.Exists(setup.Cells[id].Corners,p=>p==a) && Array.Exists(setup.Cells[id].Corners,p=>p==b));
                if(setup.Cells[other].Face==cell.Face)continue;
                var otherFace=Faces.First(f=>f.Face==setup.Cells[other].Face);
                if(Vector2.DistanceSquared(face.Project(a),otherFace.Project(a))<.001f && Vector2.DistanceSquared(face.Project(b),otherFace.Project(b))<.001f)continue;
                var midpoint=(face.Project(a)+face.Project(b))/2*fit.Scale+fit.Origin;
                var away=Vector2.Normalize(midpoint-new Vector2(rect.Center.X,rect.Center.Y));
                var outside=midpoint+away*16;
                var coordinate=WorldCoordinates.At(setup,cell);
                string label=cell.Face==2 || cell.Face==3?$"{coordinate.X},{coordinate.Y}":Math.Abs(Vector3.Dot(b-a,Vector3.Up))>.5f?coordinate.Y.ToString():coordinate.X.ToString();
                ui.Center(label,new((int)outside.X-24,(int)outside.Y-8,48,16),0.27f,new Color(213,232,239));
            }
        }
        var location=CellBounds(setup.Cells[setup.ConquerorLocations[setup.PlayerSlot]],panel);
        int bx=location.Center.X-13,by=location.Center.Y-17;
        var battery=Color.Lerp(new Color(255,240,119),Color.White,(MathF.Sin(pulse*4)+1)/2);
        ui.Box(new(bx-3,by-3,32,22),new Color(12,27,36));
        ui.Box(new(bx,by,25,16),battery);ui.Box(new(bx+25,by+4,4,8),battery);
        for(int i=0;i<3;i++)ui.Box(new(bx+3+i*7,by+3,5,10),new Color(35,112,70));
    }
}

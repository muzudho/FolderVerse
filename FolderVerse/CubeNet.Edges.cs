namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class CubeNet
{
    public int HoveredFace {get;private set;}=-1;
    public int HoveredSide {get;private set;}=-1;
    public bool IsAnimating=>_animationFrom!=null;
    private NetFace[] _animationFrom,_animationTo;
    private HashSet<int> _movingFaces;
    private float _animationProgress,_animationAngle;
    private Vector2 _oldPivot,_newPivot;
    private static Vector2 RotatePoint(Vector2 p,float angle)=>new(p.X*MathF.Cos(angle)-p.Y*MathF.Sin(angle),p.X*MathF.Sin(angle)+p.Y*MathF.Cos(angle));
    public void CancelAnimation()
    {
        if(_animationTo!=null)Faces=_animationTo;
        _animationFrom=_animationTo=null;HoveredFace=HoveredSide=-1;
    }
    public void UpdateAnimation(float elapsed)
    {
        if(!IsAnimating)return;
        _animationProgress=Math.Min(1,_animationProgress+elapsed/.85f);
        if(_animationProgress>=1)CancelAnimation();
    }
    private bool Joined(NetFace a,NetFace b)
    {
        if(Vector3.Dot(Normals[a.Face],Normals[b.Face])!=0)return false;
        var normal=Normals[b.Face];
        Vector2 delta=b.Center-a.Center;
        Vector2 axis=new(Vector3.Dot(normal,a.Right),-Vector3.Dot(normal,a.Up));
        float distance=Math.Abs(axis.X)>.5f?(a.Width+b.Width)/2:(a.Height+b.Height)/2;
        return Vector2.DistanceSquared(delta,axis*distance)<.001f;
    }
    private NetFace[] AttachPlan(int faceId,int side,out HashSet<int> moving,out float angle)
    {
        moving=new();angle=0;var anchor=Faces.First(f=>f.Face==faceId);
        Vector3 direction=side switch{0=>anchor.Up,1=>anchor.Right,2=>-anchor.Up,_=>-anchor.Right};
        var target=Faces.First(f=>Normals[f.Face]==direction);
        if(Joined(anchor,target))return null;
        // Cut the first hinge on the path from the target to the clicked face.
        var previous=new Dictionary<int,int>{{target.Face,-1}};var queue=new Queue<int>();queue.Enqueue(target.Face);
        while(queue.Count>0)
        {
            int id=queue.Dequeue();var current=Faces.First(f=>f.Face==id);
            foreach(var next in Faces.Where(f=>Joined(current,f)))if(!previous.ContainsKey(next.Face)){previous[next.Face]=id;queue.Enqueue(next.Face);}
        }
        if(!previous.ContainsKey(anchor.Face))return null;
        int cut=anchor.Face;while(previous[cut]!=target.Face)cut=previous[cut];
        queue.Enqueue(target.Face);moving.Add(target.Face);
        while(queue.Count>0)
        {
            int id=queue.Dequeue();var current=Faces.First(f=>f.Face==id);
            foreach(var next in Faces.Where(f=>Joined(current,f)))
            {
                if((id==target.Face && next.Face==cut)||(id==cut && next.Face==target.Face))continue;
                if(moving.Add(next.Face))queue.Enqueue(next.Face);
            }
        }
        var normal=Normals[anchor.Face];
        Vector3 right=side switch{0=>anchor.Right,1=>-normal,2=>anchor.Right,_=>normal};
        Vector3 up=side switch{0=>-normal,1=>anchor.Up,2=>normal,_=>anchor.Up};
        float width=Math.Abs(Vector3.Dot(right,target.Right))>.5f?target.Width:target.Height,height=Math.Abs(Vector3.Dot(up,target.Up))>.5f?target.Height:target.Width;
        var center=anchor.Center+(side switch{0=>new Vector2(0,-(anchor.Height+height)/2),1=>new Vector2((anchor.Width+width)/2,0),2=>new Vector2(0,(anchor.Height+height)/2),_=>new Vector2(-(anchor.Width+width)/2,0)});
        angle=MathF.Atan2(-Vector3.Dot(target.Right,up),Vector3.Dot(target.Right,right));
        var group=moving;float rotation=angle;
        Vector3 Snap(Vector3 v)=>new(MathF.Round(v.X),MathF.Round(v.Y),MathF.Round(v.Z));
        var result=Faces.Select(f=>!group.Contains(f.Face)?f:new NetFace(f.Face,
            Snap(f.Right*MathF.Cos(rotation)+f.Up*MathF.Sin(rotation)),Snap(-f.Right*MathF.Sin(rotation)+f.Up*MathF.Cos(rotation)),
            center+RotatePoint(f.Center-target.Center,rotation),Math.Abs(MathF.Cos(rotation))>.5f?f.Width:f.Height,Math.Abs(MathF.Cos(rotation))>.5f?f.Height:f.Width)).ToArray();
        // Keep the end state a genuine, non-overlapping cube net.
        foreach(var a in result)foreach(var b in result.Where(b=>b.Face>a.Face))
            if(Math.Min(a.Center.X+a.Width/2,b.Center.X+b.Width/2)-Math.Max(a.Center.X-a.Width/2,b.Center.X-b.Width/2)>.001f &&
               Math.Min(a.Center.Y+a.Height/2,b.Center.Y+b.Height/2)-Math.Max(a.Center.Y-a.Height/2,b.Center.Y-b.Height/2)>.001f)return null;
        return result;
    }
    public void HoverEdge(Rectangle panel,Point pointer)
    {
        HoveredFace=HoveredSide=-1;if(IsAnimating || !MapViewport(panel).Contains(pointer))return;
        var labels=SeamLabels(panel);
        foreach(var label in labels)if(label.Bounds.Contains(pointer))
        {HoveredFace=label.Face;HoveredSide=label.Side;return;}
        float nearest=8;
        foreach(var label in labels)
        {
            var a=label.A;var b=label.B;
            var point=new Vector2(pointer.X,pointer.Y);var line=b-a;
            float t=MathHelper.Clamp(Vector2.Dot(point-a,line)/line.LengthSquared(),0,1);
            float distance=Vector2.Distance(point,a+t*line);
            if(distance>=nearest)continue;
            nearest=distance;HoveredFace=label.Face;HoveredSide=label.Side;
        }
    }
    public bool ClickEdge()
    {
        if(IsAnimating || HoveredFace<0)return false;
        var plan=AttachPlan(HoveredFace,HoveredSide,out var moving,out float angle);if(plan==null)return false;
        var anchor=Faces.First(f=>f.Face==HoveredFace);var direction=HoveredSide switch{0=>anchor.Up,1=>anchor.Right,2=>-anchor.Up,_=>-anchor.Right};
        int target=Array.IndexOf(Normals,direction);
        _oldPivot=Faces.First(f=>f.Face==target).Center;_newPivot=plan.First(f=>f.Face==target).Center;
        _animationFrom=Faces;_animationTo=plan;_movingFaces=moving;_animationAngle=angle;_animationProgress=0;return true;
    }
    private static (Vector2 A,Vector2 B) EdgeEnds(NetFace face,int side)
    {
        var c=face.Center;float w=face.Width/2,h=face.Height/2;
        return side switch{0=>(c+new Vector2(-w,-h),c+new Vector2(w,-h)),1=>(c+new Vector2(w,-h),c+new Vector2(w,h)),2=>(c+new Vector2(-w,h),c+new Vector2(w,h)),_=>(c+new Vector2(-w,-h),c+new Vector2(-w,h))};
    }
    private void DrawHoveredEdge(UiPainter ui,Rectangle panel)
    {
        if(HoveredFace<0)return;
        foreach(var label in SeamLabels(panel).Where(l=>l.Name==HoveredSeam))
        {
            var a=label.A;var b=label.B;
            ui.Box(new((int)Math.Min(a.X,b.X)-2,(int)Math.Min(a.Y,b.Y)-2,Math.Max(4,(int)Math.Abs(a.X-b.X)+4),Math.Max(4,(int)Math.Abs(a.Y-b.Y)+4)),new Color(255,230,112));
        }
    }
    private void DrawAnimation(UiPainter ui,WorldSetup world,Rectangle panel,float pulse)
    {
        var fit=Fit(panel);float t=_animationProgress,eased=t*t*(3-2*t);float angle=_animationAngle*eased;
        var current=Faces;Faces=_animationTo;var targetFit=Fit(panel);Faces=current;
        fit=(MathHelper.Lerp(fit.Scale,targetFit.Scale,eased),Vector2.Lerp(fit.Origin,targetFit.Origin,eased));
        var pivot=Vector2.Lerp(_oldPivot,_newPivot,eased)+new Vector2(0,-MathF.Sin(t*MathF.PI)*.65f);
        var offset=WorldTerrain.Offset(world.WorldSeed);
        foreach(var cell in world.Cells.OrderBy(c=>_movingFaces.Contains(c.Face)?1:0))
        {
            var face=_animationFrom.First(f=>f.Face==cell.Face);bool moving=_movingFaces.Contains(cell.Face);
            Vector2 Project(Vector3 p){var point=face.Project(p);return (moving?pivot+RotatePoint(point-_oldPivot,angle):point)*fit.Scale+fit.Origin;}
            const int detail=10;
            for(int y=0;y<detail;y++)for(int x=0;x<detail;x++)
            {
                var p=cell.Origin+cell.U*((x+.5f)/detail)+cell.V*((y+.5f)/detail);float elevation=WorldTerrain.Elevation(p,offset);
                var color=WorldTerrain.ColorAt(elevation,cell.Normal);
                var post=world.Nodes.At(cell.Id,new Point(x,y));if(!ShowRoutes && post!=null)color=Color.Lerp(color,world.OwnerColor(post.Owner),post.Owner==world.PlayerSlot?.4f:.16f);
                ui.Tile(Project(p),new Vector2(fit.Scale/detail+1),moving?angle:0,color);
            }
            var corners=cell.Corners;
            for(int edge=0;!ShowRoutes && edge<4;edge++)
            {
                var neighbor=cell.Neighbors.First(id=>world.Cells[id].Corners.Contains(corners[edge]) && world.Cells[id].Corners.Contains(corners[(edge+1)%4]));
                int owner=world.Nodes.FullOwner(cell.Id);
                if(ShowRoutes && owner>=0 && world.Nodes.FullOwner(neighbor)==owner && world.Cells[neighbor].Face==cell.Face)continue;
                var a=Project(corners[edge]);var b=Project(corners[(edge+1)%4]);var delta=b-a;
                ui.Tile((a+b)/2,new Vector2(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),owner<0?new Color(170,181,191):world.OwnerColor(owner));
            }
            DrawCellRoutes(ui,world,cell.Id,Project,pulse);
            foreach(int ruler in Enumerable.Range(0,world.ActiveCount).Where(r=>ShowBatteries && world.Relations.Powered[r] && world.ConquerorLocations[r]==cell.Id).OrderBy(r=>r==world.PlayerSlot?1:0))
            {
                var at=Project(world.Routes.Position(cell.Id,world.ConquerorPoints[ruler]));float turn=moving?angle:0;
                var tint=ruler==world.PlayerSlot?Color.Lerp(new Color(255,240,119),Color.White,(MathF.Sin(pulse*4)+1)/2):world.OwnerColor(ruler);
                ui.Tile(at,new Vector2(ruler==world.PlayerSlot?29:17,14),turn,new Color(12,27,36));
                ui.Tile(at,new Vector2(ruler==world.PlayerSlot?25:13,10),turn,tint);
            }
        }
    }
}

namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    public bool FacesJoined(int a,int b)=>a==b || Joined(Faces.First(f=>f.Face==a),Faces.First(f=>f.Face==b));
    public (int Face,int Side)[] JoinPath(int a,int b)
    {
        if(FacesJoined(a,b))return Array.Empty<(int,int)>();
        var original=Faces;
        string Key(NetFace[] faces)=>string.Join(";",faces.OrderBy(f=>f.Face).Select(f=>$"{f.Face}:{f.Center-faces.First(x=>x.Face==0).Center}:{f.Right}:{f.Up}"));
        var queue=new Queue<(NetFace[] Faces,(int Face,int Side)[] Path)>();queue.Enqueue((Faces,Array.Empty<(int,int)>()));
        var seen=new HashSet<string>{Key(Faces)};
        try
        {
            while(queue.Count>0)
            {
                var state=queue.Dequeue();Faces=state.Faces;
                if(FacesJoined(a,b))return state.Path;
                foreach(var f in Faces.ToArray())for(int side=0;side<4;side++)
                {
                    var plan=AttachPlan(f.Face,side,out _,out _);if(plan==null || !seen.Add(Key(plan)))continue;
                    queue.Enqueue((plan,state.Path.Append((f.Face,side)).ToArray()));
                }
            }
        }
        finally{Faces=original;}
        throw new InvalidOperationException("No connected cube-net route for propagation.");
    }
    public bool AnimateAttachment(int face,int side)
    {HoveredFace=face;HoveredSide=side;return ClickEdge();}
    public Vector2 AnimatedNodePosition(WorldSetup world,Node node,Rectangle panel)
        =>AnimatedSurfacePosition(node.Cell,world.Routes.Position(node.Cell,node.Center),world,panel);
    private Vector2 AnimatedSurfacePosition(int cell,Vector3 position,WorldSetup world,Rectangle panel)
    {
        if(!IsAnimating){var f=Faces.First(f=>f.Face==world.Cells[cell].Face);var fitNow=Fit(panel);return RotateScreen(f.Project(position)*fitNow.Scale+fitNow.Origin,panel);}
        float t=_animationProgress,eased=t*t*(3-2*t),angle=_animationAngle*eased;
        var fit=Fit(panel);var current=Faces;Faces=_animationTo;var targetFit=Fit(panel);Faces=current;
        var scale=MathHelper.Lerp(fit.Scale,targetFit.Scale,eased);var origin=Vector2.Lerp(fit.Origin,targetFit.Origin,eased);
        var face=_animationFrom.First(f=>f.Face==world.Cells[cell].Face);
        var point=face.Project(position);
        if(_movingFaces.Contains(face.Face))point=Vector2.Lerp(_oldPivot,_newPivot,eased)+new Vector2(0,-MathF.Sin(t*MathF.PI)*.65f)+RotatePoint(point-_oldPivot,angle);
        return RotateScreen(point*scale+origin,panel);
    }
}

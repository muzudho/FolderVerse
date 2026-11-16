namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<(int Ruler,int Source,int Target),float> _movePreviewStarts=new();
    private WorldSetup _movePreviewWorld;

    private bool HasMoveReservation()=>_setup.Campaign.PlannedOrders.Any(o=>o.Peaceful && CanPlanRuler(o.Ruler));
    private void DrawMoveReservationButton()
    {
        _ui.Button(MovementButton,"   移動予約",Accent,.48f);
        var lamp=new Rectangle(MovementButton.X+10,MovementButton.Center.Y-8,16,16);
        _ui.Box(new(lamp.X-2,lamp.Y-2,20,20),new Color(10,28,35));
        _ui.Box(lamp,HasMoveReservation()?new Color(139,255,158):new Color(64,79,83));
    }

    private IEnumerable<(int Ruler,int Source,int Target)> MovePreviews()
    {
        foreach(var order in _setup.Campaign.PlannedOrders)
        {
            if(!order.Peaceful || !order.Queen || !CanPlanRuler(order.Ruler))continue;
            int source=order.SourceNode>=0?order.SourceNode:_setup.Nodes.Current(order.Ruler)?.Id??-1;
            if(source>=0)yield return (order.Ruler,source,order.TargetNode);
        }
        if(_movementOpen && !_warPlanning && _selectedMoveNode>=0 && _setup.Nodes.Current(_setup.PlayerSlot) is Node current)
            yield return (_setup.PlayerSlot,current.Id,_selectedMoveNode);
    }

    private void DrawMoveReservationMarkers()
    {
        if(!ReferenceEquals(_movePreviewWorld,_setup))
        {
            _movePreviewWorld=_setup;
            _movePreviewStarts.Clear();
        }
        var previews=MovePreviews().Distinct().ToArray();
        foreach(var key in _movePreviewStarts.Keys.Except(previews).ToArray())_movePreviewStarts.Remove(key);
        if(MapTransitionActive || _net.IsAnimating)return;
        foreach(var preview in previews)
        {
            if(!_movePreviewStarts.TryGetValue(preview,out float start))
                _movePreviewStarts[preview]=start=_animationTime;
            var path=_setup.Campaign.RobotBattles.PeacefulMarchPath(_setup,preview.Source,preview.Target);
            if(path.Length<2)continue;
            var segments=new List<(int Cell,Vector3 From,Vector3 To,float Length)>();
            float length=0;
            void AddLeg(int cell,IEnumerable<Vector3> points)
            {
                Vector3? previous=null;
                foreach(var point in points)
                {
                    if(previous is Vector3 from)
                    {
                        float distance=Vector3.Distance(from,point);
                        if(distance>.0001f){segments.Add((cell,from,point,distance));length+=distance;}
                    }
                    previous=point;
                }
            }
            for(int i=1;i<path.Length;i++)
            {
                var source=_setup.Nodes.All[path[i-1]];
                var target=_setup.Nodes.All[path[i]];
                var route=_setup.Routes.Neighbors(source).First(r=>_setup.Nodes.At(r.Target,r.Entry).Id==target.Id);
                AddLeg(source.Cell,_setup.Routes.Curve(source.Cell,route));
                if(route.Portal is {} portal)
                    AddLeg(target.Cell,route.ArrivalPath.Select(p=>_setup.Routes.Position(target.Cell,p)).Prepend(portal.Position));
            }
            if(length<=0 || !TryMoveBusDistance(_animationTime-start,length,out float offset))continue;
            foreach(var leg in segments)
            {
                if(offset>leg.Length && leg!=segments[^1]){offset-=leg.Length;continue;}
                var at=Vector3.Lerp(leg.From,leg.To,Math.Clamp(offset/leg.Length,0,1));
                DrawMoveBus(preview.Ruler,_setup.Cells[leg.Cell],at,(leg.To-leg.From)/leg.Length);
                break;
            }
        }
    }

    private static bool TryMoveBusDistance(float age,float length,out float distance)
    {
        float travel=Math.Clamp(length/.55f,2.2f,8f);
        float phase=age%(travel+1.25f);
        distance=length*Math.Clamp(phase/travel,0,1);
        return phase<travel+.55f;
    }

    private void DrawMoveBus(int ruler,SurfaceCell cell,Vector3 at,Vector3 forward)
    {
        Vector2 Project(Vector3 p)
        {
            if(!_statusGlobe)return _net.AnimatedSurfacePosition(cell.Id,p,_setup,NetPanel);
            _world.ProjectVisible(p,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var projected);
            return projected;
        }
        if(_statusGlobe && !_world.ProjectVisible(at,cell.Normal,_yaw,_pitch,StatusGlobeArea,out _))return;
        if(_statusGlobe)
            at+=cell.Normal*WorldTerrain.ReliefAt(_setup,cell,Vector3.Lerp(at,cell.Center,.001f),_setup.Terrain);
        var center=Project(at);
        var onScreen=Project(at+forward*.1f)-center;
        if(onScreen.LengthSquared()<.01f)return;
        var heading=Vector2.Normalize(onScreen);
        var across=Vector3.Cross(cell.Normal,forward);
        var side=Project(at+across*.1f)-center;
        var up=_statusGlobe?Project(at+cell.Normal*.1f)-center:new Vector2(0,-10);
        float scale=Math.Clamp(Math.Max(onScreen.Length(),side.Length()),9,20);
        float ratio=scale/Math.Max(onScreen.Length(),side.Length());
        var frontAxis=onScreen*ratio;
        var sideAxis=side*ratio;
        var upAxis=up*ratio;
        if(!_statusGlobe)upAxis=new(0,-scale*.7f);
        var chip=center-heading*34+new Vector2(0,-45+MathF.Sin(_animationTime*4)*3);
        const int radius=22;
        var map=MapViewBounds;
        map.Inflate(-4,-4);
        if(!map.Contains(new Rectangle((int)chip.X-radius-3,(int)chip.Y-radius-3,2*radius+6,2*radius+6)) ||
           !map.Contains(new Rectangle((int)center.X-35,(int)center.Y-34,70,48)))return;

        Vector2 Model(float x,float y,float z)=>center+frontAxis*x+sideAxis*y+upAxis*z;
        var gold=new Color(255,219,141);
        var hitch=Model(-1.05f,0,.64f);
        var knot=chip+new Vector2(0,radius);
        var sag=(hitch+knot)/2+new Vector2(0,7);
        var previous=hitch;
        for(int i=1;i<=8;i++)
        {
            float t=i/8f;
            var point=Vector2.Lerp(Vector2.Lerp(hitch,sag,t),Vector2.Lerp(sag,knot,t),t);
            var delta=point-previous;
            _ui.Tile((point+previous)/2,new(delta.Length()+1,2),MathF.Atan2(delta.Y,delta.X),gold);
            previous=point;
        }
        CubeNet.DrawMoveBusModel(_ui,Model,_setup.OwnerColor(ruler));
        Disc(chip+new Vector2(2,3),radius,new(11,28,40));
        Disc(chip,radius,gold);
        Disc(chip,radius-3,_setup.OwnerColor(ruler));
        DrawLocationChipFace(chip,radius-5,ruler);

        void Disc(Vector2 point,int r,Color color)
        {
            for(int y=-r;y<=r;y++)
            {
                int half=(int)MathF.Sqrt(r*r-y*y);
                _ui.Box(new((int)point.X-half,(int)point.Y+y,half*2+1,1),color);
            }
        }
    }
}

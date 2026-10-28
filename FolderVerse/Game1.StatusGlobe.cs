namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _statusGlobe=true;
    private int _globeSelectedNode=-1;
    private int _globePressedCell=-1;
    private Point _globePressPoint;
    private bool _globeCellDragged;
    private void DrawGlobeCellHover()
    {
        if(_net.ShowRoutes && (_net.NodeFocus>=0 || _net.CountryFocus>=0))return;
        if(_net.ShowRoutes){int id=HitGlobeNode(_pointer);if(id>=0){var n=_setup.Nodes.All[id];var c=_setup.Cells[n.Cell];if(_world.ProjectVisible(_setup.Routes.Position(n.Cell,n.Center),c.Normal,_yaw,_pitch,StatusGlobeArea,out var at)){_ui.Box(new((int)at.X-14,(int)at.Y-14,28,3),Color.White);_ui.Box(new((int)at.X-14,(int)at.Y+11,28,3),Color.White);}}return;} int hovered=HitGlobeCell(_pointer);
        if(hovered<0)return;
        var cell=_setup.Cells[hovered];
        var points=new Vector2[4];
        for(int i=0;i<4;i++)_world.ProjectVisible(cell.Corners[i],cell.Normal,_yaw,_pitch,StatusGlobeArea,out points[i]);
        for(int i=0;i<4;i++)
        {
            var a=points[i];var b=points[(i+1)%4];var delta=b-a;
            if(delta.LengthSquared()<.01f)continue;
            float angle=MathF.Atan2(delta.Y,delta.X);
            _ui.Tile((a+b)/2,new(delta.Length(),5),angle,new Color(12,27,36));
            _ui.Tile((a+b)/2,new(delta.Length(),2),angle,new Color(255,234,124));
        }
    }
    private int HitGlobeCell(Point pointer)
    {
        if(!MapViewBounds.Contains(pointer))return -1;
        var p=new Vector2(pointer.X,pointer.Y);
        foreach(var cell in _setup.Cells)
        {
            if(!_world.ProjectVisible(cell.Center,cell.Normal,_yaw,_pitch,StatusGlobeArea,out _))continue;
            var corners=new Vector2[4];
            for(int i=0;i<4;i++)_world.ProjectVisible(cell.Corners[i],cell.Normal,_yaw,_pitch,StatusGlobeArea,out corners[i]);
            bool positive=false,negative=false;
            for(int i=0;i<4;i++)
            {
                var a=corners[i];var b=corners[(i+1)%4];
                float cross=(b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);
                positive|=cross>.01f;negative|=cross<-.01f;
            }
            if(!(positive && negative))return cell.Id;
        }
        return -1;
    }
    private void GlobeCellClick(MouseState mouse,bool click,bool active,bool action)
    {
        if(!active || action || _movementOpen){_globePressedCell=-1;return;}
        if(click)
        {
            int node=_net.ShowRoutes?HitGlobeNode(_pointer):-1;
            _globePressedCell=node>=0?_setup.Nodes.All[node].Cell:HitGlobeCell(_pointer);_globePressPoint=_pointer;_globeCellDragged=false;
        }
        if(_globePressedCell<0)return;
        if(Vector2.DistanceSquared(new(_pointer.X,_pointer.Y),new(_globePressPoint.X,_globePressPoint.Y))>36)_globeCellDragged=true;
        if(mouse.LeftButton!=ButtonState.Released)return;
        int selected=_globePressedCell;_globePressedCell=-1;
        if(!_globeCellDragged && BatteryClick(_pointer))return;
        int selectedNode=_net.ShowRoutes?HitGlobeNode(_pointer):-1;
        if(!_globeCellDragged && selectedNode>=0)
        {_globeSelectedNode=selectedNode;OpenNodeInspector(selectedNode);return;}
        if(_globeCellDragged || _net.ShowRoutes)return;
        OpenRegionInspector(selected);_inputOutcome="globe_cell_inspected";
    }
    private Rectangle StatusGlobeArea=>OrientationAreas(NetPanel,false).Globe;
    private Rectangle OrientationResetButton=>OrientationGlobe?new(OrientationToyArea.Center.X-42,OrientationToyArea.Bottom+12,84,28):new(OrientationToyArea.Center.X-14,OrientationToyArea.Center.Y-OrientationToyArea.Width/2-37,28,28);
    private void ResetStatusOrientation()
    {
        _yaw=_pitch=0;_net.ScreenAngle=0;_dragging=false;_net.SetCenter(_setup,4);
    }
    private int HitGlobeNode(Point pointer,IEnumerable<int> candidates=null)
    {
        if(!MapViewBounds.Contains(pointer))return -1;
        int nearest=-1,twin=-1;float distance=float.PositiveInfinity,twinDistance=float.PositiveInfinity;
        foreach(var node in _setup.Nodes.All.Where(n=>_net.NodeVisible(_setup,n)))
        {
            var cell=_setup.Cells[node.Cell];
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),cell.Normal,_yaw,_pitch,StatusGlobeArea,out var at))continue;
            float candidate=Vector2.DistanceSquared(at,new(pointer.X,pointer.Y));
            _world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center)+cell.U,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var unit);
            float radius=MathHelper.Clamp(Vector2.Distance(at,unit)*.06f,4,10);
            if(node.IsHarbor)radius=MathHelper.Clamp(radius*1.7f,12,20);
            if(_net.EmphasizedNode(node.Id) && !_setup.Campaign.OwnershipChanged(node))radius*=2;
            if(_setup.Campaign.OwnershipChanged(node) && NodeOwnershipArt.HitTwin(at,radius,new Vector2(pointer.X,pointer.Y)) && candidate<twinDistance){twin=node.Id;twinDistance=candidate;}
            radius+=4; // Include the visible outline and a small pointer tolerance.
            if(candidate<=radius*radius && candidate<distance){nearest=node.Id;distance=candidate;}
        }
        // Resolve the visible object first, then check whether the action permits it.
        // Candidate filtering must never redirect a click to a neighboring node.
        if(nearest<0)nearest=twin;
        if(nearest<0){int badge=_net.HitRobotBadge(pointer);if(badge>=0 && _net.NodeVisible(_setup,_setup.Nodes.All[badge]))nearest=badge;}
        return nearest>=0 && (candidates==null || candidates.Contains(nearest))?nearest:-1;
    }
    private void FocusStatusGlobe()
    {
        _world.GlobePan=Vector2.Zero;
        _domesticMapLeft=55+480*_domesticSlide;
        _globeSelectedNode=_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1;
        FocusGlobePosition(CurrentMapPosition());
    }
    private void FocusGlobePosition(Vector3 point)
    {
        _world.GlobePan=Vector2.Zero;
        _world.GlobeAnchor=point;
        _pitch=MathF.Atan2(point.Y,point.Z);
        if(_pitch>MathHelper.PiOver2)_pitch-=MathHelper.Pi;
        if(_pitch< -MathHelper.PiOver2)_pitch+=MathHelper.Pi;
        var turned=Vector3.Transform(point,Matrix.CreateRotationX(_pitch));
        _yaw=-MathF.Atan2(turned.X,turned.Z);
        _dragging=_mapDragging=false;
    }
    private Vector3 CurrentMapPosition()
    {
        int player=_setup.PlayerSlot;var node=_setup.Nodes.Current(player);
        return _setup.Routes.Position(node?.Cell??_setup.ConquerorLocations[player],node?.Center??_setup.ConquerorPoints[player]);
    }
    private void FocusCurrentNode()
    {
        _domesticMapLeft=55+480*_domesticSlide;
        int cell=_setup.Nodes.Current(_setup.PlayerSlot)?.Cell??_setup.ConquerorLocations[_setup.PlayerSlot];
        _net.SetCenter(_setup,_setup.Cells[cell].Face);
        var face=_net.Faces[0];var fit=_net.Fit(NetPanel);
        var at=face.Project(CurrentMapPosition())*fit.Scale+fit.Origin;
        _net.Drag(new Vector2(NetPanel.Center.X,NetPanel.Center.Y)-_net.RotateScreen(at,NetPanel));
        _dragging=_mapDragging=false;
    }
    private void DrawStatusGlobeCoordinates()
    {
        if(!_net.ShowMapSymbols)return;
        var occupied=new List<Rectangle>();
        var labels=new List<(int Cell,Vector2 Foot,Vector2 Tip,Rectangle Bounds,string Longitude,string Latitude)>();
        const float scale=WorldCoordinates.MapFontScale;
        foreach(var cell in _setup.Cells)
        {
            if(!_world.ProjectVisible(cell.Center,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var foot) ||
                !_world.ProjectVisible(cell.Center,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var point,2f))continue;
            var coordinate=WorldCoordinates.At(_setup,cell);
            string longitude=coordinate.X.ToString(),latitude=coordinate.Y.ToString();
            float width=_ui.Measure(longitude,scale).X+_ui.Measure(latitude,scale).X+14;
            var label=new Rectangle((int)(point.X-width/2)-3,(int)point.Y-13,(int)Math.Ceiling(width)+6,26);
            if(!StatusGlobeArea.Contains(label) || occupied.Exists(r=>r.Intersects(label)))continue;
            occupied.Add(label);labels.Add((cell.Id,foot,point,label,longitude,latitude));
        }
        // Use the actually visible labels so clipping and thinning create new endpoints.
        var components=WorldCoordinates.GlobeScreenLabelComponents(_setup,labels.ToDictionary(l=>l.Cell,l=>l.Tip));
        for(int i=labels.Count-1;i>=0;i--)
        {
            var label=labels[i];var shown=components[label.Cell];
            if(!shown.Longitude && !shown.Latitude){labels.RemoveAt(i);continue;}
            if(!shown.Longitude)label.Longitude="";
            if(!shown.Latitude)label.Latitude="";
            float width=_ui.Measure(label.Longitude,scale).X+_ui.Measure(label.Latitude,scale).X+
                (shown.Longitude && shown.Latitude?14:0);
            label.Bounds=new((int)(label.Tip.X-width/2)-3,(int)label.Tip.Y-13,(int)Math.Ceiling(width)+6,26);
            labels[i]=label;
        }
        // Draw the normals first so they cannot paint over any coordinate text.
        var normalColor=new Color(172,202,215);
        void Line(Vector2 a,Vector2 b)
        {
            var delta=b-a;if(delta.LengthSquared()<1)return;
            _ui.Tile((a+b)/2,new(delta.Length(),1),MathF.Atan2(delta.Y,delta.X),normalColor);
        }
        foreach(var label in labels)
        {
            Line(label.Foot,label.Tip);
            _ui.Box(new((int)label.Foot.X-1,(int)label.Foot.Y-1,3,3),normalColor);
            var delta=label.Tip-label.Foot;
            if(delta.LengthSquared()<1)continue;
            var direction=Vector2.Normalize(delta);var side=new Vector2(-direction.Y,direction.X);
            var arrow=label.Tip-direction*15;
            Line(arrow,arrow-direction*6+side*3);Line(arrow,arrow-direction*6-side*3);
        }
        foreach(var label in labels)
        {
            var bounds=label.Bounds;_ui.Box(bounds,new Color(15,35,46,230));
            _ui.Text(label.Longitude,new(bounds.X+3,bounds.Y+1),scale,WorldCoordinates.LongitudeColor);
            _ui.Text(label.Latitude,new(bounds.Right-3-_ui.Measure(label.Latitude,scale).X,bounds.Y+1),scale,WorldCoordinates.LatitudeColor);
        }
    }
}

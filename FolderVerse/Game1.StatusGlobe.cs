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
        int hovered=HitGlobeCell(_pointer);
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
        if(!StatusGlobeArea.Contains(pointer))return -1;
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
        if(!active || action || _movementOpen || _populationCell>=0){_globePressedCell=-1;return;}
        if(click)
        {
            _globePressedCell=HitGlobeCell(_pointer);_globePressPoint=_pointer;_globeCellDragged=false;
        }
        if(_globePressedCell<0)return;
        if(Vector2.DistanceSquared(new(_pointer.X,_pointer.Y),new(_globePressPoint.X,_globePressPoint.Y))>36)_globeCellDragged=true;
        if(mouse.LeftButton!=ButtonState.Released)return;
        int selected=_globePressedCell;_globePressedCell=-1;
        int selectedNode=HitGlobeNode(_pointer);
        if(!_globeCellDragged && selectedNode>=0)
        {_globeSelectedNode=selectedNode;var node=_setup.Nodes.All[selectedNode];_populationCell=node.Cell;_populationNode=node.Id;_nodeFactoryOpen=_nodeTransportOpen=_nodeDisposalOpen=_nodeAssemblyOpen=false;return;}
        if(_globeCellDragged)return;
        var cell=_setup.Cells[selected];
        _dragging=_mapDragging=false;
        _net.SetCenter(_setup,cell.Face);_net.ResetView();
        // Choose the quarter turn whose screen axes best match the viewed face.
        var face=_net.Faces[0];
        _world.ProjectVisible(cell.Center,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var origin);
        _world.ProjectVisible(cell.Center+face.Right*.25f,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var right);
        _world.ProjectVisible(cell.Center+face.Up*.25f,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var up);
        var r=right-origin;var u=up-origin;int rotation=0;float best=float.NegativeInfinity;
        for(int i=0;i<4;i++)
        {
            float score=r.X-u.Y;
            if(score>best){best=score;rotation=i;}
            var old=r;r=u;u=-old;
        }
        for(int i=0;i<rotation;i++)_net.Turn(_setup,1);
        var fit=_net.Fit(NetPanel);
        _net.Drag(-_net.Faces[0].Project(cell.Center)*fit.Scale);
        _statusGlobe=false;_statusCell=selected;_inputOutcome="globe_cell_opened_net";
    }
    private static Rectangle StatusGlobeArea=>OrientationAreas(NetPanel,false).Globe;
    private Rectangle OrientationResetButton=>_statusGlobe?new(NetButton(3).X,912,343,50):NetButton(4);
    private void ResetStatusOrientation()
    {
        _yaw=_pitch=0;_dragging=false;_net.SetCenter(_setup,4);
    }
    private int HitGlobeNode(Point pointer,IEnumerable<int> candidates=null)
    {
        if(!StatusGlobeArea.Contains(pointer))return -1;
        int badge=_net.HitRobotBadge(pointer);
        if(badge>=0 && (candidates==null || candidates.Contains(badge)))return badge;
        int nearest=-1;float distance=float.PositiveInfinity;
        foreach(var node in (candidates??_setup.Nodes.All.Select(n=>n.Id)).Select(id=>_setup.Nodes.All[id]))
        {
            var cell=_setup.Cells[node.Cell];
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),cell.Normal,_yaw,_pitch,StatusGlobeArea,out var at))continue;
            float candidate=Vector2.DistanceSquared(at,new(pointer.X,pointer.Y));
            float radius=node.IsHarbor?22:12;
            if(candidate<=radius*radius && candidate<distance){nearest=node.Id;distance=candidate;}
        }
        return nearest;
    }
    private void FocusStatusGlobe()
    {
        _globeSelectedNode=_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1;
        _world.GlobeAnchor=null;
        var point=CurrentMapPosition();
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
        int cell=_setup.Nodes.Current(_setup.PlayerSlot)?.Cell??_setup.ConquerorLocations[_setup.PlayerSlot];
        _net.SetCenter(_setup,_setup.Cells[cell].Face);
        var face=_net.Faces[0];var fit=_net.Fit(NetPanel);
        var at=face.Project(CurrentMapPosition())*fit.Scale+fit.Origin;
        _net.Drag(new Vector2(NetPanel.Center.X,NetPanel.Center.Y)-at);
        _dragging=_mapDragging=false;
    }
    private void DrawStatusGlobeCoordinates()
    {
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

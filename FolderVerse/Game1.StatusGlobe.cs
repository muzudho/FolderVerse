namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _statusGlobe;
    private static Rectangle StatusGlobeArea=>OrientationAreas(NetPanel,false).Globe;
    private Rectangle OrientationResetButton=>_statusGlobe?new(NetButton(3).X,912,343,50):NetButton(4);
    private void ResetStatusOrientation()
    {
        _yaw=_pitch=0;_dragging=false;_net.SetCenter(_setup,4);
    }
    private int HitGlobeNode(Point pointer)
    {
        if(!StatusGlobeArea.Contains(pointer))return -1;
        int nearest=-1;float distance=144;
        foreach(var node in _setup.Nodes.All)
        {
            var cell=_setup.Cells[node.Cell];
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),cell.Normal,_yaw,_pitch,StatusGlobeArea,out var at))continue;
            float candidate=Vector2.DistanceSquared(at,new(pointer.X,pointer.Y));
            if(candidate<distance){nearest=node.Id;distance=candidate;}
        }
        return nearest;
    }
    private void FocusStatusGlobe()
    {
        var normal=_setup.Cells[_setup.ConquerorLocations[_setup.PlayerSlot]].Normal;
        _yaw=-MathF.Atan2(normal.X,normal.Z);
        _pitch=MathF.Asin(normal.Y);
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
        var components=WorldCoordinates.GlobeLabelComponents(_setup,labels.Select(l=>l.Cell));
        for(int i=0;i<labels.Count;i++)
        {
            var label=labels[i];var shown=components[label.Cell];
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

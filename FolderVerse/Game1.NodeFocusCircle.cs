namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void UpdateNodeFocusCircle()
    {
        _net.FocusCircleNodes.Clear();_net.FocusCircleRadius=_world.FocusCircleRadius=0;
        int id=_net.NodeFocus;
        if(id<0 || id>=_setup.Nodes.All.Length || MapTransitionActive || _net.IsAnimating)return;
        var node=_setup.Nodes.All[id];var cell=_setup.Cells[node.Cell];
        var position=_setup.Routes.Position(node.Cell,node.Center);
        int robots=_net.ShowRobots?_setup.Robots.Nodes[id].Robots.Count(r=>_net.ShowOtherCountries || _setup.Relations.Controls(r.Owner)):0;
        Vector2 center;float radius;
        if(_statusGlobe){
            if(!_world.ProjectVisible(position,cell.Normal,_yaw,_pitch,StatusGlobeArea,out center))return;
            float extent=Math.Max(.18f,robots*.12f/MathHelper.TwoPi)+.13f;
            radius=28;
            // Bound the whole deployed ring, including the height of the 3D robots.
            for(int i=0;i<16;i++)foreach(float height in new[]{0f,.2f}){
                float angle=i*MathHelper.TwoPi/16;
                var p=position+cell.U*(MathF.Cos(angle)*extent)+cell.V*(MathF.Sin(angle)*extent)+cell.Normal*height;
                _world.ProjectVisible(p,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var at);
                radius=Math.Max(radius,Vector2.Distance(center,at)+8);
            }
        }else{
            center=_net.NodePosition(_setup,node,NetPanel);
            radius=robots>0?Math.Max(48,robots*30/MathHelper.TwoPi)+38:32;
        }
        _net.FocusCircleCenter=_world.FocusCircleCenter=center;
        _net.FocusCircleRadius=_world.FocusCircleRadius=radius;
        _world.FocusCircleArea=StatusGlobeArea;_world.FocusCircleYaw=_yaw;_world.FocusCirclePitch=_pitch;
        foreach(var other in _setup.Nodes.All){
            Vector2 at;
            if(_statusGlobe){var surface=_setup.Cells[other.Cell];if(!_world.ProjectVisible(_setup.Routes.Position(other.Cell,other.Center),surface.Normal,_yaw,_pitch,StatusGlobeArea,out at))continue;}
            else at=_net.NodePosition(_setup,other,NetPanel);
            if(Vector2.DistanceSquared(at,center)<=radius*radius)_net.FocusCircleNodes.Add(other.Id);
        }
    }
}

namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawNodeTooltip()
    {
        int battery=_net.HitBattery(_pointer);
        if(battery>=0)
        {
            var robots=_setup.Robots.Retinues[battery].Robots;
            _ui.Box(new(500,4,620,76),new Color(27,49,62));
            _ui.Text($"電池 #{battery+1}・ポケット / 現在地："+_setup.Nodes.DisplayName(_setup.Nodes.Current(battery)),new(514,12),.32f,Cream);
            for(int i=0;i<robots.Count;i++)DrawRobotGlyph(new(514+i*22,42,16,26),robots[i]);
            return;
        }
        if(!_net.ShowRoutes)return; int id=_net.HitRobotBadge(_pointer);
        if(id<0)id=_statusGlobe?HitGlobeNode(_pointer):_net.HitNode(_setup,NetPanel,_pointer);if(id<0 || !_net.NodeVisible(_setup,_setup.Nodes.All[id]))return;
        var node=_setup.Nodes.All[id];string name=_setup.Nodes.Label(node);
        float scale=Math.Min(.42f,620/_font.MeasureString(name).X);
        var size=_font.MeasureString(name)*scale;
        int width=Math.Max(300,(int)Math.Ceiling(size.X)+28),height=_setup.Campaign.UseRobotCombat?72:46;
        int x=500,y=4;
        _ui.Box(new(x+3,y+3,width,height),new Color(3,13,20));
        _ui.Box(new(x,y,width,height),new Color(27,49,62));
        _ui.Box(new(x,y,width,3),_setup.OwnerColor(node.Owner));
        _ui.Text(name,new(x+14,y+11),scale,Cream);
        if(_setup.Campaign.UseRobotCombat)
        {
            var robots=_setup.Robots.Nodes[id].Robots;
            for(int i=0;i<robots.Count;i++)DrawRobotGlyph(new(x+14+i*22,y+height-30,16,26),robots[i]);
            _ui.Text(robots.Count+"/12",new(x+width-70,y+height-26),.38f,Cream);
        }
    }
}

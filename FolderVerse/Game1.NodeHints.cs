namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawNodeTooltip()
    {
        int id=_net.HitRobotBadge(_pointer);
        if(id<0)id=_statusGlobe?HitGlobeNode(_pointer):_net.HitNode(_setup,NetPanel,_pointer);if(id<0)return;
        var node=_setup.Nodes.All[id];string name=_setup.Nodes.Label(node);
        float scale=Math.Min(.52f,620/_font.MeasureString(name).X);
        var size=_font.MeasureString(name)*scale;
        int width=Math.Max(300,(int)Math.Ceiling(size.X)+28),height=(int)Math.Ceiling(size.Y)+22+(_setup.Campaign.UseRobotCombat?48:0);
        int x=Math.Clamp(_pointer.X+18,980,1884-width),y=Math.Clamp(_pointer.Y+20,142,892-height);
        _ui.Box(new(x+3,y+3,width,height),new Color(3,13,20));
        _ui.Box(new(x,y,width,height),new Color(27,49,62));
        _ui.Box(new(x,y,width,3),_setup.OwnerColor(node.Owner));
        _ui.Text(name,new(x+14,y+11),scale,Cream);
        if(_setup.Campaign.UseRobotCombat)
        {
            var robots=_setup.Robots.Nodes[id].Robots;
            for(int i=0;i<robots.Count;i++)DrawRobotGlyph(new(x+14+i*22,y+height-38,16,26),robots[i].Owner,robots[i].Parts,robots[i].Role==RobotRole.Captain);
            _ui.Text(robots.Count+"/12",new(x+width-70,y+height-34),.38f,Cream);
        }
    }
}

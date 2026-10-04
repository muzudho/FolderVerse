namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawNodeTooltip()
    {
        int id=_net.HitNode(_setup,NetPanel,_pointer);if(id<0)return;
        var node=_setup.Nodes.All[id];string name=_setup.Nodes.Label(node);
        float scale=Math.Min(.52f,620/_font.MeasureString(name).X);
        var size=_font.MeasureString(name)*scale;
        int width=(int)Math.Ceiling(size.X)+28,height=(int)Math.Ceiling(size.Y)+22;
        int x=Math.Clamp(_pointer.X+18,980,1884-width),y=Math.Clamp(_pointer.Y+20,142,892-height);
        _ui.Box(new(x+3,y+3,width,height),new Color(3,13,20));
        _ui.Box(new(x,y,width,height),new Color(27,49,62));
        _ui.Box(new(x,y,width,3),_setup.OwnerColor(node.Owner));
        _ui.Text(name,new(x+14,y+11),scale,Cream);
    }
}

namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RobotCard(int index)=>new(54+index%6*149,220+index/6*153,137,141);
    private static string PartsLabel(RobotParts parts)=>((parts&RobotParts.Head)!=0?"頭":"")+((parts&RobotParts.Body)!=0?"胴":"")+((parts&RobotParts.Legs)!=0?"脚":"");
    private void DrawRobotGlyph(Rectangle r,int owner,RobotParts parts,bool captain=false,float opacity=1)
    {
        var color=_setup.OwnerColor(owner)*opacity;
        if((parts&RobotParts.Head)!=0){_ui.Box(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4),Cream*opacity);_ui.Box(new(r.X+r.Width/5,r.Y,r.Width*3/5,captain?r.Height/7:r.Height/12),color);}
        if((parts&RobotParts.Body)!=0)_ui.Box(new(r.X+r.Width/6,r.Y+r.Height/3,r.Width*2/3,r.Height/3),color);
        if((parts&RobotParts.Legs)!=0){_ui.Box(new(r.X+r.Width/6,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);_ui.Box(new(r.X+r.Width*7/12,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);}
    }
}

namespace FolderVerse;
using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    private static Rectangle RobotCard(int index)=>new(54+index%6*149,220+index/6*153,137,141);
    private static string PartsLabel(RobotParts parts)=>((parts&RobotParts.Head)!=0?"頭":"")+((parts&RobotParts.Body)!=0?"胴":"")+((parts&RobotParts.Legs)!=0?"脚":"");
    private void DrawRobotGlyph(Rectangle r,Robot robot,bool whiteOutline=false)
    {foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((robot.Parts&part)!=0)DrawRobotGlyph(r,robot.Origin(part),part,robot.Role==RobotRole.Captain,whiteOutline:whiteOutline,damaged:robot.IsDamaged(part),cheap:robot.IsCheap(part));}
    private void DrawRobotGlyph(Rectangle r,int owner,RobotParts parts,bool captain=false,float opacity=1,bool whiteOutline=false,bool damaged=false,bool cheap=false)
    {
        var color=_setup.OwnerColor(owner)*opacity;
        if(damaged)color=Color.Lerp(color,new Color(116,119,116),.8f);
        if(whiteOutline)
        {
            void Outline(Rectangle bounds){bounds.Inflate(2,2);_ui.Box(bounds,Color.White*opacity);}
            if((parts&RobotParts.Head)!=0)
            {
                Outline(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4));
                Outline(new(r.X+r.Width/5,r.Y,r.Width*3/5,captain?r.Height/7:r.Height/12));
            }
            if((parts&RobotParts.Body)!=0)Outline(new(r.X+r.Width/6,r.Y+r.Height/3,r.Width*2/3,r.Height/3));
            if((parts&RobotParts.Legs)!=0)
            {
                Outline(new(r.X+r.Width/6,r.Y+r.Height*3/4,r.Width/4,r.Height/4));
                Outline(new(r.X+r.Width*7/12,r.Y+r.Height*3/4,r.Width/4,r.Height/4));
            }
        }
        if((parts&RobotParts.Head)!=0){_ui.Box(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4),Cream*opacity);_ui.Box(new(r.X+r.Width/5,r.Y,r.Width*3/5,captain?r.Height/7:r.Height/12),color);}
        if((parts&RobotParts.Body)!=0)_ui.Box(new(r.X+r.Width/6,r.Y+r.Height/3,r.Width*2/3,r.Height/3),color);
        if((parts&RobotParts.Legs)!=0){_ui.Box(new(r.X+r.Width/6,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);_ui.Box(new(r.X+r.Width*7/12,r.Y+r.Height*3/4,r.Width/4,r.Height/4),color);}
        if(cheap)
        {
            void Stripe(Rectangle area){for(int y=0;y<area.Height;y+=Math.Max(4,r.Height/12))_ui.Box(new(area.X,area.Y+y,area.Width,Math.Min(Math.Max(2,r.Height/28),area.Height-y)),damaged?new Color(151,150,138)*opacity:Cream*opacity);}
            if((parts&RobotParts.Head)!=0)Stripe(new(r.X+r.Width/5,r.Y,r.Width*3/5,Math.Max(3,r.Height/7)));
            if((parts&RobotParts.Body)!=0)Stripe(new(r.X+r.Width/6,r.Y+r.Height/3,r.Width*2/3,r.Height/3));
            if((parts&RobotParts.Legs)!=0){Stripe(new(r.X+r.Width/6,r.Y+r.Height*3/4,r.Width/4,r.Height/4));Stripe(new(r.X+r.Width*7/12,r.Y+r.Height*3/4,r.Width/4,r.Height/4));}
        }
        if(damaged)
        {
            // Exposed dull metal interrupts the old paint, rather than using another country's color.
            var metal=new Color(185,180,164)*opacity;
            int y=(parts&RobotParts.Head)!=0?r.Y+r.Height/12:(parts&RobotParts.Body)!=0?r.Y+r.Height/3:r.Y+r.Height*3/4;
            _ui.Box(new(r.X+r.Width/4,y+2,Math.Max(2,r.Width/5),Math.Max(2,r.Height/28)),metal);
            _ui.Box(new(r.X+r.Width/2,y+Math.Max(4,r.Height/12),Math.Max(2,r.Width/7),Math.Max(2,r.Height/28)),metal);
        }
    }
}

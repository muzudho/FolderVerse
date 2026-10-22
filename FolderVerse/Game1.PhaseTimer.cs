namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void DrawPhaseTimer(Rectangle bounds,double age,double seconds)
    {
        _ui.Box(bounds,new Color(19,38,49));
        int width=(int)((bounds.Width-4)*Math.Clamp(age/seconds,0,1));
        if(width>0)_ui.Box(new(bounds.X+2,bounds.Y+2,width,bounds.Height-4),new Color(248,194,58));
    }
}

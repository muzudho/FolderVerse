namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private string _cutInPhase="";
    private string _lastCutInPhase="";
    private double _cutInStarted=-10;
    private void DrawPhaseCutIn(double now)
    {
        // Auxiliary menus and battlefield changes retain the current campaign phase.
        string phase=_screen==Screen.Transport?"伝播フェーズ":_screen==Screen.Battle?"戦闘フェーズ":_screen==Screen.WorldStatus?"内政フェーズ":"";
        if(_screen==Screen.Title){_lastCutInPhase="";_cutInPhase="";return;}
        if(phase!="" && phase!=_lastCutInPhase){_lastCutInPhase=phase;_cutInPhase=phase;_cutInStarted=now;}
        double age=now-_cutInStarted;if(age<0 || age>=1.8 || _cutInPhase=="")return;
        float fade=(float)Math.Min(Math.Min(age/.22,(1.8-age)/.35),1);
        float slide=(float)Math.Pow(1-Math.Clamp(age/.45,0,1),3)*360;
        Color color=_cutInPhase=="内政フェーズ"?new(126,219,190):_cutInPhase=="伝播フェーズ"?new(130,208,246):new(255,183,156);
        int x=380+(int)slide,y=410;
        // A floating title card with diagonal ornaments, rather than a screen-wide band.
        _ui.Box(new(x+8,y+10,1160,240),new Color(12,27,39)*(.38f*fade));
        _ui.Box(new(x,y,1160,240),new Color(246,239,224)*(.94f*fade));
        _ui.Box(new(x+24,y+22,1112,3),color*fade);_ui.Box(new(x+24,y+215,1112,3),color*fade);
        for(int i=0;i<3;i++){_ui.Tile(new(x+50+i*26,y+120),new(15,15),MathHelper.PiOver4,color*fade);_ui.Tile(new(x+1110-i*26,y+120),new(15,15),MathHelper.PiOver4,color*fade);}
        _ui.Center(_cutInPhase,new(x+135,y+56,890,130),1.55f,new Color(35,58,70)*fade);
    }
}

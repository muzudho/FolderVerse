namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawAmbushCard(BattleState battle,int frameIndex)
    {
        var frame=battle.Frames.ElementAtOrDefault(frameIndex);if(frame==null)return;
        var activation=battle.Events.FirstOrDefault(e=>e.Detail==BattleTactics.Ambush && e.Turn==frame.Turn &&
            !battle.Events.Any(previous=>previous.Detail==BattleTactics.Ambush && previous.Unit==e.Unit && previous.Turn==e.Turn-1));
        if(activation==null)return;
        int first=battle.Frames.FindIndex(f=>f.Turn==frame.Turn);
        float age=(float)Math.Max(0,(_battleAge/RobotSeconds(_battleWave)-(first-RobotSlice(_battleWave).Start))*RobotSeconds(_battleWave));
        float slide=Math.Clamp(age/.22f,0,1);slide=1-(1-slide)*(1-slide);
        DrawAmbushCardPanel(new Rectangle(420,1080-(int)(slide*170),540,150),activation.Unit);
    }
    private void DrawAmbushCardPanel(Rectangle r,long unit)
    {
        _ui.Box(new(r.X+7,r.Y+7,r.Width,r.Height),new Color(8,20,23)*.65f);
        _ui.Box(r,new Color(66,112,85));_ui.Box(new(r.X+5,r.Y+5,r.Width-10,r.Height-10),new Color(237,227,190));
        _ui.Text(BattleTactics.Ambush,new(r.X+25,r.Y+18),.65f,new Color(40,72,53));
        _ui.Text($"ID {unit} / 守備専用",new(r.X+25,r.Y+67),.48f,new Color(40,72,53));
        _ui.Text("物陰から横を狙う",new(r.X+25,r.Y+104),.45f,new Color(40,72,53));
    }
}

namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public partial class Game1
{
    private void DrawBattleFlagCountdown(BattleState battle,BattleFrame frame){
        bool defense=frame.FlagStanding;int total=defense?battle.Rules.FlagDefenseTurns:battle.Rules.FlagCaptureTurns;
        int elapsed=defense?frame.FlagDefenseTurns:frame.FlagCaptureTurns;
        int owner=defense?battle.Defender:frame.LastFlagOwner;
        string name=owner>=0 && owner<_setup.ActiveCount?_setup.ConquerorNames[owner]:"攻撃側";
        BattleInfo($"旗：{name} / {(defense?"防衛":"奪取")}成功まであと{Math.Max(0,total-elapsed)}ターン",794,.48f);
        _ui.Box(new(1120,834,720,16),Muted);
        int width=(int)(720*Math.Clamp(elapsed/(float)total,0,1));
        if(width>0)_ui.Box(new(1120,834,width,16),defense?new Color(120,210,225):new Color(255,182,100));
    }
}

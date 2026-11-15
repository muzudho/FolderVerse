namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle GlobeSettingsMenuItem=new(1320,518,538,52);
    private static readonly Rectangle GlobeSettingsPanel=new(555,263,810,545);
    private static readonly Rectangle GlobeSettingsPalette=new(620,649,680,56);
    private static readonly Rectangle GlobeSettingsBack=new(830,728,260,52);
    private bool _globeSettingsOpen;

    private bool GlobeSettingsInput(bool click,bool escape)
    {
        if(!_globeSettingsOpen)return false;
        if(escape || click && (GlobeSettingsBack.Contains(_pointer) || !GlobeSettingsPanel.Contains(_pointer)))
            _globeSettingsOpen=false;
        else if(click && GlobeSettingsPalette.Contains(_pointer))ToggleTerrainPalette();
        return true;
    }

    private void DrawGlobeSettings()
    {
        if(!_globeSettingsOpen)return;
        _ui.Box(OpeningCanvas,Color.Black*.65f);
        _ui.Box(GlobeSettingsPanel,new Color(24,45,57));
        _ui.Center("地星設定",new(620,284,680,57),.7f,Cream);
        var rows=new[]{
            ("世界 SEED",_setup.WorldSeed),("人物 SEED",_setup.CastSeed),
            ("配置 SEED",_setup.PlacementSeed),("運否天賦 SEED",_setup.FortuneSeed)
        };
        for(int i=0;i<rows.Length;i++)
        {
            int y=357+69*i;
            _ui.Box(new(620,y,680,54),new Color(32,58,72));
            _ui.Text(rows[i].Item1,new(647,y+12),.52f,Cream);
            _ui.Center(rows[i].Item2.ToString(),new(1000,y,269,54),.63f,Cream);
        }
        DrawTerrainPaletteButton(GlobeSettingsPalette);
        _ui.Button(GlobeSettingsBack,"戻る / Esc",Muted,.48f);
    }
}

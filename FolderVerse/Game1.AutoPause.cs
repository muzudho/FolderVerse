namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _autoPaused;
    private bool _autoPauseComputerOnly;
    private int _autoPausePage;
    private static readonly Rectangle AutoPauseClose=new(1230,20,60,46);
    private bool ComputerExhibition=>!_demoActive && _setup.ComputerOnly && _setup.PlayerSlot>=0 &&
        _screen is Screen.WorldStatus or Screen.Transport or Screen.Battle or Screen.Industry or Screen.BattleResearch or Screen.Disposition;
    private bool BuildingWorld=>_screen is Screen.LandConquest or Screen.Fortune;
    private bool AutomaticGame=>!_demoActive && !BuildingWorld && (_autoPlay || ComputerExhibition);
    private bool AutoPauseInput(bool click,bool escape)
    {
        if(!_autoPaused){
            if(!AutomaticGame || !(click || escape))return false;
            _autoPauseComputerOnly=_setup.ComputerOnly;
            _autoPaused=true;_autoPausePage=0;_statusMenuOpen=true;_countrySettingsOpen=false;
            _operations.Write("auto_paused",new{screen=_screen.ToString(),turn=_setup.Population.Turn});return true;
        }
        if(click && AutoPauseClose.Contains(_pointer) || escape && !_countrySettingsOpen && _autoPausePage==0){ResumeAutoPause();return true;}
        if(_countrySettingsOpen){CountrySettingsInput(click,escape);return true;}
        if(_autoPausePage!=0){
            if(escape || click && InactiveBack.Contains(_pointer)){_autoPausePage=0;return true;}
            if(click && _autoPausePage==2){
                var rulers=Enumerable.Range(0,_setup.ActiveCount).Where(r=>!_setup.Relations.Powered[r]).ToArray();
                for(int i=0;i<rulers.Length;i++)if(InsertBatteryButton(i).Contains(_pointer)){
                    _setup.Relations.RestoreBattery(_setup,rulers[i]);_world.ShowSetup(_setup,true);_lastAutoSaveTurn=-1;break;
                }
            }
            return true;
        }
        if(!click)return true;
        if(CountrySettingsMenuItem.Contains(_pointer))OpenCountrySettings();
        else if(BgmMenuItem.Contains(_pointer))_bgmMenuOpen=true;
        else if(ComputerManualMenuItem.Contains(_pointer))OpenComputerManual();
        else if(HierarchyMenuItem.Contains(_pointer))_autoPausePage=1;
        else if(InactiveMenuItem.Contains(_pointer))_autoPausePage=2;
        else if(AutoMenuItem.Contains(_pointer))ResumeAutoPause();
        else if(EndingMenuItem.Contains(_pointer)){_autoPaused=false;StartEnding();}
        else if(QuitMenuItem.Contains(_pointer)){_autoPaused=false;ReturnToTitle();}
        return true;
    }
    private void ResumeAutoPause()
    {
        if(_countrySettingsOpen)_lastAutoSaveTurn=-1;
        _autoPaused=false;_autoPausePage=0;_countrySettingsOpen=_statusMenuOpen=false;
        if(_autoPauseComputerOnly && !_setup.ComputerOnly){
            _autoPlay=false;_autoStepAge=_autoDomesticAge=0;_autoDomesticStage=0;
            int human=_setup.TurnPlayers().FirstOrDefault(-1);
            if(human>=0){
                if(_computerDomesticNodes!=null || _computerMarches!=null){_computerViewRuler=human;_computerViewLeader=human;}
                _setup.SelectTurnRuler(human);
                if(IsStatusScreen){ResetInspector();RestoreDomesticCamera();}
            }
        }
        _operations.Write("auto_resumed",new{automatic=_setup.ComputerOnly,screen=_screen.ToString(),turn=_setup.Population.Turn});
    }
    private void DrawAutoPause()
    {
        GraphicsDevice.Clear(new Color(16,35,46));var canvas=CanvasBounds();if(canvas.Width<=0 || canvas.Height<=0)return;
        var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        _spriteBatch.Begin(transformMatrix:transform);
        if(_countrySettingsOpen)DrawCountrySettings();
        else if(_autoPausePage==1)DrawHierarchyUi();
        else if(_autoPausePage==2)DrawInactiveUi();
        else{
            _ui.Text(_autoPauseComputerOnly?"Exhibition / 一時停止":"AUTO / 一時停止",new(80,88),.8f,Cream);
            _ui.Text("×でゲームを再開",new(80,154),.5f,Cream);
            _ui.Text($"人間 {_setup.PlayerCount} 人 / コンピューター {_setup.ComputerPlayerCount} 人",new(80,220),.5f,Cream);
            _ui.Button(StatusMenuButton,"＜システムメニュー＞",Muted,.52f);
            _ui.Button(InactiveMenuItem,"電池を戻す",Muted,.5f);
            _ui.Button(HierarchyMenuItem,"征服者の関係図 / 暫定順位",Muted,.5f);
            _ui.Button(EndingMenuItem,"ここでゲームをエンディングにする",Muted,.48f);
            _ui.Button(QuitMenuItem,"ゲームを止めてタイトル画面に戻る",Muted,.48f);
            _ui.Button(AutoMenuItem,"ゲームを再開する",Muted,.48f);
            _ui.Button(CountrySettingsMenuItem,"人間とコンピューター / 国旗の設定",Muted,.46f);
            DrawComputerManualMenuItem();
            _ui.Button(BgmMenuItem,"BGM・SE音量",Muted,.5f);
        }
        if(_autoPausePage!=0 && !_countrySettingsOpen)_ui.Button(InactiveBack,"システムメニューへ",Muted,.5f);
        _ui.Button(AutoPauseClose,"×",Muted,.7f);DrawBgmMenu();_spriteBatch.End();DrawFullscreenCursor();
    }
}

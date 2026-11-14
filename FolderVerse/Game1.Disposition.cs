namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _statusMenuOpen,_dispositionReturnList;
    private static readonly Rectangle StatusMenuButton=new(1510,20,348,46),InactiveMenuItem=new(1320,70,538,52),InactiveBack=new(54,1020,270,50);
    private static readonly Rectangle HierarchyMenuItem=new(1320,126,538,52);
    private bool StatusMenuRetiring=>MapFooterAction>=0 || _primaryActionSlide>0 || _directPairOpen;
    private bool StatusMenuInput(bool click,bool escape)
    {
        if(StatusMenuRetiring){_statusMenuOpen=false;return false;}
        if(_trainingOpen || _supervisorDialog>0 || _factionPickerOpen || _inspectorFoldersOpen || _wandererPolicyOpen || _rateTarget>=0)return false;
        if(!_statusMenuOpen && !(click && StatusMenuButton.Contains(_pointer)))return false;
        if(escape)_statusMenuOpen=false;else if(click)StatusMenuClick(_pointer);
        return true;
    }
    private static Rectangle DispositionButton(int index)=>new(380+index*400,860,370,70);
    private static Rectangle InactiveCard(int index)=>new(54+index%5*362,140+index/5*214,346,198);
    private static Rectangle InsertBatteryButton(int index){var r=InactiveCard(index);return new(r.X+145,r.Y+100,190,60);}
    private void OpenDisposition()
    {
        if(_setup.Relations.Pending.Count>0){_screen=Screen.Disposition;_populationCell=-1;}
    }
    private bool StatusMenuClick(Point point)
    {
        if(StatusMenuRetiring){_statusMenuOpen=false;return false;}
        if(StatusMenuButton.Contains(point)){_statusMenuOpen=!_statusMenuOpen;return true;}
        if(_statusMenuOpen && InactiveMenuItem.Contains(point))
        {CloseMovement();_statusMenuOpen=false;_screen=Screen.InactiveList;return true;}
        if(_statusMenuOpen && HierarchyMenuItem.Contains(point))
        {CloseMovement();_statusMenuOpen=false;_screen=Screen.Hierarchy;return true;}
        if(_statusMenuOpen && AutoMenuItem.Contains(point)){StartAutoPlay();return true;}
        if(_statusMenuOpen && CountrySettingsMenuItem.Contains(point)){OpenCountrySettings();return true;}
        if(_statusMenuOpen && BgmMenuItem.Contains(point)){_bgmMenuOpen=true;return true;}
        if(_statusMenuOpen && ComputerManualMenuItem.Contains(point)){OpenComputerManual();return true;}
        if(_statusMenuOpen && EndingMenuItem.Contains(point)){StartEnding();return true;}
        if(_statusMenuOpen && QuitMenuItem.Contains(point)){ReturnToTitle();return true;}
        _statusMenuOpen=false;return false;
    }
    private void DrawStatusMenu() => DrawTutorialChrome(()=>DrawStatusMenuCore(),true);
    private void DrawStatusMenuCore()
    {
        if(MapFooterAction>=0 || _primaryActionSlide>0)_statusMenuOpen=false;
        if(_primaryActionSlide>=1)return;
        var button=StatusMenuButton;button.Y-=(int)MathF.Round(100*MathHelper.SmoothStep(0,1,_primaryActionSlide));
        _ui.Button(button,"＜システムメニュー＞",Muted,.52f);
        if(_statusMenuOpen)
        {
            _ui.Button(InactiveMenuItem,"電池を戻す",Muted,.5f);
            _ui.Button(HierarchyMenuItem,"征服者の関係図 / 暫定順位",Muted,.5f);
            _ui.Button(EndingMenuItem,"ここでゲームをエンディングにする",Muted,.48f);
            _ui.Button(QuitMenuItem,"ゲームを止めてタイトル画面に戻る",Muted,.48f);
            _ui.Button(AutoMenuItem,"コンピューターに続きをやってもらう",Muted,.48f);
            _ui.Button(CountrySettingsMenuItem,"人間とコンピューター / 国旗の設定",Muted,.46f);
            DrawComputerManualMenuItem();
            _ui.Button(BgmMenuItem,"BGM・SE音量",Muted,.5f);
        }
    }
    private void DispositionInput(bool click,bool escape)
    {
        if(_dispositionScene!=null){if(click && _dispositionSceneAge>=1.2 || escape && _dispositionSceneAge>=1.2)FinishDispositionScene();return;}
        if(_screen==Screen.Disposition && MercyInput(click,escape))return;
        if(_screen==Screen.InactiveList)
        {
            if(escape || click && InactiveBack.Contains(_pointer)){_screen=Screen.WorldStatus;AutoSaveDomestic();return;}
            if(!click)return;
            var rulers=Enumerable.Range(0,_setup.ActiveCount).Where(r=>!_setup.Relations.Powered[r]).ToArray();
            for(int i=0;i<rulers.Length;i++)if(InsertBatteryButton(i).Contains(_pointer))
            {
                _setup.Relations.RestoreBattery(_setup,rulers[i]);
                _operations.Write("battery_restored",new{ruler=rulers[i]});_lastAutoSaveTurn=-1;_world.ShowSetup(_setup,true);return;
            }
            return;
        }
        if(!click)return;
        if(_setup.Relations.Pending.Count==0){_screen=Screen.WorldStatus;return;}
        var pending=_setup.Relations.Pending[0];
        for(int i=0;i<3;i++)if(DispositionButton(i).Contains(_pointer))
        {
            var choice=(DefeatedChoice)i;
            if(choice==DefeatedChoice.Release){
                if(!_setup.Relations.CanMercy(_setup,pending))return;
                _setup.Relations.EnsureComputerMercyOffer(_setup,pending);BeginMercyInput(pending,false);return;
            }
            bool accepted=_setup.Relations.Choose(_setup,pending,choice);
            _operations.Write("conqueror_disposition",new{pending,choice=choice.ToString(),accepted});
            if(!accepted)return;
            AwardTutorialCaptiveChoice(choice);
            _world.ShowSetup(_setup,true);
            if(choice is DefeatedChoice.RemoveBattery or DefeatedChoice.Recruit){_dispositionScene=pending;_dispositionSceneChoice=choice;_dispositionSceneAge=0;return;}
            if(_setup.Relations.Pending.Count>0)return;
            _screen=_dispositionReturnList?Screen.InactiveList:Screen.WorldStatus;_dispositionReturnList=false;
            return;
        }
    }
    private void DrawDispositionUi()
    {
        if(_dispositionScene!=null){DrawDispositionScene();return;}
        if(DrawMercyUi())return;
        if(_setup.Relations.Pending.Count==0)return;
        var captive=_setup.Relations.Pending[0];bool accepts=_setup.Relations.Accepts(_setup,captive.Ruler);
        if(!_mercyInput && _setup.Relations.CanMercy(_setup,captive) && _setup.HumanPlayers.Contains(captive.Ruler) && !_setup.Relations.MercyOffers.Any(o=>o.Captive==captive))BeginMercyInput(captive,true);
        _ui.Text("征服者の処遇",new(54,30),1f,Cream);
        _ui.Center(_setup.Nodes.Label(_setup.Nodes.All[captive.Node]),new(160,110,1600,45),.58f,Cream);
        for(int side=0;side<2;side++)
        {
            int ruler=side==0?captive.Victor:captive.Ruler,x=side==0?360:1080;
            _ui.Box(new(x-6,200,492,506),_setup.OwnerColor(ruler));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(x,206,480,494));
            _ui.Center((side==0?"勝者　":"敗れた征服者　")+_setup.ConquerorNames[ruler],new(x-80,720,640,48),.65f,Cream);
        }
        _ui.Center($"敗者の直轄地点：{_setup.Relations.OwnedMercyNodes(_setup,captive)}",new(200,157,1520,36),.45f,Cream);
        if(_mercyInput){DrawMercyNumber();return;}
        _ui.Center(_setup.Relations.Message,new(200,790,1520,44),.54f,new(255,196,112));
        string[] choices={accepts?"手下にする":"手下にする（拒否）","見逃し交渉","電池を引き抜く"};
        for(int i=0;i<3;i++)_ui.Button(DispositionButton(i),choices[i],i==0 && !accepts || i==1 && !_setup.Relations.CanMercy(_setup,captive)?Muted:Accent,.65f);
        if(TutorialCaptiveAchievementsAvailable)DrawTutorialCaptiveChoiceNotes();
        else _ui.Center("見逃し：敗者の申告分の地点を譲渡 / 交渉不成立なら電池を引き抜く",new(160,970,1600,48),.48f,Cream);
        _ui.Center(ConquerorRelations.MercyMinimumExplanation,new(160,1020,1600,36),.4f,Cream);
    }
    private void DrawInactiveUi()
    {
        _ui.Text("電池を戻す / 特別措置",new(54,30),.85f,Cream);
        _ui.Text("独立した征服者としてゲームに復活します。失った領地は戻りません。",new(54,85),.45f,Cream);
        var rulers=Enumerable.Range(0,_setup.ActiveCount).Where(r=>!_setup.Relations.Powered[r]).ToArray();
        if(rulers.Length==0)_ui.Center("電池を引き抜いた征服者はいない",new(200,400,1520,80),.8f,Cream);
        for(int i=0;i<rulers.Length;i++)
        {
            int ruler=rulers[i];var r=InactiveCard(i);_ui.Box(r,new(27,49,62));
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(r.X+8,r.Y+8,130,160),false);
            _ui.Center(_setup.ConquerorNames[ruler],new(r.X+145,r.Y+14,190,70),Math.Min(.48f,185/_font.MeasureString(_setup.ConquerorNames[ruler]).X),Cream);
            _ui.Button(InsertBatteryButton(i),"電池を戻す",Accent,.49f);
            _ui.Text("Point "+(_setup.Nodes.Current(ruler).Id+1),new(r.X+10,r.Bottom-27),.38f,Cream);
        }
        _ui.Button(InactiveBack,"ゲームへ戻る",Muted,.58f);
    }
}

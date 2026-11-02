namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _countrySettingsOpen;
    private int _countrySettingsRuler;
    private static readonly Rectangle CountrySettingsMenuItem=new(1320,350,538,52);
    private static readonly Rectangle CountrySettingsClose=new(1730,85,100,50),CountryHumanButton=new(1110,305,320,60),CountryComputerButton=new(1450,305,320,60);
    private static Rectangle CountrySettingsCard(int i)=>new(90+i%5*194,200+i/5*62,186,56);
    private static Rectangle CountryColorOption(int row,int i)=>new(1110+i%10*66,455+row*120+i/10*48,58,42);
    private static Rectangle CountryPatternOption(int i)=>new(1110+i%3*220,730+i/3*64,208,54);
    private void OpenCountrySettings()
    {
        _countrySettingsRuler=Math.Clamp(_setup.PlayerSlot,0,_setup.ActiveCount-1);
        _countrySettingsOpen=true;_statusMenuOpen=false;
    }
    private bool CountrySettingsInput(bool click,bool escape)
    {
        if(!_countrySettingsOpen)return false;
        if(!IsStatusScreen){_countrySettingsOpen=false;return false;}
        if(escape || click && CountrySettingsClose.Contains(_pointer))
        {
            _countrySettingsOpen=false;
            // Keep an unfinished human's turn when its controller still exists.
            int controller=_setup.ControllerOf(_setup.PlayerSlot);
            if(controller<0 || _setup.Campaign.Submitted.Contains(controller))
            {
                int next=_setup.TurnPlayers().FirstOrDefault(r=>!_setup.Campaign.Submitted.Contains(r),-1);
                if(next<0)next=_setup.TurnPlayers().FirstOrDefault(-1);
                if(next>=0){StoreCurrentMovementDraft();CloseMovement();ResetInspector();_setup.SelectTurnRuler(next);RestoreDomesticCamera();}
            }
            if(_setup.ControllerOf(_setup.PlayerSlot)>=0)_setup.SelectTurnRuler(_setup.PlayerSlot);
            _lastAutoSaveTurn=-1;AutoSaveDomestic();
            if(_setup.ComputerOnly)StartAutoPlay();
            return true;
        }
        if(!click)return true;
        for(int i=0;i<_setup.ActiveCount;i++)if(CountrySettingsCard(i).Contains(_pointer)){_countrySettingsRuler=i;return true;}
        int ruler=_countrySettingsRuler;
        if(CountryHumanButton.Contains(_pointer) || CountryComputerButton.Contains(_pointer))
        {
            _setup.SetHumanPlayer(ruler,CountryHumanButton.Contains(_pointer));
            _operations.Write("country_controller_changed",new{ruler,human=_setup.IsHumanPlayer(ruler)});return true;
        }
        int primary=_setup.ColorIndices[ruler]%WorldSetup.Colors.Length,secondary=_setup.CountrySecondaryColor(ruler),pattern=_setup.CountryPattern(ruler);
        bool changed=false;
        for(int i=0;i<WorldSetup.Colors.Length;i++)for(int row=0;row<2;row++)if(CountryColorOption(row,i).Contains(_pointer)){if(row==0)primary=i;else secondary=i;changed=true;}
        for(int i=0;i<WorldSetup.CountryPatternNames.Length;i++)if(CountryPatternOption(i).Contains(_pointer)){pattern=i;changed=true;}
        if(changed){_setup.SetCountryAppearance(ruler,primary,secondary,pattern);_world.ShowSetup(_setup,true);_net.ClearGlobeRobotBadges();_operations.Write("country_appearance_changed",new{ruler,primary,secondary,pattern});}
        return true;
    }
    private void DrawCountrySwatch(Rectangle area,int ruler,int pattern=-1)
    {
        pattern=pattern<0?_setup.CountryPattern(ruler):pattern;
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            int left=area.X+x*area.Width/10,top=area.Y+y*area.Height/10;
            var color=WorldSetup.CountryPatternSecondary(pattern,x,y)?WorldSetup.Colors[_setup.CountrySecondaryColor(ruler)]:_setup.OwnerColor(ruler);
            _ui.Box(new(left,top,area.X+(x+1)*area.Width/10-left,area.Y+(y+1)*area.Height/10-top),color);
        }
    }
    private void DrawCountrySettings()
    {
        if(!_countrySettingsOpen)return;
        _ui.Box(new(60,65,1800,950),new(18,36,48));
        _ui.Text("人間とコンピューター / 国の色と網かけ",new(90,95),.72f,Cream);
        _ui.Button(CountrySettingsClose,"閉じる",Muted,.50f);
        _ui.Text("設定する征服者を選んでください",new(90,155),.46f,Cream);
        for(int i=0;i<_setup.ActiveCount;i++)
        {
            var r=CountrySettingsCard(i);_ui.Button(r,"",i==_countrySettingsRuler?Accent:Muted,.4f);
            DrawCountrySwatch(new(r.X+4,r.Y+7,36,40),i);
            string name=$"{i+1} {_setup.ConquerorNames[i]}";
            _ui.Text(name,new(r.X+46,r.Y+5),Math.Min(.36f,134/_font.MeasureString(name).X),Cream);
            _ui.Text(_setup.IsHumanPlayer(i)?"人間":"コンピューター",new(r.X+46,r.Y+30),.29f,Cream);
        }
        int ruler=_countrySettingsRuler;
        _portraitRenderer.Draw(_spriteBatch,_setup.Looks[ruler],new(1110,160,120,130),false);
        _ui.Text(_setup.ConquerorNames[ruler],new(1260,200),.64f,Cream);
        bool human=_setup.IsHumanPlayer(ruler);
        _ui.Button(CountryHumanButton,"人間",human?Accent:Muted,.6f);
        _ui.Button(CountryComputerButton,"コンピューター",human?Muted:Accent,.56f);
        _ui.Text("人間の手下は自分のターンで操作します。",new(1110,380),.40f,Cream);
        _ui.Text("基本の色",new(1110,420),.45f,Cream);
        _ui.Text("模様の色",new(1110,540),.45f,Cream);
        for(int row=0;row<2;row++)for(int i=0;i<WorldSetup.Colors.Length;i++)
        {
            var r=CountryColorOption(row,i);
            bool selected=i==(row==0?_setup.ColorIndices[ruler]%WorldSetup.Colors.Length:_setup.CountrySecondaryColor(ruler));
            _ui.Box(r,selected?Cream:Muted);_ui.Box(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),WorldSetup.Colors[i]);
        }
        _ui.Text("網かけ / 回転による向き違いは共通",new(1110,685),.45f,Cream);
        for(int i=0;i<WorldSetup.CountryPatternNames.Length;i++)
        {
            var r=CountryPatternOption(i);_ui.Button(r,"",i==_setup.CountryPattern(ruler)?Accent:Muted,.4f);
            DrawCountrySwatch(new(r.X+6,r.Y+6,52,42),ruler,i);_ui.Text(WorldSetup.CountryPatternNames[i],new(r.X+72,r.Y+15),.45f,Cream);
        }
        _ui.Text("国の色表示で地図にも模様が付きます。",new(1110,885),.43f,Cream);
        _ui.Text("コンピューターの手下は人間の親分が操作します。",new(1110,930),.37f,Cream);
        _ui.Text("人間が０人になると自動プレイを開始します。",new(1110,970),.37f,Cream);
    }
}

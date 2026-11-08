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
    private static readonly Rectangle CountryShowOperations=new(1450,250,320,44);
    private static Rectangle CountrySettingsCard(int i)=>new(90+i%5*194,200+i/5*62,186,56);
    private static Rectangle CountryColorOption(int row,int i)=>new(1110+i%10*66,645+row*120+i/10*48,58,42);
    private static Rectangle CountryPatternOption(int i)=>new(1110+i%3*220,430+i/3*64,208,54);
    private static Rectangle CountryEmblemOption(int i)=>new(1110+i%8*87,896+i/8*74,80,68);
    private static Rectangle CountryFlagShapeOption(int i)=>new(1110+i*170,565,162,38);
    private void OpenCountrySettings()
    {
        _countrySettingsRuler=Math.Clamp(_setup.PlayerSlot,0,_setup.ActiveCount-1);
        _countrySettingsOpen=true;_statusMenuOpen=false;
    }
    private bool CountrySettingsInput(bool click,bool escape)
    {
        if(!_countrySettingsOpen)return false;
        if(!IsStatusScreen && !_autoPaused){_countrySettingsOpen=false;return false;}
        if(escape || click && CountrySettingsClose.Contains(_pointer))
        {
            _countrySettingsOpen=false;
            if(_autoPaused){_lastAutoSaveTurn=-1;return true;}
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
        if(CountryShowOperations.Contains(_pointer))
        {
            if(!_setup.IsHumanPlayer(ruler))_setup.ToggleComputerOperations(ruler);
            return true;
        }
        if(CountryHumanButton.Contains(_pointer) || CountryComputerButton.Contains(_pointer))
        {
            _setup.SetHumanPlayer(ruler,CountryHumanButton.Contains(_pointer));
            _operations.Write("country_controller_changed",new{ruler,human=_setup.IsHumanPlayer(ruler)});return true;
        }
        int primary=_setup.ColorIndices[ruler]%WorldSetup.Colors.Length,secondary=_setup.CountrySecondaryColor(ruler),pattern=_setup.CountryPattern(ruler);
        bool changed=false;
        for(int i=0;i<WorldSetup.Colors.Length;i++)for(int row=0;row<(pattern==0 && _setup.CountryEmblem(ruler)==0?1:2);row++)if((row==0 || i!=primary) && CountryColorOption(row,i).Contains(_pointer)){if(row==0)primary=i;else secondary=i;changed=true;}
        for(int i=0;i<WorldSetup.CountryPatternNames.Length;i++)if(CountryPatternOption(i).Contains(_pointer)){pattern=i;changed=true;}
        for(int i=0;i<WorldSetup.CountryEmblemNames.Length;i++)if(CountryEmblemOption(i).Contains(_pointer)){_setup.SetCountryEmblem(ruler,i);changed=true;}
        for(int i=0;i<WorldSetup.CountryFlagShapeNames.Length;i++)if(CountryFlagShapeOption(i).Contains(_pointer)){_setup.SetCountryFlagShape(ruler,i);changed=true;}
        if(changed){_setup.SetCountryAppearance(ruler,primary,secondary,pattern);secondary=_setup.CountrySecondaryColor(ruler);_world.ShowSetup(_setup,true);_net.ClearGlobeRobotBadges();_operations.Write("country_appearance_changed",new{ruler,primary,secondary,pattern});}
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
        int side=Math.Min(area.Width,area.Height)*3/4;
        DrawCountryEmblem(new(area.Center.X-side/2,area.Center.Y-side/2,side,side),ruler);
    }
    private void DrawCountrySettings()
    {
        if(!_countrySettingsOpen)return;
        _ui.Text("人間とコンピューター / 国旗の設定",new(90,95),.72f,Cream);
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
        var toggle=CountryShowOperations;
        bool show=_setup.ShowComputerOperations(ruler);
        _ui.Box(toggle,human?new Color(30,44,57):toggle.Contains(_pointer)?Color.Lerp(Muted,Color.White,.14f):Muted);
        var box=new Rectangle(toggle.X+16,toggle.Y+10,24,24);
        var ink=human?new Color(144,157,168):Cream;
        _ui.Box(box,ink);
        box.Inflate(-2,-2);
        _ui.Box(box,new Color(18,36,48));
        if(show)
        {
            box.Inflate(-3,-3);
            _ui.Box(box,human?ink:Accent);
        }
        _ui.Text("操作を表示",new(toggle.X+54,toggle.Y+8),.50f,ink);
        _ui.Text("人間の手下は自分のターンで操作します。",new(1110,380),.40f,Cream);
        CountryFlagArt.Draw(_ui,_setup,ruler,new(1580,160,180,100));
        _ui.Text("国旗の形",new(1110,549),.28f,Cream);
        for(int i=0;i<WorldSetup.CountryFlagShapeNames.Length;i++)_ui.Button(CountryFlagShapeOption(i),WorldSetup.CountryFlagShapeNames[i],_setup.CountryFlagShape(ruler)==i?Accent:Muted,.32f);
        _ui.Text("基本の色",new(1110,610),.45f,Cream);
        if(_setup.CountryPattern(ruler)!=0 || _setup.CountryEmblem(ruler)!=0)_ui.Text("２色目",new(1110,730),.45f,Cream);
        for(int row=0;row<(_setup.CountryPattern(ruler)==0 && _setup.CountryEmblem(ruler)==0?1:2);row++)for(int i=0;i<WorldSetup.Colors.Length;i++)
        {
            if(row==1 && i==_setup.ColorIndices[ruler]%WorldSetup.Colors.Length)continue;
            var r=CountryColorOption(row,i);
            bool selected=i==(row==0?_setup.ColorIndices[ruler]%WorldSetup.Colors.Length:_setup.CountrySecondaryColor(ruler));
            _ui.Box(r,selected?Cream:Muted);_ui.Box(new(r.X+4,r.Y+4,r.Width-8,r.Height-8),WorldSetup.Colors[i]);
        }
        _ui.Text("網かけ / 回転による向き違いは共通",new(1110,405),.45f,Cream);
        for(int i=0;i<WorldSetup.CountryPatternNames.Length;i++)
        {
            var r=CountryPatternOption(i);_ui.Button(r,"",i==_setup.CountryPattern(ruler)?Accent:Muted,.4f);
            DrawCountrySwatch(new(r.X+6,r.Y+6,52,42),ruler,i);_ui.Text(WorldSetup.CountryPatternNames[i],new(r.X+72,r.Y+15),.45f,Cream);
        }
        _ui.Text("ワンポイント / ２色目で表示",new(1110,864),.4f,Cream);
        for(int i=0;i<WorldSetup.CountryEmblemNames.Length;i++){
            var option=CountryEmblemOption(i);_ui.Button(option,"",_setup.CountryEmblem(ruler)==i?Accent:Muted,.4f);
            if(i!=0)CountryEmblemArt.Draw(_ui,new(option.Center.X-20,option.Y+3,40,40),i,WorldSetup.Colors[_setup.CountrySecondaryColor(ruler)]);
            string label=WorldSetup.CountryEmblemNames[i];
            _ui.Center(label,new(option.X,option.Bottom-25,option.Width,22),Math.Min(.28f,(option.Width-4)/_font.MeasureString(label).X),Cream);
        }
    }
}

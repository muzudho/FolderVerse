namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>index switch {0=>new(780,112,184,36),4=>new(972,112,112,36),5=>new(1092,112,140,36),1=>new(1240,112,112,36),6=>new(1360,112,112,36),2=>new(1480,112,190,36),_=>new(1678,112,180,36)};
    private bool MapOverlayAvailable(int index)=>!MapTransitionActive && !OrientationRotating && !_net.IsAnimating && (index!=0 || _statusGlobe);

    private static readonly Rectangle MapViewButton=new(1674,146,184,48);
    private int _selectedMapCell=-1;
    private bool _simpleDisplayOpen;
    private static readonly Rectangle SimpleDisplayButton=new(1260,76,360,36);
    private static Rectangle SimpleDisplayOption(int i)=>new(1260,204+i*44,650,44);
    private bool _mapLayerListOpen;
    private bool _trafficLayerListOpen;
    private static readonly string[] MapLayerNames={"地形","国区画色分け"};
    private static readonly string[] TrafficLayerNames={"地域を選択","地点を選択"};
    private static Rectangle MapLayerOption(int i)=>new(1480,146+i*52,190,52);
    private static Rectangle TrafficLayerOption(int i)=>new(1638,146+i*52,220,52);
    private bool RouteLayerClick(Point pointer)
    {
        if(_simpleDisplayOpen){
            if(DisplayBreakerAutomaticRow.Contains(pointer)){_displayBreaker.Automatic=!_displayBreaker.Automatic;return true;}
            for(int i=0;i<8;i++)if(SimpleDisplayOption(i).Contains(pointer)){
                bool enabled=!DisplayFeatureEnabled(i);SetDisplayFeature(i,enabled);
                _displayBreaker.Manual(i,enabled,DisplayClock);return true;
            }
            _simpleDisplayOpen=false;return true;
        }
        if(SimpleDisplayButton.Contains(pointer)){_simpleDisplayOpen=true;_mapLayerListOpen=_trafficLayerListOpen=false;return true;}
        if(FolderTreeToolbarClick(pointer))return true;
        if(_mapLayerListOpen || _trafficLayerListOpen)
        {
            bool traffic=_trafficLayerListOpen;_mapLayerListOpen=_trafficLayerListOpen=false;
            for(int i=0;i<2;i++)if((traffic?TrafficLayerOption(i):MapLayerOption(i)).Contains(pointer))
            {
                if(traffic){if(i==0 && _movementOpen)CloseMovement();_net.ShowRoutes=i==1;}
                else {_net.ShowCountryColors=i>0;_world.CountryColors=i>0;_net.ControlledCountryColors=_world.ControlledCountryColors=!_net.ShowOtherCountries;_world.ShowSetup(_setup,true);}
                _inputOutcome="layer_changed";return true;
            }
            return true;
        }
        if(!_setup.IsPrimaplana && MapViewButton.Contains(pointer)){StartMapTransition();_inputOutcome="view_transition_started";_dragging=_mapDragging=false;return true;}
        for(int i=0;i<7;i++)if((i is 2 or 3 or 6) && RouteLayerButton(i).Contains(pointer))
        {
            CloseReservationDirectory();
            if((i==0 || i==4) && !MapOverlayAvailable(i))return true;
            if(i==5){_net.ShowRobots=!_net.ShowRobots;_net.ClearGlobeRobotBadges();}
            if(i==6){_net.ShowOtherCountries=!_net.ShowOtherCountries;_net.ControlledCountryColors=_world.ControlledCountryColors=!_net.ShowOtherCountries;_world.ShowSetup(_setup,true);_net.ClearGlobeRobotBadges();}
            if(i==4)_net.ShowEquator=!_net.ShowEquator;
            if(i==0)_net.ShowMapSymbols=!_net.ShowMapSymbols;
            if(i==1)_net.ShowBatteries=!_net.ShowBatteries;
            if(i==2)_mapLayerListOpen=true;
            if(i==3)_trafficLayerListOpen=true;
            return true;
        }
        return false;
    }
    private void DrawRouteLayerButtons() => DrawTutorialChrome(()=>DrawRouteLayerButtonsCore(),true);
    private void DrawRouteLayerButtonsCore()
    {
        DrawFolderTreeButton();DrawReservationDirectoryButton();
        string[] names={"緯番・経番","電池","赤道","ロボット","他国"};bool[] enabled={_net.ShowMapSymbols,_net.ShowBatteries,_net.ShowEquator,_net.ShowRobots,_net.ShowOtherCountries};int[] indices={0,1,4,5,6};
        for(int i=4;i<5;i++){
            if(_setup.IsPrimaplana && indices[i] is 0 or 4)continue;
            var r=RouteLayerButton(indices[i]);bool available=indices[i] is 1 or 5 or 6 || MapOverlayAvailable(indices[i]);
            var color=available?Accent:new Color(75,78,83);
            _ui.Button(r,"",color,.5f);
            PortraitHudLayout.CenterLines(_ui,_font,new[]{names[i]},new Rectangle(r.X+28,r.Y,r.Width-30,r.Height),.42f,available?UiPainter.ContrastInk(color):new Color(155,158,163));
            _ui.Box(new(r.X+9,r.Center.Y-6,12,12),new Color(23,30,36));
            _ui.Box(new(r.X+11,r.Center.Y-4,8,8),!available?new Color(105,108,113):enabled[i]?new Color(114,255,143):new Color(38,65,30));
        }
        _ui.Button(SimpleDisplayButton,"表示ブレーカー  v",Accent,.42f);
        int mode=_net.ShowCountryColors?1:0,traffic=_net.ShowRoutes?1:0;
        _ui.Button(RouteLayerButton(2),MapLayerNames[mode]+"  v",Accent,.42f);
        DrawSelectionIcon(RouteLayerButton(3),traffic==1);
        if(!_setup.IsPrimaplana)_ui.Button(MapViewButton,_statusGlobe?"展開図":"組立",MapTransitionActive?Muted:Accent,.6f);
        if(_mapLayerListOpen)for(int i=0;i<2;i++)_ui.Button(MapLayerOption(i),MapLayerNames[i],i==mode?Accent:Muted,.52f);
        if(_trafficLayerListOpen)for(int i=0;i<2;i++){var r=TrafficLayerOption(i);_ui.Button(r,"",i==traffic?Accent:Muted,.52f);DrawSelectionGlyph(r.X+30,r.Center.Y,i==1,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));_ui.Text(TrafficLayerNames[i],new(r.X+62,r.Y+15),.4f,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));}
    }
    private void DrawSimpleDisplayMenu()
    {
        if(_simpleDisplayOpen){
            _ui.Button(DisplayBreakerAutomaticRow,"",Muted,.4f);
            DrawBreakerCheck(DisplayBreakerAutomaticRow,_displayBreaker.Automatic);
            _ui.Text("自動OFF / 目標18 FPS",new(1302,128),.4f,Cream);
            _ui.Box(new(1260,160,650,44),new Color(23,43,56));
            _ui.Text("表示項目",new(1302,172),.38f,Cream);_ui.Text("CPU ms/frame",new(1650,172),.35f,Cream);
            for(int i=0;i<8;i++){
                var r=SimpleDisplayOption(i);_ui.Button(r,"",Muted,.4f);DrawBreakerCheck(r,DisplayFeatureEnabled(i));
                _ui.Text(DisplayFeatureNames[i],new(r.X+42,r.Y+12),.38f,Cream);
                string timing=i==7?"－（個別計測なし）":_displayBreaker.LastCost[i].ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" ms";
                if(_displayBreaker.Tripped[i])timing+=" / 自動OFF";
                _ui.Text(timing,new(r.X+380,r.Y+12),.32f,Cream);
            }
            _ui.Box(new(1260,556,650,70),new Color(23,43,56));
            _ui.Text("旗の時間は旗全体。高低差は手動切替。",new(1274,565),.31f,Cream);
            _ui.Text(_breakerMessage.Length>0?_breakerMessage:"自動ではONに戻りません / 手動ON後10秒は保護",new(1274,595),.30f,Cream);
        }
    }
    private void DrawSelectionIcon(Rectangle rect,bool node)
    {
        _ui.Button(rect,"",Accent,.52f);
        int x=rect.X+rect.Width/2-20,y=rect.Center.Y;
        DrawSelectionGlyph(x,y,node); for(int i=0;i<7;i++)_ui.Box(new(x+28+i,y-3+i,13-i*2,1),SelectionInk); if(rect.Contains(_pointer) && !_trafficLayerListOpen)_ui.Text(TrafficLayerNames[node?1:0],new(rect.X,rect.Bottom+4),.45f,Cream); } private void DrawSelectionGlyph(int x,int y,bool node,Color? ink=null){var glyphInk=ink??SelectionInk; if(node){
            _ui.Box(new(x-18,y-2,36,4),glyphInk);_ui.Box(new(x-2,y-18,4,36),glyphInk);
            for(int dy=-8;dy<=8;dy++){int w=(int)System.Math.Sqrt(64-dy*dy);_ui.Box(new(x-w,y+dy,w*2+1,1),glyphInk);}
        }else{
            for(int i=0;i<3;i++){_ui.Box(new(x-16+i*16,y-16,1,32),glyphInk);_ui.Box(new(x-16,y-16+i*16,32,1),glyphInk);}
            _ui.Box(new(x+2,y+2,12,12),glyphInk);
        }

    }}


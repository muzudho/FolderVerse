namespace FolderVerse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>index==2?new(1260,146,344,44):new(1616,146,242,44);

    private static readonly Rectangle MapViewButton=new(1572,86,286,48);
    private Texture2D _mapViewToggleArt;
    private readonly Rectangle[] _mapViewToggleIcons=new Rectangle[4];
    private int _selectedMapCell=-1;
    private bool _simpleDisplayOpen;
    private bool _displayFilterOpen;
    private const int DefaultDomesticHiddenMapFilters=(1<<8)|(1<<21)|(1<<23); // Hidden foreign edges; danger style for own/subordinate borders.
    private static readonly Rectangle SimpleDisplayButton=new(1260,86,144,48),DisplayFilterButton=new(1416,86,144,48);
    private static readonly Rectangle DisplayBreakerAutomaticRow=new(1260,246,650,44);
    private static readonly Rectangle DisplayFilterHeader=new(1416,202,470,44);
    private static readonly Rectangle DisplayFilterReset=new(1416,692,470,44);
    private static readonly string[] DisplayFilterLabels={
        "自国の地点","手下の地点","他国の地点",
        "自国と自国の地点間の辺","自国と手下の地点間の辺","自国と他国の地点間の辺",
        "手下と手下の地点間の辺","手下と他国の地点間の辺","他国と他国の地点間の辺"
    };
    private static Rectangle DisplayFilterRow(int index)=>new(DisplayFilterHeader.X,DisplayFilterHeader.Bottom+index*44+(index>=3?36:0),DisplayFilterHeader.Width,44);
    private int DomesticHiddenMapFilters=>_folderTreeLayers?.HiddenFilters??(_reservationDirectory?_directoryBefore.HiddenFilters:_net.HiddenMapFilters);
    private static Rectangle SimpleDisplayOption(int i)=>new(1260,334+i*44,650,44);
    private Texture2D _displayControlIcons;
    private readonly Rectangle[] _displayControlIconSources=new Rectangle[2];
    private bool _mapLayerListOpen;
    private bool _trafficLayerListOpen;
    private static readonly string[] MapLayerNames={"地形","国区画色分け"};
    private static readonly string[] TrafficLayerNames={"地域を選択","地点を選択"};
    private static Rectangle MapLayerOption(int i)=>new(1260,202+i*52,344,52);
    private static Rectangle TrafficLayerOption(int i)=>new(1616,202+i*52,242,52);

    private bool RouteLayerMenuInput(bool active,bool click,bool escape)
    {
        if(!_simpleDisplayOpen && !_displayFilterOpen && !_mapLayerListOpen && !_trafficLayerListOpen)return false;
        if(active && escape)_simpleDisplayOpen=_displayFilterOpen=_mapLayerListOpen=_trafficLayerListOpen=false;
        else if(active && click)RouteLayerClick(_pointer);
        return true;
    }

    private bool RouteLayerClick(Point pointer)
    {
        if(SimpleDisplayButton.Contains(pointer) || DisplayFilterButton.Contains(pointer)){
            bool breaker=SimpleDisplayButton.Contains(pointer);
            _simpleDisplayOpen=breaker && !_simpleDisplayOpen;
            _displayFilterOpen=!breaker && !_displayFilterOpen;
            _mapLayerListOpen=_trafficLayerListOpen=false;
            if(_displayFilterOpen){CloseFolderTreeState();CloseReservationDirectory();}
            return true;
        }
        if(_simpleDisplayOpen){
            if(DisplayBreakerAutomaticRow.Contains(pointer)){_displayBreaker.Automatic=!_displayBreaker.Automatic;return true;}
            for(int i=0;i<8;i++)if(SimpleDisplayOption(i).Contains(pointer)){
                bool enabled=!DisplayFeatureEnabled(i);SetDisplayFeature(i,enabled);
                _displayBreaker.Manual(i,enabled,DisplayClock);return true;
            }
            _simpleDisplayOpen=false;return true;
        }
        if(_displayFilterOpen){
            for(int i=0;i<DisplayFilterLabels.Length;i++)if(DisplayFilterRow(i).Contains(pointer)){
                _net.CycleMapFilter(i);
                RefreshDisplayFilters();
                return true;
            }
            if(DisplayFilterReset.Contains(pointer)){
                _net.HiddenMapFilters=DefaultDomesticHiddenMapFilters;
                RefreshDisplayFilters();
            }else if(!DisplayFilterHeader.Contains(pointer) && !DisplayFilterEdgeHeading.Contains(pointer))_displayFilterOpen=false;
            return true;
        }
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
        if(FolderTreeToolbarClick(pointer))return true;
        if(!_setup.IsPrimaplana && MapViewButton.Contains(pointer)){StartMapTransition();_inputOutcome="view_transition_started";_dragging=_mapDragging=false;return true;}
        _mapLayerListOpen=RouteLayerButton(2).Contains(pointer);
        _trafficLayerListOpen=RouteLayerButton(3).Contains(pointer);
        if(!_mapLayerListOpen && !_trafficLayerListOpen)return false;
        CloseReservationDirectory();return true;
    }
    private void RefreshDisplayFilters()
    {
        bool controlledOnly=!_net.ShowOtherCountries;
        _net.ControlledCountryColors=controlledOnly;
        if(_world.ControlledCountryColors!=controlledOnly){
            _world.ControlledCountryColors=controlledOnly;
            _world.ShowSetup(_setup,true);
        }
        _net.HoveredNode=-1;
        _net.ClearGlobeRobotBadges();
    }
    private void DrawRouteLayerButtons() => DrawTutorialChrome(()=>DrawRouteLayerButtonsCore(),true);
    private void DrawRouteLayerButtonsCore()
    {
        DrawFolderTreeButton();DrawReservationDirectoryButton();
        DrawDisplayControlButton(SimpleDisplayButton,0,_simpleDisplayOpen);
        DrawDisplayControlButton(DisplayFilterButton,1,_displayFilterOpen);
        int mode=_net.ShowCountryColors?1:0,traffic=_net.ShowRoutes?1:0;
        _ui.Button(RouteLayerButton(2),MapLayerNames[mode]+"  v",Accent,.42f);
        DrawSelectionIcon(RouteLayerButton(3),traffic==1);
        if(!_setup.IsPrimaplana)DrawMapViewButton();
        if(_mapLayerListOpen)for(int i=0;i<2;i++)_ui.Button(MapLayerOption(i),MapLayerNames[i],i==mode?Accent:Muted,.52f);
        if(_trafficLayerListOpen)for(int i=0;i<2;i++){var r=TrafficLayerOption(i);_ui.Button(r,"",i==traffic?Accent:Muted,.52f);DrawSelectionGlyph(r.X+30,r.Center.Y,i==1,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));_ui.Text(TrafficLayerNames[i],new(r.X+62,r.Y+15),.4f,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));}
    }
    private void DrawDisplayControlButton(Rectangle button,int index,bool open)
    {
        if(_displayControlIcons==null){
            _displayControlIcons=LoadUiTexture("map-display-controls-v001.png");
            int half=_displayControlIcons.Width/2;
            for(int i=0;i<_displayControlIconSources.Length;i++)
                _displayControlIconSources[i]=UiArtInkBounds(_displayControlIcons,new(i*half,0,half,_displayControlIcons.Height));
        }
        bool filtered=index==1 && DomesticHiddenMapFilters!=DefaultDomesticHiddenMapFilters;
        bool flash=filtered && (int)(_animationTime*2)%2==0;
        DrawPrimaryActionSurface(button,flash?new Color(255,194,73):Accent);
        var track=new Rectangle(button.X+6,button.Y+4,button.Width-12,button.Height-8);
        var trackColor=flash?new Color(80,56,22):open || button.Contains(_pointer)?new Color(29,70,86):new Color(14,35,47);
        PrimaryRoundBox(track,trackColor,8);
        var source=_displayControlIconSources[index];
        int width=source.Width*36/source.Height;
        _spriteBatch.Draw(_displayControlIcons,new Rectangle(button.Center.X-width/2-8,button.Center.Y-18,width,36),source,Color.White);
        for(int i=0;i<6;i++)_ui.Box(new(button.Right-30+i,button.Center.Y-2+i,11-i*2,1),Cream);
        if(filtered)PrimaryRoundBox(new(button.X+13,button.Y+10,8,8),new Color(255,194,73),4);
    }
    private void DrawMapViewButton()
    {
        var button=MapViewButton;
        if(_mapViewToggleArt==null)
        {
            _mapViewToggleArt=LoadUiTexture("map-view-toggle-icons-v001.png");
            int width=_mapViewToggleArt.Width/2,height=_mapViewToggleArt.Height/2;
            for(int i=0;i<_mapViewToggleIcons.Length;i++)
                _mapViewToggleIcons[i]=UiArtInkBounds(_mapViewToggleArt,new(i%2*width,i/2*height,width,height));
        }
        DrawPrimaryActionSurface(button,MapTransitionActive?Muted:Accent);
        var track=new Rectangle(button.X+6,button.Y+4,button.Width-12,button.Height-8);
        PrimaryRoundBox(track,new Color(14,35,47),8);
        int selected=_statusGlobe?0:1;
        for(int view=0;view<2;view++)
        {
            var slot=new Rectangle(track.X+view*track.Width/2,track.Y,track.Width/2,track.Height);
            bool highlighted=view==selected;
            if(highlighted)PrimaryRoundBox(new(slot.X+2,slot.Y+1,slot.Width-4,slot.Height-2),new Color(29,70,86),7);
            // Atlas columns are globe and net. The second row is the highlighted pair.
            var source=_mapViewToggleIcons[view+(highlighted?2:0)];
            const int iconHeight=36;
            int iconWidth=source.Width*iconHeight/source.Height;
            var icon=new Rectangle(slot.Center.X-iconWidth/2,slot.Center.Y-iconHeight/2,iconWidth,iconHeight);
            _spriteBatch.Draw(_mapViewToggleArt,icon,source,Color.White);
        }
    }
    private void DrawDisplayControlMenus()
    {
        if(_simpleDisplayOpen){
            _ui.Box(new(1260,202,650,44),new Color(23,43,56));
            _ui.Center("表示ブレーカー",new(1260,202,650,44),.46f,Cream);
            _ui.Button(DisplayBreakerAutomaticRow,"",Muted,.4f);
            DrawBreakerCheck(DisplayBreakerAutomaticRow,_displayBreaker.Automatic);
            _ui.Text("自動OFF / 目標18 FPS",new(1302,258),.4f,Cream);
            _ui.Box(new(1260,290,650,44),new Color(23,43,56));
            _ui.Text("表示項目",new(1302,302),.38f,Cream);_ui.Text("CPU ms/frame",new(1650,302),.35f,Cream);
            for(int i=0;i<8;i++){
                var r=SimpleDisplayOption(i);_ui.Button(r,"",Muted,.4f);DrawBreakerCheck(r,DisplayFeatureEnabled(i));
                _ui.Text(DisplayFeatureNames[i],new(r.X+42,r.Y+12),.38f,Cream);
                string timing=i==7?"－（個別計測なし）":_displayBreaker.LastCost[i].ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+" ms";
                if(_displayBreaker.Tripped[i])timing+=" / 自動OFF";
                _ui.Text(timing,new(r.X+380,r.Y+12),.32f,Cream);
            }
            _ui.Box(new(1260,686,650,70),new Color(23,43,56));
            _ui.Text("旗の時間は旗全体。高低差は手動切替。",new(1274,695),.31f,Cream);
            _ui.Text(_breakerMessage.Length>0?_breakerMessage:"自動ではONに戻りません / 手動ON後10秒は保護",new(1274,725),.30f,Cream);
        }else if(_displayFilterOpen){
            var header=DisplayFilterHeader;
            _ui.Box(header,new Color(23,43,56));
            _ui.Center("表示フィルター",header,.46f,Cream);
            _ui.Box(DisplayFilterEdgeHeading,new Color(23,43,56));
            _ui.Center("地点間の辺",DisplayFilterEdgeHeading,.39f,Cream);
            for(int i=0;i<DisplayFilterLabels.Length;i++){
                var row=DisplayFilterRow(i);
                _ui.Button(row,"",Muted,.4f);
                DrawBreakerCheck(row,_net.MapFilterEnabled(i));
                bool modes=i==5 || i==7;
                _ui.Text(DisplayFilterLabels[i],new(row.X+42,row.Y+12),modes?.32f:.4f,Cream);
                if(modes)_ui.Text(_net.MapFilterMode(i) switch {1=>"危険表示",2=>"非表示",_=>"表示"},new(row.Right-85,row.Y+12),.31f,Cream);
            }
            _ui.Button(DisplayFilterReset,"初期設定に戻す",_net.HiddenMapFilters!=DefaultDomesticHiddenMapFilters?Accent:Muted,.4f);
        }else if(!_mapLayerListOpen && !_trafficLayerListOpen && !_mapOverlayMenu && !GuideVisible){
            if(SimpleDisplayButton.Contains(_pointer))DrawRulebookHint("表示ブレーカー","地図の描画項目と自動OFFを設定します。");
            else if(DisplayFilterButton.Contains(_pointer))DrawRulebookHint("表示フィルター","地点と地点間の辺の表示を切り替えます。初期設定では他国同士の辺は非表示、自国・手下と他国を結ぶ辺は危険表示です。未所属は他国に含めます。");
        }
    }
    private static Rectangle DisplayFilterEdgeHeading=>new(DisplayFilterHeader.X,DisplayFilterRow(2).Bottom,DisplayFilterHeader.Width,36);
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


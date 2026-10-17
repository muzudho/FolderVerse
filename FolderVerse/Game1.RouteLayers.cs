namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>index switch {0=>new(984,82,184,48),4=>new(1176,82,120,48),1=>new(1304,82,120,48),2=>new(1432,82,210,48),_=>new(1650,82,208,48)};
    private bool MapOverlayAvailable(int index)=>!MapTransitionActive && !OrientationRotating && !_net.IsAnimating && (index!=0 || _statusGlobe);

    private static readonly Rectangle MapViewButton=new(1674,136,184,48);
    private int _selectedMapCell=-1;
    private bool _mapLayerListOpen;
    private bool _trafficLayerListOpen;
    private static readonly string[] MapLayerNames={"地形","国セル色分け","自国・手下セル色分け"};
    private static readonly string[] TrafficLayerNames={"セルを選択","ノードを選択"};
    private static Rectangle MapLayerOption(int i)=>new(1432,134+i*52,210,52);
    private static Rectangle TrafficLayerOption(int i)=>new(1650,134+i*52,208,52);
    private bool RouteLayerClick(Point pointer)
    {
        if(_mapLayerListOpen || _trafficLayerListOpen)
        {
            bool traffic=_trafficLayerListOpen;_mapLayerListOpen=_trafficLayerListOpen=false;
            for(int i=0;i<(traffic?2:3);i++)if((traffic?TrafficLayerOption(i):MapLayerOption(i)).Contains(pointer))
            {
                if(traffic){if(i==0 && _movementOpen)CloseMovement();_net.ShowRoutes=i==1;}
                else {_net.ShowCountryColors=i>0;_world.CountryColors=i>0;_net.ControlledCountryColors=_world.ControlledCountryColors=i==2;_world.ShowSetup(_setup,true);}
                _inputOutcome="layer_changed";return true;
            }
            return true;
        }
        if(MapViewButton.Contains(pointer)){StartMapTransition();_inputOutcome="view_transition_started";_dragging=_mapDragging=false;return true;}
        for(int i=0;i<5;i++)if(RouteLayerButton(i).Contains(pointer))
        {
            if((i==0 || i==4) && !MapOverlayAvailable(i))return true;
            if(i==4)_net.ShowEquator=!_net.ShowEquator;
            if(i==0)_net.ShowMapSymbols=!_net.ShowMapSymbols;
            if(i==1)_net.ShowBatteries=!_net.ShowBatteries;
            if(i==2)_mapLayerListOpen=true;
            if(i==3)_trafficLayerListOpen=true;
            return true;
        }
        return false;
    }
    private void DrawRouteLayerButtons()
    {
        string[] names={"緯番・経番","電池","赤道"};bool[] enabled={_net.ShowMapSymbols,_net.ShowBatteries,_net.ShowEquator};int[] indices={0,1,4};
        for(int i=0;i<3;i++){
            var r=RouteLayerButton(indices[i]);bool available=indices[i]==1 || MapOverlayAvailable(indices[i]);
            var color=available?Accent:new Color(75,78,83);
            _ui.Button(r,"",color,.5f);
            _ui.Text(names[i],new(r.X+28,r.Y+14),i==0?.47f:.5f,available?UiPainter.ContrastInk(color):new Color(155,158,163));
            _ui.Box(new(r.X+9,r.Y+18,12,12),new Color(23,30,36));
            _ui.Box(new(r.X+11,r.Y+20,8,8),!available?new Color(105,108,113):enabled[i]?new Color(114,255,143):new Color(38,65,48));
        }
        int mode=_net.ShowCountryColors?(_net.ControlledCountryColors?2:1):0,traffic=_net.ShowRoutes?1:0;
        _ui.Button(RouteLayerButton(2),MapLayerNames[mode]+"  v",Accent,mode==2?.35f:.52f);
        DrawSelectionIcon(RouteLayerButton(3),traffic==1);
        _ui.Button(MapViewButton,_statusGlobe?"展開図":"組立",MapTransitionActive?Muted:Accent,.6f);
        if(_mapLayerListOpen)for(int i=0;i<3;i++)_ui.Button(MapLayerOption(i),MapLayerNames[i],i==mode?Accent:Muted,i==2?.35f:.52f);
        if(_trafficLayerListOpen)for(int i=0;i<2;i++){var r=TrafficLayerOption(i);_ui.Button(r,"",i==traffic?Accent:Muted,.52f);DrawSelectionGlyph(r.X+30,r.Center.Y,i==1,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));_ui.Text(TrafficLayerNames[i],new(r.X+62,r.Y+15),.4f,i==traffic?SelectionInk:UiPainter.ContrastInk(Muted));}
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


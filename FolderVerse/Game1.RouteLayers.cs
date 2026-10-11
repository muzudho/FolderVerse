namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>index switch {0=>new(984,82,144,48),1=>new(1136,82,144,48),2=>new(1288,82,220,48),_=>new(1516,82,342,48)};
    private static readonly Rectangle MapViewButton=new(1674,136,184,48);
    private bool _mapLayerListOpen;
    private bool _trafficLayerListOpen;
    private static readonly string[] MapLayerNames={"地形","国セル色分け"};
    private static readonly string[] TrafficLayerNames={"交通路・節点非表示","交通路・節点表示"};
    private static Rectangle MapLayerOption(int i)=>new(1288,134+i*52,220,52);
    private static Rectangle TrafficLayerOption(int i)=>new(1516,134+i*52,342,52);
    private bool RouteLayerClick(Point pointer)
    {
        if(_mapLayerListOpen || _trafficLayerListOpen)
        {
            bool traffic=_trafficLayerListOpen;_mapLayerListOpen=_trafficLayerListOpen=false;
            for(int i=0;i<2;i++)if((traffic?TrafficLayerOption(i):MapLayerOption(i)).Contains(pointer))
            {
                if(traffic){if(i==0 && _movementOpen)CloseMovement();_net.ShowRoutes=i==1;}
                else {_net.ShowCountryColors=i==1;_world.CountryColors=i==1;_world.ShowSetup(_setup,true);}
                _inputOutcome="layer_changed";return true;
            }
            return true;
        }
        if(MapViewButton.Contains(pointer)){StartMapTransition();_inputOutcome="view_transition_started";_dragging=_mapDragging=false;return true;}
        for(int i=0;i<4;i++)if(RouteLayerButton(i).Contains(pointer))
        {
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
        string[] names={"地図記号","電池"};bool[] enabled={_net.ShowMapSymbols,_net.ShowBatteries};
        for(int i=0;i<2;i++){var r=RouteLayerButton(i);_ui.Button(r,names[i],enabled[i]?Accent:Muted,.55f);_ui.Box(new(r.X+8,r.Y+18,10,12),enabled[i]?new Color(255,234,124):new Color(26,41,50));}
        int mode=_net.ShowCountryColors?1:0,traffic=_net.ShowRoutes?1:0;
        _ui.Button(RouteLayerButton(2),MapLayerNames[mode]+"  v",Accent,.52f);
        _ui.Button(RouteLayerButton(3),TrafficLayerNames[traffic]+"  v",Accent,.52f);
        _ui.Button(MapViewButton,_statusGlobe?"展開図":"組立",MapTransitionActive?Muted:Accent,.6f);
        if(_mapLayerListOpen)for(int i=0;i<2;i++)_ui.Button(MapLayerOption(i),MapLayerNames[i],i==mode?Accent:Muted,.52f);
        if(_trafficLayerListOpen)for(int i=0;i<2;i++)_ui.Button(TrafficLayerOption(i),TrafficLayerNames[i],i==traffic?Accent:Muted,.52f);
    }
}

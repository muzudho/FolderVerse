namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>new(984+index*186,82,index==2?306:174,48);
    private static readonly Rectangle MapViewButton=new(1674,82,184,48);
    private bool _mapLayerListOpen;
    private static readonly string[] MapLayerNames={"地形のみ","交通路","国セル色分け"};
    private static Rectangle MapLayerOption(int i)=>new(1356,134+i*52,306,52);
    private bool RouteLayerClick(Point pointer)
    {
        if(_mapLayerListOpen)
        {
            _mapLayerListOpen=false;
            for(int i=0;i<3;i++)if(MapLayerOption(i).Contains(pointer))
            {
                if(i!=1 && _movementOpen)CloseMovement();
                _net.ShowRoutes=i==1;_net.ShowCountryColors=i==2;
                _world.CountryColors=i==2;_world.ShowSetup(_setup,true);
                _inputOutcome="layer_changed";return true;
            }
            return true;
        }
        if(MapViewButton.Contains(pointer))
        {
            StartMapTransition();_inputOutcome="view_transition_started";
            _dragging=_mapDragging=false;return true;
        }
        for(int i=0;i<3;i++)if(RouteLayerButton(i).Contains(pointer))
        {
            if(i==0)_net.ShowMapSymbols=!_net.ShowMapSymbols;
            if(i==1)_net.ShowBatteries=!_net.ShowBatteries;
            if(i==2)_mapLayerListOpen=true;
            return true;
        }
        return false;
    }
    private void DrawRouteLayerButtons()
    {
        string[] names={"地図記号","電池"};bool[] enabled={_net.ShowMapSymbols,_net.ShowBatteries};
        for(int i=0;i<2;i++)
        {
            var r=RouteLayerButton(i);_ui.Button(r,names[i],enabled[i]?Accent:Muted,.62f);
            _ui.Box(new(r.X+12,r.Y+18,12,12),enabled[i]?new Color(255,234,124):new Color(26,41,50));
        }
        int mode=_net.ShowCountryColors?2:_net.ShowRoutes?1:0;
        var selector=RouteLayerButton(2);_ui.Button(selector,MapLayerNames[mode]+"  v",Accent,.57f);
        _ui.Button(MapViewButton,_statusGlobe?"展開図":"組立",MapTransitionActive?Muted:Accent,.6f);
        if(_mapLayerListOpen)for(int i=0;i<3;i++)_ui.Button(MapLayerOption(i),MapLayerNames[i],i==mode?Accent:Muted,.6f);
    }
}

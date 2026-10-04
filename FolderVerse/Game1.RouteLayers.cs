namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>new(984+index*186,82,index==2?306:174,48);
    private static readonly Rectangle MapViewButton=new(1674,82,184,48);
    private bool RouteLayerClick(Point pointer)
    {
        if(MapViewButton.Contains(pointer))
        {
            _statusGlobe=!_statusGlobe;_inputOutcome="view_changed";
            _dragging=_mapDragging=false;_statusCell=-1;return true;
        }
        for(int i=0;i<3;i++)if(RouteLayerButton(i).Contains(pointer))
        {
            if(i==0)_net.ShowFlags=!_net.ShowFlags;
            if(i==1)_net.ShowBatteries=!_net.ShowBatteries;
            if(i==2)
            {
                bool traffic=!_net.ShowRoutes;
                bool cancel=!traffic && _movementOpen;
                if(cancel)CloseMovement();
                _inputOutcome=cancel?"movement_cancelled_for_grid":"layer_changed";
                _net.ShowRoutes=traffic;
            }
            return true;
        }
        return false;
    }
    private void DrawRouteLayerButtons()
    {
        string[] names={"旗","電池"};bool[] enabled={_net.ShowFlags,_net.ShowBatteries};
        for(int i=0;i<2;i++)
        {
            var r=RouteLayerButton(i);_ui.Button(r,names[i],enabled[i]?Accent:Muted,.62f);
            _ui.Box(new(r.X+12,r.Y+18,12,12),enabled[i]?new Color(255,234,124):new Color(26,41,50));
        }
        var mode=RouteLayerButton(2);_ui.Button(mode,"",Accent,.62f);
        _ui.Box(new(mode.X+14,mode.Y+18,12,12),!_net.ShowRoutes?new Color(255,234,124):new Color(26,41,50));
        _ui.Text("グリッド",new(mode.X+34,mode.Y+8),.57f,Cream);
        _ui.Box(new(mode.X+164,mode.Y+18,12,12),_net.ShowRoutes?new Color(255,234,124):new Color(26,41,50));
        _ui.Text("交通路",new(mode.X+184,mode.Y+8),.57f,Cream);
        _ui.Button(MapViewButton,"",Accent);
        for(int i=0;i<2;i++)
        {
            int x=MapViewButton.X+i*92;
            _ui.Box(new(x+5,MapViewButton.Y+19,10,10),(_statusGlobe==(i==0))?new Color(255,234,124):new Color(26,41,50));
            _ui.Text(i==0?"地星儀":"展開図",new(x+19,MapViewButton.Y+13),.36f,Cream);
        }
    }
}

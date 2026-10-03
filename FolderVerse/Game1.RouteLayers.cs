namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static Rectangle RouteLayerButton(int index)=>new(984+index*186,82,174,48);
    private bool RouteLayerClick(Point pointer)
    {
        for(int i=0;i<3;i++)if(RouteLayerButton(i).Contains(pointer))
        {
            if(i==0)_net.ShowFlags=!_net.ShowFlags;
            if(i==1)_net.ShowBatteries=!_net.ShowBatteries;
            if(i==2)_net.ShowRoutes=!_net.ShowRoutes;
            return true;
        }
        return false;
    }
    private void DrawRouteLayerButtons()
    {
        string[] names={"旗","電池","航路"};bool[] enabled={_net.ShowFlags,_net.ShowBatteries,_net.ShowRoutes};
        for(int i=0;i<3;i++)
        {
            var r=RouteLayerButton(i);_ui.Button(r,names[i],enabled[i]?Accent:Muted,.62f);
            _ui.Box(new(r.X+12,r.Y+18,12,12),enabled[i]?new Color(255,234,124):new Color(26,41,50));
        }
    }
}

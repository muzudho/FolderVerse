namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const int LocationChipRadius=36;

    private bool WandererLocationVisible(out Vector2 at)
    {
        at=Vector2.Zero;int ruler=_setup.PlayerSlot;
        if(ruler<0 || !_setup.Relations.Powered[ruler] || MapTransitionActive || _net.IsAnimating)return false;
        var node=_setup.Nodes.Current(ruler);if(node==null)return false;
        if(_statusGlobe){
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return false;
        }else at=_net.NodePosition(_setup,node,NetPanel);
        return MapViewBounds.Contains(at.ToPoint());
    }
    private void DrawWandererLocation()
    {
        if(MovePreviews().Any(preview=>preview.Ruler==_setup.PlayerSlot))return;
        if(!WandererLocationVisible(out var at)){
            int ruler=_setup.PlayerSlot;
            if(ruler>=0 && _setup.Relations.Powered[ruler] && !MapTransitionActive && !_net.IsAnimating){
                var hint=new Rectangle(MapViewBounds.X+16,MapViewBounds.Y+105,480,38);
                _ui.Box(hint,new(12,31,43));_ui.Center("現在地は画面外 / 征服者はどこ？ で表示",hint,.42f,Cream);
            }
            return;
        }
        var gold=new Color(255,231,133);
        var center=new Vector2(Math.Clamp(at.X-94,MapViewBounds.Left+40,MapViewBounds.Right-40),Math.Clamp(at.Y-100,MapViewBounds.Top+40,MapViewBounds.Bottom-40));
        var delta=at-center;_ui.Tile((center+at)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),gold);
        DrawLocationChip(center,_setup.PlayerSlot);
    }

    private void DrawLocationChip(Vector2 center,int ruler)=>DrawFaceChip(center,ruler,LocationChipRadius,animate:true);
}

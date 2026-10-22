namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool WandererLocationVisible(out Vector2 at)
    {
        at=Vector2.Zero;int ruler=_setup.PlayerSlot;
        if(ruler<0 || !_setup.Relations.Powered[ruler] || _setup.TerritoryCounts[ruler]>0 || MapTransitionActive || _net.IsAnimating)return false;
        var node=_setup.Nodes.Current(ruler);if(node==null)return false;
        if(_statusGlobe){
            if(!_world.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),_setup.Cells[node.Cell].Normal,_yaw,_pitch,StatusGlobeArea,out at))return false;
        }else at=_net.NodePosition(_setup,node,NetPanel);
        return MapViewBounds.Contains(at.ToPoint());
    }
    private void DrawWandererLocation()
    {
        if(!WandererLocationVisible(out var at)){
            int ruler=_setup.PlayerSlot;
            if(ruler>=0 && _setup.Relations.Powered[ruler] && _setup.TerritoryCounts[ruler]==0 && !MapTransitionActive && !_net.IsAnimating){
                var hint=new Rectangle(MapViewBounds.X+16,MapViewBounds.Y+105,480,38);
                _ui.Box(hint,new(12,31,43));_ui.Center("現在地は画面外 / 現在地へ移動 で表示",hint,.42f,Cream);
            }
            return;
        }
        var gold=new Color(255,231,133);float radius=15+MathF.Sin(_animationTime*5)*3;
        for(int i=0;i<4;i++){
            float angle=i*MathHelper.PiOver2;var direction=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
            _ui.Tile(at+direction*radius,new(10,3),angle,gold);
        }
        var label=new Rectangle((int)Math.Clamp(at.X-94,MapViewBounds.Left+4,MapViewBounds.Right-192),(int)Math.Clamp(at.Y-62,MapViewBounds.Top+4,MapViewBounds.Bottom-38),188,34);
        _ui.Box(label,new Color(12,31,43));_ui.Center("現在地 #"+(_setup.PlayerSlot+1),label,.46f,gold);
        var end=new Vector2(label.Center.X,label.Bottom);var delta=at-end;
        _ui.Tile((end+at)/2,new(delta.Length(),2),MathF.Atan2(delta.Y,delta.X),gold);
    }
}

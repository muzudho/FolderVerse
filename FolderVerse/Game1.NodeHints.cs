namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawNodeTooltip()
    {
        if(_inspectorFoldersOpen || _mapOverlayMenu)return;
        if(_directPairOpen || !MapViewBounds.Contains(_pointer))return;
        if(_salvageHits.Exists(h=>h.Bounds.Contains(_pointer))){DrawRulebookHint("廃品回収計画","クリックでこの地点の廃品カタログを開きます。");return;}
        if(_directTrucks.Exists(t=>t.Bounds.Contains(_pointer))){
            var delivery=_directTrucks.Find(t=>t.Bounds.Contains(_pointer));
            DrawRulebookHint(DirectSeaRoute(delivery.A,delivery.B)?"小型フェリー":"トラック","ロボットパーツの直送便です。予約したパーツを伝播フェーズで配送します。");return;
        }
        int battery=_net.HitBattery(_pointer);
        if(battery>=0)
        {
            var robots=_setup.Robots.Retinues[battery].Robots;
            DrawRulebookHint($"電池 #{battery+1}・ポケット","現在地："+_setup.Nodes.DisplayName(_setup.Nodes.Current(battery))+" / ロボット "+robots.Count);
            return;
        }
        if(!_net.ShowRoutes)return; int id=_statusGlobe?HitGlobeNode(_pointer):_net.HitNode(_setup,NetPanel,_pointer);if(id<0 || !_net.NodeVisible(_setup,_setup.Nodes.All[id]))return;
        var node=_setup.Nodes.All[id];string name=_setup.Nodes.Label(node);
        if(_setup.Campaign.OwnershipChanged(node)){
            int before=_setup.Campaign.PreviousNodeOwners[id];
            name+=" / 直前ターン："+(before<0?"無所属":$"国＃{before+1}")+" → "+(node.Owner<0?"無所属":$"国＃{node.Owner+1}");
        }
        string landmark="目印："+NodeLandmarks.At(_setup,id).Description;
        DrawRulebookHint(name,landmark+(_setup.Campaign.UseRobotCombat?" / ロボット "+_setup.Robots.Nodes[id].Robots.Count+"/12":""));
    }
}

namespace FolderVerse;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _mapOverlayMenu;
    private static readonly Rectangle MapOverlayButton=new(1004,1006,318,64);
    private static Rectangle MapOverlayOption(int i)=>new(1004,838+i*54,318,52);
    private static readonly string[] MapOverlayLabels={"なし","ロボットパーツ直送便","廃品回収計画"};
    private int MapOverlaySelection=>_salvagePlan?2:_directMap?1:0;
    private bool MapMarkerMenuAvailable=>!_directPairOpen && !_trainingOpen && _supervisorDialog==0 && !_inspectorFoldersOpen && !_salvageAbandonConfirmation.HasValue && !_hangarDrag.HasValue && !_seedDialog.IsOpen;
    private bool MapOverlayClick(Point p,bool escape)
    {
        if(!MapMarkerMenuAvailable)return false;
        if(escape && _mapOverlayMenu){_mapOverlayMenu=false;return true;}
        if(MapOverlayButton.Contains(p)){_mapOverlayMenu=!_mapOverlayMenu;return true;}
        if(!_mapOverlayMenu)return false;
        _mapOverlayMenu=false;
        for(int i=0;i<3;i++)if(MapOverlayOption(i).Contains(p)){
            if(i==0){if(_salvagePlan)ResetInspector();else CloseDirectDelivery();}
            else if(i==1){if(TransportOverviewAllowed)OpenDirectMap();}
            else OpenSalvagePlan();
            break;
        }
        return true;
    }
    private void DrawMapOverlayMenu()
    {
        if(!MapMarkerMenuAvailable)return;
        _ui.Button(MapOverlayButton,MapOverlayLabels[MapOverlaySelection]+" ▼",Accent,.4f);
        if(!_mapOverlayMenu)return;
        for(int i=0;i<3;i++)_ui.Button(MapOverlayOption(i),MapOverlayLabels[i],MapOverlaySelection==i?Cream:Muted,.4f);
    }
}

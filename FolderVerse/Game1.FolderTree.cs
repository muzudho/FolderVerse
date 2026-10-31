namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public partial class Game1
{
    private bool _folderTreeOpen;
    private int _folderTreeKind=-1,_folderTreeId=-1,_folderTreeOwner=-1;
    private readonly HashSet<int> _expandedFolderCountries=new();
    private (bool Routes,bool Others,bool Controlled,bool WorldControlled)? _folderTreeLayers;
    private static readonly Rectangle FolderTreeButton=new(54,70,60,60);
    private bool FolderTreeFocus=>_folderTreeOpen && !_nodeInspectorClosing && _folderTreeKind>=0;
    private int FolderTreeLocationNode=>_folderTreeKind==2?_folderTreeId:_setup.Nodes.All.FirstOrDefault(n=>_folderTreeKind==1?n.Cell==_folderTreeId:n.Owner==_folderTreeOwner)?.Id??-1;
    private void CloseFolderTreeState()
    {
        if(_folderTreeLayers is {} layers){
            _net.ShowRoutes=layers.Routes;_net.ShowOtherCountries=layers.Others;
            _net.ControlledCountryColors=layers.Controlled;_world.ControlledCountryColors=layers.WorldControlled;
            _folderTreeLayers=null;_world.ShowSetup(_setup,true);
        }
        if(_folderTreeOpen){
            _net.FolderSearchFocus=false;_net.FocusBrightness=_world.FocusBrightness=.65f;
            _net.NodeFocus=_world.NodeFocus=_net.CountryFocus=_world.CountryFocus=_net.RegionFocusCell=_world.RegionFocusCell=-1;
            _net.FocusCircles.Clear();_world.FocusCircles.Clear();_net.FocusCircleNodes.Clear();
        }
        _folderTreeOpen=false;_folderTreeKind=_folderTreeId=_folderTreeOwner=-1;
    }
    private void OpenFolderTree()
    {
        ResetInspector();_folderTreeOpen=true;_folderScroll=0;_mapDragListOpen=_mapLayerListOpen=_trafficLayerListOpen=false;
        _folderTreeLayers=(_net.ShowRoutes,_net.ShowOtherCountries,_net.ControlledCountryColors,_world.ControlledCountryColors);
        _net.ShowRoutes=_net.ShowOtherCountries=true;_net.ControlledCountryColors=_world.ControlledCountryColors=false;
        _world.ShowSetup(_setup,true);
        var seen=new HashSet<int>();
        for(int ruler=_setup.PlayerSlot;ruler>=0 && seen.Add(ruler);ruler=_setup.Relations.Superiors[ruler])_expandedFolderCountries.Add(ruler);
        var current=_setup.Nodes.Current(_setup.PlayerSlot);if(current!=null)_expandedFolderRegions.Add(current.Cell);
        BuildFolderTree();
        _folderScroll=Math.Clamp(_inspectorFolderRows.FindIndex(row=>row.Kind==0 && row.Id==_setup.PlayerSlot)-2,0,Math.Max(0,_inspectorFolderRows.Count-10));
    }
    private bool FolderTreeToolbarClick(Point p)
    {
        if(!FolderTreeButton.Contains(p) || !IsStatusScreen || _conquestOverview || _demoActive || _autoPlay || _tutorialAttackGuide || _tutorialMessagesOpen || _homePersuasionActive || _directPairOpen || _invasionPicker>=0 || _hangarDrag.HasValue || _supervisorDialog!=0 || _trainingOpen || _salvageAbandonConfirmation.HasValue || _seedDialog?.IsOpen==true)return false;
        if(_folderTreeOpen)CloseNodeInspector();else OpenFolderTree();
        return true;
    }
    private void BuildFolderTree()
    {
        _inspectorFolderRows.Clear();var seen=new HashSet<int>();
        void Regions(int owner,int depth){
            foreach(var cell in _setup.Cells.Where(c=>_setup.Nodes.InCell(c.Id).Any(n=>n.Owner==owner))){
                _inspectorFolderRows.Add((1,cell.Id,depth,owner,"地域："+WorldCoordinates.Label(_setup,cell.Id)));
                if(!_expandedFolderRegions.Contains(cell.Id))continue;
                foreach(var node in _setup.Nodes.InCell(cell.Id).Where(n=>n.Owner==owner))
                    _inspectorFolderRows.Add((2,node.Id,depth+1,owner,"地点："+_setup.Nodes.DisplayName(node)));
            }
        }
        void Country(int owner,int depth,bool visible){
            if(!seen.Add(owner))return;
            if(visible)_inspectorFolderRows.Add((0,owner,depth,owner,"征服者："+_setup.ConquerorFullNames[owner]+(owner==_setup.PlayerSlot?"（現在のターン）":"")));
            bool expanded=visible && _expandedFolderCountries.Contains(owner);
            if(expanded)Regions(owner,depth+1);
            for(int child=0;child<_setup.ActiveCount;child++)if(_setup.Relations.Superiors[child]==owner)Country(child,depth+1,expanded);
        }
        for(int ruler=0;ruler<_setup.ActiveCount;ruler++)if(_setup.Relations.Superiors[ruler]<0)Country(ruler,0,true);
        for(int ruler=0;ruler<_setup.ActiveCount;ruler++)if(!seen.Contains(ruler))Country(ruler,0,true);
        if(_setup.Nodes.All.Any(n=>n.Owner<0)){
            _inspectorFolderRows.Add((0,-1,0,-1,"未征服"));if(_expandedFolderCountries.Contains(-1))Regions(-1,1);
        }
        _folderScroll=Math.Clamp(_folderScroll,0,Math.Max(0,_inspectorFolderRows.Count-10));
    }
    private void FocusFolderTreeRow(int index)
    {
        if(index<0 || index>=_inspectorFolderRows.Count)return;
        var row=_inspectorFolderRows[index];
        if(row.Kind==0 && row.Owner<0){_expandedFolderCountries.Add(-1);BuildFolderTree();return;}
        _folderTreeKind=row.Kind;_folderTreeId=row.Id;_folderTreeOwner=row.Owner;
        int node=row.Kind==2?row.Id:_setup.Nodes.All.FirstOrDefault(n=>row.Kind==1?n.Cell==row.Id:n.Owner==row.Owner)?.Id??-1;
        if(node<0)return;
        var site=_setup.Nodes.All[node];var cell=_setup.Cells[site.Cell];
        var point=row.Kind==1?cell.Center:_setup.Routes.Position(site.Cell,site.Center);
        _domesticMapLeft=55+480*_domesticSlide;
        _inspectorCenterPending=false;_inspectorCenterAge=-1;
        if(_statusGlobe){
            _world.ManualOrientation=null;_world.GlobeZoom=row.Kind==1?1.8f:2.5f;FocusGlobePosition(point);
            _world.ProjectVisible(point,cell.Normal,_yaw,_pitch,StatusGlobeArea,out var at);
            var shift=MapViewBounds.Center.ToVector2()-at;
            _world.GlobePan+=new Vector2(shift.X/StatusGlobeArea.Width,shift.Y/StatusGlobeArea.Height);
        }else{
            float angle=_net.ScreenAngle;_net.SetCenter(_setup,cell.Face);_net.ResetView();_net.ScreenAngle=angle;
            var fit=_net.Fit(NetPanel);var face=_net.Faces.First(f=>f.Face==cell.Face);
            var corners=cell.Corners.Select(p=>_net.RotateScreen(face.Project(p)*fit.Scale+fit.Origin,NetPanel)).ToArray();
            float width=corners.Max(p=>p.X)-corners.Min(p=>p.X),height=corners.Max(p=>p.Y)-corners.Min(p=>p.Y);
            float zoom=Math.Clamp(Math.Min(MapViewBounds.Width*.65f/Math.Max(1,width),MapViewBounds.Height*.65f/Math.Max(1,height)),1,32);
            _net.SetFitCamera(NetPanel,fit.Scale*zoom,Vector2.Zero);
            fit=_net.Fit(NetPanel);var at=_net.RotateScreen(face.Project(point)*fit.Scale+fit.Origin,NetPanel);
            _net.Drag(MapViewBounds.Center.ToVector2()-at);
        }
        _dragging=_mapDragging=false;
    }
    private bool FolderTreeInput(MouseState mouse,bool click,bool escape,bool active)
    {
        if(!_folderTreeOpen)return false;
        if(!IsStatusScreen){CloseFolderTreeState();return false;}
        if(!active || _nodeInspectorClosing)return false;
        if(escape){CloseNodeInspector();return true;}
        var p=new Point(_pointer.X-NodeInspectorOffset,_pointer.Y);
        if(!new Rectangle(54,150,850,825).Contains(p))return false;
        if(_domesticSlide<1)return click;
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;
        _folderScroll=Math.Clamp(_folderScroll-wheel/120*3,0,Math.Max(0,_inspectorFolderRows.Count-10));
        if(click && p.Y>=298 && p.Y<858){
            int index=_folderScroll+(p.Y-298)/56;
            if(index<_inspectorFolderRows.Count){
                var row=_inspectorFolderRows[index];int x=90+Math.Min(row.Depth,10)*25;
                if(row.Kind<2 && p.X>=x && p.X<x+24){
                    var expanded=row.Kind==0?_expandedFolderCountries:_expandedFolderRegions;
                    if(!expanded.Add(row.Id))expanded.Remove(row.Id);BuildFolderTree();
                }else FocusFolderTreeRow(index);
            }
        }
        return click || wheel!=0;
    }
    private void DrawFolderTreeButton()
    {
        PrimaryRoundBox(FolderTreeButton,_folderTreeOpen?Color.Lerp(Accent,Color.White,.2f):Accent,12);
        DrawCountryFolder(new(FolderTreeButton.X+12,FolderTreeButton.Y+15,36,30),_setup.PlayerSlot);
        if(FolderTreeButton.Contains(_pointer))_ui.Text("所属フォルダーツリー",new(124,98),.38f,Cream);
    }
    private void DrawFolderTree()
    {
        if(!_folderTreeOpen)return;
        DrawInspector(()=>{
            _ui.Box(new(54,150,850,825),new Color(27,49,62));
            _ui.Text("所属フォルダーツリー",new(84,178),.75f,Cream);
            _ui.Text("地域・地点の名前を選ぶと、地図で場所を表示",new(84,222),.42f,Cream);
            _ui.Box(InspectorFolderPanel,new Color(23,43,56));
            _ui.Text("所属フォルダー",new(90,254),.6f,Cream);
            for(int i=0;i<10;i++){
                int index=_folderScroll+i;if(index>=_inspectorFolderRows.Count)break;
                var row=_inspectorFolderRows[index];int x=90+Math.Min(row.Depth,10)*25,y=298+i*56;
                var r=new Rectangle(80,y,788,52);
                if(FolderTreeFocus && row.Kind==_folderTreeKind && row.Id==_folderTreeId && row.Owner==_folderTreeOwner){_ui.Box(r,new Color(65,96,106));_ui.Box(new(r.X,y,5,r.Height),Accent);}
                else if(r.Contains(_ui.Pointer))_ui.Box(r,new Color(48,72,87));
                if(row.Depth>0){_ui.Box(new(x-13,y-17,1,43),Muted);_ui.Box(new(x-13,y+25,18,1),Muted);}
                if(row.Kind<2)_ui.Text((row.Kind==0?_expandedFolderCountries.Contains(row.Id):_expandedFolderRegions.Contains(row.Id))?"－":"+",new(x,y+13),.4f,Cream);
                DrawCountryFolder(new(x+25,y+12,30,28),row.Owner);
                _ui.Text(row.Label,new(x+64,y+15),Math.Min(.42f,(850-x-64)/Math.Max(1,_font.MeasureString(row.Label).X)),Cream);
            }
            _ui.Text("ホイール：スクロール / ＋：展開 / 名前：地図で探す",new(90,863),.32f,Cream);
            if(_inspectorFolderRows.Count>10){
                int height=Math.Max(24,5600/_inspectorFolderRows.Count),top=298+_folderScroll*(560-height)/(_inspectorFolderRows.Count-10);
                _ui.Box(new(874,298,5,560),Muted);_ui.Box(new(874,top,5,height),Accent);
            }
            _ui.Text("× / Esc：閉じる",new(84,925),.4f,Cream);
        });
    }
}

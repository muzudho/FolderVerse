namespace FolderVerse;
using System.Linq;
public partial class Game1
{
    private void UpdateMapInteractionState()
    {
        _net.HoverSuppressedNode=_movementOpen && !_warPlanning?(_setup.Nodes.Current(_setup.PlayerSlot)?.Id??-1):-1;
        _net.RestrictNodeActions=_movementOpen && _warPlanning;_net.KeepFlagsEnabled=CompactWarInspector;
        _net.FolderSearchFocus=FolderTreeFocus;
        if(_folderTreeOpen)_net.NodeFocus=FolderTreeFocus && _folderTreeKind==2?_folderTreeId:-1;
        _net.RegionFocusCell=RegionMapFocusCell;
        _net.EnabledNodes.Clear();_net.FocusCells.Clear();_net.SelectedNodes.Clear();_net.AttentionNodes.Clear();
        _net.CountryFocus=_reignView && !_nodeInspectorClosing?(_strategyRuler>=0?_strategyRuler:_setup.PlayerSlot):-1;
        if(_movementOpen && _warPlanning){
            _net.CountryFocus=-1;_net.NodeFocus=_selectedMoveNode;_net.RegionFocusCell=-1;
            if(TutorialReviewAvailable && !_autoPlay && !_demoActive && !_tutorialMessagesOpen && _selectedMoveNode<0)
                _net.AttentionNodes.UnionWith(_net.ActionTargetNodes);
        }else{
            if(FolderTreeFocus && _folderTreeKind==0)_net.CountryFocus=_folderTreeOwner;
            if(FolderTreeFocus && _folderTreeKind==1)foreach(var node in _setup.Nodes.InCell(_folderTreeId))_net.EnabledNodes.Add(node.Id);
            if(_salvagePlan && !_nodeInspectorClosing){
                RefreshSalvageFocus();_net.CountryFocus=_setup.PlayerSlot;
                _net.EnabledNodes.UnionWith(_salvageFocusNodes);
                if(SalvageMapTargetNode is >=0 and var target)_net.SelectedNodes.Add(target);
            }
            if(_net.CountryFocus>=0){
                foreach(var cell in _setup.Cells)if(_setup.Nodes.FullOwner(cell.Id)==_net.CountryFocus)_net.FocusCells.Add(cell.Id);
                foreach(var node in _setup.Nodes.All)if(node.Owner==_net.CountryFocus || _net.FocusCells.Contains(node.Cell))_net.EnabledNodes.Add(node.Id);
            }
        }
        _net.AppearanceTime=_animationTime;
        _world.RestrictNodeActions=_net.RestrictNodeActions;_world.CountryFocus=_net.CountryFocus;_world.NodeFocus=_net.NodeFocus;
        _world.FocusCells.Clear();_world.FocusCells.UnionWith(_net.FocusCells);
        _net.PrepareTerrainHighlights(_setup);
        _world.HighlightTiles.Clear();_world.HighlightTiles.UnionWith(_net.HighlightTiles);
        _net.DawnMeridian=WorldDayCycle.DawnPhase(_setup,_setup.PlayerSlot);
    }
}

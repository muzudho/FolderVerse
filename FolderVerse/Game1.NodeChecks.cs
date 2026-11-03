namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<int,HashSet<int>> _nodeChecksByPlayer=new();
    private readonly Dictionary<int,HashSet<int>> _warChecksByPlayer=new();
    private bool WarCheckMode=>_movementOpen && _warPlanning;
    private Dictionary<int,HashSet<int>> ActiveNodeChecks=>WarCheckMode?_warChecksByPlayer:_nodeChecksByPlayer;
    private int CheckTargetNode=>WarCheckMode?_selectedMoveNode:_populationNode;
    private Color ActiveNodeCheckColor=>WarCheckMode?new Color(194,55,63):new Color(39,143,75);
    private readonly List<(Rectangle Bounds,int Node)> _nodeCheckHits=new();
    private WorldSetup _nodeChecksWorld;
    private int _nodeChecksTurn=-1;
    private const string NodeCheckButtonHint="地図ビューにチェックマークを付けることができます。ターン終了時に消えます。好きなようにお使いください。";
    private const string NodeCheckMarkerHint="チェックマークはあなたが付けました。";
    private Rectangle NodeCheckButton=>new(912+NodeInspectorOffset,520,48,48);
    private bool NodeCheckButtonVisible=>WarCheckMode?InspectorVisible && _selectedMoveNode>=0 && _selectedMoveNode<_setup.Nodes.All.Length && SelectedWithdrawal==null:InspectorVisible && _populationCell>=0 && _populationNode>=0 && _populationNode<_setup.Nodes.All.Length && !_reignView && !_movementOpen && !_transportOverview && !_salvagePlan;
    private int NodeCheckPlayer=>_setup.PlayerSlot>=0 && _setup.ControllerOf(_setup.PlayerSlot)>=0 && !_demoActive && !_autoPlay?_setup.TurnPlayer:-1;
    private void EnsureNodeCheckContext()
    {
        if(ReferenceEquals(_nodeChecksWorld,_setup) && _nodeChecksTurn==_setup.Population.Turn)return;
        ClearAllNodeChecks();_nodeChecksWorld=_setup;_nodeChecksTurn=_setup.Population.Turn;
    }
    private void ClearAllNodeChecks(){_nodeChecksByPlayer.Clear();_warChecksByPlayer.Clear();_directChecksByPlayer.Clear();_nodeCheckHits.Clear();_nodeChecksWorld=null;_nodeChecksTurn=-1;}
    private bool NodeChecked(int node)
    {
        EnsureNodeCheckContext();return _nodeChecksByPlayer.TryGetValue(NodeCheckPlayer,out var checks) && checks.Contains(node);
    }
    private void ClearCurrentPlayerNodeChecks(){EnsureNodeCheckContext();_nodeChecksByPlayer.Remove(NodeCheckPlayer);_warChecksByPlayer.Remove(NodeCheckPlayer);_directChecksByPlayer.Remove(NodeCheckPlayer);_nodeCheckHits.Clear();}
    private bool NodeCheckClick(Point point)
    {
        if(!NodeCheckButtonVisible || !NodeCheckButton.Contains(point))return false;
        int player=NodeCheckPlayer;if(player<0 || _nodeInspectorClosing)return true;
        EnsureNodeCheckContext();
        if(!ActiveNodeChecks.TryGetValue(player,out var checks))ActiveNodeChecks[player]=checks=new();
        if(!checks.Remove(CheckTargetNode))checks.Add(CheckTargetNode);
        return true;
    }
    private void DrawNodeCheckGlyph(Rectangle bounds,Color color)
    {
        void Stroke(Vector2 a,Vector2 b){var d=b-a;_ui.Tile((a+b)/2,new(d.Length(),Math.Max(3,bounds.Width*.10f)),MathF.Atan2(d.Y,d.X),color);}
        Stroke(new(bounds.X+bounds.Width*.23f,bounds.Y+bounds.Height*.52f),new(bounds.X+bounds.Width*.43f,bounds.Y+bounds.Height*.72f));
        Stroke(new(bounds.X+bounds.Width*.43f,bounds.Y+bounds.Height*.72f),new(bounds.X+bounds.Width*.80f,bounds.Y+bounds.Height*.27f));
    }
    private void DrawNodeCheckButton()
    {
        EnsureNodeCheckContext();
        bool selected=ActiveNodeChecks.TryGetValue(NodeCheckPlayer,out var checks) && checks.Contains(CheckTargetNode);var r=NodeCheckButton;
        var color=selected?ActiveNodeCheckColor:Muted;
        if(r.Contains(_pointer) && !_nodeInspectorClosing)color=Color.Lerp(color,Color.White,.15f);
        PrimaryRoundBox(r,color,6);DrawNodeCheckGlyph(r,selected?Color.White:new Color(172,192,181));
    }
    private bool DrawFolderNodeCheck(int kind,int node,Rectangle bounds)
    {
        if(WarCheckMode || kind!=2 || !NodeChecked(node))return false;
        PrimaryRoundBox(bounds,new Color(39,143,75),5);DrawNodeCheckGlyph(bounds,Color.White);return true;
    }
    private void DrawNodeCheckMarkers(Rectangle panel,bool directPair=false)
    {
        _nodeCheckHits.Clear();EnsureNodeCheckContext();
        if(MapTransitionActive || _net.IsAnimating || directPair && (_directPairSlide<1 || _directPairClosing) || !(directPair?_nodeChecksByPlayer:ActiveNodeChecks).TryGetValue(NodeCheckPlayer,out var checks))return;
        foreach(int node in checks.OrderBy(n=>n)){
            if(node<0 || node>=_setup.Nodes.All.Length)continue;
            Vector2 at;bool visible=directPair?DirectPairVisible(node,out at):OperationPosition(node,out at);
            if(!visible)continue;
            var r=new Rectangle((int)MathF.Round(at.X)-17,(int)MathF.Round(at.Y)-17,34,34);
            if(!panel.Contains(r))continue;
            PrimaryRoundBox(new(r.X+2,r.Y+3,r.Width,r.Height),new Color(7,23,35,200),6);
            PrimaryRoundBox(r,directPair?new Color(39,143,75):ActiveNodeCheckColor,6);DrawNodeCheckGlyph(r,Color.White);_nodeCheckHits.Add((r,node));
        }
    }
    private bool DrawNodeCheckHint()
    {
        if(_trainingOpen || _supervisorDialog>0 || _invasionPicker>=0 || _inspectorFoldersOpen || _folderTreeOpen || _factionPickerOpen || _hangarDrag.HasValue)return false;
        if(NodeCheckButtonVisible && !_nodeInspectorClosing && NodeCheckButton.Contains(_pointer)){
            var r=RulebookHintArea;DrawRulebookPaper(r);_ui.Text(WarCheckMode?"開戦予約のチェック":"地点のチェック",new(r.X+24,r.Y+20),.46f,RulebookInk);
            _ui.Box(new(r.X+24,r.Y+51,r.Width-48,1),RulebookInk*.5f);
            string hint=WarCheckMode?"開戦予約モードの地図に赤いチェックマークを付けます。ターン終了時に消えます。好きなようにお使いください。":NodeCheckButtonHint;
            int split=hint.IndexOf('。')+1;
            _ui.Text(hint[..split],new(r.X+24,r.Y+65),.38f,RulebookInk);
            _ui.Text(hint[split..],new(r.X+24,r.Y+93),.38f,RulebookInk);return true;
        }
        if(_nodeCheckHits.Any(h=>h.Bounds.Contains(_pointer))){DrawRulebookHint(WarCheckMode?"開戦予約のチェック":"地点のチェック",NodeCheckMarkerHint);return true;}
        return false;
    }
}

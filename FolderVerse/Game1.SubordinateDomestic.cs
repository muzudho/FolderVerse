namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int[] _subordinateNodes;
    private HashSet<int> _subordinateAllowed;
    private int _subordinateIndex,_subordinateController;
    private double _subordinateAge;
    private bool _subordinateActed;
    private SubordinateDomesticResult _subordinateResult;
    private bool BeginSubordinateDomestic()
    {
        if(_demoActive || _autoPlay || !DomesticOperationsAllowed)return false;
        EnsureNodeCheckContext();int controller=_setup.TurnPlayer;
        var checks=_nodeChecksByPlayer.GetValueOrDefault(controller)??new();
        var pending=SubordinateDomestic.Pending(_setup,controller,checks);
        if(pending.Length==0)return false;
        _subordinateNodes=pending;_subordinateAllowed=pending.ToHashSet();_subordinateController=controller;
        _subordinateIndex=0;_subordinateAge=0;_subordinateActed=false;_subordinateResult=null;
        _statusMenuOpen=false;_conquestOverview=false;
        BeginComputerView(_setup.Nodes.All[pending[0]].Owner);ShowSubordinateNode();return true;
    }
    private void ShowSubordinateNode()
    {
        var node=_setup.Nodes.All[_subordinateNodes[_subordinateIndex]];
        ShowComputerView(node.Owner);FocusDemoNode(node);
        OpenNodeInspector(node.Id);OpenNodeAssembly();UpdateDomesticSlide(1f);FocusInspectorLocation();
        _subordinateAge=0;_subordinateActed=false;_subordinateResult=null;
    }
    private bool UpdateSubordinateDomestic(double elapsed,bool skip)
    {
        if(_subordinateNodes==null)return false;
        _subordinateAge+=elapsed;
        do{
            int node=_subordinateNodes[_subordinateIndex];bool detailed=_setup.ShowComputerOperations(_setup.Nodes.All[node].Owner);
            double markAt=detailed?.9:.25,duration=detailed?1.8:.7;
            if(!_subordinateActed && (skip || _subordinateAge>=markAt)){
                _subordinateResult=SubordinateDomestic.Operate(_setup,_subordinateController,node,_subordinateAllowed);
                if(!_nodeChecksByPlayer.TryGetValue(_subordinateController,out var checks))_nodeChecksByPlayer[_subordinateController]=checks=new();
                checks.Add(node);_subordinateActed=true;
                _operations.Write("subordinate_domestic",_subordinateResult);
            }
            if(!skip && _subordinateAge<duration)return true;
            if(++_subordinateIndex>=_subordinateNodes.Length)break;
            _subordinateAge=0;_subordinateActed=false;_subordinateResult=null;
            if(!skip)ShowSubordinateNode();
        }while(skip);
        if(_subordinateIndex<_subordinateNodes.Length)return true;
        _subordinateNodes=null;_subordinateAllowed=null;EndComputerView();SubmitDomesticPhase(false);return true;
    }
    private void DrawSubordinateDomestic(Matrix transform)
    {
        if(_subordinateNodes==null)return;
        var node=_setup.Nodes.All[_subordinateNodes[_subordinateIndex]];bool detailed=_setup.ShowComputerOperations(node.Owner);
        _spriteBatch.Begin(transformMatrix:transform);
        var panel=new Rectangle(1000,778,800,180);_ui.Box(panel,new Color(18,36,48,245));
        _ui.Text("手下が未チェックの地点を確認 / "+_setup.ConquerorNames[node.Owner],new(1020,794),.46f,Cream);
        string location=_setup.Nodes.DisplayName(node);_ui.Text(location,new(1020,834),Math.Min(.38f,750/Math.Max(1,_font.MeasureString(location).X)),Cream);
        string action=_subordinateResult?.Action??"製造・組立・出撃の予約を確認";
        _ui.Text(action,new(1020,870),Math.Min(.38f,750/Math.Max(1,_font.MeasureString(action).X)),_subordinateActed?new Color(141,228,161):Cream);
        _ui.Text($"{_subordinateIndex+1} / {_subordinateNodes.Length} 地点 / Escでスキップ",new(1020,912),.34f,Cream);
        var button=NodeCheckButton;
        if(_subordinateActed){PrimaryRoundBox(button,new Color(39,143,75),6);DrawNodeCheckGlyph(button,Color.White);}
        float t=Math.Clamp((float)(_subordinateAge/(detailed?.9:.25)),0,1);
        DrawDemoHand(Vector2.SmoothStep(new(1120,740),button.Center.ToVector2(),t),_subordinateActed?.3f:0);
        _spriteBatch.End();
    }
}

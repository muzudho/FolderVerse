namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private Node[] _computerDomesticNodes;
    private int _computerDomesticIndex;
    private double _computerDomesticAge;
    private bool _computerAssembled;
    private const double ComputerAssemblyTime=1.55,ComputerDomesticSeconds=2.4;

    private bool ShowsComputerTurn(int ruler)=>!_setup.IsHumanPlayer(ruler) &&
        _setup.ControllerOf(ruler)<0 && _setup.ShowComputerOperations(ruler);

    private void BeginComputerView(int ruler)
    {
        _computerViewRuler=_setup.PlayerSlot;
        _computerViewLeader=_setup.Relations.Leader;
        ShowComputerView(ruler);
    }

    private void ShowComputerView(int ruler)
    {
        ResetInspector();
        _setup.SelectTurnRuler(ruler);
        RestorePlayerView();
    }

    private void EndComputerView()
    {
        ResetInspector();
        _setup.SelectPlayer(_computerViewRuler);
        _setup.Relations.Leader=_computerViewLeader;
        RestorePlayerView();
    }

    private bool ShowComputerDomestic()
    {
        if(_demoActive)return false;
        var nodes=_setup.Nodes.All.Where(node=>node.Owner>=0 && ShowsComputerTurn(node.Owner) &&
            _setup.Relations.CanAct(node.Owner) && !_setup.Campaign.RobotBattles.LockedNode(node.Id))
            .GroupBy(node=>node.Owner).OrderBy(group=>group.Key)
            .Select(group=>group.OrderByDescending(node=>_setup.Robots.AssemblySelection(node.Id,node.Owner,true)!=null).First())
            .ToArray();
        if(nodes.Length==0)return false;
        _computerDomesticNodes=nodes;
        _computerDomesticIndex=0;
        _computerDomesticAge=0;
        _computerAssembled=false;
        BeginComputerView(nodes[0].Owner);
        return true;
    }

    private bool UpdateComputerDomestic(double elapsed,bool skip)
    {
        if(_computerDomesticNodes==null)return false;
        double previousAge=_computerDomesticAge;
        _computerDomesticAge+=elapsed;
        do
        {
            var node=_computerDomesticNodes[_computerDomesticIndex];
            if(previousAge<ComputerAssemblyTime && (skip || _computerDomesticAge>=ComputerAssemblyTime) &&
                _setup.Robots.AssemblySelection(node.Id,node.Owner,true) is {} parts)
            {
                _computerAssembled=_setup.Robots.Assemble(node.Id,parts,node.Owner)!=null;
            }
            if(!skip && _computerDomesticAge<ComputerDomesticSeconds)return true;
            _computerDomesticAge=previousAge=0;
            _computerAssembled=false;
            if(++_computerDomesticIndex==_computerDomesticNodes.Length)break;
            if(!skip)ShowComputerView(_computerDomesticNodes[_computerDomesticIndex].Owner);
        }while(skip);
        if(!skip && _computerDomesticIndex<_computerDomesticNodes.Length)return true;
        _computerDomesticNodes=null;
        EndComputerView();
        _setup.Campaign.BeginDelivery(_setup);
        FinishDomesticDelivery();
        return true;
    }

    private void DrawComputerDomestic(Matrix transform)
    {
        if(_computerDomesticNodes==null)return;
        var node=_computerDomesticNodes[_computerDomesticIndex];
        float age=(float)(_computerDomesticAge-ComputerViewLeadIn);
        string location=_setup.Nodes.DisplayName(node);
        string action=_computerAssembled?"ロボットを組み立てました":
            _computerDomesticAge>=ComputerAssemblyTime?"格納庫の確認完了":"格納庫を確認";
        _spriteBatch.Begin(transformMatrix:transform);
        _ui.Box(ComputerMarchPanel,new Color(18,36,48));
        _ui.Text("コンピューターの操作 / "+_setup.ConquerorNames[node.Owner],new(82,199),.58f,Cream);
        _ui.Button(ComputerMarchTarget,location,Muted,Math.Min(.45f,670/_font.MeasureString(location).X));
        _ui.Button(ComputerMarchOrder,action,_computerDomesticAge>=ComputerAssemblyTime?Accent:Muted,.45f);
        _ui.Text("内政フェーズ / Escでスキップ",new(82,403),.39f,Cream);
        if(age>=0)DrawDemoHand(Vector2.SmoothStep(ComputerMarchTarget.Center.ToVector2(),ComputerMarchOrder.Center.ToVector2(),
            Math.Clamp(age/.8f,0,1)),Math.Clamp(1.2f-age,0,.4f));
        _spriteBatch.End();
    }
}

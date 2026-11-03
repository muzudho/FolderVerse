namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private int _functionTransferSource=-1,_functionTransferPage;
    private string _functionTransferMessage="";
    private static readonly Rectangle FunctionTransferButton=new(84,836,784,48);
    private static readonly Rectangle FunctionTransferCancel=new(684,889,184,26);
    private int[] FunctionTransferTargets=>_functionTransferSource<0?Array.Empty<int>():_setup.Routes.Neighbors(_setup.Nodes.All[_functionTransferSource])
        .Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id).Where(n=>_setup.Campaign.FunctionTransfers.CanReserve(_setup,_functionTransferSource,n)).Distinct().OrderBy(n=>n).ToArray();
    private bool FunctionTransferOfficeClick(Point local)
    {
        if(!_nodeOfficeOpen || _populationNode<0)return false;
        if(FunctionTransferCancel.Contains(local)){
            if(DomesticOperationsAllowed)_setup.Campaign.FunctionTransfers.Cancel(_populationNode);_functionTransferMessage="予約を取り消しました";return true;
        }
        if(!FunctionTransferButton.Contains(local))return false;
        if(!DomesticOperationsAllowed || !_setup.Nodes.All[_populationNode].Functional || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        _functionTransferSource=_populationNode;_functionTransferPage=0;_functionTransferMessage="";return true;
    }
    private bool FunctionTransferInput(bool click,bool escape)
    {
        if(_functionTransferSource<0)return false;
        if(escape || !DomesticOperationsAllowed){_functionTransferSource=-1;return true;}
        if(!click)return false;
        var targets=FunctionTransferTargets;int picked=-1;
        if(new Rectangle(810+NodeInspectorOffset,260,60,40).Contains(_pointer)){_functionTransferSource=-1;return true;}
        if(new Rectangle(84+NodeInspectorOffset,839,120,40).Contains(_pointer))_functionTransferPage=Math.Max(0,_functionTransferPage-1);
        if(new Rectangle(714+NodeInspectorOffset,839,120,40).Contains(_pointer))_functionTransferPage=Math.Min(Math.Max(0,(targets.Length-1)/6),_functionTransferPage+1);
        for(int i=0;i<6;i++)if(new Rectangle(84+NodeInspectorOffset,330+i*78,784,65).Contains(_pointer) && _functionTransferPage*6+i<targets.Length)picked=targets[_functionTransferPage*6+i];
        if(MapViewBounds.Contains(_pointer))picked=_statusGlobe?HitGlobeNode(_pointer,targets):_net.HitNode(_setup,NetPanel,_pointer,targets);
        if(picked>=0){
            _setup.Campaign.FunctionTransfers.Reserve(_setup,_functionTransferSource,picked);
            _functionTransferSource=-1;_functionTransferMessage="配送フェーズの最初に実施します";
        }
        return true;
    }
    private void DrawFunctionTransferOffice()
    {
        bool enabled=DomesticOperationsAllowed && _setup.Nodes.All[_populationNode].Functional && CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Button(FunctionTransferButton,"地点の機能移転の予約",enabled?Accent:Muted,.5f);
        var order=_setup.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==_populationNode);
        if(order!=null){
            string label="予約先："+_setup.Nodes.DisplayName(_setup.Nodes.All[order.Target]);
            _ui.Text(label,new(84,895),Math.Min(.32f,580/Math.Max(1,_font.MeasureString(label).X)),Cream);
            _ui.Button(FunctionTransferCancel,"予約取消",Muted,.38f);
        }
        if(order==null && _functionTransferMessage.Length>0)_ui.Text(_functionTransferMessage,new(84,895),.32f,Cream);
        if(_setup.Nodes.All[_populationNode].FunctionUnits>1)_ui.Text($"機能 {_setup.Nodes.All[_populationNode].FunctionUnits} 地点分 / 参照元："+string.Join("・",NodeFunctionTransfers.Members(_setup,_populationNode)),new(84,764),.25f,Cream);
        if(_functionTransferSource!=_populationNode)return;
        _ui.Box(new(70,256,810,658),new Color(18,35,46));
        _ui.Text("機能の操作先を選ぶ / 配送時に実施",new(84,274),.5f,Cream);_ui.Button(new(810,260,60,40),"×",Muted,.5f);
        var targets=FunctionTransferTargets;
        for(int i=0;i<6;i++){
            int index=_functionTransferPage*6+i;if(index>=targets.Length)break;var node=_setup.Nodes.All[targets[index]];
            _ui.Button(new(84,330+i*78,784,65),(node.Functional?"併合：":"引っ越し：")+_setup.Nodes.DisplayName(node),Muted,.37f);
        }
        if(targets.Length==0)_ui.Text("移転できる隣の地点がありません",new(84,350),.43f,Cream);
        _ui.Button(new(84,839,120,40),"←",Muted,.45f);_ui.Button(new(714,839,120,40),"→",Muted,.45f);
        _ui.Center($"{_functionTransferPage+1} / {Math.Max(1,(targets.Length+5)/6)}",new(220,839,480,40),.4f,Cream);
    }
}

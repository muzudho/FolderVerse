namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private static readonly Rectangle FunctionTransferCancel=new(684,889,184,26);
    private bool FunctionTransferOfficeClick(Point local)=>_nodeOfficeOpen && _populationNode>=0 &&
        DomesticOperationsAllowed && FunctionTransferCancel.Contains(local) && _setup.Campaign.FunctionTransfers.Cancel(_populationNode);

    private void DrawFunctionTransferOffice()
    {
        var order=_setup.Campaign.FunctionTransfers.Orders.FirstOrDefault(o=>o.Source==_populationNode);
        if(order!=null){
            string label="予約先："+_setup.Nodes.DisplayName(_setup.Nodes.All[order.Target]);
            _ui.Text(label,new(84,895),Math.Min(.32f,580/Math.Max(1,_font.MeasureString(label).X)),Cream);
            _ui.Button(FunctionTransferCancel,"予約取消",Muted,.38f);
        }
        if(_setup.Nodes.All[_populationNode].FunctionUnits>1)_ui.Text($"機能 {_setup.Nodes.All[_populationNode].FunctionUnits} 地点分 / 参照元："+string.Join("・",NodeFunctionTransfers.Members(_setup,_populationNode)),new(84,764),.25f,Cream);
    }
}

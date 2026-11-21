namespace FolderVerse;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private void DrawDismantlingLane(RobotWorkshop line,int node,int row,long? worker)
    {
        int y=FactoryRowY(row);_ui.Text("解体中は元の格納庫を確保します",new(352,y+240),.30f,Cream);
        _ui.Text("解体レーン",new(185,y+15),.55f,Cream);
        if(worker is long id && _setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==id) is {} robot)DrawRobotGlyph(new(176,y+60,68,104),robot);
        if(line.DisposalTarget is long target){
            var cargo=_setup.Robots.Nodes[node].Robots.FirstOrDefault(r=>r.Id==target);
            if(cargo!=null)DrawRobotGlyph(new(352,y+55,100,140),cargo);
            _ui.Text($"{IndustryTime.Format(line.DisposalAge)} / {IndustryTime.Format(line.DisposalPeriod)} 産業ターン",new(470,y+80),.4f,Cream);
            if(line.DisposalAge>=line.DisposalPeriod)_ui.Text("スクラップの置き場を待っています",new(352,y+208),.36f,Cream);
        }else _ui.Text("格納庫の解体予約付きの荷を待っています",new(305,y+100),.38f,Cream);
    }
}

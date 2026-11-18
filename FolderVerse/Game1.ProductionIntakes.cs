namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private RobotWorkshop _intakeLine;
    private int _intakeNode,_intakePage;
    private bool _pendingIntakesOpen;
    private static readonly Rectangle FactoryPendingIntakesButton=new(90,926,550,38);
    private static readonly Rectangle IntakeClose=new(1430,840,180,50),IntakePrevious=new(350,840,110,50),IntakeNext=new(480,840,110,50);
    private static Rectangle IntakeRow(int i)=>new(350,295+i*62,1260,54);
    private static Rectangle FactoryIntakeButton(int row)=>new(274,FactoryRowY(row)+219,92,28);
    private int[] IntakeChoices=>_setup.Routes.Neighbors(_setup.Nodes.All[_intakeNode]).Select(r=>_setup.Nodes.At(r.Target,r.Entry).Id)
        .Where(n=>PlanningFunctional(n) && CanManageNodeFactory(_setup.Nodes.All[n].Owner)).Distinct().Prepend(-1).ToArray();
    private bool ProductionIntakeInput(bool click,bool escape)
    {
        if(_pendingIntakesOpen){
            if(escape || click && IntakeClose.Contains(_pointer)){_pendingIntakesOpen=false;return true;}
            if(!click)return true;
            var pending=_setup.Robots.ProductionIntakes.Where(i=>i.Source==_intakeNode).ToArray();
            if(IntakePrevious.Contains(_pointer))_intakePage=Math.Max(0,_intakePage-1);
            else if(IntakeNext.Contains(_pointer))_intakePage=Math.Min(Math.Max(0,(pending.Length-1)/8),_intakePage+1);
            else for(int row=0;row<8;row++){
                int index=_intakePage*8+row;if(index>=pending.Length)break;
                if(IntakeRow(row).Contains(_pointer)){
                    _intakeLine=pending[index].Line;_intakePage=0;_pendingIntakesOpen=false;break;
                }
            }
            return true;
        }
        if(_intakeLine==null)return false;
        if(escape || click && IntakeClose.Contains(_pointer)){_intakeLine=null;return true;}
        if(!click)return true;
        var choices=IntakeChoices;
        if(IntakePrevious.Contains(_pointer))_intakePage=Math.Max(0,_intakePage-1);
        else if(IntakeNext.Contains(_pointer))_intakePage=Math.Min(Math.Max(0,(choices.Length-1)/8),_intakePage+1);
        else for(int i=0;i<8;i++){
            int index=_intakePage*8+i;if(index>=choices.Length)break;
            if(IntakeRow(i).Contains(_pointer)){
                int? target=choices[index]<0?null:choices[index];
                _setup.Robots.SetIntakeTarget(_intakeLine,target);_intakeLine=null;return true;
            }
        }
        return true;
    }
    private void DrawProductionIntakePicker()
    {
        if(_pendingIntakesOpen){
            _ui.Box(new(0,0,1920,1080),Color.Black*.75f);_ui.Box(new(310,185,1340,730),new Color(27,49,62));
            _ui.Text("完成品の入荷待ち",new(350,215),.7f,Cream);
            _ui.Text("作業員を解除した後も、完成した品はここで入荷先を選べます。",new(350,265),.38f,Cream);
            var waitingProducts=_setup.Robots.ProductionIntakes.Where(i=>i.Source==_intakeNode).ToArray();
            _intakePage=Math.Clamp(_intakePage,0,Math.Max(0,(waitingProducts.Length-1)/8));
            for(int row=0;row<8;row++){
                int index=_intakePage*8+row;if(index>=waitingProducts.Length)break;
                var item=waitingProducts[index];var bounds=IntakeRow(row);
                _ui.Button(bounds,$"ID {item.Product.Id} / {PartsLabel(item.Product.Parts)} / {item.Status} / 入荷先を選ぶ",Muted,.38f);
            }
            if(waitingProducts.Length==0)_ui.Text("入荷待ちはありません",new(350,320),.5f,Cream);
            _ui.Button(IntakePrevious,"←",Muted,.5f);_ui.Button(IntakeNext,"→",Muted,.5f);
            _ui.Button(IntakeClose,"閉じる",Accent,.5f);return;
        }
        if(_intakeLine==null)return;
        _ui.Box(new(0,0,1920,1080),Color.Black*.75f);_ui.Box(new(310,185,1340,730),new Color(27,49,62));
        _ui.Text("製造品の入荷先を予約",new(350,215),.7f,Cream);
        _ui.Text("同地点または隣接地点 / 製造後の内政で操作を予約し、配送時に入荷",new(350,265),.38f,Cream);
        var choices=IntakeChoices;
        for(int i=0;i<8;i++){
            int index=_intakePage*8+i;if(index>=choices.Length)break;int node=choices[index];
            string label=node<0?"同地点（工場の移転に追従）":_setup.Nodes.DisplayName(_setup.Nodes.All[node]);
            bool selected=node<0?!_intakeLine.IntakeTarget.HasValue:_intakeLine.IntakeTarget==node;
            _ui.Button(IntakeRow(i),(selected?"✓ ":"□ ")+label,selected?Accent:Muted,Math.Min(.48f,1180/_font.MeasureString(label).X));
        }
        _ui.Button(IntakePrevious,"←",Muted,.5f);_ui.Button(IntakeNext,"→",Muted,.5f);
        var pending=_setup.Robots.ProductionIntakes.Where(i=>ReferenceEquals(i.Line,_intakeLine)).ToArray();
        if(pending.Length>0)_ui.Text($"入荷予約 {pending.Length}件 / {pending[0].Status}",new(350,800),.4f,Cream);
        else if(!string.IsNullOrEmpty(_intakeLine.IntakeFailure))_ui.Text(_intakeLine.IntakeFailure,new(350,800),.4f,Cream);
        _ui.Button(IntakeClose,"閉じる",Accent,.5f);
    }
}

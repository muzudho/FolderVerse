namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private Texture2D _robotFactoryIcon;
    private bool _nodeFactoryOpen;
    private int _factoryDropDown=-1,_factoryLine,_factoryPlanLine=-1;
    private string _factoryMessage="";
    private static readonly Rectangle NodeFactoryTile=new(282,210,190,40);
    private bool NodeFactoriesAllowed=>_setup.TutorialStep!=1;
    private bool CanManageNodeFactory(int owner)=>_setup.ControllerOf(owner)==_setup.TurnPlayer;
    private void OpenNodeFactory(){if(!NodeFactoriesAllowed)return;if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length)return;if(_setup.Robots.Workshops[_populationNode].ProductionLines.Length==0 || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return;_factoryPage=0;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;_nodeOfficeOpen=false;_lastNodeTab=1;}
    private static int FactoryProductionWidth(int period)=>Math.Min(15,Math.Max(1,period))*24;
    private static Rectangle FactoryRobotButton(int line,int period)=>new(160+Math.Max(0,FactoryProductionWidth(period)-72),288+line*200,72,88);
    private static Rectangle FactoryRobotArrow(int line,int period){var r=FactoryRobotButton(line,period);return new(r.Right+2,r.Y+30,24,28);}
    private Rectangle FactoryProductionButton(int line)
    {
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length)return Rectangle.Empty;
        var lines=_setup.Robots.Workshops[_populationNode].ProductionLines;
        if(line<0 || line>=lines.Length)return Rectangle.Empty;
        return FactoryRobotButton(line%3,lines[line].Manufacturing?lines[line].ProductionPeriod:3);
    }
    private static Rectangle FactoryPlanButton(int i)=>new(94+i%4*194,410+i/4*160,180,144);
    private Rectangle FactoryDropButton(int field)
    {
        if(field==2)return new(420,884,110,30);
        var line=_setup.Robots.Workshops[_populationNode].ProductionLines[_factoryLine];
        int width=FactoryProductionWidth(line.Manufacturing?line.ProductionPeriod:3),rest=Math.Min(4,line.RestTurns),y=284+(_factoryLine%3)*200;
        int restLeft=160+width+rest*72/2-34;
        return field==0?new(Math.Min(160+width/2-34,restLeft-72),y+155,68,44):new(restLeft,y+155,68,44);
    }
    private Rectangle FactoryDropOption(int field,int index){var r=FactoryDropButton(field);int count=field==2?_setup.Robots.Workshops[_populationNode].ReservedSlotLimit+1:9;if(field==2 && count>13){int columns=(count+12)/13,width=784/columns;return new(84+index/13*width,490+index%13*30,width-3,30);}int top=Math.Max(260,r.Y-count*30);return new(r.X,top+index*30,r.Width,30);}
    private bool NodeFactoryClick(Point pointer)
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length || _populationNode>=_setup.Nodes.All.Length){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        var factory=_setup.Robots.Workshops[_populationNode];
        if(FactoryPageClick(pointer))return true;
        if(_factoryLine<0 || _factoryLine>=factory.ProductionLines.Length){_factoryLine=0;_factoryDropDown=-1;}
        if(_factoryPlanLine>=factory.ProductionLines.Length)_factoryPlanLine=-1;
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<(field==2?_setup.Robots.Workshops[_populationNode].ReservedSlotLimit+1:9);i++)if(FactoryDropOption(field,i).Contains(pointer)){
            if(field==2)factory.SetReservedSlots(i);
            else {var line=factory.ProductionLines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<11;i++)if(FactoryPlanButton(i).Contains(pointer)){factory.ProductionLines[line].SelectPlan(i);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=_factoryPage*3;l<Math.Min(factory.ProductionLines.Length,(_factoryPage+1)*3);l++){
            var line=factory.ProductionLines[l];if(FactoryProductionButton(l).Contains(pointer) || FactoryRobotArrow(l%3,line.Manufacturing?line.ProductionPeriod:3).Contains(pointer)){_factoryPlanLine=l;_factoryLine=l;return true;}
            _factoryLine=l;for(int field=0;field<2;field++)if(FactoryDropButton(field).Contains(pointer)){_factoryDropDown=field;return true;}
        }
        if(FactoryDropButton(2).Contains(pointer))_factoryDropDown=2;
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void FactoryDrop(int field,int value)
    {var r=FactoryDropButton(field);_ui.Button(r,(field==0?"× ":field==1?"休 ":"")+value,Accent,.48f);for(int row=0;row<8;row++)_ui.Box(new(r.Right-23+row,r.Center.Y-3+row,16-row*2,1),SelectionInk);}
    private void DrawNodeFactory()
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;return;}
        var node=_setup.Nodes.All[_populationNode];var factory=_setup.Robots.Workshops[node.Id];
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));


        int savedLine=_factoryLine;
        RobotWorkshop hoveredCaret=null;
        for(int l=Math.Max(_factoryPage*3,factory.ProductionLines.Length);l<(_factoryPage+1)*3;l++){
            int y=260+(l%3)*200;var area=new Rectangle(78,y,782,196);
            if(l>=factory.LineLimit){
                _ui.Box(area,new Color(24,78,110));
                for(int row=0;row<7;row++)for(int x=area.X+12;x<area.Right-30;x+=64){int wave=(int)(MathF.Sin(x*.05f+row)*5);_ui.Box(new(x,area.Y+20+row*27+wave,34,3),new Color(76,151,178));}
                _ui.Center("海上 / レーン上限１",area,.5f,Cream);
            }else{_ui.Box(area,new Color(24,43,54));_ui.Center("空きレーン / 工場作業員予約で追加",area,.45f,Cream*.7f);}
        }
        for(int l=_factoryPage*3;l<Math.Min(factory.ProductionLines.Length,(_factoryPage+1)*3);l++){
            var line=factory.ProductionLines[l];int y=284+(l%3)*200,period=line.Manufacturing?line.ProductionPeriod:3;_factoryLine=l;
            _ui.Box(new(78,y-24,782,196),l%2==0?new Color(37,63,77):new Color(46,67,79));
            _ui.Center((l+1).ToString(),new(84,y+22,62,96),1.35f,new Color(167,190,194));
            int productionWidth=FactoryProductionWidth(period),shownRest=line.RestEnabled?Math.Min(4,line.RestTurns):0;
            _ui.Box(new(160,y+96,productionWidth,8),Accent);
            _ui.Box(new(160+productionWidth,y+96,shownRest*72,8),new Color(137,151,157));
            for(int unit=0;unit<=Math.Min(15,period);unit++)_ui.Box(new(160+unit*24-1,y+96,unit%3==0?2:1,unit%3==0?8:4),unit%3==0?new Color(27,49,62):Color.White);
            for(int unit=0;unit<=shownRest*3;unit++)_ui.Box(new(160+productionWidth+unit*24-1,y+96,unit%3==0?2:1,unit%3==0?8:4),unit%3==0?new Color(27,49,62):Color.White);
            DrawIndustryDuration(0,new(146,y+109,28,30),Cream,.3f);
            DrawIndustryDuration(period,new(160+productionWidth-40,y+109,80,30),Cream,.3f);
            if(shownRest>0)DrawIndustryDuration(line.CycleTurns,new(160+productionWidth+shownRest*72-40,y+109,80,30),Cream,.3f);
            int caret=160;
            if(line.Manufacturing){
                // Reuse the one-robot stretch for each repeat; only the final repeat enters rest.
                _ui.Box(new(160+productionWidth-1,y+93,2,14),Cream);
                double progress=line.LaneProgress;
                caret=160+(int)Math.Round(progress<=period
                    ?productionWidth*progress/period
                    :productionWidth+shownRest*72*(progress-period)/IndustryTime.Whole(line.RestTurns));
            }
            var robot=FactoryProductionButton(l);_ui.Box(robot,_factoryPlanLine==l || robot.Contains(_pointer)?Accent:Muted);
            var arrow=FactoryRobotArrow(l%3,period);for(int row=0;row<10;row++)_ui.Box(new(arrow.X+2+row,arrow.Y+6+row,20-row*2,1),Cream);
            if(line.Manufacturing)DrawRobotGlyph(robot,line.CurrentProductionPlan.Preview(node.Owner),whiteOutline:true);else _ui.Center("×",robot,.7f,Cream);
            if(period>15)DrawFactoryOmission(160+2*72,y);
            if(line.RestEnabled && line.RestTurns>shownRest)DrawFactoryOmission(160+productionWidth+72,y);
            if(line.Manufacturing){
                DrawFactoryCaret(caret,y,line);
                if(new Rectangle(caret-12,y+27,24,77).Contains(_pointer))hoveredCaret=line;
            }
            int width=productionWidth,mid=160+width/2;_ui.Box(new(160,y+140,width,2),Cream);_ui.Box(new(160,y+135,2,7),Cream);_ui.Box(new(158+width,y+135,2,7),Cream);for(int i=0;i<5;i++){_ui.Box(new(mid-5+i,y+142+i,1,1),Cream);_ui.Box(new(mid+5-i,y+142+i,1,1),Cream);}FactoryDrop(0,line.RestEvery);FactoryDrop(1,line.RestTurns);
            string status=!line.Manufacturing?"製造しない":line.Paused?"一時停止":line.RestRemaining>0?$"休憩中 あと{IndustryTime.Format(line.RestRemaining)} / 次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン":line.Waiting?$"出荷待ち {IndustryTime.Format(line.WaitingProductionTurns)}生産ターン / 格納できるまで停止":$"次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン / {line.ProducedSinceRest+1}台目・工期{IndustryTime.Format(line.ProductionAge)}/{IndustryTime.Format(period)}";
            if(line.Manufacturing)status=$"周期 {IndustryTime.Format(line.CycleProgress)} / {IndustryTime.Format(line.CycleTurns)} / "+status;
            if(line.FunctionOriginNode is int origin)status="参照元："+_setup.Nodes.DisplayName(_setup.Nodes.All[origin])+" / "+status;
            _ui.Text(status,new(160,y-19),Math.Min(.3f,690/_font.MeasureString(status).X),Cream);
            if(factory.WorkerAt(l) is long workerId){var worker=_setup.Robots.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==workerId);if(worker!=null){DrawRobotGlyph(new(754,y+8,60,84),line.WorkerSnapshot??worker);_ui.Text("工場作業員",new(674,y+8),.3f,Accent);_ui.Text("ID "+workerId,new(674,y+36),.28f,Cream);if(line.WorkerSnapshot!=null)_ui.Text("任命時",new(674,y+62),.25f,Cream);}}
            else _ui.Text("常設レーン",new(674,y+8),.3f,Cream*.7f);
        }
        _factoryLine=savedLine;
        _ui.Text("格納庫は必ず",new(180,888),.43f,Cream);FactoryDrop(2,factory.ReservedSlots);_ui.Text("個空ける",new(552,888),.43f,Cream);
        DrawNodeTabs();DrawFactoryPages();
        _ui.Text($"産業ターン累計 {IndustryTime.Format(_setup.Robots.IndustryTurn)}   /   柱：現在位置　緑：生産　黄：休憩　赤：空き枠待ち",new(160,248),.25f,Cream);
        if(_factoryPlanLine>=0){
            var line=factory.ProductionLines[_factoryPlanLine];_ui.Box(new(84,360,790,590),new(22,40,54));
            _ui.Text($"生産ライン {_factoryPlanLine+1} / 正規・安い・一体型",new(160,374),.45f,Cream);
            ProductionPlan hovered=null;
            for(int i=0;i<11;i++){
                var r=FactoryPlanButton(i);bool selected=line.SelectedPlan==i;
                _ui.Box(r,selected?Accent:Muted);
                if(i==3)_ui.Center("製造しない",r,.42f,selected?SelectionInk:Cream);
                else{
                    var plan=line.PlanAt(i);DrawRobotGlyph(new(r.X+54,r.Y+8,72,80),plan.Preview(node.Owner),whiteOutline:true);
                    _ui.Center(plan.IntegratedParts==0?"通常品":IntegratedLabel(plan.IntegratedParts),new(r.X,r.Y+96,r.Width,22),.28f,selected?SelectionInk:Cream);
                    DrawIndustryDuration(plan.Turns,new(r.X+4,r.Bottom-27,r.Width-8,26),selected?SelectionInk:Cream,prefix:plan.QualityLabel+" ",suffix:" 産業ターン");
                    if(r.Contains(_pointer))hovered=plan;
                }
            }
            _ui.Center(line.WorkerSnapshot!=null?"作業員レーン：任命時のパーツで候補を固定":"常設レーン：地点の製造候補",new(96,886,764,24),.32f,Cream);
            if(hovered!=null){
                var preview=hovered.Preview(node.Owner);
                string detail=string.Join(" / ",new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(part=>(preview.Parts&part)!=0).Select(part=>$"{PartsLabel(part)} #{preview.Origin(part)+1} {(preview.IsCheap(part)?"安い":"正規")}"));
                _ui.Center(detail,new(96,916,764,26),Math.Min(.34f,750/_font.MeasureString(detail).X),Cream);
            }
        }
        if(_factoryDropDown>=0){
            int field=_factoryDropDown,count=field==2?_setup.Robots.Workshops[_populationNode].ReservedSlotLimit+1:9;
            for(int i=0;i<count;i++){
                var option=FactoryDropOption(field,i);
                _ui.Button(option,(field==0?"× ":field==1?"休 ":"")+(field==2?i:i+1),Accent,.40f);
            }
        }
        if(hoveredCaret!=null && _factoryDropDown<0 && _factoryPlanLine<0){
            string hint=hoveredCaret.Paused?"一時停止中だよ。再開すると製造が進むよ":hoveredCaret.Waiting?$"{IndustryTime.Format(hoveredCaret.WaitingProductionTurns)}生産ターンも待っていますよ！ 格納できれば出荷するよ":$"あと{IndustryTime.Format(hoveredCaret.TurnsUntilNextProduction)}生産ターンでロボットを製造できるよ";
            if(hoveredCaret.RestRemaining>0)hint+=$"（休憩あと{IndustryTime.Format(hoveredCaret.RestRemaining)}ターンを含む）";
            const float scale=.38f;
            int hintWidth=(int)Math.Ceiling(_font.MeasureString(hint).X*scale)+24;
            var hintBox=new Rectangle(Math.Clamp(_pointer.X+18,70,888-hintWidth),Math.Clamp(_pointer.Y-48,260,922),hintWidth,38);
            _ui.Box(hintBox,Cream);_ui.Box(new(hintBox.X+2,hintBox.Y+2,hintBox.Width-4,hintBox.Height-4),new Color(16,32,43));
            _ui.Text(hint,new(hintBox.X+12,hintBox.Y+9),scale,Cream);
        }
    }
    private void DrawFactoryCaret(int x,int y,RobotWorkshop line)
    {
        var ink=new Color(16,32,43);
        var signal=line.Paused?new Color(167,190,194):line.RestRemaining>0?new Color(255,204,112):line.Waiting?new Color(255,113,105):new Color(102,244,175);
        float pulse=.78f+.22f*(.5f+.5f*MathF.Sin(_animationTime*2.4f));
        // A fluorescent tube and its narrow socket share the exact tick center.
        // Keep the glow vertical so it cannot be mistaken for a range on the floor.
        _ui.Box(new(x-10,y+29,20,64),Color.Lerp(ink,signal,.12f*pulse));
        _ui.Box(new(x-8,y+29,16,64),ink);
        _ui.Box(new(x-5,y+32,10,58),Color.Lerp(signal*.65f,signal,pulse));
        _ui.Box(new(x-2,y+33,4,56),Cream);
        _ui.Box(new(x-6,y+27,12,6),ink);
        _ui.Box(new(x-4,y+28,8,3),Cream);
        _ui.Box(new(x-6,y+90,12,7),ink);
        _ui.Box(new(x-3,y+91,6,5),Cream);
        _ui.Box(new(x-2,y+96,4,8),ink);
        _ui.Box(new(x-1,y+96,2,8),Cream);
    }
    private void DrawFactoryOmission(int x,int y)
    {
        for(int offset=0;offset<=8;offset+=8)for(int row=-8;row<132;row++){
            int bend=(int)Math.Round(Math.Sin(row*.16)*3);
            _ui.Box(new(x+offset+bend-2,y+row,7,1),new(27,49,62));
            _ui.Box(new(x+offset+bend,y+row,2,1),Cream);
        }
    }
    private void DrawNodeFactoryTile(bool active)
    {
        if(!NodeFactoriesAllowed)return;
        if(_setup.Robots.Workshops[_populationNode].ProductionLines.Length==0)return;
        bool enabled=CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner);
        _ui.Box(NodeFactoryTile,active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted);
        _ui.Center("ロボット製造工場",NodeFactoryTile,.38f,enabled?(active || NodeFactoryTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }
}

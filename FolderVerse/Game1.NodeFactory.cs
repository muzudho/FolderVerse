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
    private bool CanOpenNodeFactory=>NodeFactoriesAllowed && _populationNode>=0 &&
        _populationNode<_setup.Robots.Workshops.Length && _populationNode<_setup.Nodes.All.Length &&
        CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && HasFactoryScreenLines();
    private Robot[] PendingFactoryWorkers(RobotWorkshop factory)
    {
        if(!DomesticOperationsAllowed)return Array.Empty<Robot>();
        var assigned=Enumerable.Range(factory.BaseLineCount,factory.ProductionLines.Length-factory.BaseLineCount)
            .Select(factory.WorkerAt).ToHashSet();
        return PlanningStock.Nodes[_populationNode].Robots.Where(r=>r.FactoryWorker && !assigned.Contains(r.Id) &&
                _setup.Robots.PartPlanning.WorkerReservation(r.Id)!=null)
            .OrderBy(r=>r.Id).ToArray();
    }
    private RobotWorkshop[] FactoryScreenLines(RobotWorkshop factory, Robot[] pending)
    {
        var node=_setup.Nodes.All[_populationNode];
        return factory.ProductionLines.Concat(pending.Take(Math.Max(0,factory.LineLimit-factory.ProductionLines.Length)).Select(worker=>{
            var line=factory.CreateWorkerLine(worker,_setup.WorldSeed,node.Id,node.Owner);
            _setup.Robots.PartPlanning.WorkerReservation(worker.Id)?.WorkerLane?.Apply(line);
            return line;
        })).ToArray();
    }
    private bool CanManageNodeFactory(int owner)=>_setup.ControllerOf(owner)==_setup.TurnPlayer;
    private bool HasFactoryScreenLines()
    {
        var factory=PlanningWorkshop(_populationNode);
        return factory.ProductionLines.Length>0 || factory.LineLimit>0 && PendingFactoryWorkers(factory).Length>0;
    }
    private void OpenNodeFactory(){if(!CanOpenNodeFactory)return;_factoryPage=0;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;_nodeOfficeOpen=false;_lastNodeTab=1;}
    private static int FactoryProductionWidth(int period)=>Math.Min(15,Math.Max(1,period))*24;
    private static Rectangle FactoryRobotButton(int line,int period)=>new(160+Math.Max(0,FactoryProductionWidth(period)-72),288+line*200,72,88);
    private static Rectangle FactoryRobotArrow(int line,int period){var r=FactoryRobotButton(line,period);return new(r.Right+2,r.Y+30,24,28);}
    private Rectangle FactoryProductionButton(int line)
    {
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length)return Rectangle.Empty;
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,PendingFactoryWorkers(factory));
        if(line<0 || line>=lines.Length)return Rectangle.Empty;
        return FactoryRobotButton(line%3,lines[line].Manufacturing?lines[line].EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3);
    }
    private static Rectangle FactoryPlanButton(int i)=>new(94+i%4*194,410+i/4*160,180,144);
    private static string FactoryProductLabel(ProductionPlan plan)
    {
        if(plan.IntegratedParts!=0)return PartsLabel(plan.Preview(0));
        var parts=new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs}.Where(part=>(plan.Product&part)!=0).Select(PartsLabel).ToArray();
        return string.Join("／",parts)+(parts.Length>1?"分割型":"");
    }
    private Rectangle FactoryDropButton(int field,RobotWorkshop line=null)
    {
        if(field==2)return new(420,884,110,30);
        line??=PlanningWorkshop(_populationNode).ProductionLines[_factoryLine];
        int width=FactoryProductionWidth(line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3),rest=Math.Min(4,line.RestTurns),y=284+(_factoryLine%3)*200;
        int restLeft=160+width+rest*72/2-34;
        return field==0?new(Math.Min(160+width/2-34,restLeft-72),y+150,68,24):new(restLeft,y+150,68,24);
    }
    private Rectangle FactoryDropOption(int field,int index,RobotWorkshop line=null){var r=FactoryDropButton(field,line);int count=field==2?PlanningWorkshop(_populationNode).ReservedSlotLimit+1:9;if(field==2 && count>13){int columns=(count+12)/13,width=784/columns;return new(84+index/13*width,490+index%13*30,width-3,30);}int top=Math.Max(260,r.Y-count*30);return new(r.X,top+index*30,r.Width,30);}
    private Rectangle FactoryNoRepeatButton(RobotWorkshop line){var rest=FactoryDropButton(1,line);return new(rest.Right+10,rest.Y,145,rest.Height);}
    private bool NodeFactoryClick(Point pointer)
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length || _populationNode>=_setup.Nodes.All.Length){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        var factory=PlanningWorkshop(_populationNode);
        var pending=PendingFactoryWorkers(factory);
        var lines=FactoryScreenLines(factory,pending);
        int liveCount=factory.ProductionLines.Length;
        void SavePending(int index)
        {
            if(index>=liveCount)_setup.Robots.PartPlanning.SetWorkerLane(pending[index-liveCount].Id,lines[index]);
        }
        if (FactoryAlbumOpen)
        {
            int line = _factoryPlanLine;
            _factoryPlanLine = -1;
            if (line >= lines.Length || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) ||
                _setup.Campaign.RobotBattles.LockedNode(_populationNode)) return true;
            if(FactoryStopButton.Contains(pointer)){
                lines[line].SelectPlan(3);SavePending(line);return true;
            }
            if(FactoryHarvestTab.Contains(pointer)){
                if(_setup.Robots.MapleSupplyEnabled)_factoryHarvestPage=!_factoryHarvestPage;
                _factoryPlanLine=line;return true;
            }
            if(_factoryHarvestPage){
                int available=FactoryHarvestAvailable(_populationNode,lines,lines[line]);
                for(int loads=1;loads<=3;loads++){
                    if(!FactoryAlbumCard(loads-1).Contains(pointer))continue;
                    if(loads>available){_factoryPlanLine=line;return true;}
                    lines[line].SelectMapleHarvest(loads);SavePending(line);break;
                }
                return true;
            }
            for (int i = 0; i < lines[line].PlanCount; i++)
            {
                if (i==3 || !FactoryRecipeCard(i).Contains(pointer)) continue;
                lines[line].SelectPlan(i);
                SavePending(line);
                break;
            }
            return true;
        }
        if(FactoryPageClick(pointer))return true;
        if(_factoryLine<0 || _factoryLine>=lines.Length){_factoryLine=0;_factoryDropDown=-1;}
        if(_factoryPlanLine>=lines.Length)_factoryPlanLine=-1;
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<(field==2?factory.ReservedSlotLimit+1:9);i++)if(FactoryDropOption(field,i,field==2?null:lines[_factoryLine]).Contains(pointer)){
            if(field==2)SetPlanningReservedSlots(_populationNode,i);
            else {var line=lines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);SavePending(_factoryLine);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<lines[line].PlanCount;i++)if(FactoryPlanButton(i).Contains(pointer)){lines[line].SelectPlan(i);SavePending(line);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=_factoryPage*3;l<Math.Min(lines.Length,(_factoryPage+1)*3);l++){
            if(l<liveCount && lines[l].MapleRemaining==0 && FactoryIntakeButton(l).Contains(pointer)){_intakeLine=lines[l];_intakeNode=_populationNode;_intakePage=0;return true;}
            var line=lines[l];int period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;
            if(FactoryRobotButton(l%3,period).Contains(pointer) || FactoryRobotArrow(l%3,period).Contains(pointer)){_factoryHarvestPage=false;_factoryPlanLine=l;_factoryLine=l;return true;}
            _factoryLine=l;for(int field=0;field<2;field++)if(FactoryDropButton(field,line).Contains(pointer)){_factoryDropDown=field;return true;}
            if(FactoryNoRepeatButton(line).Contains(pointer)){line.SetDoNotRepeat(!line.DoNotRepeat);SavePending(l);return true;}
        }
        if(FactoryDropButton(2).Contains(pointer))_factoryDropDown=2;
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void FactoryDrop(int field,int value,RobotWorkshop line=null)
    {var r=FactoryDropButton(field,line);_ui.Button(r,(field==0?"× ":field==1?"休 ":"")+value,Accent,.48f);for(int row=0;row<8;row++)_ui.Box(new(r.Right-23+row,r.Center.Y-3+row,16-row*2,1),SelectionInk);}
    private void DrawNodeFactory()
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;return;}
        var node=_setup.Nodes.All[_populationNode];var factory=PlanningWorkshop(node.Id);
        var pending=PendingFactoryWorkers(factory);
        var lines=FactoryScreenLines(factory,pending);
        int displayed=lines.Length;
        int liveCount=factory.ProductionLines.Length;
        _factoryPage=Math.Clamp(_factoryPage,0,Math.Max(0,(displayed-1)/3));
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));


        int savedLine=_factoryLine;
        RobotWorkshop hoveredCaret=null;
        for(int l=Math.Max(_factoryPage*3,displayed);l<(_factoryPage+1)*3;l++){
            int y=260+(l%3)*200;var area=new Rectangle(78,y,782,196);
            if(l>=factory.LineLimit){
                _ui.Box(area,new Color(24,78,110));
                for(int row=0;row<7;row++)for(int x=area.X+12;x<area.Right-30;x+=64){int wave=(int)(MathF.Sin(x*.05f+row)*5);_ui.Box(new(x,area.Y+20+row*27+wave,34,3),new Color(76,151,178));}
                _ui.Center("海上 / レーン上限１",area,.5f,Cream);
            }else{_ui.Box(area,new Color(24,43,54));_ui.Center("空きレーン / 工場作業員予約で追加",area,.45f,Cream*.7f);}
        }
        for(int l=_factoryPage*3;l<Math.Min(displayed,(_factoryPage+1)*3);l++){
            var line=lines[l];int y=284+(l%3)*200,period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;_factoryLine=l;
            bool planned=l>=liveCount;
            bool mapleWait=line.WaitingForMaple(_setup.Robots,node.Id);
            _ui.Box(new(78,y-24,782,196),l%2==0?new Color(37,63,77):new Color(46,67,79));
            _ui.Center((l+1).ToString(),new(84,y+22,62,96),1.35f,new Color(167,190,194));
            if(!planned && line.MapleRemaining==0)_ui.Button(FactoryIntakeButton(l),"入荷先",Muted,.34f);
            if(line.MapleRemaining>0){
                DrawFactoryHarvestLane(line,node.Id,l%3,y);
                continue;
            }
            int productionWidth=FactoryProductionWidth(period);
            int caret=DrawFactoryLaneTrack(line,160,y);
            var robot=FactoryRobotButton(l%3,period);_ui.Box(robot,_factoryPlanLine==l || robot.Contains(_pointer)?Accent:Muted);
            var arrow=FactoryRobotArrow(l%3,period);for(int row=0;row<10;row++)_ui.Box(new(arrow.X+2+row,arrow.Y+6+row,20-row*2,1),Cream);
            if(line.Manufacturing)DrawRobotGlyph(robot,line.CurrentProductionPlan.Preview(node.Owner),whiteOutline:true);else _ui.Center("×",robot,.7f,Cream);
            DrawFactoryLaneOmissions(line,160,y);
            if(line.Manufacturing){
                DrawFactoryCaret(caret,y,line,mapleWait);
                if(!planned && new Rectangle(caret-12,y+27,24,77).Contains(_pointer))hoveredCaret=line;
            }
            int width=productionWidth,mid=160+width/2;_ui.Box(new(160,y+140,width,2),Cream);_ui.Box(new(160,y+135,2,7),Cream);_ui.Box(new(158+width,y+135,2,7),Cream);for(int i=0;i<5;i++){_ui.Box(new(mid-5+i,y+142+i,1,1),Cream);_ui.Box(new(mid+5-i,y+142+i,1,1),Cream);}FactoryDrop(0,line.RestEvery,line);FactoryDrop(1,line.RestTurns,line);
            _ui.Button(FactoryNoRepeatButton(line),(line.DoNotRepeat?"✓ ":"□ ")+"繰り返さない",line.DoNotRepeat?Accent:Muted,.34f);
            string status=!line.Manufacturing?"製造しない":line.CycleFinished?"製造終了 / 繰り返さない":line.Paused?"一時停止":line.RestRemaining>0?$"休憩中 あと{IndustryTime.Format(line.RestRemaining)} / 次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン":line.Waiting?$"出荷待ち {IndustryTime.Format(line.WaitingProductionTurns)}生産ターン / 格納できるまで停止":$"次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン / {line.ProducedSinceRest+1}台目・工期{IndustryTime.Format(line.ProductionAge)}/{IndustryTime.Format(period)}";
            if(line.Manufacturing)status=$"周期 {IndustryTime.Format(line.CycleProgress)} / {IndustryTime.Format(line.CycleTurns)} / "+status;
            if(!string.IsNullOrEmpty(line.IntakeFailure))status=line.IntakeFailure+" / "+status;
            if(mapleWait)status="原材料不足 / 楓の木を待っています";
            if(line.FunctionOriginNode is int origin)status="参照元："+_setup.Nodes.DisplayName(_setup.Nodes.All[origin])+" / "+status;
            if(planned)status="追加予定 / 内政確定後に稼働";
            _ui.Text(status,new(160,y-19),Math.Min(.3f,690/_font.MeasureString(status).X),Cream);
            int intakes=_setup.Robots.ProductionIntakes.Count(o=>ReferenceEquals(o.Line,line));
            if(intakes>0)_ui.Text($"入荷予約 {intakes}件",new(460,y+112),.3f,Accent);
            if((planned?pending[l-liveCount].Id:factory.WorkerAt(l)) is long workerId){var worker=planned?pending[l-liveCount]:_setup.Robots.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==workerId);if(worker!=null){DrawRobotGlyph(new(754,y+8,60,84),line.WorkerSnapshot??worker);_ui.Text("工場作業員",new(674,y+8),.3f,Accent);_ui.Text("ID "+workerId,new(674,y+36),.28f,Cream);if(!planned && line.WorkerSnapshot!=null)_ui.Text("任命時",new(674,y+62),.25f,Cream);}}
            else _ui.Text("常設レーン",new(674,y+8),.3f,Cream*.7f);
        }
        _factoryLine=savedLine;
        _ui.Text("格納庫は必ず",new(180,888),.43f,Cream);FactoryDrop(2,factory.ReservedSlots);_ui.Text("個空ける",new(552,888),.43f,Cream);
        DrawNodeTabs();DrawFactoryPages();
        if(pending.Length>displayed-factory.ProductionLines.Length)
            _ui.Text("工場作業員の交代予定あり",new(160,857),.38f,Accent);
        _ui.Text($"産業ターン累計 {IndustryTime.Format(_setup.Robots.IndustryTurn)}   /   蛍光灯：現在位置　緑：生産　黄：休暇　赤：空き枠・原材料待ち",new(160,248),.25f,Cream);
        if(!FactoryAlbumOpen && _factoryPlanLine>=0 && _factoryPlanLine<displayed){
            var line=lines[_factoryPlanLine];_ui.Box(new(84,360,790,590),new(22,40,54));
            string catalogue=_setup.EquipmentMode==RobotEquipmentMode.Materials?"素材別の製造候補":"正規・安い・一体型"+(line.PlanCount>11?"・高級":"");
            _ui.Text($"生産ライン {_factoryPlanLine+1} / {catalogue}",new(160,374),.45f,Cream);
            ProductionPlan hovered=null;
            for(int i=0;i<line.PlanCount;i++){
                var r=FactoryPlanButton(i);bool selected=line.SelectedPlan==i;
                _ui.Box(r,selected?Accent:Muted);
                if(i==3)_ui.Center("製造しない",r,.42f,selected?SelectionInk:Cream);
                else{
                    var plan=line.PlanAt(i);DrawRobotGlyph(new(r.X+54,r.Y+8,72,80),plan.Preview(node.Owner),whiteOutline:true);
                    _ui.Center(FactoryProductLabel(plan),new(r.X,r.Y+96,r.Width,22),.28f,selected?SelectionInk:Cream);
                    DrawIndustryDuration(plan.Turns,new(r.X+4,r.Bottom-27,r.Width-8,26),selected?SelectionInk:Cream,
                        prefix:(_setup.EquipmentMode==RobotEquipmentMode.Materials?RobotMaterials.Label(plan.Material):plan.QualityLabel)+" ",suffix:" 産業ターン");
                    if(r.Contains(_pointer))hovered=plan;
                }
            }
            _ui.Center(line.WorkerSnapshot!=null?"作業員レーン：任命時のパーツで候補を固定":"常設レーン：地点の製造候補",new(96,886,764,24),.32f,Cream);
            if(hovered!=null){
                var preview=hovered.Preview(node.Owner);
                string detail=string.Join(" / ",RobotMaterials.Parts.Where(part=>(preview.Parts&part)!=0).Select(part=>$"{PartsLabel(part)} #{preview.Origin(part)+1} {(_setup.EquipmentMode==RobotEquipmentMode.Materials?RobotMaterials.Label(preview.Material(part)):preview.IsPremium(part)?"高級":preview.IsCheap(part)?"安い":"正規")}"));
                if(_setup.EquipmentMode==RobotEquipmentMode.Legacy && preview.PremiumParts!=0)detail+=" / 高級１つで盾・短剣・小銃 各＋１";
                _ui.Center(detail,new(96,916,764,26),Math.Min(.34f,750/_font.MeasureString(detail).X),Cream);
            }
        }
        if(_factoryDropDown==2 || _factoryDropDown>=0 && _factoryLine>=0 && _factoryLine<displayed){
            int field=_factoryDropDown,count=field==2?PlanningWorkshop(_populationNode).ReservedSlotLimit+1:9;
            for(int i=0;i<count;i++){
                var option=FactoryDropOption(field,i,field==2?null:lines[_factoryLine]);
                _ui.Button(option,(field==0?"× ":field==1?"休 ":"")+(field==2?i:i+1),Accent,.40f);
            }
        }
        if(hoveredCaret!=null && _factoryDropDown<0 && _factoryPlanLine<0){
            string hint=hoveredCaret.CycleFinished?"製造と休暇が終了しました。繰り返さないのチェックを外すと再開します":hoveredCaret.Paused?"一時停止中だよ。再開すると製造が進むよ":hoveredCaret.Waiting?$"{IndustryTime.Format(hoveredCaret.WaitingProductionTurns)}生産ターンも待っていますよ！ 格納できれば出荷するよ":$"あと{IndustryTime.Format(hoveredCaret.TurnsUntilNextProduction)}生産ターンでロボットを製造できるよ";
            if(hoveredCaret.WaitingForMaple(_setup.Robots,node.Id))hint="原材料不足 / 楓の木を待っています";
            if(hoveredCaret.RestRemaining>0)hint+=$"（休憩あと{IndustryTime.Format(hoveredCaret.RestRemaining)}ターンを含む）";
            const float scale=.38f;
            int hintWidth=(int)Math.Ceiling(_font.MeasureString(hint).X*scale)+24;
            var hintBox=new Rectangle(Math.Clamp(_pointer.X+18,70,888-hintWidth),Math.Clamp(_pointer.Y-48,260,922),hintWidth,38);
            _ui.Box(hintBox,Cream);_ui.Box(new(hintBox.X+2,hintBox.Y+2,hintBox.Width-4,hintBox.Height-4),new Color(16,32,43));
            _ui.Text(hint,new(hintBox.X+12,hintBox.Y+9),scale,Cream);
        }
    }
    private void DrawFactoryHarvestLane(RobotWorkshop line,int node,int row,int y)
    {
        var product=FactoryRobotButton(row,IndustryTime.UnitsPerTurn);
        _ui.Box(product,Accent);
        DrawMaplePackage(product,new MaplePackage(0,line.MapleRemaining*16));
        int deposit=_setup.Robots.MapleDeposit(node);
        bool waiting=!_setup.Robots.CanHarvestMaple(node,line.MapleRemaining);
        string status=line.Paused?"一時停止":deposit==0?"採取できる埋蔵量がありません":waiting?"格納枠の空きを待っています":"採取中";
        _ui.Text($"採取 / {status} / 予約残り {line.MapleRemaining} 荷",new(160,y-19),.33f,Cream);
        _ui.Text($"楓の木 / 残り埋蔵量 {deposit} 荷",new(400,y+46),.45f,Cream);
        _ui.Text($"次の入荷まで {IndustryTime.Format(Math.Max(0,line.EffectiveHarvestPeriod(_setup.Nodes.All[_populationNode].Purpose)-line.MapleProgress))} 産業ターン",new(400,y+90),.33f,Cream);
        DrawFactoryCaret(160+line.MapleProgress*34,y,line,waiting);
    }
    private void DrawFactoryCaret(int x,int y,RobotWorkshop line,bool mapleWait=false)
    {
        var ink=new Color(16,32,43);
        var signal=line.CycleFinished?new Color(65,78,83):line.Paused?new Color(167,190,194):line.RestRemaining>0?new Color(255,204,112):line.Waiting || mapleWait?new Color(255,113,105):new Color(102,244,175);
        float pulse=line.CycleFinished?0:.78f+.22f*(.5f+.5f*MathF.Sin(_animationTime*2.4f));
        // A fluorescent tube and its narrow socket share the exact tick center.
        // Keep the glow vertical so it cannot be mistaken for a range on the floor.
        _ui.Box(new(x-10,y+29,20,64),Color.Lerp(ink,signal,.12f*pulse));
        _ui.Box(new(x-8,y+29,16,64),ink);
        _ui.Box(new(x-5,y+32,10,58),Color.Lerp(signal*.65f,signal,pulse));
        _ui.Box(new(x-2,y+33,4,56),line.CycleFinished?new Color(45,57,63):Cream);
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
        bool enabled=CanOpenNodeFactory;
        _ui.Box(NodeFactoryTile,active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted);
        _ui.Center("ロボット製造工場",NodeFactoryTile,.38f,enabled?(active || NodeFactoryTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }
}

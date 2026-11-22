namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
public partial class Game1
{
    private Texture2D _robotFactoryIcon;
    private int _factoryHolidayLane=-1;
    private bool _nodeFactoryOpen;
    private int _factoryDropDown=-1,_factoryLine,_factoryPlanLine=-1;
    private string _factoryMessage="";
    private static readonly Rectangle NodeFactoryTile=new(282,210,190,40);
    private bool NodeFactoriesAllowed=>_setup.TutorialStep!=1;
    private bool CanOpenNodeFactory=>NodeFactoriesAllowed && _populationNode>=0 &&
        _populationNode<_setup.Robots.Workshops.Length && _populationNode<_setup.Nodes.All.Length &&
        CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) && HasFactoryScreenLines();
    private RobotWorkshop[] FactoryScreenLines(RobotWorkshop factory,out System.Collections.Generic.Dictionary<int,long> planned)
    {
        planned=new();
        if(!DomesticOperationsAllowed)return factory.ProductionLines;
        var node=_setup.Nodes.All[_populationNode];
        var reservations=_setup.Robots.PartPlanning;
        var roleChanges=reservations.Orders.Where(o=>o.Role!=HangarReservation.None).Select(o=>o.RobotId).ToHashSet();
        var forecast=PlanningStock.Nodes[node.Id].Robots.Where(r=>r.FactoryWorker).ToDictionary(r=>r.Id);
        var workers=forecast.Values.Where(r=>reservations.WorkerReservation(r.Id)!=null)
            .ToDictionary(r=>r.Id);
        var lines=new System.Collections.Generic.List<RobotWorkshop>();
        void AddWorker(Robot worker)
        {
            var line=factory.CreateWorkerLine(worker,_setup.WorldSeed,node.Id,node.Owner);
            reservations.WorkerReservation(worker.Id)?.WorkerLane?.Apply(line);
            lines.Add(line);
        }
        for(int i=0;i<factory.ProductionLines.Length;i++){
            var line=factory.ProductionLines[i];
            if(factory.WorkerAt(i) is long id)
            {
                if(roleChanges.Contains(id) && !forecast.ContainsKey(id))continue;
                if(workers.Remove(id,out var worker))
                {
                    planned.Add(lines.Count,id);
                    AddWorker(worker);
                    continue;
                }
            }
            lines.Add(line);
        }
        foreach(var worker in workers.Values.OrderBy(r=>r.Id).Take(Math.Max(0,factory.LineLimit-lines.Count))){
            planned.Add(lines.Count,worker.Id);
            AddWorker(worker);
        }
        return lines.ToArray();
    }
    private bool CanManageNodeFactory(int owner)=>_setup.ControllerOf(owner)==_setup.TurnPlayer ||
        (_computerDomesticNodes!=null && owner==_setup.PlayerSlot);
    private bool HasFactoryScreenLines()
    {
        var factory=PlanningWorkshop(_populationNode);
        return FactoryScreenLines(factory,out _).Length>0 || _setup.Robots.ProductionIntakes.Any(i=>i.Source==_populationNode);
    }
    private void OpenNodeFactory(){if(!CanOpenNodeFactory)return;_factoryPage=0;_factoryStartLane=-1;_workpieceLane=-1;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;_nodeOfficeOpen=false;_lastNodeTab=1;_factoryOpenedAt=_operations.ElapsedMilliseconds;BeginFactoryViewport();}
    private Rectangle FactoryProductionButton(int line)
    {
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length)return Rectangle.Empty;
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,out _);
        if(line<_factoryPage || line>=lines.Length || line>=_factoryPage+FactoryVisibleLanes)return Rectangle.Empty;
        var lane=lines[line];
        int row=line-_factoryPage;
        return lane.HarvestOnly || lane.MapleRemaining>0?FactoryRobotButton(row,3):
            FactoryConveyorProduct(row,lane.Manufacturing?lane.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3,lane);
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
        int y=FactoryRowY(_factoryLine-_factoryPage);
        return field==0?new(190,y+219,76,28):new(290,y+219,76,28);
    }
    private Rectangle FactoryDropOption(int field,int index,RobotWorkshop line=null){var r=FactoryDropButton(field,line);int top=Math.Max(260,r.Y-9*30);return new(r.X,top+index*30,r.Width,30);}
    private Rectangle FactoryNoRepeatButton(RobotWorkshop line)=>new(698,FactoryRowY(_factoryLine-_factoryPage)+188,145,26);
    private bool NodeFactoryClick(Point pointer)
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length || _populationNode>=_setup.Nodes.All.Length){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,out var planned);
        if(FactoryWorkpieceClick(pointer,lines))return true;
        if(_factoryStartLane>=0){
            if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) || _setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryStartLane=-1;return true;}
            if(FactoryStartClick(pointer,factory,lines))return true;
        }
        if(!FactoryOverviewActive)_factoryPage=Math.Min(_factoryPage,Math.Max(0,lines.Length-FactoryVisibleLanes));
        void SavePending(int index)
        {
            if(planned.TryGetValue(index,out long worker))_setup.Robots.PartPlanning.SetWorkerLane(worker,lines[index]);
        }
        if (FactoryAlbumOpen)
        {
            int line = _factoryPlanLine;
            _factoryPlanLine = -1;
            if (line >= lines.Length || !CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner) ||
                _setup.Campaign.RobotBattles.LockedNode(_populationNode)) return true;
            if(FactoryStopButton.Contains(pointer)){
                _setup.Robots.PartPlanning.CancelWorkpieceResume(lines[line],_setup.Nodes.All[_populationNode].Owner);lines[line].SelectPlan(3);_setup.Robots.CollectInterruptedWorkpieces(_setup);SavePending(line);return true;
            }
            if(_factoryHarvestPage){
                foreach(var material in RobotWorld.RawMaterials.Where(_setup.Robots.RawRequired))
                {
                    if(!HarvestAlbumCard((int)material).Contains(pointer))continue;
                    int available=FactoryHarvestAvailable(_populationNode,lines,lines[line],material);
                    if(available==0){
                        _factoryPlanLine=line;return true;
                    }
                    if(lines[line].MapleRemaining==0 || lines[line].HarvestMaterial!=material){
                        lines[line].SelectMapleHarvest(1,material);SavePending(line);
                    }
                    return true;
                }
                if(FactoryAlbumBounds.Contains(pointer))_factoryPlanLine=line;
                return true;
            }
            for (int i = 0; i < lines[line].PlanCount; i++)
            {
                if (i==3 || !FactoryRecipeCard(i).Contains(pointer)) continue;
                _setup.Robots.PartPlanning.CancelWorkpieceResume(lines[line],_setup.Nodes.All[_populationNode].Owner);lines[line].SelectPlan(i);_setup.Robots.CollectInterruptedWorkpieces(_setup);
                SavePending(line);
                return true;
            }
            if(FactoryAlbumBounds.Contains(pointer))_factoryPlanLine=line;
            return true;
        }
        if(FactoryPageClick(pointer))return true;
        if(FactoryOverviewActive && FactoryViewport.Contains(pointer)){
            StopFactoryIntro();
            for(int i=0;i<lines.Length;i++)if(FactoryOverviewBounds(i,lines.Length).Contains(pointer)){
                _factoryZoom=0;
                _factoryPage=Math.Min(i,Math.Max(0,lines.Length-FactoryVisibleLanes));
                break;
            }
            return true;
        }
        if(_factoryLine<0 || _factoryLine>=lines.Length){_factoryLine=0;_factoryDropDown=-1;}
        if(_factoryPlanLine>=lines.Length)_factoryPlanLine=-1;
        if(!CanManageNodeFactory(_setup.Nodes.All[_populationNode].Owner))return true;
        if(_setup.Campaign.RobotBattles.LockedNode(_populationNode)){_factoryMessage="継戦中は製造計画を変更できません";return true;}
        if(FactoryPendingIntakesButton.Contains(pointer) && _setup.Robots.ProductionIntakes.Any(i=>i.Source==_populationNode))
        {_pendingIntakesOpen=true;_intakeNode=_populationNode;_intakePage=0;return true;}
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<9;i++)if(FactoryDropOption(field,i,lines[_factoryLine]).Contains(pointer)){
            {var line=lines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);SavePending(_factoryLine);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<lines[line].PlanCount;i++)if(FactoryPlanButton(i).Contains(pointer)){_setup.Robots.PartPlanning.CancelWorkpieceResume(lines[line],_setup.Nodes.All[_populationNode].Owner);lines[line].SelectPlan(i);_setup.Robots.CollectInterruptedWorkpieces(_setup);SavePending(line);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=_factoryPage;l<Math.Min(lines.Length,_factoryPage+FactoryVisibleLanes);l++){if(lines[l].DismantleOnly)continue;
            if(!planned.ContainsKey(l) && !lines[l].HarvestOnly && lines[l].MapleRemaining==0 && FactoryIntakeButton(l-_factoryPage).Contains(pointer)){_intakeLine=lines[l];_intakeNode=_populationNode;_intakePage=0;return true;}
            var line=lines[l];int period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;
            bool harvest=line.HarvestOnly || line.MapleRemaining>0;
            var product=harvest?FactoryRobotButton(l-_factoryPage,3):FactoryConveyorProduct(l-_factoryPage,period,line);
            if(product.Contains(pointer) || !harvest && FactoryConveyorArrow(product).Contains(pointer)){_factoryHarvestPage=harvest;_factoryPlanLine=l;_factoryLine=l;return true;}
            if(harvest){
                if(line.MapleRemaining>0)
                    for(int loads=1;loads<=3;loads++){
                        if(!HarvestLoadButton(l-_factoryPage,loads).Contains(pointer))continue;
                        if(loads!=line.MapleRemaining && loads<=FactoryHarvestAvailable(_populationNode,lines,line,line.HarvestMaterial)){
                            line.SelectMapleHarvest(loads,line.HarvestMaterial);SavePending(l);
                        }
                        return true;
                    }
                continue;
            }
            var track=FactoryHolidayTrack(l-_factoryPage);track.Inflate(6,0);
            if(track.Contains(pointer)){_factoryHolidayLane=l;return true;}
            _factoryLine=l;for(int field=0;field<2;field++)if(FactoryDropButton(field,line).Contains(pointer)){_factoryDropDown=field;return true;}
            if(WorkpieceOpen(l-_factoryPage).Contains(pointer)){if(_setup.Robots.PartPlanning.CancelWorkpieceResume(line,_setup.Nodes.All[_populationNode].Owner)){_factoryMessage="仕掛品の再開予約を取り消しました";return true;}_workpieceLane=l;_workpiecePage=0;return true;}
            if(FactoryStartButton(l-_factoryPage).Contains(pointer)){_factoryStartLane=l;return true;}
            if(FactoryNoRepeatButton(line).Contains(pointer)){line.SetDoNotRepeat(!line.DoNotRepeat);SavePending(l);return true;}
        }
        if(NodeFactoryTile.Contains(pointer))_nodeFactoryOpen=false;
        return new Rectangle(54,150,850,825).Contains(pointer);
    }
    private void FactoryDrop(int field,int value,RobotWorkshop line=null)
    {var r=FactoryDropButton(field,line);_ui.Button(r,(field==0?"× ":field==1?"休 ":"")+value,Accent,.48f);for(int row=0;row<8;row++)_ui.Box(new(r.Right-23+row,r.Center.Y-3+row,16-row*2,1),SelectionInk);}
    private void DrawNodeFactory()
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;return;}
        var node=_setup.Nodes.All[_populationNode];var factory=PlanningWorkshop(node.Id);
        var lines=FactoryScreenLines(factory,out var plannedWorkers);
        int displayed=lines.Length;
        if(!FactoryOverviewActive)_factoryPage=Math.Clamp(_factoryPage,0,Math.Max(0,displayed-FactoryVisibleLanes));
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));


        int savedLine=_factoryLine;

        if(FactoryOverviewActive)DrawFactoryOverview(lines);
        else {
        for(int l=Math.Max(_factoryPage,displayed);l<_factoryPage+FactoryVisibleLanes;l++){
            var area=FactoryLaneBounds(l-_factoryPage);
            if(l>=factory.LineLimit){
                _ui.Box(area,new Color(24,78,110));
                for(int row=0;row<7;row++)for(int x=area.X+12;x<area.Right-30;x+=64){int wave=(int)(MathF.Sin(x*.05f+row)*5);_ui.Box(new(x,area.Y+20+row*27+wave,34,3),new Color(76,151,178));}
                _ui.Center("海上 / レーン上限１",area,.5f,Cream);
            }else{_ui.Box(area,new Color(24,43,54));_ui.Center("空きレーン / 製造作業員予約で追加",area,.45f,Cream*.7f);}
        }
        for(int l=_factoryPage;l<Math.Min(displayed,_factoryPage+FactoryVisibleLanes);l++){
            var line=lines[l];int y=FactoryRowY(l-_factoryPage),period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;_factoryLine=l;
            bool planned=plannedWorkers.TryGetValue(l,out long plannedWorkerId);
            long? workerId=planned?plannedWorkerId:line.WorkerSnapshot?.Id??factory.WorkerAt(Array.IndexOf(factory.ProductionLines,line));
            bool mapleWait=line.WaitingForMaple(_setup.Robots,node.Id);
            DrawFactoryLaneFrame(l-_factoryPage,l+1,l);if(line.DismantleOnly){DrawDismantlingLane(line,node.Id,l-_factoryPage,workerId);continue;}
            if(!planned && !line.HarvestOnly && line.MapleRemaining==0)_ui.Button(FactoryIntakeButton(l-_factoryPage),"入荷先",Muted,.34f);
            if(line.HarvestOnly || line.MapleRemaining>0){
                var harvester=line.WorkerSnapshot;
                if(harvester==null && workerId is long harvesterId)
                    harvester=_setup.Robots.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==harvesterId);
                DrawFactoryHarvestLane(line,node.Id,l-_factoryPage,y,harvester,planned,
                    FactoryHarvestAvailable(node.Id,lines,line,line.HarvestMaterial));
                continue;
            }
            DrawFactoryConveyor(line,node.Owner,l-_factoryPage,period);
            var product=FactoryConveyorProduct(l-_factoryPage,period,line);
            var arrow=FactoryConveyorArrow(product);
            _ui.Button(arrow,"",arrow.Contains(_pointer)?Accent:Muted,.34f);
            for(int row=0;row<8;row++)_ui.Box(new(arrow.X+6+row,arrow.Y+10+row,16-row*2,1),Cream);
            if(!line.Manufacturing)_ui.Center("×",product,.7f,Cream);
            FactoryDrop(0,line.RestEvery,line);DrawFactoryHoliday(line,l-_factoryPage);
            _ui.Button(FactoryNoRepeatButton(line),(!line.DoNotRepeat?"✓ ":"□ ")+"繰り返す",!line.DoNotRepeat?Accent:Muted,.34f);
            _ui.Button(FactoryStartButton(l-_factoryPage),factory.StartConditionLabel(line),line.StartWaiting?Accent:Muted,.30f);
            DrawFactoryRawProgress(line,y,node.Purpose);
            bool resumePlanned=_setup.Robots.PartPlanning.WorkpieceResumes.Any(o=>ReferenceEquals(o.Line,line));
            _ui.Button(WorkpieceOpen(l-_factoryPage),resumePlanned?"取消":"再開",resumePlanned?Accent:Muted,.30f);
            if(WorkpieceOpen(l-_factoryPage).Contains(_pointer))DrawRulebookHint("仕掛品から製造再開","格納庫の仕掛品を、このレーンで続きから製造します");
            string status=!line.Manufacturing?"製造しない":line.CycleFinished?"製造終了 / 繰り返さない":line.Paused?"一時停止":line.RestRemaining>0?$"休憩中 あと{IndustryTime.Format(line.RestRemaining)} / 次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン":line.Waiting?$"出荷待ち {IndustryTime.Format(line.WaitingProductionTurns)}生産ターン / 格納できるまで停止":$"次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン / {line.ProducedSinceRest+1}台目・工期{IndustryTime.Format(line.ProductionAge)}/{IndustryTime.Format(period)}";
            if(line.Manufacturing)status=$"周期 {IndustryTime.Format(line.CycleProgress)} / {IndustryTime.Format(line.CycleTurns)} / "+status;
            if(!string.IsNullOrEmpty(line.IntakeFailure))status=line.IntakeFailure+" / "+status;
            if(mapleWait)status=$"原材料不足 / {RobotWorld.RawName(line.MissingRawMaterial(_setup.Robots,node.Id).Value)}を待っています";
            if(line.FunctionOriginNode is int origin)status="参照元："+_setup.Nodes.DisplayName(_setup.Nodes.All[origin])+" / "+status;
            if(planned)status="追加予定 / 内政確定後に稼働";
            if(resumePlanned)status="仕掛品の再開予約 / ターン終了時に実施";
            if(line.StartWaiting)status=factory.StartConditionLabel(line);
            bool running=line.Manufacturing && !planned && !line.CycleFinished && !line.Paused &&
                line.RestRemaining==0 && !line.Waiting && !line.StartWaiting && !mapleWait && !_setup.Campaign.RobotBattles.LockedNode(node.Id);
            if(_setup.Campaign.RobotBattles.LockedNode(node.Id))status="継戦中 / 一時停止";
            DrawFactoryLaneStatus(y,running,status);
            int intakes=_setup.Robots.ProductionIntakes.Count(o=>ReferenceEquals(o.Line,line));
            if(intakes>0)_ui.Text($"入荷予約 {intakes}件",new(460,y+112),.3f,Accent);
            if(workerId is long manufacturerId){
                var worker=line.WorkerSnapshot??_setup.Robots.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==manufacturerId);
                if(worker!=null){
                    DrawFactoryWorker(y,worker);
                    _ui.Text("製造作業員",new(91,y+58),.27f,Cream);
                    _ui.Text("ID "+manufacturerId,new(91,y+80),.26f,Cream);
                    if(!planned && line.WorkerSnapshot!=null)_ui.Text("任命時",new(91,y+102),.25f,Cream);
                }
            }else _ui.Text("常設レーン",new(91,y+58),.27f,Cream*.7f);
        }
        }
        _factoryLine=savedLine;
        int pendingProducts=_setup.Robots.ProductionIntakes.Count(i=>i.Source==_populationNode);
        if(pendingProducts>0)_ui.Button(FactoryPendingIntakesButton,$"完成品の入荷待ち / {pendingProducts}件",Muted,.38f);
        DrawNodeTabs();DrawFactoryPages();
        _ui.Text($"産業ターン累計 {IndustryTime.Format(_setup.Robots.IndustryTurn)}",new(160,248),.25f,Cream);
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
        if(_factoryMessage.Length>0)_ui.Text(_factoryMessage,new(94,920),.32f,Cream);
        DrawFactoryStartMenu(factory,lines);
        if(_factoryDropDown>=0 && _factoryLine>=0 && _factoryLine<displayed){
            int field=_factoryDropDown,count=9;
            for(int i=0;i<count;i++){
                var option=FactoryDropOption(field,i,lines[_factoryLine]);
                _ui.Button(option,(field==0?"× ":field==1?"休 ":"")+(i+1),Accent,.40f);
            }
        }

    }
    private void DrawFactoryCaret(int x,int y,RobotWorkshop line,bool mapleWait=false)
    {
        var ink=new Color(16,32,43);
        var signal=line.CycleFinished?new Color(65,78,83):line.Paused?new Color(167,190,194):line.RestRemaining>0?new Color(255,204,112):line.Waiting || mapleWait?new Color(255,113,105):new Color(102,244,175);
        float pulse=line.CycleFinished?0:1;
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
    private void DrawNodeFactoryTile(bool active)
    {
        if(!NodeFactoriesAllowed)return;
        bool enabled=CanOpenNodeFactory;
        _ui.Box(NodeFactoryTile,active || enabled && NodeFactoryTile.Contains(_pointer)?Accent:Muted);
        _ui.Center("ロボット製造工場",NodeFactoryTile,.38f,enabled?(active || NodeFactoryTile.Contains(_pointer)?SelectionInk:Cream):Cream*.4f);
        if(active)_ui.Box(new(NodeFactoryTile.X,NodeFactoryTile.Y,NodeFactoryTile.Width,4),Cream);
    }

    private bool FactoryHolidayInput(MouseState mouse)
    {
        if(!_nodeFactoryOpen || mouse.LeftButton==ButtonState.Released){
            _factoryHolidayLane=-1;return false;
        }
        if(_factoryHolidayLane<0)return false;
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,out var planned);

        if(_factoryHolidayLane>=lines.Length || _setup.Campaign.RobotBattles.LockedNode(_populationNode)){
            _factoryHolidayLane=-1;return true;
        }
        var line=lines[_factoryHolidayLane];
        var track=FactoryHolidayTrack(_factoryHolidayLane-_factoryPage);
        int turns=Math.Clamp((int)MathF.Round((_pointer.X-track.X)/30f),1,9);
        if(turns!=line.RestTurns){
            line.SetRest(line.RestEvery,turns);
            if(planned.TryGetValue(_factoryHolidayLane,out long worker))_setup.Robots.PartPlanning.SetWorkerLane(worker,line);
        }
        return true;
    }
}

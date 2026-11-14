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
    private RobotWorkshop[] FactoryScreenLines(RobotWorkshop factory,out System.Collections.Generic.Dictionary<int,long> planned)
    {
        planned=new();
        if(!DomesticOperationsAllowed)return factory.ProductionLines;
        var node=_setup.Nodes.All[_populationNode];
        var reservations=_setup.Robots.PartPlanning;
        var workers=PlanningStock.Nodes[node.Id].Robots.Where(r=>r.FactoryWorker && reservations.WorkerReservation(r.Id)!=null)
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
            if(factory.WorkerAt(i) is long id && workers.Remove(id,out var worker)){
                planned.Add(lines.Count,id);
                AddWorker(worker);
            }else lines.Add(line);
        }
        foreach(var worker in workers.Values.OrderBy(r=>r.Id).Take(Math.Max(0,factory.LineLimit-lines.Count))){
            planned.Add(lines.Count,worker.Id);
            AddWorker(worker);
        }
        return lines.ToArray();
    }
    private bool CanManageNodeFactory(int owner)=>_setup.ControllerOf(owner)==_setup.TurnPlayer;
    private bool HasFactoryScreenLines()
    {
        var factory=PlanningWorkshop(_populationNode);
        return FactoryScreenLines(factory,out _).Length>0;
    }
    private void OpenNodeFactory(){if(!CanOpenNodeFactory)return;_factoryPage=0;_factoryDropDown=_factoryPlanLine=-1;_factoryLine=0;_factoryMessage="";_nodeAssemblyOpen=_nodeDisposalOpen=_nodeTransportOpen=false;_nodeFactoryOpen=true;_nodeOfficeOpen=false;_lastNodeTab=1;BeginFactoryViewport();}
    private static int FactoryProductionWidth(int period)=>Math.Min(15,Math.Max(1,period))*24;
    private static int FactoryRowY(int row)=>284+row*290;
    private static Rectangle FactoryRobotButton(int line,int period)=>new(301+FactoryConveyorTravel(period),FactoryRowY(line)+67,72,88);
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
        if(field==2)return new(420,884,110,30);
        int y=FactoryRowY(_factoryLine-_factoryPage);
        return field==0?new(190,y+219,76,28):new(290,y+219,76,28);
    }
    private Rectangle FactoryDropOption(int field,int index,RobotWorkshop line=null){var r=FactoryDropButton(field,line);int count=field==2?PlanningWorkshop(_populationNode).ReservedSlotLimit+1:9;if(field==2 && count>13){int columns=(count+12)/13,width=784/columns;return new(84+index/13*width,490+index%13*30,width-3,30);}int top=Math.Max(260,r.Y-count*30);return new(r.X,top+index*30,r.Width,30);}
    private Rectangle FactoryNoRepeatButton(RobotWorkshop line)=>new(698,FactoryRowY(_factoryLine-_factoryPage)+188,145,26);
    private bool NodeFactoryClick(Point pointer)
    {
        if(!NodeFactoriesAllowed){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        if(_populationNode<0 || _populationNode>=_setup.Robots.Workshops.Length || _populationNode>=_setup.Nodes.All.Length){_nodeFactoryOpen=false;_factoryDropDown=_factoryPlanLine=-1;return false;}
        var factory=PlanningWorkshop(_populationNode);
        var lines=FactoryScreenLines(factory,out var planned);
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
                lines[line].SelectPlan(3);SavePending(line);return true;
            }
            if(!lines[line].HarvestOnly && FactoryHarvestTab.Contains(pointer)){
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
        if(_factoryDropDown>=0){int field=_factoryDropDown;_factoryDropDown=-1;for(int i=0;i<(field==2?factory.ReservedSlotLimit+1:9);i++)if(FactoryDropOption(field,i,field==2?null:lines[_factoryLine]).Contains(pointer)){
            if(field==2)SetPlanningReservedSlots(_populationNode,i);
            else {var line=lines[_factoryLine];if(field==0)line.SetRest(i+1,line.RestTurns);else line.SetRest(line.RestEvery,i+1);SavePending(_factoryLine);}return true;
        }return true;}
        if(_factoryPlanLine>=0){int line=_factoryPlanLine;for(int i=0;i<lines[line].PlanCount;i++)if(FactoryPlanButton(i).Contains(pointer)){lines[line].SelectPlan(i);SavePending(line);_factoryPlanLine=-1;return true;}_factoryPlanLine=-1;return true;}
        for(int l=_factoryPage;l<Math.Min(lines.Length,_factoryPage+FactoryVisibleLanes);l++){
            if(!planned.ContainsKey(l) && !lines[l].HarvestOnly && lines[l].MapleRemaining==0 && FactoryIntakeButton(l-_factoryPage).Contains(pointer)){_intakeLine=lines[l];_intakeNode=_populationNode;_intakePage=0;return true;}
            var line=lines[l];int period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;
            bool harvest=line.HarvestOnly || line.MapleRemaining>0;
            var product=harvest?FactoryRobotButton(l-_factoryPage,3):FactoryConveyorProduct(l-_factoryPage,period,line);
            if(product.Contains(pointer) || !harvest && FactoryConveyorArrow(product).Contains(pointer)){_factoryHarvestPage=harvest;_factoryPlanLine=l;_factoryLine=l;return true;}
            if(line.HarvestOnly || line.MapleRemaining>0)continue;
            var track=FactoryHolidayTrack(l-_factoryPage);track.Inflate(6,0);
            if(track.Contains(pointer)){_factoryHolidayLane=l;return true;}
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
        var lines=FactoryScreenLines(factory,out var plannedWorkers);
        int displayed=lines.Length;
        if(!FactoryOverviewActive)_factoryPage=Math.Clamp(_factoryPage,0,Math.Max(0,displayed-FactoryVisibleLanes));
        _ui.Box(new(54,150,850,825),new(27,49,62));_ui.Box(new(54,150,850,6),_setup.OwnerColor(node.Owner));


        int savedLine=_factoryLine;

        if(FactoryOverviewActive)DrawFactoryOverview(lines);
        else {
        for(int l=Math.Max(_factoryPage,displayed);l<_factoryPage+FactoryVisibleLanes;l++){
            int y=FactoryRowY(l-_factoryPage)-24;var area=new Rectangle(78,y,782,286);
            if(l>=factory.LineLimit){
                _ui.Box(area,new Color(24,78,110));
                for(int row=0;row<7;row++)for(int x=area.X+12;x<area.Right-30;x+=64){int wave=(int)(MathF.Sin(x*.05f+row)*5);_ui.Box(new(x,area.Y+20+row*27+wave,34,3),new Color(76,151,178));}
                _ui.Center("海上 / レーン上限１",area,.5f,Cream);
            }else{_ui.Box(area,new Color(24,43,54));_ui.Center("空きレーン / 製造作業員予約で追加",area,.45f,Cream*.7f);}
        }
        for(int l=_factoryPage;l<Math.Min(displayed,_factoryPage+FactoryVisibleLanes);l++){
            var line=lines[l];int y=FactoryRowY(l-_factoryPage),period=line.Manufacturing?line.EffectiveProductionPeriod(_setup.Nodes.All[_populationNode].Purpose):3;_factoryLine=l;
            bool planned=plannedWorkers.TryGetValue(l,out long plannedWorkerId);
            bool mapleWait=line.WaitingForMaple(_setup.Robots,node.Id);
            _ui.Box(new(78,y-24,782,286),l%2==0?new Color(37,63,77):new Color(46,67,79));
            _ui.Center((l+1).ToString(),new(84,y-12,66,40),.85f,new Color(167,190,194));
            if(!planned && !line.HarvestOnly && line.MapleRemaining==0)_ui.Button(FactoryIntakeButton(l-_factoryPage),"入荷先",Muted,.34f);
            if(line.MapleRemaining>0){
                DrawFactoryHarvestLane(line,node.Id,l-_factoryPage,y);
                continue;
            }
            if(line.HarvestOnly){_ui.Text(planned?"採取レーン / 予約中":"採取レーン",new(250,y),.48f,Cream);_ui.Button(FactoryRobotButton(l-_factoryPage,3),"採取",Muted,.34f);continue;}
            DrawFactoryConveyor(line,node.Owner,l-_factoryPage,period);
            var product=FactoryConveyorProduct(l-_factoryPage,period,line);
            var arrow=FactoryConveyorArrow(product);
            _ui.Button(arrow,"",arrow.Contains(_pointer)?Accent:Muted,.34f);
            for(int row=0;row<8;row++)_ui.Box(new(arrow.X+6+row,arrow.Y+10+row,16-row*2,1),Cream);
            if(!line.Manufacturing)_ui.Center("×",product,.7f,Cream);
            FactoryDrop(0,line.RestEvery,line);DrawFactoryHoliday(line,l-_factoryPage);
            _ui.Button(FactoryNoRepeatButton(line),(line.DoNotRepeat?"✓ ":"□ ")+"繰り返さない",line.DoNotRepeat?Accent:Muted,.34f);
            string status=!line.Manufacturing?"製造しない":line.CycleFinished?"製造終了 / 繰り返さない":line.Paused?"一時停止":line.RestRemaining>0?$"休憩中 あと{IndustryTime.Format(line.RestRemaining)} / 次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン":line.Waiting?$"出荷待ち {IndustryTime.Format(line.WaitingProductionTurns)}生産ターン / 格納できるまで停止":$"次の完成まで{IndustryTime.Format(line.TurnsUntilNextProduction)}産業ターン / {line.ProducedSinceRest+1}台目・工期{IndustryTime.Format(line.ProductionAge)}/{IndustryTime.Format(period)}";
            if(line.Manufacturing)status=$"周期 {IndustryTime.Format(line.CycleProgress)} / {IndustryTime.Format(line.CycleTurns)} / "+status;
            if(!string.IsNullOrEmpty(line.IntakeFailure))status=line.IntakeFailure+" / "+status;
            if(mapleWait)status="原材料不足 / 楓の木を待っています";
            if(line.FunctionOriginNode is int origin)status="参照元："+_setup.Nodes.DisplayName(_setup.Nodes.All[origin])+" / "+status;
            if(planned)status="追加予定 / 内政確定後に稼働";
            bool running=line.Manufacturing && !planned && !line.CycleFinished && !line.Paused &&
                line.RestRemaining==0 && !line.Waiting && !mapleWait && !_setup.Campaign.RobotBattles.LockedNode(node.Id);
            if(_setup.Campaign.RobotBattles.LockedNode(node.Id))status="継戦中 / 一時停止";
            var lamp=running?new Color(91,240,158):new Color(255,113,105);
            _ui.Box(new(91,y+199,18,18),new Color(13,27,35));
            _ui.Box(new(94,y+202,12,12),lamp);
            _ui.Box(new(96,y+203,4,3),Color.Lerp(lamp,Color.White,.55f));
            _ui.Text(status,new(116,y+200),Math.Min(.32f,570/_font.MeasureString(status).X),Cream);
            int intakes=_setup.Robots.ProductionIntakes.Count(o=>ReferenceEquals(o.Line,line));
            if(intakes>0)_ui.Text($"入荷予約 {intakes}件",new(460,y+112),.3f,Accent);
            if((planned?plannedWorkerId:factory.WorkerAt(l)) is long workerId){
                var worker=line.WorkerSnapshot??_setup.Robots.Nodes[node.Id].Robots.FirstOrDefault(r=>r.Id==workerId);
                if(worker!=null){
                    DrawFactoryWorker(y,worker);
                    _ui.Text("製造作業員",new(91,y+58),.27f,Cream);
                    _ui.Text("ID "+workerId,new(91,y+80),.26f,Cream);
                    if(!planned && line.WorkerSnapshot!=null)_ui.Text("任命時",new(91,y+102),.25f,Cream);
                }
            }else _ui.Text("常設レーン",new(91,y+58),.27f,Cream*.7f);
        }
        }
        _factoryLine=savedLine;
        _ui.Text("格納庫は必ず",new(180,888),.43f,Cream);FactoryDrop(2,factory.ReservedSlots);_ui.Text("個空ける",new(552,888),.43f,Cream);
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
        if(_factoryDropDown==2 || _factoryDropDown>=0 && _factoryLine>=0 && _factoryLine<displayed){
            int field=_factoryDropDown,count=field==2?PlanningWorkshop(_populationNode).ReservedSlotLimit+1:9;
            for(int i=0;i<count;i++){
                var option=FactoryDropOption(field,i,field==2?null:lines[_factoryLine]);
                _ui.Button(option,(field==0?"× ":field==1?"休 ":"")+(field==2?i:i+1),Accent,.40f);
            }
        }

    }
    private void DrawFactoryHarvestLane(RobotWorkshop line,int node,int row,int y)
    {
        var product=FactoryRobotButton(row,3);
        _ui.Box(product,Accent);
        DrawMaplePackage(product,new MaplePackage(0,line.MapleRemaining*16));
        int deposit=_setup.Robots.MapleDeposit(node);
        bool waiting=!_setup.Robots.CanHarvestMaple(node,line.MapleRemaining);
        string status=line.Paused?"一時停止":deposit==0?"採取できる埋蔵量がありません":waiting?"格納枠の空きを待っています":"採取中";
        _ui.Text($"採取レーン / {status} / 予約残り {line.MapleRemaining} 荷",new(160,y-19),.33f,Cream);
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

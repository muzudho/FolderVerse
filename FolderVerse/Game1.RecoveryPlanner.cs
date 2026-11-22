namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private bool _recoveryOpen,_recoveryMap;
    private bool _recoveryLocation;
    private int _recoveryQuestion;
    private int _recoveryDestination=-1;
    private bool _recoveryGlobe;
    private WorldPreview _recoveryWorld;
    private const float RecoveryYaw=0,RecoveryPitch=0;
    private static readonly Rectangle RecoveryGlobeToggle=new(1290,84,310,48),RecoveryFit=new(960,935,220,48),RecoveryChangeLocation=new(690,935,250,48);
    private readonly List<long> _recoveryWorkers=new();
    private static readonly Rectangle RecoveryAdd=new(980,1000,420,60);
    private static readonly Rectangle RecoveryPanel=new(670,350,1230,570),RecoveryPocket=new(1460,930,200,54);
    private int _recoveryStage,_recoveryPage,_recoveryScope=-1;
    private long _recoveryWorker=-1,_recoveryDetail=-1;
    private int _recoverySource=-1;private bool _recoveryPocket;
    private RecoveryFilter _recoveryFilter=new();
    private readonly HashSet<RecoveryTarget> _recoveryTargets=new();
    private readonly CubeNet _recoveryNet=new();
    private string _recoveryMessage="";
    private static readonly Rectangle RecoveryBack=new(1660,24,220,54),RecoveryNext=new(1590,1000,290,60),RecoveryCancel=new(690,1000,270,60),RecoveryStatus=new(1290,24,310,54),RecoveryMap=new(1620,84,260,48);
    private static Rectangle RecoveryCard(int i)=>new(690+i%4*295,355+i/4*185,280,175);
    private static Rectangle RecoveryCheck(int i,int y)=>new(690+i%3*390,y+i/3*65,370,54);
    private static string RecoveryShapeName(RecoveryShape shape)=>shape switch{RecoveryShape.Head=>"頭パーツ単品",RecoveryShape.Body=>"胴パーツ単品",RecoveryShape.Legs=>"脚パーツ単品",RecoveryShape.HeadBody=>"頭／胴一体型",RecoveryShape.BodyLegs=>"胴／脚一体型",RecoveryShape.Complete=>"完全体",RecoveryShape.Manufacturing=>"製造の仕掛品",RecoveryShape.Dismantling=>"解体の仕掛品",_=>"スクラップ"};
    private (int Node,bool Pocket,Robot Robot)[] RecoveryWorkerChoices=>_setup.Salvage.RecoveryWorkers(_setup,_setup.PlayerSlot).OrderByDescending(p=>p.Pocket).ThenBy(p=>p.Node).ThenBy(p=>p.Robot.Id).ToArray();
    private RecoveryChoice[] RecoveryCatalog=>_setup.Salvage.RecoveryChoices(_setup,_recoverySource,_recoveryFilter,_recoveryDestination>=0?_recoveryDestination:null).Where(c=>_recoveryScope<0||c.Node==_recoveryScope).ToArray();
    private void OpenRecoveryPlanner(int scope=-1)
    {
        ResetInspector();_setup.Salvage.EnableExpeditions(_setup);_recoveryOpen=true;_recoveryStage=4;_recoveryPage=0;_recoveryScope=scope;_recoveryDestination=_recoverySource=-1;_recoveryWorker=-1;_recoveryWorkers.Clear();_recoveryTargets.Clear();_recoveryFilter=new();_recoveryMap=false;_recoveryMessage="";
    }
    private bool RecoveryButton(Rectangle area,string label,bool selected=false,bool enabled=true,float scale=.4f)
    {
        _ui.StateButton(area,label,Accent,Muted,selected,enabled&&area.Contains(_pointer),scale:scale,seconds:_animationTime);
        if(!enabled)_ui.Box(area,new Color(10,20,26)*.5f);return enabled&&area.Contains(_pointer);
    }
    private void RecoveryPlannerInput(bool click,bool escape)
    {
        if(click&&RecoveryBack.Contains(_pointer)){CancelRecoveryDraft();_recoveryOpen=false;return;}
        if(escape){if(RecoveryDraftActive)CancelRecoveryDraft();else _recoveryOpen=false;return;}
        if(!click)return;
        _recoveryMessage="";
        if(RecoveryDraftActive&&RecoveryCancel.Contains(_pointer)){CancelRecoveryDraft();return;}
        if(RecoveryStatus.Contains(_pointer)){CancelRecoveryDraft();return;}
        if(RecoveryMapVisible){
            if(RecoveryGlobeToggle.Contains(_pointer)&&!_setup.IsPrimaplana){_recoveryGlobe=!_recoveryGlobe;FitRecoveryMap();return;}
            if(RecoveryFit.Contains(_pointer)){FitRecoveryMap();return;}
        }
        if(_recoveryStage is 0 or 6 && _recoveryLocation && RecoveryChangeLocation.Contains(_pointer)){_recoveryLocation=false;_recoveryPage=0;FitRecoveryMap();return;}
        if(RecoveryHasPages){
            if(new Rectangle(1290,84,150,48).Contains(_pointer)){_recoveryPage=Math.Max(0,_recoveryPage-1);return;}
            if(new Rectangle(1450,84,150,48).Contains(_pointer)){_recoveryPage++;return;}
        }
        if(_recoveryStage==5){if(RecoveryNext.Contains(_pointer)){_recoveryStage=4;_recoveryPage=0;}return;}
        if(_recoveryStage==6){
            if(!_recoveryLocation){int hit=HitRecoveryMap(RecoveryRecipientNodes);if(hit>=0)SelectRecoveryDestination(hit);return;}
            if(RecoveryNext.Contains(_pointer)){_recoveryStage=1;_recoveryQuestion=0;_recoveryPage=0;_recoveryLocation=false;}
        }else if(_recoveryStage==0){
            if(!_recoveryLocation){
                if(RecoveryPocket.Contains(_pointer)&&RecoveryWorkerChoices.Any(p=>p.Pocket)){SelectRecoveryLocation(_setup.Nodes.Current(_setup.PlayerSlot).Id,true);return;}
                int hit=HitRecoveryMap(RecoveryWorkerChoices.Where(p=>!p.Pocket).Select(p=>p.Node));if(hit>=0)SelectRecoveryLocation(hit,false);
                return;
            }
            var workers=RecoveryHangar;
            for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=workers.Length)break;var r=workers[index];if(!RecoveryCard(i).Contains(_pointer)||!RecoveryWorkerChoices.Any(p=>p.Robot.Id==r.Id))continue;if(!_recoveryWorkers.Remove(r.Id)&&_recoveryWorkers.Count<RecoveryWorkerLimit)_recoveryWorkers.Add(r.Id);_recoveryWorker=_recoveryWorkers.FirstOrDefault(-1);_recoveryTargets.Clear();return;}
            if(RecoveryAdd.Contains(_pointer)){_recoveryMessage="追加するロボットをクリックしてください";return;}
            if(RecoveryNext.Contains(_pointer)){if(_recoveryWorker<0){_recoveryMessage="回収へ向かう完全体を選んでください";return;}_recoveryStage=1;_recoveryQuestion=3;_recoveryPage=0;}
        }else if(_recoveryStage==1){
            bool first=new Rectangle(780,239,430,54).Contains(_pointer),second=new Rectangle(1230,239,430,54).Contains(_pointer);
            if(_recoveryQuestion==0&&(first||second)){_recoveryFilter.Anything=second;_recoveryQuestion=second?4:1;return;}
            if(_recoveryQuestion is 1 or 2){
                for(int i=0;i<9;i++)if(RecoveryCheck(i,410).Contains(_pointer)){if(_recoveryQuestion==1){var v=(RecoveryShape)i;if(!_recoveryFilter.Shapes.Remove(v))_recoveryFilter.Shapes.Add(v);}else{var v=(RobotMaterial)i;if(!_recoveryFilter.Materials.Remove(v))_recoveryFilter.Materials.Add(v);}return;}
                if(RecoveryNext.Contains(_pointer)){if((_recoveryQuestion==1?_recoveryFilter.Shapes.Count:_recoveryFilter.Materials.Count)==0){_recoveryMessage="１つ以上選んでください";return;}_recoveryQuestion=_recoveryQuestion==2?4:2;}return;
            }
            if(_recoveryQuestion==3&&(first||second)){_recoveryFilter.IncludeUnreachable=second;_recoveryStage=2;_recoveryTargets.Clear();_recoveryPage=0;_recoveryMap=true;FitRecoveryMap();return;}
            if(_recoveryQuestion==4&&(first||second)){_recoveryFilter.AllowDamaged=second;_recoveryTargets.Clear();_recoveryStage=0;_recoveryLocation=false;_recoveryPage=0;FitRecoveryMap();}
        }else if(_recoveryStage==2){
            if(RecoveryMap.Contains(_pointer)){_recoveryMap=!_recoveryMap;if(_recoveryMap){_recoveryScope=-1;FitRecoveryMap();}return;}
            if(new Rectangle(690,930,220,48).Contains(_pointer)){_recoveryScope=-1;_recoveryPage=0;return;}
            if(_recoveryMap){int hit=HitRecoveryMap(RecoveryCatalog.Select(c=>c.Node));if(hit>=0){_recoveryScope=hit;_recoveryMap=false;_recoveryPage=0;return;}}
            var items=RecoveryCatalog;
            if(!_recoveryMap)for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=items.Length)break;if(!RecoveryCard(i).Contains(_pointer))continue;var target=items[index].Target;if(!_recoveryTargets.Remove(target)){
                foreach(var existing in _recoveryTargets.Where(t=>t.Item==target.Item&&(t.Parts&target.Parts)!=0).ToArray())_recoveryTargets.Remove(existing);
                _recoveryTargets.Add(target);
            }return;}
            if(RecoveryNext.Contains(_pointer)){if(_recoveryTargets.Count==0){_recoveryMessage="品物を１つ以上選んでください";return;}_recoveryStage=3;_recoveryMap=false;}
        }else if(_recoveryStage==3&&RecoveryNext.Contains(_pointer)){
            try{ReserveRecoveryTeam();_recoveryTargets.Clear();_recoveryStage=4;_recoveryPage=0;_recoveryMessage="出発を予約しました。陣営のターン終了で出発します";}
            catch(InvalidOperationException e){_recoveryMessage=e.Message;}
        }else if(_recoveryStage==4){
            if(RecoveryNext.Contains(_pointer)){if(RecoveryWorkerLimit==0){_recoveryMessage="回収班は最大６班です。帰還するまでお待ちください";return;}_recoveryFilter=new();_recoveryTargets.Clear();_recoveryDestination=_recoverySource=-1;_recoveryStage=6;_recoveryWorker=-1;_recoveryWorkers.Clear();_recoveryLocation=false;_recoveryPage=0;FitRecoveryMap();return;}
            var missions=_setup.Salvage.Missions.Where(m=>m.Owner==_setup.PlayerSlot).OrderBy(m=>m.Finished).ThenByDescending(m=>m.Id).Skip(_recoveryPage*6).Take(6).ToArray();
            for(int i=0;i<missions.Length;i++)if(new Rectangle(710+i%2*590,519+i/2*165,530,30).Contains(_pointer)){
                var m=missions[i];if(m.Pending)_setup.Salvage.CancelExpedition(_setup,m.Id,m.Owner);else if(!m.Returning&&!m.Finished)_setup.Salvage.ReserveRecoveryReturn(m.Id,m.Owner);return;
            }
            for(int i=0;i<missions.Length;i++)if(new Rectangle(690+i%2*590,385+i/2*165,570,130).Contains(_pointer)){_recoveryDetail=missions[i].Id;_recoveryStage=5;_recoveryPage=0;return;}
        }
    }
    private bool RecoveryHasPages=>_recoveryStage switch{
        0 or 6=>_recoveryLocation&&RecoveryHangar.Length>12,
        2=>!_recoveryMap&&RecoveryCatalog.Length>12,
        3=>_recoveryTargets.Count>8,
        4=>_setup.Salvage.Missions.Count(m=>m.Owner==_setup.PlayerSlot)>6,
        5=>_setup.Salvage.Missions.Any(m=>m.Id==_recoveryDetail&&m.Cargo.Count+m.Targets.Count+m.History.Count>15),
        _=>false
    };
    private void RecoveryMapCamera(MouseState mouse){
        if(!RecoveryMapVisible||!RecoveryPanel.Contains(_pointer))return;
        int wheel=mouse.ScrollWheelValue-_previousMouse.ScrollWheelValue;if(wheel!=0){if(_recoveryGlobe)_recoveryWorld.GlobeZoom=MathHelper.Clamp(_recoveryWorld.GlobeZoom*MathF.Pow(1.2f,wheel/120f),.5f,16);else _recoveryNet.ZoomAt(RecoveryPanel,_pointer,wheel);}
        if(mouse.RightButton==ButtonState.Pressed&&_previousMouse.RightButton==ButtonState.Pressed){var before=CanvasPoint(_previousMouse);var delta=_pointer.ToVector2()-before.ToVector2();if(_recoveryGlobe){var orientation=_recoveryWorld.ViewOrientation(RecoveryYaw,RecoveryPitch);_recoveryWorld.ManualOrientation=Quaternion.CreateFromRotationMatrix(orientation*Matrix.CreateRotationY(delta.X*.006f)*Matrix.CreateRotationX(delta.Y*.006f));}else _recoveryNet.Drag(delta);}
    }
    private void DrawRecoveryPocketButton(){
        bool enabled=RecoveryWorkerChoices.Any(p=>p.Pocket);var r=RecoveryPocket;int radius=16;
        var color=enabled&&r.Contains(_pointer)?Accent:Muted;
        for(int y=0;y<r.Height;y++){int dy=y<radius?radius-y:y>=r.Height-radius?y-(r.Height-radius):0;int inset=dy==0?0:radius-(int)MathF.Sqrt(radius*radius-dy*dy);_ui.Box(new(r.X+inset,r.Y+y,r.Width-2*inset,1),enabled?color:color*.45f);}
        _ui.Center("ポケット",r,.42f,Cream);
    }
    private long RecoveryTargetWorker(RecoveryTarget target){var ordered=_recoveryTargets.OrderBy(t=>t.Item).ThenBy(t=>t.Parts).ToArray();return _recoveryWorkers[Array.IndexOf(ordered,target)%_recoveryWorkers.Count];}
    private int RecoveryWorkerLimit=>Math.Max(0,6-_setup.Salvage.Missions.Count(m=>m.Owner==_setup.PlayerSlot&&!m.Finished));
    private void ReserveRecoveryTeam(){
        if(_recoveryTargets.Count<_recoveryWorkers.Count)throw new InvalidOperationException("各ロボットが取りに行く品を１つ以上選んでください");
        var made=new List<RecoveryMission>();var targets=_recoveryTargets.OrderBy(t=>t.Item).ThenBy(t=>t.Parts).ToArray();
        try{for(int i=0;i<_recoveryWorkers.Count;i++)made.Add(_setup.Salvage.ReserveExpedition(_setup,_setup.PlayerSlot,_recoverySource,_recoveryPocket,_recoveryWorkers[i],targets.Where((t,index)=>index%_recoveryWorkers.Count==i),_recoveryDestination));}
        catch{foreach(var m in made)_setup.Salvage.CancelExpedition(_setup,m.Id,m.Owner);throw;}
    }
    private Robot[] RecoveryHangar=>(_recoveryStage==6?_setup.Robots.Nodes[_recoveryDestination]:_recoveryPocket?_setup.Robots.Retinues[_setup.PlayerSlot]:_setup.Robots.Nodes[_recoverySource]).Robots.ToArray();
    private void FitRecoveryMap(){_recoveryGlobe&=!_setup.IsPrimaplana;int n=_recoverySource>=0?_recoverySource:_recoveryDestination>=0?_recoveryDestination:_setup.Nodes.Current(_setup.PlayerSlot)?.Id??0;_recoveryNet.ShowRobots=false;_recoveryNet.ShowBatteries=false;_recoveryNet.ShowSmoke=false;_recoveryNet.ShowMapSymbols=false;_recoveryNet.SetCenter(_setup,_setup.Cells[_setup.Nodes.All[n].Cell].Face);_recoveryNet.FitAll(RecoveryPanel,.82f);
        if(_recoveryGlobe){_recoveryWorld??=new WorldPreview(GraphicsDevice);_recoveryWorld.ShowSetup(_setup,true);_recoveryWorld.ManualOrientation=ConquestFace(_setup.Cells[_setup.Nodes.All[n].Cell].Normal);_recoveryWorld.GlobeZoom=.9f;_recoveryWorld.GlobeAnchor=null;_recoveryWorld.GlobePan=Vector2.Zero;}
    }
    private void SelectRecoveryLocation(int node,bool pocket){_recoverySource=node;_recoveryPocket=pocket;_recoveryLocation=true;_recoveryPage=0;_recoveryWorker=-1;_recoveryWorkers.Clear();}
    private void DrawRecoveryLocationMap(bool source){
        var choices=RecoveryWorkerChoices;var catalog=RecoveryCatalog;
        var availableNodes=(_recoveryStage==6?RecoveryRecipientNodes:source?choices.Where(p=>!p.Pocket).Select(p=>p.Node):catalog.Select(c=>c.Node)).ToHashSet();
        _recoveryNet.RestrictNodeActions=true;_recoveryNet.ActionTargetNodes.Clear();
        _recoveryNet.ActionTargetNodes.UnionWith(availableNodes);
        _recoveryNet.HoveredNode=HitRecoveryMap(availableNodes);
        _recoveryNet.SelectedNodes.Clear();
        int selected=_recoveryStage==6?_recoveryDestination:source?_recoverySource:-1;
        if(availableNodes.Contains(selected))_recoveryNet.SelectedNodes.Add(selected);
        _recoveryNet.PrepareTerrainHighlights(_setup);
        _recoveryNet.DawnMeridian=WorldDayCycle.DawnPhase(_setup,_setup.PlayerSlot);
        var canvas=CanvasBounds();var transform=Matrix.CreateScale(canvas.Width/1920f,canvas.Height/1080f,1)*Matrix.CreateTranslation(canvas.X,canvas.Y,0);
        var old=GraphicsDevice.ScissorRectangle;_spriteBatch.End();
        using(var clip=new RasterizerState{ScissorTestEnable=true}){
            GraphicsDevice.ScissorRectangle=new(canvas.X+(int)(RecoveryPanel.X*canvas.Width/1920f),canvas.Y+(int)(RecoveryPanel.Y*canvas.Height/1080f),(int)(RecoveryPanel.Width*canvas.Width/1920f),(int)(RecoveryPanel.Height*canvas.Height/1080f));
            DrawRecoveryMapSurface(canvas,transform,clip);
        }
        GraphicsDevice.ScissorRectangle=old;_spriteBatch.Begin(transformMatrix:transform);
        _ui.Text(_recoveryGlobe?"ホイールで拡大・縮小 / 右ドラッグで回転":"ホイールで拡大・縮小 / 右ドラッグで移動",new(690,920),.28f,Cream);
        int hovered=source?HitRecoveryMap(_setup.Nodes.All.Select(n=>n.Id)):-1;
        foreach(var n in _setup.Nodes.All){if(!RecoveryMapPosition(n.Id,out var at)||!RecoveryPanel.Contains(at.ToPoint()))continue;
            bool available=availableNodes.Contains(n.Id);
            if(available)_ui.Box(new((int)at.X-5,(int)at.Y-5,10,10),new Color(255,214,93));
            _ui.Text(n.Id.ToString(),at+new Vector2(18,-9),.32f,_recoveryNet.NodeTint(n.Id,Cream));
            if(!available&&source&&_recoveryStage!=6&&n.Owner==_setup.PlayerSlot&&hovered==n.Id)_ui.Text("回収に向かえるロボットがいません",new(1020,920),.32f,Cream);
        }
    }
    private void DrawRecoveryPlanner()
    {
        int owner=_setup.PlayerSlot;var node=_setup.Nodes.Current(owner);int kind=node==null?3:NodeSupervisors.For(node,_setup.Cells[node.Cell].Face);
        if(kind!=_guideSceneryKind){_guideScenery?.Dispose();_guideScenery=LoadGuideImage("background-"+NodeSupervisors.Kinds[kind]+"-v001.png");_guideSceneryKind=kind;}
        PortraitHudLayout.DrawBackground(_spriteBatch,_guideScenery);_ui.Box(PortraitHudLayout.MainPanel,new Color(16,35,47));
        EnsureGuidePortrait(_setup.EnsureGuides().Bodies[owner],owner);_spriteBatch.Draw(_guidePortrait,new Rectangle(-40,285,430,710),Color.White);
        PortraitHudLayout.DrawSplitter(_ui);
        DrawPhasePlaqueLabel("廃品回収","回収計画",7);
        DrawSpeechBalloon(new(500,149,1380,180));
        string[][] speech={new[]{"どの地点、またはポケットの","自国の完全体を向かわせますか？","素材・汚損で性能は変わりません。"},new[]{"何を探しに行きましょう？","一体型は単品の代わりにはなりません。","各欄の複数選択はOR、欄同士はANDです。"},new[]{"持ち帰りたい品を選んでください。","近い品から、辺数の少ない順に回ります。","到達不能品も先に試し、無理なら次へ。"},new[]{"出発前の最終確認です。","本人１枠＋荷物の数だけ、","ポケットを貸し出します。"},new[]{"回収班の現在の状況です。","出発後は自動で行動します。","帰還指示後は回収を再開しません。"}};
        if(_recoveryStage==0&&_recoveryLocation)speech[0]=new[]{"この格納庫から回収へ向かうロボットを選んでください。","複数選択できます。回収班は同時に最大６班です。","素材・汚損で性能は変わりません。"};
        if(_recoveryStage==3)speech[3]=new[]{"出発前の最終確認です。","回収員も荷物と一緒に受け取り先へ到着します。","ポケットは、回収員の人数＋荷物の数だけ貸し出します。"};
        if(_recoveryStage==6)speech[4]=new[]{"どこが廃品を欲しがっていますか？","受け取り先の地点を選び、格納庫を確認してください。","誰が取りに行くかは、このあとで決めます。"};
        if(_recoveryStage==0&&!_recoveryLocation)speech[0]=new[]{"どこのロボットが回収に行きますか？","出発地点、またはポケットを選んでください。","受け取り先とは別の地点でもかまいません。"};
        if(_recoveryStage!=1)DrawSpeechParagraph(speech[Math.Min(4,_recoveryStage)],new Rectangle(780,180,840,108),false);
        RecoveryButton(RecoveryBack,"地図へ戻る");RecoveryButton(RecoveryStatus,"回収班の状況",_recoveryStage==4);
        if(RecoveryDraftActive)RecoveryButton(RecoveryCancel,"やっぱ止める");
        if(RecoveryMapVisible){RecoveryButton(RecoveryGlobeToggle,_recoveryGlobe?"展開図で見る":"地星儀で見る",enabled:!_setup.IsPrimaplana);RecoveryButton(RecoveryFit,"全体を表示");}
        if(_recoveryStage is 0 or 6 && _recoveryLocation)RecoveryButton(RecoveryChangeLocation,"別の地点を選ぶ");
        if(RecoveryHasPages){RecoveryButton(new(1290,84,150,48),"前のページ");RecoveryButton(new(1450,84,150,48),"次のページ");}

        string next=_recoveryStage==3?"回収に行かせる":_recoveryStage==4?"新しい回収班":_recoveryStage==5?"状況一覧へ":"次へ";
        if((_recoveryStage is not (0 or 6)||_recoveryLocation)&&(_recoveryStage!=1||_recoveryQuestion is 1 or 2))_ui.StateButton(RecoveryNext,_recoveryStage==6?"この地点で受け取る":_recoveryStage==0?"決定":next,Accent,Muted,false,RecoveryNext.Contains(_pointer),attention:true,seconds:_animationTime,scale:.43f);
        int loans=_setup.Salvage.LoanCount(owner),free=_setup.Robots.Retinues[owner].StorageLimit-_setup.Robots.Retinues[owner].Count;
        _ui.Box(new(20,825,600,108),new Color(12,30,39)*.9f);
        _ui.Text($"ポケット貸出中 {loans} 枠 / 新たに貸せる空き {free} 枠",new(35,837),.35f,Cream);
        _ui.Text("貸出中の枠は、帰還まで使えません。",new(35,868),.34f,Cream);
        _ui.Text("帰還不能になると、ポケットが狭いままになります。",new(35,899),.3f,Cream);
        _ui.Text(_recoveryMessage,new(690,1060),Math.Min(.3f,1180/Math.Max(1,_font.MeasureString(_recoveryMessage).X)),new Color(255,210,120));
        if(_recoveryStage==6){
            if(!_recoveryLocation)DrawRecoveryLocationMap(true);
            else DrawRecoveryReceivingHangar();
        }else if(_recoveryStage==0){
            if(!_recoveryLocation){DrawRecoveryLocationMap(true);DrawRecoveryPocketButton();}
            else{RecoveryButton(RecoveryAdd,$"{_recoveryWorkers.Count+1}体目を選ぶ / 選択 {_recoveryWorkers.Count} 体",enabled:_recoveryWorkers.Count<RecoveryWorkerLimit);var workers=RecoveryHangar;_recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(workers.Length-1)/12));
                for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=workers.Length)break;var r=workers[index];var card=RecoveryCard(i);bool eligible=RecoveryWorkerChoices.Any(p=>p.Robot.Id==r.Id);RecoveryButton(card,"",_recoveryWorkers.Contains(r.Id));DrawHangarRobot(new(card.X+100,card.Y+5,80,110),r);_ui.Center("ID "+r.Id,new(card.X,card.Y+120,card.Width,24),.35f,Cream);if(!eligible){_ui.Box(card,new Color(0,0,0)*.7f);_ui.Center("回収に向かえません",new(card.X,card.Bottom-28,card.Width,24),.3f,Cream);}}
            }
        }else if(_recoveryStage==1){
            string[] questions={"欲しいものは決まっていますか？","どんな形のものを探しますか？（複数選択）","何製のものを探しますか？（複数選択）","回収して受け取り先へ届けられる品だけを探しますか？","汚損した品も回収しますか？"};

            DrawSpeechParagraph(new[]{questions[_recoveryQuestion]},new Rectangle(780,174,880,45));
            if(_recoveryQuestion is 1 or 2){_ui.Text("同じ欄の複数選択はOR、形と素材はANDです。一体型は単品の代わりにはなりません。",new(690,660),.32f,Cream);for(int i=0;i<9;i++)RecoveryButton(RecoveryCheck(i,410),((_recoveryQuestion==1?_recoveryFilter.Shapes.Contains((RecoveryShape)i):_recoveryFilter.Materials.Contains((RobotMaterial)i))?"[x] ":"[ ] ")+(_recoveryQuestion==1?RecoveryShapeName((RecoveryShape)i):RobotMaterials.Label((RobotMaterial)i)),_recoveryQuestion==1?_recoveryFilter.Shapes.Contains((RecoveryShape)i):_recoveryFilter.Materials.Contains((RobotMaterial)i));}
            else{string first=_recoveryQuestion==0?"欲しいものが決まっている":_recoveryQuestion==3?"回収・搬入できる品だけ":"全品状態良";string second=_recoveryQuestion==0?"なんでもいい":_recoveryQuestion==3?"到達できない品も含める":"汚損可（良品も含む）";RecoveryButton(new(780,239,430,54),first);RecoveryButton(new(1230,239,430,54),second);}
        }else if(_recoveryStage is 2 or 3){
            var catalog=RecoveryCatalog;var chosen=_setup.Salvage.RecoveryChoices(_setup,_recoverySource,_recoveryFilter).Where(c=>_recoveryTargets.Contains(c.Target)).ToArray();
            if(_recoveryStage==2){RecoveryButton(RecoveryMap,_recoveryMap?"カタログから探す":"展開図で探す",_recoveryMap);RecoveryButton(new(690,930,220,48),"世界中の品",_recoveryScope<0);}
            _ui.Text($"選択 {_recoveryTargets.Count} 個 / 新たな貸出予定 {_recoveryWorkers.Count+_recoveryTargets.Count} 枠",new(980,930),.42f,Cream);
            if(_recoveryMap)DrawRecoveryLocationMap(false);
            else{var display=_recoveryStage==3?chosen:catalog;int pageSize=_recoveryStage==3?8:12;int pages=Math.Max(1,(display.Length+pageSize-1)/pageSize);_recoveryPage=Math.Min(_recoveryPage,pages-1);for(int i=0;i<pageSize;i++){int index=_recoveryPage*pageSize+i;if(index>=display.Length)break;var item=display[index];var card=RecoveryCard(i);RecoveryButton(card,"",_recoveryTargets.Contains(item.Target));DrawHangarRobot(new(card.X+100,card.Y+4,80,85),item.Robot);_ui.Center("#"+item.Target.Item+" / "+RecoveryShapeName(item.Shape),new(card.X+3,card.Y+92,card.Width-6,25),.31f,Cream);_ui.Center(_recoveryStage==3?"回収担当 ID "+RecoveryTargetWorker(item.Target):item.Reachable?"回収・搬入可能":"現在は到達不能",new(card.X,card.Y+120,card.Width,22),.3f,item.Reachable?Cream:new Color(255,190,115));_ui.Center(_setup.Nodes.Description(_setup.Nodes.All[item.Node]),new(card.X+4,card.Bottom-25,card.Width-8,20),.24f,Cream);}
                if(display.Length==0)_ui.Text("条件に合う品がありません。回収班の状況から新しく選び直せます。",new(690,400),.45f,Cream);}
            if(_recoveryStage==3){for(int i=0;i<_recoveryWorkers.Count;i++){var robot=RecoveryWorkerChoices.FirstOrDefault(p=>p.Robot.Id==_recoveryWorkers[i]).Robot;if(robot!=null){DrawHangarRobot(new(710+i*185,735,65,90),robot);_ui.Text("ID "+robot.Id,new(700+i*185,830),.3f,Cream);}}_ui.Text("取りに行く：ID "+string.Join("・",_recoveryWorkers)+" / 出発："+(_recoveryPocket?"ポケット":_setup.Nodes.DisplayName(_setup.Nodes.All[_recoverySource])),new(690,865),.38f,Cream);_ui.Text("受け取り先："+_setup.Nodes.DisplayName(_setup.Nodes.All[_recoveryDestination]),new(690,900),.45f,Cream);_ui.Text("本人と荷物を受け取り先へ届けます。満杯・陥落・到達不能ならポケットへ帰ります。",new(690,975),.35f,Cream);}
        }else if(_recoveryStage==5){
            var m=_setup.Salvage.Missions.FirstOrDefault(m=>m.Id==_recoveryDetail&&m.Owner==owner);
            if(m!=null){
                _ui.Text($"回収班 {m.Id} / {m.Status} / 貸出 {m.LoanCount} 枠",new(690,350),.55f,Cream);
                _ui.Text("現在地："+_setup.Nodes.DisplayName(_setup.Nodes.All[m.Node]),new(690,395),.4f,Cream);
                _ui.Text("受け取り先："+(m.DeliveryNode==null&&m.Pocket?"自分のポケット":_setup.Nodes.DisplayName(_setup.Nodes.All[m.DeliveryNode??m.Source])),new(690,475),.3f,Cream);
                _ui.Text("次の目的地："+(m.Destination<0?"未定":_setup.Nodes.DisplayName(_setup.Nodes.All[m.Destination])),new(690,445),.4f,Cream);
                DrawHangarRobot(new(1700,350,120,130),m.Worker);
                var lines=m.Cargo.Select(r=>"積荷："+SalvagePartLabel(r)).Concat(m.Targets.Select(t=>"回収予定：#"+t.Item+" / "+PartsLabel(t.Parts))).Concat(m.History.Select(h=>"記録："+h)).ToArray();
                _recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(lines.Length-1)/15));int row=0;
                foreach(string text in lines.Skip(_recoveryPage*15).Take(15))_ui.Text(text,new(690,520+row++*28),Math.Min(.42f,1170/Math.Max(1,_font.MeasureString(text).X)),Cream);
                if(lines.Length==0)_ui.Text("積荷・残り予定・記録はありません",new(690,520),.45f,Cream);
            }
        }else{
            _ui.Text("班をクリックすると、積荷と諦めた品の記録を確認できます。",new(690,335),.4f,Cream);
            var all=_setup.Salvage.Missions.Where(m=>m.Owner==owner).OrderBy(m=>m.Finished).ThenByDescending(m=>m.Id).ToArray();_recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(all.Length-1)/6));var missions=all.Skip(_recoveryPage*6).Take(6).ToArray();
            for(int i=0;i<missions.Length;i++){var m=missions[i];int x=690+i%2*590,y=385+i/2*165;_ui.Box(new(x,y,570,155),Muted);DrawHangarRobot(new(x+12,y+25,75,100),m.Worker);_ui.Text($"回収班 {m.Id} / ID {m.Worker.Id} / {m.Status}",new(x+100,y+15),.33f,Cream);_ui.Text($"現在地 {m.Node} → 目的地 {m.Destination} / 貸出 {m.LoanCount} 枠",new(x+100,y+48),.31f,Cream);_ui.Text($"積荷 {m.Cargo.Count} 個 / 残り予定 {m.Targets.Count} 個",new(x+100,y+80),.34f,Cream);
                int line=0;foreach(string history in m.History.TakeLast(2))_ui.Text(history,new(x+12,y+105+line++*20),Math.Min(.3f,540/Math.Max(1,_font.MeasureString(history).X)),Cream);
                if(!m.Finished&&!m.Returning)RecoveryButton(new(x+20,y+134,530,30),m.Pending?"出発予約を取り消す":m.ReturnRequested?"帰還予約を取り消す":"すぐ帰れ（帰還を予約）",m.ReturnRequested);
            }
            if(missions.Length==0)_ui.Text("回収班はいません。右下から新しい班を予約できます。",new(690,400),.5f,Cream);
        }
    }
}
namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private bool _recoveryOpen,_recoveryMap;
    private int _recoveryStage,_recoveryPage,_recoveryScope=-1;
    private long _recoveryWorker=-1,_recoveryDetail=-1;
    private int _recoverySource=-1;private bool _recoveryPocket;
    private RecoveryFilter _recoveryFilter=new();
    private readonly HashSet<RecoveryTarget> _recoveryTargets=new();
    private readonly CubeNet _recoveryNet=new();
    private string _recoveryMessage="";
    private static readonly Rectangle RecoveryBack=new(1660,24,220,54),RecoveryNext=new(340,954,270,60),RecoveryPrevious=new(30,954,270,60),RecoveryStatus=new(690,24,270,54),RecoveryMap=new(980,24,270,54);
    private static Rectangle RecoveryCard(int i)=>new(690+i%4*295,175+i/4*230,280,216);
    private static Rectangle RecoveryCheck(int i,int y)=>new(690+i%3*390,y+i/3*65,370,54);
    private static string RecoveryShapeName(RecoveryShape shape)=>shape switch{RecoveryShape.Head=>"頭パーツ単品",RecoveryShape.Body=>"胴パーツ単品",RecoveryShape.Legs=>"脚パーツ単品",RecoveryShape.HeadBody=>"頭／胴一体型",RecoveryShape.BodyLegs=>"胴／脚一体型",RecoveryShape.Complete=>"完全体",RecoveryShape.Manufacturing=>"製造の仕掛品",RecoveryShape.Dismantling=>"解体の仕掛品",_=>"スクラップ"};
    private (int Node,bool Pocket,Robot Robot)[] RecoveryWorkerChoices=>_setup.Salvage.RecoveryWorkers(_setup,_setup.PlayerSlot).OrderByDescending(p=>p.Pocket).ThenBy(p=>p.Node).ThenBy(p=>p.Robot.Id).ToArray();
    private RecoveryChoice[] RecoveryCatalog=>_setup.Salvage.RecoveryChoices(_setup,_recoverySource,_recoveryFilter).Where(c=>_recoveryScope<0||c.Node==_recoveryScope).ToArray();
    private void OpenRecoveryPlanner(int scope=-1)
    {
        ResetInspector();_setup.Salvage.EnableExpeditions(_setup);_recoveryOpen=true;_recoveryStage=0;_recoveryPage=0;_recoveryScope=scope;_recoveryWorker=-1;_recoveryTargets.Clear();_recoveryFilter=new();_recoveryMap=false;_recoveryMessage="";
    }
    private bool RecoveryButton(Rectangle area,string label,bool selected=false,bool enabled=true,float scale=.4f)
    {
        _ui.StateButton(area,label,Accent,Muted,selected,enabled&&area.Contains(_pointer),scale:scale,seconds:_animationTime);
        if(!enabled)_ui.Box(area,new Color(10,20,26)*.5f);return enabled&&area.Contains(_pointer);
    }
    private void RecoveryPlannerInput(bool click,bool escape)
    {
        if(escape||click&&RecoveryBack.Contains(_pointer)){_recoveryOpen=false;return;}
        if(!click)return;
        _recoveryMessage="";
        if(RecoveryStatus.Contains(_pointer)){_recoveryStage=4;_recoveryPage=0;_recoveryMap=false;return;}
        if(RecoveryPrevious.Contains(_pointer)){_recoveryStage=Math.Max(0,_recoveryStage==5?4:_recoveryStage==4?0:_recoveryStage-1);_recoveryPage=0;_recoveryMap=false;return;}
        if(_recoveryStage is 0 or 2 or 4 or 5){
            if(new Rectangle(1290,24,150,54).Contains(_pointer)){_recoveryPage=Math.Max(0,_recoveryPage-1);return;}
            if(new Rectangle(1450,24,150,54).Contains(_pointer)){_recoveryPage++;return;}
        }
        if(_recoveryStage==5){if(RecoveryNext.Contains(_pointer)){_recoveryStage=4;_recoveryPage=0;}return;}
        if(_recoveryStage==0){
            var workers=RecoveryWorkerChoices;
            for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=workers.Length)break;if(!RecoveryCard(i).Contains(_pointer))continue;var p=workers[index];_recoveryWorker=p.Robot.Id;_recoverySource=p.Node;_recoveryPocket=p.Pocket;_recoveryTargets.Clear();return;}
            if(RecoveryNext.Contains(_pointer)){if(_recoveryWorker<0){_recoveryMessage="回収へ向かう完全体を選んでください";return;}_recoveryStage=1;_recoveryPage=0;}
        }else if(_recoveryStage==1){
            if(new Rectangle(340,350,270,54).Contains(_pointer))_recoveryFilter.Anything=false;
            if(new Rectangle(340,414,270,54).Contains(_pointer))_recoveryFilter.Anything=true;
            if(new Rectangle(340,520,270,54).Contains(_pointer))_recoveryFilter.IncludeUnreachable=false;
            if(new Rectangle(340,584,270,54).Contains(_pointer))_recoveryFilter.IncludeUnreachable=true;
            if(new Rectangle(340,690,270,54).Contains(_pointer))_recoveryFilter.AllowDamaged=false;
            if(new Rectangle(340,754,270,54).Contains(_pointer))_recoveryFilter.AllowDamaged=true;
            if(!_recoveryFilter.Anything){
                for(int i=0;i<9;i++){if(RecoveryCheck(i,185).Contains(_pointer)){var v=(RecoveryShape)i;if(!_recoveryFilter.Shapes.Remove(v))_recoveryFilter.Shapes.Add(v);}
                    if(RecoveryCheck(i,540).Contains(_pointer)){var v=(RobotMaterial)i;if(!_recoveryFilter.Materials.Remove(v))_recoveryFilter.Materials.Add(v);}}
                foreach(int row in new[]{0,1}){int y=row==0?415:770;var all=new Rectangle(690,y,220,48);var none=new Rectangle(930,y,220,48);if(all.Contains(_pointer)){if(row==0)_recoveryFilter.Shapes=new(Enum.GetValues<RecoveryShape>());else _recoveryFilter.Materials=new(Enum.GetValues<RobotMaterial>());}if(none.Contains(_pointer)){if(row==0)_recoveryFilter.Shapes.Clear();else _recoveryFilter.Materials.Clear();}}
            }
            if(RecoveryNext.Contains(_pointer)){if(_recoveryFilter.Error.Length>0){_recoveryMessage=_recoveryFilter.Error;return;}_recoveryTargets.Clear();_recoveryStage=2;_recoveryPage=0;}
        }else if(_recoveryStage==2){
            if(RecoveryMap.Contains(_pointer)){_recoveryMap=!_recoveryMap;if(_recoveryMap){_recoveryNet.SetCenter(_setup,_setup.Cells[_setup.Nodes.All[_recoverySource].Cell].Face);_recoveryNet.FitAll(new(660,140,1240,780));}return;}
            if(new Rectangle(690,100,220,48).Contains(_pointer)){_recoveryScope=-1;_recoveryPage=0;return;}
            var items=RecoveryCatalog;
            if(!_recoveryMap)for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=items.Length)break;if(!RecoveryCard(i).Contains(_pointer))continue;var target=items[index].Target;if(!_recoveryTargets.Remove(target)){
                foreach(var existing in _recoveryTargets.Where(t=>t.Item==target.Item&&(t.Parts&target.Parts)!=0).ToArray())_recoveryTargets.Remove(existing);
                _recoveryTargets.Add(target);
            }return;}
            if(RecoveryNext.Contains(_pointer)){if(_recoveryTargets.Count==0){_recoveryMessage="品物を１つ以上選んでください";return;}_recoveryStage=3;_recoveryMap=false;}
        }else if(_recoveryStage==3&&RecoveryNext.Contains(_pointer)){
            try{_setup.Salvage.ReserveExpedition(_setup,_setup.PlayerSlot,_recoverySource,_recoveryPocket,_recoveryWorker,_recoveryTargets);_recoveryTargets.Clear();_recoveryStage=4;_recoveryPage=0;_recoveryMessage="出発を予約しました。陣営のターン終了で出発します";}
            catch(InvalidOperationException e){_recoveryMessage=e.Message;}
        }else if(_recoveryStage==4){
            if(RecoveryNext.Contains(_pointer)){_recoveryStage=0;_recoveryWorker=-1;_recoveryPage=0;return;}
            var missions=_setup.Salvage.Missions.Where(m=>m.Owner==_setup.PlayerSlot).OrderBy(m=>m.Finished).ThenByDescending(m=>m.Id).Skip(_recoveryPage*6).Take(6).ToArray();
            for(int i=0;i<missions.Length;i++)if(new Rectangle(710+i%2*590,354+i/2*245,530,42).Contains(_pointer)){
                var m=missions[i];if(m.Pending)_setup.Salvage.CancelExpedition(_setup,m.Id,m.Owner);else if(!m.Returning&&!m.Finished)_setup.Salvage.ReserveRecoveryReturn(m.Id,m.Owner);return;
            }
            for(int i=0;i<missions.Length;i++)if(new Rectangle(690+i%2*590,170+i/2*245,570,180).Contains(_pointer)){_recoveryDetail=missions[i].Id;_recoveryStage=5;_recoveryPage=0;return;}
        }
    }
    private void DrawRecoveryPlanner()
    {
        int owner=_setup.PlayerSlot;var node=_setup.Nodes.Current(owner);int kind=node==null?3:NodeSupervisors.For(node,_setup.Cells[node.Cell].Face);
        if(kind!=_guideSceneryKind){_guideScenery?.Dispose();_guideScenery=LoadGuideImage("background-"+NodeSupervisors.Kinds[kind]+"-v001.png");_guideSceneryKind=kind;}
        PortraitHudLayout.DrawBackground(_spriteBatch,_guideScenery);_ui.Box(PortraitHudLayout.MainPanel,new Color(16,35,47));
        EnsureGuidePortrait(_setup.EnsureGuides().Bodies[owner],owner);_spriteBatch.Draw(_guidePortrait,new Rectangle(-40,285,430,710),Color.White);
        DrawSpeechBalloon(new(0,25,630,280));
        string[][] speech={new[]{"どの地点、またはポケットの","自国の完全体を向かわせますか？","素材・汚損で性能は変わりません。"},new[]{"何を探しに行きましょう？","一体型は単品の代わりにはなりません。","各欄の複数選択はOR、欄同士はANDです。"},new[]{"持ち帰りたい品を選んでください。","近い品から、辺数の少ない順に回ります。","到達不能品も先に試し、無理なら次へ。"},new[]{"出発前の最終確認です。","本人１枠＋荷物の数だけ、","ポケットを貸し出します。"},new[]{"回収班の現在の状況です。","出発後は自動で行動します。","帰還指示後は回収を再開しません。"}};
        for(int i=0;i<3;i++)NewsLine(speech[Math.Min(4,_recoveryStage)][i],150,95+i*35,440,.35f,new Color(57,52,48));
        PortraitHudLayout.DrawSplitter(_ui);
        RecoveryButton(RecoveryBack,"地図へ戻る");RecoveryButton(RecoveryStatus,"回収班の状況",_recoveryStage==4);
        if(_recoveryStage is 0 or 2 or 4 or 5){RecoveryButton(new(1290,24,150,54),"前のページ");RecoveryButton(new(1450,24,150,54),"次のページ");}
        RecoveryButton(RecoveryPrevious,"戻る");
        string next=_recoveryStage==3?"回収に行かせる":_recoveryStage==4?"新しい回収班":_recoveryStage==5?"状況一覧へ":"次へ";
        _ui.StateButton(RecoveryNext,next,Accent,Muted,false,RecoveryNext.Contains(_pointer),attention:true,seconds:_animationTime,scale:.43f);
        int loans=_setup.Salvage.LoanCount(owner),free=_setup.Robots.Retinues[owner].StorageLimit-_setup.Robots.Retinues[owner].Count;
        _ui.Box(new(20,825,600,108),new Color(12,30,39)*.9f);
        _ui.Text($"ポケット貸出中 {loans} 枠 / 新たに貸せる空き {free} 枠",new(35,837),.35f,Cream);
        _ui.Text("貸出中の枠は、帰還まで使えません。",new(35,868),.34f,Cream);
        _ui.Text("帰還不能になると、ポケットが狭いままになります。",new(35,899),.3f,Cream);
        _ui.Text(_recoveryMessage,new(690,1015),Math.Min(.4f,1180/Math.Max(1,_font.MeasureString(_recoveryMessage).X)),new Color(255,210,120));
        if(_recoveryStage==0){var workers=RecoveryWorkerChoices;int pages=Math.Max(1,(workers.Length+11)/12);_recoveryPage=Math.Min(_recoveryPage,pages-1);_ui.Text($"回収に出せる完全体 {workers.Length} 体 / {_recoveryPage+1}/{pages}",new(690,110),.5f,Cream);
            for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=workers.Length)break;var p=workers[index];var card=RecoveryCard(i);RecoveryButton(card,"",p.Robot.Id==_recoveryWorker);DrawHangarRobot(new(card.X+85,card.Y+24,110,135),p.Robot);_ui.Center("ID "+p.Robot.Id+" / "+(p.Pocket?"ポケット":"地点 "+p.Node),new(card.X,card.Y+170,card.Width,25),.35f,Cream);_ui.Center(p.Pocket?"自分のポケット":_setup.Nodes.DisplayName(_setup.Nodes.All[p.Node]),new(card.X+5,card.Bottom-25,card.Width-10,20),.22f,Cream);}
            if(workers.Length==0)_ui.Text("予約のない自国の完全体がありません",new(690,220),.55f,Cream);
        }else if(_recoveryStage==1){
            RecoveryButton(new(340,350,270,54),"欲しいものが決まっている",!_recoveryFilter.Anything,scale:.32f);RecoveryButton(new(340,414,270,54),"なんでもいい",_recoveryFilter.Anything);
            RecoveryButton(new(340,520,270,54),"到達できる品だけ",!_recoveryFilter.IncludeUnreachable);RecoveryButton(new(340,584,270,54),"到達できない品も含める",_recoveryFilter.IncludeUnreachable,scale:.32f);
            RecoveryButton(new(340,690,270,54),"全品状態良",!_recoveryFilter.AllowDamaged);RecoveryButton(new(340,754,270,54),"汚損可（良品も含む）",_recoveryFilter.AllowDamaged,scale:.34f);
            _ui.Text("もの（複数選択）",new(690,125),.55f,Cream);_ui.Text("何製（複数選択）",new(690,480),.55f,Cream);
            for(int i=0;i<9;i++){RecoveryButton(RecoveryCheck(i,185),RecoveryShapeName((RecoveryShape)i),_recoveryFilter.Shapes.Contains((RecoveryShape)i),!_recoveryFilter.Anything);RecoveryButton(RecoveryCheck(i,540),RobotMaterials.Label((RobotMaterial)i),_recoveryFilter.Materials.Contains((RobotMaterial)i),!_recoveryFilter.Anything);}
            foreach(int y in new[]{415,770}){RecoveryButton(new(690,y,220,48),"全部選ぶ",enabled:!_recoveryFilter.Anything);RecoveryButton(new(930,y,220,48),"全部消す",enabled:!_recoveryFilter.Anything);}
            _ui.Text("混成品は条件に合う分解可能な部位だけ回収します。",new(690,870),.4f,Cream);
        }else if(_recoveryStage is 2 or 3){
            var catalog=RecoveryCatalog;var chosen=catalog.Where(c=>_recoveryTargets.Contains(c.Target)).ToArray();
            if(_recoveryStage==2){RecoveryButton(RecoveryMap,_recoveryMap?"カタログを表示":"地図上で表示",_recoveryMap);RecoveryButton(new(690,100,220,48),"世界中の品",_recoveryScope<0);}
            _ui.Text($"選択 {_recoveryTargets.Count} 個 / 新たな貸出予定 {1+_recoveryTargets.Count} 枠",new(980,110),.42f,Cream);
            if(_recoveryMap){var panel=new Rectangle(660,160,1240,760);_recoveryNet.Draw(_ui,_setup,panel,-1,_animationTime);foreach(var group in catalog.GroupBy(c=>c.Node)){var at=_recoveryNet.AnimatedNodePosition(_setup,_setup.Nodes.All[group.Key],panel);_ui.Box(new((int)at.X-7,(int)at.Y-7,14,14),new Color(255,214,93));_ui.Text(group.Count()+" 個",at+new Vector2(12,-8),.35f,Cream);}}
            else{var display=_recoveryStage==3?chosen:catalog;int pages=Math.Max(1,(display.Length+11)/12);_recoveryPage=Math.Min(_recoveryPage,pages-1);for(int i=0;i<12;i++){int index=_recoveryPage*12+i;if(index>=display.Length)break;var item=display[index];var card=RecoveryCard(i);RecoveryButton(card,"",_recoveryTargets.Contains(item.Target));DrawHangarRobot(new(card.X+85,card.Y+20,110,118),item.Robot);_ui.Center("#"+item.Target.Item+" / "+RecoveryShapeName(item.Shape),new(card.X+3,card.Y+142,card.Width-6,25),.31f,Cream);_ui.Center(item.Reachable?"到達可能":"現在は到達不能",new(card.X,card.Y+170,card.Width,22),.3f,item.Reachable?Cream:new Color(255,190,115));_ui.Center(_setup.Nodes.Description(_setup.Nodes.All[item.Node]),new(card.X+4,card.Bottom-25,card.Width-8,20),.24f,Cream);}
                if(display.Length==0)_ui.Text("条件に合う品がありません。戻って条件を変えられます。",new(690,220),.45f,Cream);}
            if(_recoveryStage==3){_ui.Text("帰還先："+(_recoveryPocket?"自分のポケット":_setup.Nodes.DisplayName(_setup.Nodes.All[_recoverySource])),new(690,920),.45f,Cream);_ui.Text("地点が満杯・到達不能なら、移動中の征服者のポケットへ帰ります。",new(690,965),.35f,Cream);}
        }else if(_recoveryStage==5){
            var m=_setup.Salvage.Missions.FirstOrDefault(m=>m.Id==_recoveryDetail&&m.Owner==owner);
            if(m!=null){
                _ui.Text($"回収班 {m.Id} / {m.Status} / 貸出 {m.LoanCount} 枠",new(690,120),.55f,Cream);
                _ui.Text("現在地："+_setup.Nodes.DisplayName(_setup.Nodes.All[m.Node]),new(690,175),.4f,Cream);
                _ui.Text("次の目的地："+(m.Destination<0?"未定":_setup.Nodes.DisplayName(_setup.Nodes.All[m.Destination])),new(690,220),.4f,Cream);
                DrawHangarRobot(new(1700,110,120,170),m.Worker);
                var lines=m.Cargo.Select(r=>"積荷："+SalvagePartLabel(r)).Concat(m.Targets.Select(t=>"回収予定：#"+t.Item+" / "+PartsLabel(t.Parts))).Concat(m.History.Select(h=>"記録："+h)).ToArray();
                _recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(lines.Length-1)/15));int row=0;
                foreach(string text in lines.Skip(_recoveryPage*15).Take(15))_ui.Text(text,new(690,310+row++*42),Math.Min(.42f,1170/Math.Max(1,_font.MeasureString(text).X)),Cream);
                if(lines.Length==0)_ui.Text("積荷・残り予定・記録はありません",new(690,320),.45f,Cream);
            }
        }else{
            _ui.Text("班をクリックすると、積荷と諦めた品の記録を確認できます。",new(690,115),.4f,Cream);
            var all=_setup.Salvage.Missions.Where(m=>m.Owner==owner).OrderBy(m=>m.Finished).ThenByDescending(m=>m.Id).ToArray();_recoveryPage=Math.Min(_recoveryPage,Math.Max(0,(all.Length-1)/6));var missions=all.Skip(_recoveryPage*6).Take(6).ToArray();
            for(int i=0;i<missions.Length;i++){var m=missions[i];int x=690+i%2*590,y=170+i/2*245;_ui.Box(new(x,y,570,230),Muted);DrawHangarRobot(new(x+12,y+25,75,100),m.Worker);_ui.Text($"回収班 {m.Id} / ID {m.Worker.Id} / {m.Status}",new(x+100,y+15),.33f,Cream);_ui.Text($"現在地 {m.Node} → 目的地 {m.Destination} / 貸出 {m.LoanCount} 枠",new(x+100,y+48),.31f,Cream);_ui.Text($"積荷 {m.Cargo.Count} 個 / 残り予定 {m.Targets.Count} 個",new(x+100,y+80),.34f,Cream);
                int line=0;foreach(string history in m.History.TakeLast(2))_ui.Text(history,new(x+12,y+135+line++*27),Math.Min(.3f,540/Math.Max(1,_font.MeasureString(history).X)),Cream);
                if(!m.Finished&&!m.Returning)RecoveryButton(new(x+20,y+184,530,42),m.Pending?"出発予約を取り消す":m.ReturnRequested?"帰還予約を取り消す":"すぐ帰れ（帰還を予約）",m.ReturnRequested);
            }
            if(missions.Length==0)_ui.Text("回収班はいません。左下から新しい班を予約できます。",new(690,220),.5f,Cream);
        }
    }
}
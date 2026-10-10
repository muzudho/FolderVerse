namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private BattleState[] _robotScenes=Array.Empty<BattleState>();
    private bool _robotPlaying=true;
    private RobotBattlePlayback _robotPlayback=new(Array.Empty<BattleState>());
    private int _robotRound;
    private int _robotPendingFocus=-1;
    private bool _robotPendingRound;
    private bool _robotSummary;
    private int _robotSummaryPage;
    private int[] RobotPriority=>RobotBattlePlayback.Priority(_robotScenes,_robotPlayback.Rounds[_robotRound],new[]{_setup.PlayerSlot});
    private RobotBattleSlice RobotSlice(int scene)=>_robotPlayback.Rounds[_robotRound].First(s=>s.Scene==scene);
    private double RobotRoundEnd=>_robotPlayback.Rounds[_robotRound].Max(s=>s.End-s.Start+1)*RobotFrameSeconds;
    private double _transportAge;
    private int _transportPage;
    private double _robotResultAge;
    private const double RobotResultSeconds=5;
    private const double RobotFrameSeconds=.08;
    private int RobotFrameIndex(BattleState scene)
    {var slice=RobotSlice(_battleWave);return Math.Clamp(slice.Start+(int)Math.Floor(_battleAge/RobotFrameSeconds+1e-8),slice.Start,slice.End);}
    private static readonly Rectangle RobotBattlePrevious=new(54,108,150,48),RobotBattleNext=new(1700,108,150,48);
    private static readonly Rectangle RobotPause=new(1000,934,250,48),RobotStepBack=new(1264,934,250,48),RobotStepNext=new(1528,934,250,48);
    private static readonly Rectangle RobotResultBar=new(54,1020,900,40),RobotResultPause=new(1000,1020,250,50);
    private void OpenRobotBattle()
    {
        _robotScenes=_setup.Campaign.RobotBattles.Scenes.ToArray();_battleTiles=Array.Empty<CharacterTile>();_battleWave=0;_battleAge=0;_robotResultAge=0;_robotPlaying=true;
        _robotPlayback=new(_robotScenes);_robotRound=0;_robotSummaryPage=0;_robotPendingFocus=-1;_robotPendingRound=false;_robotSummary=_robotPlayback.Rounds.Length==0;
        if(!_robotSummary)_battleWave=RobotPriority[0];
        _populationCell=-1;_dragging=false;
        if(_setup.Robots.Transport.LastTransfers.Count>0){_screen=Screen.Transport;_transportAge=0;_transportPage=0;return;}
        FinishTransport();
    }
    private void FinishTransport()
    {if(_robotScenes.Length>0)_screen=Screen.Battle;else{_screen=Screen.WorldStatus;OpenDisposition();}}
    private void UpdateTransport(double elapsed,bool proceed)
    {
        _transportAge=Math.Min(2,_transportAge+elapsed);
        if(!proceed)return;
        if(_transportAge<2){_transportAge=2;return;}
        if((++_transportPage)*8<_setup.Robots.Transport.LastTransfers.Count)_transportAge=0;else FinishTransport();
    }
    private void RobotBattleInput(double elapsed,bool click,bool enter)
    {
        if(_robotScenes.Length==0){_screen=Screen.WorldStatus;return;}
        if(_robotSummary)
        {if(click && RobotBattlePrevious.Contains(_pointer))_robotSummaryPage=Math.Max(0,_robotSummaryPage-1);
         if(click && RobotBattleNext.Contains(_pointer))_robotSummaryPage=Math.Min((_robotScenes.Length-1)/66,_robotSummaryPage+1);
         if(enter || click && BattleContinue.Contains(_pointer)){_screen=Screen.WorldStatus;_world.ShowSetup(_setup,true);OpenDisposition();}return;}
        var scene=_robotScenes[_battleWave];double end=RobotRoundEnd;
        if(click && (RobotBattlePrevious.Contains(_pointer) || RobotBattleNext.Contains(_pointer)))
        {if(_robotPendingFocus>=0)return;var priority=RobotPriority;int at=Array.IndexOf(priority,_battleWave);int next=priority[Math.Clamp(at+(RobotBattlePrevious.Contains(_pointer)?-1:1),0,priority.Length-1)];if(next!=_battleWave){_robotPendingFocus=next;_robotPendingRound=false;_robotResultAge=0;}return;}
        if(click && (RobotPause.Contains(_pointer) || RobotResultPause.Contains(_pointer))){_robotPlaying=!_robotPlaying;return;}
        if(click && (RobotStepBack.Contains(_pointer) || RobotStepNext.Contains(_pointer)))
        {if(_robotPendingFocus>=0)return;_robotPlaying=false;int step=RobotStepBack.Contains(_pointer)?-1:1;_battleAge=Math.Clamp(_battleAge+step*RobotFrameSeconds,0,end);return;}
        if(enter || click && BattleContinue.Contains(_pointer))
        {
            if(_robotPendingFocus>=0){SwitchRobotFocus();return;}
            if(_battleAge<end){_battleAge=end;return;}
            NextRobotScene();return;
        }
        if(_robotPlaying)
        {
            if(_robotPendingFocus>=0){_robotResultAge=Math.Min(RobotResultSeconds,_robotResultAge+elapsed);if(_robotResultAge>=RobotResultSeconds)SwitchRobotFocus();return;}
            _battleAge=Math.Min(end,_battleAge+elapsed);
            if(_battleAge>=end)NextRobotScene();
        }
    }
    private void NextRobotScene()
    {
        if(_robotRound+1>=_robotPlayback.Rounds.Length){_robotSummary=true;return;}
        int next=RobotBattlePlayback.Priority(_robotScenes,_robotPlayback.Rounds[_robotRound+1],new[]{_setup.PlayerSlot})[0];
        if(next!=_battleWave){_robotPendingFocus=next;_robotPendingRound=true;_robotResultAge=0;return;}
        _robotRound++;_battleAge=0;
    }
    private void SwitchRobotFocus()
    {if(_robotPendingRound){_robotRound++;_battleAge=0;}_battleWave=_robotPendingFocus;_robotPendingFocus=-1;_robotPendingRound=false;_robotResultAge=0;}
    private static string PhaseLabel(BattlePhase phase)=>phase switch
    {BattlePhase.Choose=>"行動選択",BattlePhase.Place=>"配置",BattlePhase.Rollback=>"巻き戻し",BattlePhase.Attack=>"攻撃",BattlePhase.Receive=>"被攻撃",BattlePhase.Consume=>"消費",BattlePhase.Fall=>"転倒",BattlePhase.Flag=>"旗更新",BattlePhase.Reinforce=>"増援",_=>"戦意・勝敗"};
    private void DrawSmokePuff(Vector2 center,float radius,float opacity)
    {
        int extent=(int)Math.Ceiling(radius);
        for(int y=-extent;y<=extent;y+=2)
        {
            float width=MathF.Sqrt(Math.Max(0,radius*radius-y*y));
            if(width>0)_ui.Box(new((int)(center.X-width),(int)center.Y+y,Math.Max(1,(int)(width*2)),2),Color.White*opacity);
        }
    }
    private void DrawRobotBattle()
    {
        if(_robotSummary){DrawRobotBattleSummary();return;}
        var battle=_robotScenes[_battleWave];int frameIndex=RobotFrameIndex(battle);
        var frame=battle.Frames.Count==0?new BattleFrame(battle.Turn,BattlePhase.Result,Array.Empty<BattleUnitView>(),battle.FlagStanding,battle.LastFlagOwner):battle.Frames[frameIndex];
        _ui.Text($"自動戦闘 / 全体ターン {_setup.Population.Turn} / 同時進行 {_robotRound+1}/{_robotPlayback.Rounds.Length} / 戦場 {_battleWave+1}/{_robotScenes.Length}",new(54,30),.75f,Cream);
        _ui.Button(RobotBattlePrevious,"前の戦場",Muted,.5f);_ui.Button(RobotBattleNext,"次の戦場",Muted,.5f);
        _ui.Center($"{(battle.SplitBoard?"分割野戦":battle.Kind==BattleKind.Edge?"辺戦":"節戦")} / Node {battle.TargetNode+1} / {battle.Terrain} / 戦闘ターン {frame.Turn} / {PhaseLabel(frame.Phase)}",new(224,108,1450,48),.6f,Cream);
        const int left=100,top=232,size=68;
        var terrainNode=_setup.Nodes.All[battle.TargetNode];var terrainCell=_setup.Cells[terrainNode.Cell];
        float terrainHeight=WorldTerrain.Elevation(_setup.Routes.Position(terrainNode.Cell,terrainNode.Center),WorldTerrain.Offset(_setup.WorldSeed));
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            var r=new Rectangle(left+x*size,top+y*size,size-2,size-2);bool wall=battle.Walls.Contains(new(x,y));
            if(battle.Terrain==BattleTerrain.Sea)
            {
                // Sea obstacles are water; the walkable outer areas are decks and the central cross is a bridge.
                bool bridge=x is 4 or 5 || y is 4 or 5;
                _ui.Box(r,wall?new Color(28,69,101):bridge?new Color(162,121,73):new Color(107,79,55));
                if(wall)BattleObstacleArt.Draw(_ui,r,BattleObstacleArt.Style(true,bridge,terrainHeight,terrainCell.Normal.Y));
                else _ui.Box(new(r.X+4,r.Y+size-14,size-10,2),new Color(194,151,96));
            }
            else
            {
                _ui.Box(r,Math.Abs(terrainCell.Normal.Y)>.5f?new Color(161,192,203):terrainHeight<.55f?new Color(163,137,91):new Color(35,64,58));
                if(wall)BattleObstacleArt.Draw(_ui,r,BattleObstacleArt.Style(false,false,terrainHeight,terrainCell.Normal.Y));
            }
        }
        foreach(var army in battle.Armies)
        {
            var border=BattleObstacleArt.HomeBorder(army.Side,new Rectangle(left,top,size*10,size*10));
            _ui.Box(border,_setup.OwnerColor(army.Owner));
            _ui.Center($"#{army.Owner+1} 退却",army.Side is 0 or 2?new Rectangle(border.X,border.Y+(army.Side==0?-52:10),border.Width,24):new Rectangle(army.Side==3?left-85:left+size*10+12,top+300,70,30),.32f,_setup.OwnerColor(army.Owner));
        }
        for(int i=0;i<10;i++)
        {_ui.Text(i.ToString(),new(left+i*size+25,top-32),.45f,Cream);_ui.Text(((char)('A'+i)).ToString(),new(left-30,top+i*size+18),.45f,Cream);}
        if(battle.Kind==BattleKind.Node)
        {
            var f=new Rectangle(left+battle.Flag.X*size,top+battle.Flag.Y*size,size-2,size-2);
            foreach(var patch in BattlePieceArt.Flags[frame.FlagStanding?0:1])
            {
                var b=patch.Bounds;var color=patch.Ink==0?_setup.OwnerColor(battle.Defender):patch.Ink==2?new Color(215,198,159):new Color(255,224,115);
                _ui.Box(new(f.X+b.X*2,f.Y+b.Y,f.Width>b.Width*2?b.Width*2:f.Width,b.Height),color);
            }
        }
        foreach(var unit in frame.Units.Where(u=>!u.Retreated && BattleState.Inside(u.Position)))
        {
            var r=new Rectangle(left+unit.Position.X*size+13,top+unit.Position.Y*size+5,42,58);
            int firstFall=unit.Fallen?battle.Frames.FindIndex(f=>f.Units.Any(u=>u.Id==unit.Id && u.Fallen)):-1;
            float opacity=unit.Fallen?Math.Clamp(1-(frameIndex-firstFall)/5f,0,1):1f;
            if(opacity<=0)continue;
            foreach(var patch in BattlePieceArt.Pieces[BattlePieceArt.Index(unit.Role,unit.Facing)])
            {
                var b=patch.Bounds;
                var color=patch.Ink switch{0=>_setup.OwnerColor(unit.Owner),1=>new Color(255,222,180),2=>new Color(19,25,34),3=>new Color(255,224,115),4=>new Color(116,62,38),_=>new Color(249,135,149)};
                _ui.Box(new(r.X+b.X*r.Width/32,r.Y+b.Y*r.Height/48,Math.Max(1,b.Width*r.Width/32),Math.Max(1,b.Height*r.Height/48)),color*opacity);
            }
            if(unit.Role==BattleRole.Queen)
            {
                var glow=new Color(255,228,135)*opacity;
                _ui.Box(new(r.X-5,r.Y-5,r.Width+10,3),glow);
                _ui.Box(new(r.X-5,r.Bottom+2,r.Width+10,3),glow);
            }
            bool salute=unit.Role==BattleRole.Captain && !unit.Fallen && frame.Phase is BattlePhase.Place or BattlePhase.Rollback && battle.Events.Any(e=>e.Turn==frame.Turn && e.Unit==unit.Id && e.Detail=="隊形成 / 剣を振り上げた");
            if(salute)_ui.Tile(new(r.Right+7,r.Y+3),new(4,28),-.35f,new Color(255,225,125));
        }
        if(frame.Phase is BattlePhase.Attack or BattlePhase.Receive)
        foreach(var attack in battle.Attacks.Where(a=>a.Turn==frame.Turn))
        {
            var from=new Vector2(left+attack.From.X*size+size/2,top+attack.From.Y*size+size/2);
            var to=new Vector2(left+attack.To.X*size+size/2,top+attack.To.Y*size+size/2);var delta=to-from;
            float fraction=(float)Math.Clamp(_battleAge/RobotFrameSeconds-(frameIndex-RobotSlice(_battleWave).Start),0,1);
            if(!_robotPlaying)fraction=.5f;
            float progress=((frame.Phase==BattlePhase.Receive?1:0)+fraction)/2;
            if(attack.Weapon==BattleWeapon.Rifle)
            {
                if(delta.Length()<2*size)continue; // A wall or board edge before the second square stops the shot.
                var launch=from+Vector2.Normalize(delta)*(2*size);
                float burst=Math.Clamp(progress/.3f,0,1);
                for(int puff=0;puff<7;puff++)
                {
                    float angle=puff*MathHelper.TwoPi/7;
                    var offset=new Vector2(MathF.Cos(angle),MathF.Sin(angle))*(4+burst*15);
                    DrawSmokePuff(launch+offset,4+burst*3,1-burst*.65f);
                }
                float flight=Math.Clamp((progress-.18f)/.82f,0,1);
                var ball=Vector2.Lerp(launch,to,flight);
                _ui.Tile(ball,new(9,9),MathHelper.PiOver4,new Color(255,244,211));
            }
            else if(delta.LengthSquared()>0)
            {
                var forward=Vector2.Normalize(delta);var pivot=from+forward*13;
                float angle=MathF.Atan2(delta.Y,delta.X)+(progress-.5f)*1.8f;
                var blade=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
                _ui.Tile(pivot+blade*6,new(13,6),angle,new Color(133,76,42));
                _ui.Tile(pivot+blade*12,new(4,16),angle,new Color(255,222,116));
                _ui.Tile(pivot+blade*27,new(28,6),angle,new Color(225,241,250));
                _ui.Tile(pivot+blade*39,new(6,6),angle+MathHelper.PiOver4,Color.White);
                var opponent=battle.Attacks.FirstOrDefault(other=>BattleAttackArt.Clashes(attack,other));
                if(opponent!=null && attack.Attacker<opponent.Attacker)
                {
                    var impact=(from+to)/2;float spread=8+progress*24;
                    for(int spark=0;spark<8;spark++)
                    {
                        float sparkAngle=spark*MathHelper.TwoPi/8+.2f;
                        var ray=new Vector2(MathF.Cos(sparkAngle),MathF.Sin(sparkAngle));
                        _ui.Tile(impact+ray*spread,new(9,3),sparkAngle,new Color(255,235,134)*(1-progress*.6f));
                    }
                    _ui.Tile(impact,new(11,11),MathHelper.PiOver4,Color.White);
                }
            }
        }
        int armyIndex=0;
        foreach(var army in battle.Armies)
        {
            var r=new Rectangle(1000,220+armyIndex++*142,830,128);_ui.Box(r,Muted);
            _portraitRenderer.Draw(_spriteBatch,_setup.Looks[army.Owner],new(r.X+8,r.Y+8,105,105));
            var alive=frame.Units.Where(u=>u.Owner==army.Owner && !u.Fallen && !u.Retreated).ToArray();
            _ui.Text($"#{army.Owner+1} {_setup.ConquerorNames[army.Owner]} / 生存 {alive.Length}",new(r.X+130,r.Y+15),.59f,_setup.OwnerColor(army.Owner));
            _ui.Text($"短剣 {alive.Sum(u=>u.Daggers)} / 小銃 {alive.Sum(u=>u.Rifles)} / 盾 {alive.Sum(u=>u.Shields)}",new(r.X+130,r.Y+57),.49f,Cream);
            _ui.Text("帰還先 Node "+(army.HomeNode+1),new(r.X+130,r.Y+92),.4f,Cream);
        }
        string result=$"全 {_robotPlayback.Rounds[_robotRound].Length} 戦場で１戦闘ターンを同時進行 / 結果は最後に表示";
        var recent=battle.Events.LastOrDefault(e=>e.Turn==frame.Turn && e.Phase==frame.Phase && e.Unit!=0);
        if(recent!=null)_ui.Text($"ID {recent.Unit} / {recent.Detail}",new(1000,786),.42f,Cream);
        _ui.Text(result,new(1000,822),.55f,Cream);
        _ui.Text("観戦中は駒を操作できません / 再生速度は戦闘結果に影響しません",new(1000,873),.41f,Cream);
        bool end=_battleAge>=RobotRoundEnd;
        _ui.Button(RobotPause,_robotPlaying?"一時停止":"再生",Muted,.5f);
        _ui.Button(RobotStepBack,"１フェーズ戻る",Muted,.46f);_ui.Button(RobotStepNext,"１フェーズ進む",Muted,.46f);
        _ui.Text($"再生 {frameIndex+1}/{battle.Frames.Count} / 王冠：クイーン 高帽子：隊長 低帽子：兵",new(54,965),.48f,Cream);
        if(_robotPendingFocus>=0)
        {
            _ui.Box(RobotResultBar,Muted);
            int width=(int)(RobotResultBar.Width*_robotResultAge/RobotResultSeconds);
            if(width>0)_ui.Box(new(RobotResultBar.X,RobotResultBar.Y,width,RobotResultBar.Height),Accent);
            _ui.Center($"戦場 {_robotPendingFocus+1} へ切替 / {RobotResultSeconds-_robotResultAge:0.0} 秒",RobotResultBar,.45f,Cream);
            _ui.Button(RobotResultPause,_robotPlaying?"一時停止":"再開",Muted,.5f);
        }
        _ui.Button(BattleContinue,_robotPendingFocus>=0?"戦場を切り替える":!end?"このターンを進める":"次の戦闘ターンへ",Accent,.55f);
    }
    private void DrawRobotBattleSummary()
    {
        _ui.Text("全戦場の戦闘結果",new(54,30),.9f,Cream);
        _ui.Text("今回の戦闘進行が完了しました / 継戦中の戦場は次の全体ターンへ",new(54,105),.55f,Cream);
        if(_robotScenes.Length>66){_ui.Button(RobotBattlePrevious,"前へ",Muted,.5f);_ui.Button(RobotBattleNext,"次へ",Muted,.5f);}
        for(int at=0;at<66 && _robotSummaryPage*66+at<_robotScenes.Length;at++)
        {
            int i=_robotSummaryPage*66+at;var b=_robotScenes[i];int column=at/22,row=at%22;
            string outcome=!b.Finished?"継戦":b.Winner<0?"勝者なし":$"勝者 #{b.Winner+1}";
            _ui.Text($"戦場 {i+1} / Node {b.TargetNode+1} / {outcome}",new(54+column*600,180+row*35),.48f,Cream);
        }
        _ui.Button(BattleContinue,"世界へ戻る",Accent,.55f);
    }
    private void DrawTransport()
    {
        var transfers=_setup.Robots.Transport.LastTransfers;float progress=(float)(_transportAge/2);
        _ui.Text($"ロボット輸送 / 確定 {transfers.Count} 便 / {_transportPage+1}/{(transfers.Count+7)/8}",new(54,30),.9f,Cream);
        _ui.Text("衝突を解決して成立した輸送を表示します",new(54,100),.55f,Cream);
        for(int i=0;i<8;i++)
        {
            int index=_transportPage*8+i;if(index>=transfers.Count)break;var shipment=transfers[index];int y=185+i*97;
            _ui.Text($"Node {shipment.Source+1}",new(80,y+16),.6f,Cream);_ui.Text($"Node {shipment.Target+1}",new(1600,y+16),.6f,Cream);
            _ui.Box(new(350,y+44,1180,3),Muted);
            // Ownership belongs to the departed individual even if its ID merged on arrival.
            int owner=_setup.Robots.Transport.LastOwners.GetValueOrDefault(shipment.RobotId);
            DrawRobotGlyph(new(350+(int)(1120*progress),y,36,55),owner,shipment.Parts);
            _ui.Text(PartsLabel(shipment.Parts)+(shipment.Disposal?" / 廃棄としての移送":" / ふつうの移送"),new(720,y+58),.42f,Cream);
        }
        _ui.Button(BattleContinue,_transportAge<2?"到着まで進む":(_transportPage+1)*8<transfers.Count?"続く輸送へ":"観戦へ",Accent,.6f);
    }
}

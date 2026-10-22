namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public partial class Game1
{
    private readonly Dictionary<(int Battle,long Unit),float> _fallFadeStarted=new();
    private BattleState[] _robotScenes=Array.Empty<BattleState>();
    private bool _robotPlaying=true;
    private const double BattlePartsHoldSeconds=0,BattleForeignFadeSeconds=1.5,BattleNativeHoldSeconds=2,BattlePieceFadeSeconds=1.5,BattleBoardRevealSeconds=1.5;
    private double _battleIntroAge=BattlePartsHoldSeconds+BattleForeignFadeSeconds+BattleNativeHoldSeconds+BattlePieceFadeSeconds+BattleBoardRevealSeconds;
    private bool BattleIntroActive=>_battleIntroAge<BattlePartsHoldSeconds+BattleForeignFadeSeconds+BattleNativeHoldSeconds+BattlePieceFadeSeconds+BattleBoardRevealSeconds;
    private float BattlePieceOpacity=>BattleIntroActive?(float)Math.Clamp((_battleIntroAge-BattlePartsHoldSeconds-BattleForeignFadeSeconds-BattleNativeHoldSeconds)/BattlePieceFadeSeconds,0,1):1;
    private float BattlePartOpacity(bool native)=>!BattleIntroActive?0:native?1-BattlePieceOpacity:(float)Math.Clamp(1-(_battleIntroAge-BattlePartsHoldSeconds)/BattleForeignFadeSeconds,0,1);
    private float BattleBoardDim=>BattleIntroActive?1-(float)Math.Clamp((_battleIntroAge-BattlePartsHoldSeconds-BattleForeignFadeSeconds-BattleNativeHoldSeconds-BattlePieceFadeSeconds)/BattleBoardRevealSeconds,0,1):0;
    private RobotBattlePlayback _robotPlayback=new(Array.Empty<BattleState>());
    private int _robotRound;
    private int _robotPendingFocus=-1;
    private bool _robotPendingRound;
    private bool _robotSummary;
    private bool _robotPendingSummary;

    private int[] BattlePlayers=>_setup.HumanPlayers.Count>0?_setup.HumanPlayers.ToArray():new[]{_setup.PlayerSlot};
    private int[] RobotPriority=>RobotBattlePlayback.Priority(_robotScenes,_robotPlayback.Rounds[_robotRound],BattlePlayers);
    private RobotBattleSlice RobotSlice(int scene)=>_robotPlayback.Rounds[_robotRound].First(s=>s.Scene==scene);
    private double RobotSeconds(int scene)=>RobotBattlePlayback.SecondsPerFrame(_robotScenes[scene],RobotSlice(scene),RobotFrameSeconds);
    private bool RobotMovementRound=>_robotPlayback.Rounds[_robotRound].All(s=>RobotBattlePlayback.MovementOnly(_robotScenes[s.Scene],s));
    private double RobotRoundEnd=>_robotPlayback.Rounds[_robotRound].Max(s=>(s.End-s.Start+1)*RobotSeconds(s.Scene));
    private double _transportAge;
    private double _robotResultAge;
    private const double RobotResultSeconds=5;
    // At 18 rendered frames/sec, each phase lasts at least four frames.
    private const double RobotFrameSeconds=.25;
    private int RobotFrameIndex(BattleState scene)
    {var slice=RobotSlice(_battleWave);return Math.Clamp(slice.Start+(int)Math.Floor(_battleAge/RobotSeconds(_battleWave)+1e-8),slice.Start,slice.End);}
    private static readonly Rectangle RobotBattlePrevious=new(1120,154,350,48),RobotBattleNext=new(1490,154,350,48);
    private static readonly Rectangle RobotPause=new(1120,934,230,48),RobotStepBack=new(1365,934,230,48),RobotStepNext=new(1610,934,230,48);
    private static readonly Rectangle RobotResultBar=new(1120,990,720,20),RobotResultPause=new(1120,1020,270,50);
    private void OpenRobotBattle()
    {
        _newspaperMusicMood=(NewspaperMusicMood)(_setup.Population.Turn%4);
        QueueTutorialAttackAchievement();
        _newspaperIssue=_newspaperPreviousIssue=null;_newspaperSlideAge=1;_newspaperClock=0;
        _fallFadeStarted.Clear();_robotScenes=_setup.Campaign.RobotBattles.Scenes.ToArray();_battleTiles=Array.Empty<CharacterTile>();_battleWave=0;_battleAge=0;_robotResultAge=0;_robotPlaying=true;
        _robotPlayback=new(_robotScenes);_robotRound=0;_summaryReadAge=0;_summaryReadPapers=0;_robotPendingFocus=-1;_robotPendingRound=false;_robotPendingSummary=false;_robotSummary=_robotPlayback.Rounds.Length==0;
        if(!_robotSummary){var priority=RobotPriority;_battleWave=_tutorialAttackPending?priority.FirstOrDefault(i=>TutorialAchievements.AttackedScene(_setup,_robotScenes[i]),priority[0]):priority[0];}
        _populationCell=-1;_dragging=false;
        _statusGlobe=false;_net.ResetView();_net.CancelAnimation();
        _screen=Screen.Transport;_transportAge=0;BeginPropagation();
    }
    private void FinishTransport()
    {if(_robotScenes.Length>0){_screen=Screen.Battle;_battleIntroAge=0;}else if(_setup.Lessons.Pending.Count>0)BeginBattleResearch();else BeginIndustry();}
    private void UpdateTransport(double elapsed,bool proceed,double now)
    {
        // Wait for Draw to register the card, then advance only time after it ends.
        if(_lastCutInPhase!="伝播フェーズ")return;
        elapsed=Math.Min(elapsed,Math.Max(0,now-_cutInStarted-PhaseCutInSeconds));
        if(elapsed<=0)return;
        if(!PropagationComplete){UpdatePropagationMovement(elapsed);return;}
        _transportAge=Math.Min(2,_transportAge+elapsed);
        if(_transportAge>=2)FinishTransport();
    }
    private void RobotBattleInput(double elapsed,bool click,bool enter)
    {
        if(_tutorialAchievementAge>=0){UpdateTutorialAchievementCutIn(elapsed);return;}
        if(BattleIntroActive && !_robotSummary){
            _battleIntroAge=Math.Min(BattlePartsHoldSeconds+BattleForeignFadeSeconds+BattleNativeHoldSeconds+BattlePieceFadeSeconds+BattleBoardRevealSeconds,_battleIntroAge+elapsed);
            if(!BattleIntroActive)RevealTutorialAttackAchievement();
            return;
        }
        _newspaperClock+=elapsed;_newspaperSlideAge+=elapsed;
        if(click && _robotSummary && NewspaperMusicButton.Contains(_pointer)){_newspaperMusicMood=(NewspaperMusicMood)(((int)_newspaperMusicMood+1)%4);return;}
        if(click && _robotSummary && SelectSummaryPaper())return;
        if(click && (_robotSummary?SummaryNewspaperSwitch.Contains(_pointer):NewspaperSwitch.Contains(_pointer)))
        {SwitchNewspaper();return;}
        if(!_robotSummary && _newspaperClock>=10)SwitchNewspaper();
        if(_robotScenes.Length==0){BeginIndustry();return;}
        if(_robotSummary)
        {

         if(enter || click && BattleContinue.Contains(_pointer)){BeginBattleResearch();}return;}
        var scene=_robotScenes[_battleWave];double end=RobotRoundEnd;
        if(click && (RobotBattlePrevious.Contains(_pointer) || RobotBattleNext.Contains(_pointer)))
        {if(_robotPendingFocus>=0)return;var priority=RobotPriority;int at=Array.IndexOf(priority,_battleWave);int next=priority[Math.Clamp(at+(RobotBattlePrevious.Contains(_pointer)?-1:1),0,priority.Length-1)];if(next!=_battleWave){_robotPendingFocus=next;_robotPendingRound=false;_robotResultAge=0;}return;}
        if(click && (RobotPause.Contains(_pointer) || RobotResultPause.Contains(_pointer))){_robotPlaying=!_robotPlaying;return;}
        if(click && (RobotStepBack.Contains(_pointer) || RobotStepNext.Contains(_pointer)))
        {if(_robotPendingFocus>=0)return;_robotPlaying=false;int step=RobotStepBack.Contains(_pointer)?-1:1;_battleAge=Math.Clamp(_battleAge+step*RobotSeconds(_battleWave),0,end);return;}
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
        if(_robotRound+1>=_robotPlayback.Rounds.Length){_robotPendingSummary=true;_robotPendingFocus=_battleWave;_robotResultAge=0;return;}
        int next=RobotBattlePlayback.Priority(_robotScenes,_robotPlayback.Rounds[_robotRound+1],new[]{_setup.PlayerSlot})[0];
        if(RobotMovementRound){_robotRound++;_battleAge=0;_battleWave=next;return;}
        if(next!=_battleWave){_robotPendingFocus=next;_robotPendingRound=true;_robotResultAge=0;return;}
        _robotRound++;_battleAge=0;
    }
    private void SwitchRobotFocus()
    {if(_robotPendingSummary){_robotSummary=true;_robotPendingSummary=false;}if(_robotPendingRound){_robotRound++;_battleAge=0;}_battleWave=_robotPendingFocus;_robotPendingFocus=-1;_robotPendingRound=false;_robotResultAge=0;}
    private static string PhaseLabel(BattlePhase phase)=>phase switch
    {BattlePhase.Persuade=>"説得中",BattlePhase.Converted=>"説得の結果",BattlePhase.Choose=>"行動選択",BattlePhase.Place=>"配置",BattlePhase.Rollback=>"巻き戻し",BattlePhase.Attack=>"攻撃",BattlePhase.Receive=>"被攻撃",BattlePhase.Consume=>"消費",BattlePhase.Fall=>"転倒",BattlePhase.Flag=>"旗更新",BattlePhase.Reinforce=>"増援",BattlePhase.Retreat=>"戦意喪失 / 全軍撤退",_=>"戦意・勝敗"};
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
        if(_robotPendingFocus>=0)
        {
            var previous=_robotScenes[_battleWave];
            bool decided=previous.Finished && (previous.Frames.Count==0 || RobotFrameIndex(previous)==previous.Frames.Count-1);
            if(!decided)
            {
                DrawPropagation(_robotPendingFocus,(float)(decided?(_robotResultAge/RobotResultSeconds-.6)/.4:_robotResultAge/RobotResultSeconds),false);
                if(decided)DrawBattleNewspaper(previous,previous.Frames.Count==0?new BattleFrame(previous.Turn,BattlePhase.Result,Array.Empty<BattleUnitView>(),previous.FlagStanding,previous.LastFlagOwner):previous.Frames[^1]);
                return;
            }
        }
        var battle=_robotScenes[_battleWave];int frameIndex=RobotFrameIndex(battle);
        var frame=battle.Frames.Count==0?new BattleFrame(battle.Turn,BattlePhase.Result,Array.Empty<BattleUnitView>(),battle.FlagStanding,battle.LastFlagOwner):battle.Frames[frameIndex];
        _ui.Text($"自動戦闘 / 全体ターン {_setup.Population.Turn}",new(1120,24),.65f,Cream);
        _ui.Text($"同時進行 {_robotRound+1}/{_robotPlayback.Rounds.Length} / 戦場 {_battleWave+1}/{_robotScenes.Length}",new(1120,68),.55f,Cream);
        _ui.Button(RobotBattlePrevious,"前の戦場",Muted,.5f);_ui.Button(RobotBattleNext,"次の戦場",Muted,.5f);
        BattleInfo($"{(battle.SplitBoard?"分割野戦":battle.Kind==BattleKind.Edge?"辺戦":"節戦")} / Node {battle.TargetNode+1} / {battle.Terrain} / 戦闘ターン {frame.Turn} / {PhaseLabel(frame.Phase)}",112,.5f);
        const int left=40,top=40,size=100;
        _ui.Box(new(left,top,size*10,size*10),new Color(24,43,51));
        var terrainNode=_setup.Nodes.All[battle.TargetNode];var terrainCell=_setup.Cells[terrainNode.Cell];
        float terrainHeight=WorldTerrain.Elevation(_setup.Routes.Position(terrainNode.Cell,terrainNode.Center),WorldTerrain.Offset(_setup.WorldSeed));
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            var r=new Rectangle(left+x*size,top+y*size,size-2,size-2);bool wall=battle.Walls.Contains(new(x,y));
            if(battle.Terrain==BattleTerrain.Sea)
            {
                bool log=battle.SeaLogs.Contains(new(x,y));
                _ui.Box(r,new Color(28,88,125));
                if(wall)
                {
                    if(!battle.SeaClearWater.Contains(new(x,y)))BattleObstacleArt.Draw(_ui,r,BattleObstacleArt.Style(true,false,terrainHeight,terrainCell.Normal.Y));
                }
                else if(log)
                {
                    bool horizontal=battle.SeaLogs.Contains(new(x-1,y)) || battle.SeaLogs.Contains(new(x+1,y));
                    var timber=horizontal?new Rectangle(r.X,r.Y+28,r.Width,44):new Rectangle(r.X+28,r.Y,44,r.Height);
                    _ui.Box(timber,new Color(151,101,52));
                    for(int stripe=0;stripe<3;stripe++)
                        _ui.Box(horizontal?new(timber.X,timber.Y+8+stripe*12,timber.Width,3):new(timber.X+8+stripe*12,timber.Y,3,timber.Height),new Color(212,165,91));
                    _ui.Box(horizontal?new(timber.X+16,timber.Y,5,timber.Height):new(timber.X,timber.Y+16,timber.Width,5),new Color(91,61,35));
                }
                else
                {
                    // Sloping hull edges distinguish the deck from a rectangular land tile.
                    for(int sy=0;sy<r.Height;sy+=2)
                    {
                        int inset=(int)(12*Math.Abs(sy-r.Height/2f)/(r.Height/2f));
                        _ui.Box(new(r.X+inset,r.Y+sy,r.Width-inset*2,2),new Color(112,75,47));
                    }
                    for(int plank=1;plank<5;plank++)_ui.Box(new(r.X+12,r.Y+plank*18,r.Width-24,2),new Color(193,145,88));
                }            }
            else
            {
                _ui.Box(r,Math.Abs(terrainCell.Normal.Y)>.5f?new Color(161,192,203):terrainHeight<.55f?new Color(163,137,91):new Color(35,64,58));
                if(wall)BattleObstacleArt.Draw(_ui,r,BattleObstacleArt.Style(false,false,terrainHeight,terrainCell.Normal.Y));
            }
        }
        for(int line=0;line<=10;line++){_ui.Box(new(left+line*size,top,2,size*10),new Color(24,43,51));_ui.Box(new(left,top+line*size,size*10,2),new Color(24,43,51));}
        foreach(var army in battle.Armies)
        {
            var border=BattleObstacleArt.HomeBorder(army.Side,new Rectangle(left,top,size*10,size*10));
            _ui.Box(border,_setup.OwnerColor(army.Owner));
            // Keep retreat markers inside the left 1080-pixel battlefield area.
            _ui.Center($"#{army.Owner+1}",army.Side is 0 or 2?new Rectangle(border.X,border.Y+(army.Side==0?-28:8),border.Width,24):new Rectangle(army.Side==3?0:1042,top+450,36,30),.3f,_setup.OwnerColor(army.Owner));
        }
        for(int i=0;i<10;i++)
        {_ui.Text(i.ToString(),new(left+i*size+42,top-32),.45f,Cream);_ui.Text(((char)('A'+i)).ToString(),new(left-30,top+i*size+35),.45f,Cream);}
        if(battle.Kind==BattleKind.Node)
        {
            var f=new Rectangle(left+battle.Flag.X*size,top+battle.Flag.Y*size,size-2,size-2);
            foreach(var patch in BattlePieceArt.Flags[frame.FlagStanding?0:1])
            {
                var b=patch.Bounds;var color=patch.Ink==0?_setup.OwnerColor(battle.Defender):patch.Ink==2?new Color(215,198,159):new Color(255,224,115);
                _ui.Box(new(f.X+b.X*size/34,f.Y+b.Y*size/68,Math.Min(f.Width,b.Width*size/34),Math.Max(1,b.Height*size/68)),color);
            }
        }
        if(BattleIntroActive){
            float dim=BattleBoardDim;
            _ui.Box(new(left,top,size*10,size*10),new Color(105,110,116)*(dim*.9f));
        }
        DrawPersuasionRange(battle,frame,left,top,size);
        float impactProgress=BattleAttackArt.ImpactProgress(battle,frameIndex,_robotPlaying?RobotBattlePlayback.FramePosition(RobotSlice(_battleWave),_battleAge,RobotSeconds(_battleWave)):frameIndex+.5);
        foreach(var unit in frame.Units)
        {
            if(unit.Retreated && BattleState.Inside(unit.Position))continue;
            int firstExit=unit.Retreated?battle.Frames.FindIndex(f=>f.Units.Any(u=>u.Id==unit.Id && u.Retreated)):-1;
            if(unit.Retreated && (firstExit<0 || frameIndex-firstExit>5))continue;
            float exitAge=unit.Retreated?(float)(RobotBattlePlayback.FramePosition(RobotSlice(_battleWave),_battleAge,RobotSeconds(_battleWave))-firstExit)/5f:0;
            var pos=new Vector2(left+unit.Position.X*size+19,top+unit.Position.Y*size+7);
            pos+=BattleAttackArt.Recoil(battle,frame.Turn,unit.Id,impactProgress);
            if(unit.Retreated){var last=battle.Frames.Take(firstExit).LastOrDefault(f=>f.Units.Any(u=>u.Id==unit.Id && !u.Retreated));var prior=last?.Units.FirstOrDefault(u=>u.Id==unit.Id);if(prior!=null && BattleState.Inside(prior.Position)){var from=new Vector2(left+prior.Position.X*size+19,top+prior.Position.Y*size+7);var exit=battle.Frames[firstExit].Units.First(u=>u.Id==unit.Id);var step=BattleState.Forward(prior.Facing);var delta=new Vector2(exit.Position.X-prior.Position.X,exit.Position.Y-prior.Position.Y);var direction=delta.LengthSquared()>0?Vector2.Normalize(delta):new Vector2(step.X,step.Y);pos=Vector2.Lerp(from,from+direction*70,Math.Clamp(exitAge*2,0,1));}}
            var r=new Rectangle((int)pos.X,(int)pos.Y,62,85);
            int firstFall=unit.Fallen?battle.Frames.FindIndex(f=>f.Units.Any(u=>u.Id==unit.Id && u.Fallen)):-1;
            float opacity=1f;
            var fadeKey=(battle.Id,unit.Id);
            if(unit.Fallen){
                if(!_fallFadeStarted.TryGetValue(fadeKey,out float started)){started=_animationTime;_fallFadeStarted[fadeKey]=started;}
                opacity=Math.Clamp(1-(_animationTime-started)/1.25f,0,1);
            }else _fallFadeStarted.Remove(fadeKey);
            if(unit.Retreated)opacity*=Math.Clamp(1-exitAge,0,1);
            if(opacity<=0)continue;
            var physical=unit.Role==BattleRole.Queen?null:unit.Robot??battle.Units.Concat(battle.Armies.SelectMany(a=>a.Waiting)).FirstOrDefault(u=>u.Id==unit.Id)?.Robot;
            if(physical!=null && BattleIntroActive)
                foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})
                    if((physical.Parts&part)!=0)
                        DrawRobotGlyph(r,physical.Origin(part),part,physical.Role==RobotRole.Captain,
                            opacity*BattlePartOpacity(physical.Affiliation(part)==unit.Owner),damaged:physical.IsDamaged(part),cheap:physical.IsCheap(part));
            float pieceOpacity=opacity*(physical==null?1:BattlePieceOpacity);
            foreach(var patch in BattlePieceArt.Pieces[BattlePieceArt.Index(unit.Role,unit.Facing)])
            {
                var b=patch.Bounds;
                var color=patch.Ink switch{0=>_setup.OwnerColor(unit.Owner),1=>new Color(255,222,180),2=>new Color(19,25,34),3=>new Color(255,224,115),4=>new Color(116,62,38),_=>new Color(249,135,149)};
                var area=new Rectangle(r.X+b.X*r.Width/32,r.Y+b.Y*r.Height/48,Math.Max(1,b.Width*r.Width/32),Math.Max(1,b.Height*r.Height/48));
                var part=b.Y<26?RobotParts.Head:b.Y<37?RobotParts.Body:RobotParts.Legs;
                if(physical!=null && patch.Ink==0)
                {
                    color=physical.IsDamaged(part)?new Color(136,137,127):_setup.OwnerColor(physical.Origin(part));
                    if(physical.IsCheap(part))
                    {
                        for(int row=0;row<area.Height;row++)_ui.Box(new(area.X,area.Y+row,area.Width,1),((area.Y+row)%6<3?color:physical.IsDamaged(part)?new Color(165,162,146):Cream)*pieceOpacity);
                        continue;
                    }
                }
                _ui.Box(area,color*pieceOpacity);
            }
            if(unit.Role==BattleRole.Queen)
            {
                var glow=new Color(255,228,135)*opacity;
                _ui.Box(new(r.X-5,r.Y-5,r.Width+10,3),glow);
                _ui.Box(new(r.X-5,r.Bottom+2,r.Width+10,3),glow);
            }
            bool salute=unit.Role==BattleRole.Captain && !unit.Fallen && frame.Phase is BattlePhase.Place or BattlePhase.Rollback && battle.Events.Any(e=>e.Turn==frame.Turn && e.Unit==unit.Id && e.Detail=="隊形成 / 剣を振り上げた");
            if(salute)_ui.Tile(new(r.Right+7,r.Y+3),new(4,28),-.35f,new Color(255,225,125));
            if(_robotPendingFocus<0 && !_robotPendingSummary && !unit.Fallen && !unit.Retreated && frame.Phase is BattlePhase.Receive or BattlePhase.Consume &&
                battle.Events.Any(e=>e.Turn==frame.Turn && e.Unit==unit.Id && e.Detail=="盾で防いだ")){
                var forward=BattleState.Forward(unit.Facing);var normal=new Vector2(forward.X,forward.Y);
                var center=new Vector2(r.Center.X,r.Center.Y)+normal*38;var tangent=new Vector2(-normal.Y,normal.X);
                // The barrier sits on the defended front, perpendicular to the incoming hit.
                _ui.Tile(center,new(48,9),MathF.Atan2(tangent.Y,tangent.X),new Color(125,221,255));
                _ui.Tile(center+normal*5,new(36,4),MathF.Atan2(tangent.Y,tangent.X),Color.White);
                for(int spark=0;spark<5;spark++){
                    float offset=(spark-2)*12;var at=center+tangent*offset+normal*(8+MathF.Sin(_animationTime*16+spark)*5);
                    _ui.Tile(at,new(8,3),MathF.Atan2(normal.Y,normal.X),new Color(255,238,143));
                }
            }
            if((_robotPendingFocus>=0 || _robotPendingSummary) && battle.Finished && battle.Winner>=0 &&
                frameIndex==battle.Frames.Count-1 && !unit.Fallen && !unit.Retreated && battle.Allied(unit.Owner,battle.Winner)){
                var front=BattleState.Forward(unit.Facing);var direction=new Vector2(front.X,front.Y);
                var mouth=new Vector2(r.Center.X,r.Y+28);float angle=MathF.Atan2(direction.Y,direction.X);
                // Trumpet held to the mouth, bell pointing toward the unit's front.
                _ui.Tile(mouth+direction*24,new(40,7),angle,new Color(255,210,85));
                _ui.Tile(mouth+direction*43,new(12,24),angle,new Color(255,231,145));
                for(int ray=0;ray<3;ray++){
                    var at=mouth+direction*(55+ray*10);var side=new Vector2(-direction.Y,direction.X);
                    _ui.Tile(at+side*MathF.Sin(_animationTime*8+ray)*7,new(4,13),angle,new Color(255,240,170));
                }
                _ui.Tile(new(r.X-6,r.Y+10),new(7,30),-.55f,new Color(255,222,180));
            }
        }
        if(frame.Phase is BattlePhase.Attack or BattlePhase.Receive)
        foreach(var attack in battle.Attacks.Where(a=>a.Turn==frame.Turn))
        {
            var from=new Vector2(left+attack.From.X*size+size/2,top+attack.From.Y*size+size/2);
            var to=new Vector2(left+attack.To.X*size+size/2,top+attack.To.Y*size+size/2);var delta=to-from;
            float fraction=(float)Math.Clamp(_battleAge/RobotSeconds(_battleWave)-(frameIndex-RobotSlice(_battleWave).Start),0,1);
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
        if(impactProgress>=0 && !BattleIntroActive)
        foreach(var attack in battle.Attacks.Where(a=>a.Turn==frame.Turn && a.Weapon==BattleWeapon.Dagger && a.Target!=null))
        {
            var incoming=new Vector2(attack.From.X-attack.To.X,attack.From.Y-attack.To.Y);
            if(incoming.LengthSquared()==0)continue;
            incoming.Normalize();var tangent=new Vector2(-incoming.Y,incoming.X);
            var center=new Vector2(left+attack.To.X*size+50,top+attack.To.Y*size+49)+incoming*27+BattleAttackArt.Recoil(battle,frame.Turn,attack.Target.Value,impactProgress);
            float pulse=MathF.Sin(impactProgress*MathHelper.Pi),spread=9+pulse*13;
            for(int ray=0;ray<8;ray++){
                float angle=ray*MathHelper.TwoPi/8;var direction=new Vector2(MathF.Cos(angle),MathF.Sin(angle));
                _ui.Tile(center+direction*spread,new(14,4),angle,new Color(255,194,75)*(.45f+pulse*.55f));
            }
            _ui.Tile(center,new(17,17),MathHelper.PiOver4,new Color(255,246,199));
            _ui.Tile(center+tangent*4,new(5,26),MathF.Atan2(incoming.Y,incoming.X),Color.White);
        }
        DrawPersuasionEffects(battle,frame,left,top,size);
        // Draw counters after attack/shield effects so the remaining supplies stay readable.
        foreach(var unit in frame.Units)
            DrawBattleSupplies(unit,left+unit.Position.X*size+(size-72)/2,top+(unit.Position.Y+1)*size-12,BattlePieceOpacity);
        DrawBattleNewspaper(battle,frame);
        string result=$"全 {_robotPlayback.Rounds[_robotRound].Length} 戦場で１戦闘ターンを同時進行 / 結果は最後に表示";
        var recent=battle.Events.LastOrDefault(e=>e.Turn==frame.Turn && e.Phase==frame.Phase && e.Unit!=0);
        if(battle.Kind==BattleKind.Node && !battle.SplitBoard)DrawBattleFlagCountdown(battle,frame);
        else if(recent!=null)BattleInfo($"ID {recent.Unit} / {recent.Detail}",794,.42f);
        if(battle.Kind==BattleKind.Node && !battle.SplitBoard){
            BattleInfo(frame.Units.Any(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated)?$"説得：上下左右{battle.Rules.PersuasionRadius}マス以内 / {battle.Rules.PersuasionPercent}％ / 壁越し有効":"足元は左から盾・短剣・弾の残数",872,.4f);
        }
        else if(frame.Units.Any(u=>u.Role==BattleRole.Queen && !u.Fallen && !u.Retreated))
        {
            BattleInfo($"説得：上下左右に{battle.Rules.PersuasionRadius}マス以内 / １体ごと{battle.Rules.PersuasionPercent}％",833,.44f);
            BattleInfo("距離による低下なし・壁越しも有効 / 説得中は範囲を色枠で表示",872,.4f);
        }
        else
        {
            BattleInfo(result,833,.48f);
            BattleInfo("足元は左から盾・短剣・弾の残数 / 再生速度は戦闘結果に影響しません",872,.41f);
        }
        bool end=_battleAge>=RobotRoundEnd;
        _ui.Button(RobotPause,_robotPlaying?"一時停止":"再生",Muted,.5f);
        _ui.Button(RobotStepBack,"１フェーズ戻る",Muted,.46f);_ui.Button(RobotStepNext,"１フェーズ進む",Muted,.46f);
        BattleInfo($"再生 {frameIndex+1}/{battle.Frames.Count} / 王冠：クイーン 高帽子：隊長 低帽子：兵",901,.42f);
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
    private void BattleInfo(string text,int y,float scale)
        =>_ui.Text(text,new(1120,y),Math.Min(scale,720/_font.MeasureString(text).X),Cream);
    private void DrawTransport()
    {
        DrawPropagation(_robotScenes.Length>0?_battleWave:-1,PropagationComplete?.6f+.4f*(float)(_transportAge/2):.6f*_propagationTravel.Values.DefaultIfEmpty(0).Average(),true);
    }
}

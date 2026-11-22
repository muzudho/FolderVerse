namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
public partial class Game1
{
    private bool _conquestOverviewEnabled=true;
    private int _conquestOverviewTurn=-1,_conquestStage,_conquestCard;
    private double _conquestAge;
    private bool _conquestOverview;
    private float _conquestExitOpacity;
    private ConquestTally _conquestTally;
    private int _conquestPainted;
    private ConquestPresentation _conquestPresentation;
    private WorldPreview _conquestGlobe;
    private ConquestMusic _conquestMusic;
    private SoundEffect _curtainSound;
    private bool _conquestAudioUnavailable;
    private Vector3 _conquestEnvironment;
    private Quaternion _conquestFrom=Quaternion.Identity,_conquestTo=Quaternion.Identity;
    private static Rectangle ConquestGlobeArea{get{var r=ConquestMapSpace;return new(r.X+8,r.Y+28,r.Width-16,r.Height-36);}}
    private static Rectangle ConquestSkipButton=>new(1680,1018,190,48);
    private ConquestPresentation.Card ConquestCard=>_conquestPresentation.Cards[_conquestCard];
    private double ConquestStudioEnd=>Math.Max(_conquestTally.FaceStart,_conquestTally.CountEnd);
    private bool ConquestDetailedCard=>!ConquestCard.Reminder && ConquestBroadcast.Detailed(_setup,ConquestCard.Ruler,ConquestCard.Rank);
    private double ConquestCardSeconds=>ConquestCard.Reminder?.8:
        (ConquestDetailedCard?Math.Max(_conquestTally.Duration,ConquestStudioEnd+4):_conquestTally.Duration)+(_conquestBroadcast.Changed(ConquestCard.Ruler)?4:0);
    private ConquestBroadcast _conquestBroadcast;
    private bool _conquestRelations;
    private bool ConquestStudioCut=>_conquestStage==4 && !ConquestCard.Reminder &&
        ConquestDetailedCard && _conquestAge<ConquestStudioEnd;
    private void BeginConquestOverview(){
        if(_setup.TutorialStep is 1 or 2 or 3 or 4 || !_conquestOverviewEnabled || _conquestOverviewTurn==_setup.Population.Turn)return;
        _conquestOverviewTurn=_setup.Population.Turn;_conquestPresentation=new(_setup);_conquestCard=0;_conquestStage=0;_conquestAge=0;_conquestOverview=true;_conquestExitOpacity=1;
        _conquestRevealed.Clear();_conquestRelations=false;_conquestBroadcast=new(_setup);
        var ranking=_setup.Relations.ProvisionalRanking(_setup).OrderBy(r=>r.Ruler).ToArray();
        var contenders=ranking.Where(r=>_setup.Relations.CanWinConquest(r.Ruler)).ToArray();
        _conquestTiles=contenders.Length==0?Array.Empty<OverviewTile>():
            CharacterSelectionLayout.CreateOverview(contenders.Select(r=>r.Cells).ToArray(),contenders.Select(r=>r.Rank).ToArray())
                .Select(t=>t with {Slot=contenders[t.Slot].Ruler}).ToArray();
        _conquestGlobe??=new WorldPreview(GraphicsDevice);_conquestGlobe.ManualOrientation=null;_conquestGlobe.GlobeAnchor=null;_conquestGlobe.GlobeZoom=1;
        _conquestFrom=Quaternion.CreateFromRotationMatrix(WorldPreview.Orientation(0,0));SetConquestCard();PlayCurtain();
    }
    private void PlayCurtain(){
        if(_conquestAudioUnavailable)return;
        try{_curtainSound??=ConquestMusic.Curtain();_curtainSound.Play(.55f*SeGain,0,0);}
        catch(NoAudioHardwareException e){_conquestAudioUnavailable=true;RecordFailure(e,"conquest_audio");}
    }
    private void SetConquestCard(){
        _conquestTally=new(_setup,ConquestCard,!ConquestCard.Reminder && !ConquestBroadcast.Detailed(_setup,ConquestCard.Ruler,ConquestCard.Rank));
        if(!_conquestTally.Compact)_conquestEnvironment=ConquestPresentation.Environment(_setup,ConquestCard.Ruler);
        _conquestPainted=_conquestTally.Compact?_conquestTally.Cells.Length:0;
        if(_conquestTally.Compact)_conquestRevealed.Add(ConquestCard.Ruler);
        _conquestGlobe.PresentationCells=ConquestCard.Reminder?null:_conquestTally.Cells.Take(_conquestPainted).ToHashSet();
        _conquestGlobe.PresentationOwner=ConquestCard.Ruler;_conquestGlobe.ShowLandConquest(_setup,true);
        _conquestFrom=_conquestGlobe.ManualOrientation??_conquestFrom;
        if(_conquestTally.Compact && ConquestCard.Faces.Length>0)_conquestGlobe.ManualOrientation=ConquestFace(_setup.Cells.First(c=>c.Face==ConquestCard.Faces[0]).Normal);
    }
    private static Quaternion ConquestFace(Vector3 normal){
        float pitch=MathF.Atan2(normal.Y,MathF.Sqrt(normal.X*normal.X+normal.Z*normal.Z));
        var v=Vector3.Transform(normal,Matrix.CreateRotationX(pitch));
        return Quaternion.CreateFromRotationMatrix(WorldPreview.Orientation(-MathF.Atan2(v.X,v.Z),pitch));
    }
    private void FinishConquestOverview(){
        _conquestOverview=false;_conquestMusic?.Dispose();_conquestMusic=null;
    }
    private void UpdateConquestOverview(double elapsed,bool skip){
        if(_conquestRelations)return;
        if(skip && _conquestStage<5){_conquestExitOpacity=_conquestStage==3?(float)Math.Clamp(_conquestAge/1.5,0,1):_conquestStage==4?1:0;_conquestStage=5;_conquestAge=0;}
        _conquestAge+=Math.Min(elapsed,.25);
        double duration=_conquestStage switch{0 or 2 or 6 or 8=>.65,1 or 3 or 5 or 7=>1.5,4=>ConquestCardSeconds,_=>0};
        if(_conquestStage==4){
            if(!ConquestCard.Reminder){
                if(_conquestAge>=_conquestTally.FaceStart)_conquestRevealed.Add(ConquestCard.Ruler);
                int painted=_conquestTally.Painted(_setup,_conquestAge);
                if(painted!=_conquestPainted){_conquestPainted=painted;_conquestGlobe.PresentationCells=_conquestTally.Cells.Take(painted).ToHashSet();_conquestGlobe.ShowLandConquest(_setup,true);}
            }
            var faces=ConquestCard.Faces;
            if(faces.Length>0){
                int index=0;
                if(!ConquestCard.Reminder && !_conquestTally.Compact)for(int i=1;i<faces.Length;i++)if(_conquestAge>=_conquestTally.FaceStarts[i])index=i;
                double faceAge=ConquestCard.Reminder?_conquestAge:_conquestAge-_conquestTally.FaceStarts[index];
                _conquestTo=ConquestFace(_setup.Cells.First(c=>c.Face==faces[index]).Normal);
                var from=index==0?_conquestFrom:ConquestFace(_setup.Cells.First(c=>c.Face==faces[index-1]).Normal);
                float blend=_conquestTally.Compact?1:MathHelper.SmoothStep(0,1,(float)Math.Min(1,faceAge/(ConquestCard.Reminder?.7:.9)));
                float turn=(float)Math.Sin(_conquestAge*.65)*.20f+.12f;
                _conquestGlobe.ManualOrientation=Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(Quaternion.Slerp(from,_conquestTo,blend))*Matrix.CreateRotationY(turn)*Matrix.CreateRotationX(.12f));
            }
        }
        while(_conquestAge>=duration){
            // Carry sub-frame time across short cards so their combined duration stays five seconds.
            _conquestAge-=duration;
            if(_conquestStage==4 && ++_conquestCard<_conquestPresentation.Cards.Length)SetConquestCard();
            else{_conquestAge=0;_conquestStage++;if(_conquestStage==5){_conquestCard=Math.Min(_conquestCard,_conquestPresentation.Cards.Length-1);}
                if(_conquestStage is 2 or 6 or 8)PlayCurtain();if(_conquestStage>8){FinishConquestOverview();return;}}
            duration=_conquestStage switch{0 or 2 or 6 or 8=>.65,1 or 3 or 5 or 7=>1.5,4=>ConquestCardSeconds,_=>0};
        }
        if(_conquestStage is >=2 and <=6 && !_conquestAudioUnavailable && (_conquestStage<5 || _conquestMusic!=null)){
            try{if(_conquestMusic==null){_conquestMusic=new();_conquestMusic.SetGain(BgmGain);}var env=_conquestEnvironment;
                float volume=_conquestStage==5?(float)(1-_conquestAge/1.5):_conquestStage==6?0:Math.Min(1,(float)(_conquestStage==2?_conquestAge/.65:1));
                _conquestMusic.Update(env,.36f*volume);
            }catch(NoAudioHardwareException e){_conquestAudioUnavailable=true;RecordFailure(e,"conquest_music");}
        }
    }
    private void DrawConquestOverview(Rectangle canvas,Matrix transform,Viewport viewport){
        GraphicsDevice.Clear(new Color(34,56,67));
        if(!_conquestRelations && !ConquestStudioCut)EnsureConquestFocusedBackdrop();
        _spriteBatch.Begin(transformMatrix:transform);
        if(_conquestRelations){DrawHierarchyUi();_spriteBatch.End();return;}
        DrawConquestBroadcastBackground();
        if(!ConquestStudioCut)DrawConquestMosaic();
        _spriteBatch.End();
        if(ConquestStudioCut){
            GraphicsDevice.Viewport=OrientationViewport(ConquestGlobeArea,canvas);
            _conquestGlobe.Draw(0,0,0,false,true);GraphicsDevice.Viewport=viewport;
        }
        _spriteBatch.Begin(transformMatrix:transform);
        if(ConquestStudioCut)DrawConquestTally();
        DrawConquestBroadcastHost();
        _ui.Text("世界征服状況フェーズ / 朝の放送",new(30,24),.57f,Cream);
        _ui.Box(new(1530,95,350,45),new Color(23,42,48)*.85f);_ui.Center($"{TurnNewspaper.DayLabel(_setup.Population.Turn)} / 全体ターン {_setup.Population.Turn}",new(1530,95,350,45),.38f,Cream);
        _ui.Button(ConquestRelationsButton,"所属関係図",Muted,.46f);
        _ui.Text("Enter / Esc : 朝刊へ",new(1230,1028),.35f,Cream);
        if(!_demoActive && !ComputerExhibition && _conquestStage<5)_ui.Button(ConquestSkipButton,"スキップ",Accent,.5f);
        if(_conquestStage==4)DrawPhaseTimer(new(700,1000,1150,8),_conquestAge,ConquestCardSeconds);
        _spriteBatch.End();
    }
    private void DrawConquestTransition(){
        float progress=MathHelper.SmoothStep(0,1,(float)(_conquestAge/.65));
        float cover=_conquestStage switch{0 or 6=>progress,1 or 7=>1,2 or 8=>1-progress,_=>0};
        DrawConquestCurtain(cover);
    }
    private void DrawConquestCurtain(float cover){
        int width=(int)(960*cover);
        for(int side=0;side<2;side++)for(int x=0;x<width;x+=8){
            float fold=.55f+.45f*MathF.Pow((MathF.Cos(x*.065f)+1)*.5f,2);var color=new Color((int)(145*fold),(int)(15*fold),(int)(32*fold));
            _ui.Box(new(side==0?x:1920-x-8,0,Math.Min(8,width-x),1080),color);
        }
    }
}

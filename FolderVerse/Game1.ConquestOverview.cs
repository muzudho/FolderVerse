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
    private double ConquestCardSeconds=>ConquestCard.Reminder?.8:_conquestTally.Duration;
    private void BeginConquestOverview(){
        if(!_conquestOverviewEnabled || _autoPlay || _conquestOverviewTurn==_setup.Population.Turn)return;
        _conquestOverviewTurn=_setup.Population.Turn;_conquestPresentation=new(_setup);_conquestCard=0;_conquestStage=0;_conquestAge=0;_conquestOverview=true;_conquestExitOpacity=1;
        _conquestRevealed.Clear();
        var ranking=_setup.Relations.ProvisionalRanking(_setup).OrderBy(r=>r.Ruler).ToArray();
        _conquestTiles=CharacterSelectionLayout.CreateOverview(ranking.Select(r=>r.Cells).ToArray(),ranking.Select(r=>r.Rank).ToArray());
        _conquestGlobe??=new WorldPreview(GraphicsDevice);_conquestGlobe.ManualOrientation=null;_conquestGlobe.GlobeAnchor=null;_conquestGlobe.GlobeZoom=1;
        _conquestFrom=Quaternion.CreateFromRotationMatrix(WorldPreview.Orientation(0,0));SetConquestCard();PlayCurtain();
    }
    private void PlayCurtain(){
        if(_conquestAudioUnavailable)return;
        try{_curtainSound??=ConquestMusic.Curtain();_curtainSound.Play(.55f,0,0);}
        catch(NoAudioHardwareException e){_conquestAudioUnavailable=true;RecordFailure(e,"conquest_audio");}
    }
    private void SetConquestCard(){
        _conquestTally=new(_setup,ConquestCard,!ConquestCard.Reminder && !ConquestPresentation.Detailed(_setup,ConquestCard.Ruler,ConquestCard.Rank));
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
            try{_conquestMusic??=new();var env=_conquestEnvironment;
                float volume=_conquestStage==5?(float)(1-_conquestAge/1.5):_conquestStage==6?0:Math.Min(1,(float)(_conquestStage==2?_conquestAge/.65:1));
                _conquestMusic.Update(env,.36f*volume);
            }catch(NoAudioHardwareException e){_conquestAudioUnavailable=true;RecordFailure(e,"conquest_music");}
        }
    }
    private void DrawConquestOverview(Rectangle canvas,Matrix transform,Viewport viewport){
        GraphicsDevice.Clear(Color.Black);
        if(_conquestStage is 3 or 4 or 5){_spriteBatch.Begin(transformMatrix:transform);DrawConquestMosaic();_spriteBatch.End();}
        if(_conquestStage is 3 or 4 || _conquestStage==5 && _conquestExitOpacity>0){
            GraphicsDevice.Viewport=OrientationViewport(ConquestGlobeArea,canvas);_conquestGlobe.Draw(0,0,0,false,true);GraphicsDevice.Viewport=viewport;
            DrawConquestOrientation(canvas,transform,viewport);
        }
        _spriteBatch.Begin(transformMatrix:transform);
        if(_conquestStage==4){
            int ruler=ConquestCard.Ruler;var color=_setup.OwnerColor(ruler);
            foreach(var node in _setup.Nodes.All.Where(n=>n.Owner==ruler)){
                var cell=_setup.Cells[node.Cell];if(!_conquestGlobe.ProjectVisible(_setup.Routes.Position(node.Cell,node.Center),cell.Normal,0,0,ConquestGlobeArea,out var pos,.03f))continue;
                _ui.Box(new((int)pos.X-6,(int)pos.Y-6,12,12),Color.Black*.7f);_ui.Box(new((int)pos.X-5,(int)pos.Y-5,10,10),Color.White);_ui.Box(new((int)pos.X-3,(int)pos.Y-3,6,6),color);
            }
            if(ruler<_setup.ConquerorLocations.Length){
                int location=_setup.ConquerorLocations[ruler];var cell=_setup.Cells[location];
                if(_conquestGlobe.ProjectVisible(_setup.Routes.Position(location,_setup.ConquerorPoints[ruler]),cell.Normal,0,0,ConquestGlobeArea,out var at,.055f)){
                    _ui.Tile(at,new(21,21),MathHelper.PiOver4,Color.Black*.7f);
                    _ui.Tile(at,new(16,16),MathHelper.PiOver4,new Color(255,229,120));
                    _ui.Box(new((int)at.X+12,(int)at.Y-20,94,34),Color.Black*.65f);
                    _ui.Text("現在地",at+new Vector2(18,-16),.32f,Cream);
                }
            }

        }
        if(_conquestStage is 3 or 4 or 5)DrawConquestTally();
        if(_conquestStage is 3 or 5)_ui.Box(new(0,0,1920,1080),Color.Black*(float)Math.Clamp(_conquestStage==3?1-_conquestAge/1.5:1-_conquestExitOpacity*(1-_conquestAge/1.5),0,1));
        _ui.Text("世界征服状況フェーズ",new(55,35),.7f,Cream);
        _ui.Center($"全体ターン {_setup.Population.Turn} / 征服者 {_setup.ActiveCount}人 / 敵 {Enumerable.Range(0,_setup.ActiveCount).Count(r=>!_setup.Relations.Allied(r,_setup.PlayerSlot))}人",new(580,35,950,45),.5f,Cream);
        _ui.Text("Enter / Esc : 内政へ",new(1260,1020),.4f,Cream);
        if(!_demoActive && _conquestStage<5)_ui.Button(ConquestSkipButton,"スキップ",Accent,.55f);
        if(_conquestStage<3 || _conquestStage>=6){
            float cover=_conquestStage switch{0=>(float)(_conquestAge/.65),1 or 7=>1,2 or 8=>1-(float)(_conquestAge/.65),6=>(float)(_conquestAge/.65),_=>0};
            DrawConquestCurtain(MathHelper.SmoothStep(0,1,Math.Clamp(cover,0,1)));
        }
        _spriteBatch.End();
    }
    private void DrawConquestCurtain(float cover){
        int width=(int)(960*cover);
        for(int side=0;side<2;side++)for(int x=0;x<width;x+=8){
            float fold=.55f+.45f*MathF.Pow((MathF.Cos(x*.065f)+1)*.5f,2);var color=new Color((int)(145*fold),(int)(15*fold),(int)(32*fold));
            _ui.Box(new(side==0?x:1920-x-8,0,Math.Min(8,width-x),1080),color);
        }
    }
}

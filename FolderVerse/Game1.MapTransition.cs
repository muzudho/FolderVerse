namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private const float MapTransitionDuration=3.6f;
    private float _mapTransitionAge=-1;
    private bool _mapUnfold;
    private Quaternion _mapOrientation;
    private BasicEffect _mapEffect;
    private SoundEffect _mapCrack;
    private int _mapSoundStep=-1;
    private bool MapTransitionActive=>_mapTransitionAge>=0;
    private void StartMapTransition()
    {
        if(MapTransitionActive)return;
        _mapDragListOpen=false;
        bool unfold=_statusGlobe;
        _net.CancelAnimation();
        if(unfold)
        {
            int selected=HitGlobeCell(new Point(StatusGlobeArea.Center.X,StatusGlobeArea.Center.Y));
            if(selected<0)selected=_setup.Cells.OrderByDescending(c=>Vector3.Transform(c.Center,_world.DisplayOrientation(_yaw,_pitch)).Z).First().Id;
            _statusCell=selected;_net.SetCenter(_setup,_setup.Cells[selected].Face);_net.ResetView();
            _net.Drag(Vector2.TransformNormal(-_net.Faces[0].Project(_setup.Cells[selected].Center)*_net.Fit(NetPanel).Scale,Matrix.CreateRotationZ(_net.ScreenAngle)));
        }
        if(unfold && DestinationOperation){_statusGlobe=false;FitDestinationView();_statusGlobe=true;}
        BeginMapTransitionAnimation(_net,_world.DisplayOrientation(_yaw,_pitch),unfold);
    }
    private void BeginMapTransitionAnimation(CubeNet net,Matrix globeOrientation,bool unfold)
    {
        _mapUnfold=unfold;
        var face=net.Faces[0];var normal=CubeNet.Normals[face.Face];
        var basis=new Matrix(face.Right.X,face.Right.Y,face.Right.Z,0,face.Up.X,face.Up.Y,face.Up.Z,0,normal.X,normal.Y,normal.Z,0,0,0,0,1);
        _mapOrientation=Quaternion.CreateFromRotationMatrix(basis*globeOrientation);
        _mapTransitionAge=0;_mapSoundStep=-1;_dragging=_mapDragging=false;_globePressedCell=-1;
        _mapEffect??=new BasicEffect(GraphicsDevice){VertexColorEnabled=true};
        if(_mapCrack==null)
        {
            try
            {
                const int rate=22050;var bytes=new byte[rate/5*2];var random=new Random(31415);
                for(int i=0;i<bytes.Length/2;i++)
                {
                    float t=i/(float)(bytes.Length/2);short sample=(short)((random.NextDouble()*2-1)*15000*Math.Pow(1-t,4));
                    bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
                }
                _mapCrack=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            catch(NoAudioHardwareException error){RecordFailure(error,"map_audio_init");}
        }
        UpdateMapTransition(0);
    }
    private void UpdateMapTransition(float elapsed)
    {
        if(!MapTransitionActive)return;
        _mapTransitionAge+=elapsed;
        float progress=MathHelper.Clamp(_mapTransitionAge/MapTransitionDuration,0,1);
        int step=_mapUnfold?Math.Min(4,(int)(progress*6)):(int)(progress*6)-1;
        if(step>=0 && step<5 && step!=_mapSoundStep)
        {
            _mapSoundStep=step;
            try{_mapCrack?.Play(.65f*SeGain,0,0);}catch(NoAudioHardwareException error){RecordFailure(error,"map_audio_play");}
        }
        if(progress<1)return;
        if(_screen==Screen.OpeningReview)_openingInspectionUnfolded=_mapUnfold;
        else _statusGlobe=!_mapUnfold;
        _mapTransitionAge=-1;_inputOutcome="view_changed";
    }
    private void DrawMapTransition()=>DrawMapHinges(_net,NetPanel,StatusGlobeArea);
}

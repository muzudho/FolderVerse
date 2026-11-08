namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

public partial class Game1
{
    private SoundEffect _shipmentChime,_seaWave;
    private bool _presentationAudioUnavailable;
    private double _seaPresentationAge,_seaSoundAge;

    private void PlayPresentationSound(bool sea)
    {
        if(_presentationAudioUnavailable)return;
        try{
            ref SoundEffect sound=ref (sea?ref _seaWave:ref _shipmentChime);
            if(sound==null){
                const int rate=22050;
                double duration=sea?2.4:.65;
                var bytes=new byte[(int)(rate*duration)*2];
                var random=new Random(3187);double filtered=0;
                for(int i=0;i<bytes.Length/2;i++){
                    double t=(double)i/rate,value;
                    if(sea){
                        filtered=.94*filtered+.06*(random.NextDouble()*2-1);
                        value=filtered*Math.Pow(Math.Sin(Math.PI*t/duration),2)*.7;
                    }else{
                        value=0;
                        for(int note=0;note<3;note++){
                            double age=t-note*.12;
                            if(age>=0)value+=Math.Sin(2*Math.PI*new[]{523.25,659.25,783.99}[note]*age)*Math.Exp(-age*8)*Math.Min(1,age*150)*.17;
                        }
                    }
                    short sample=(short)(Math.Clamp(value,-1,1)*short.MaxValue);
                    bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
                }
                sound=new SoundEffect(bytes,rate,AudioChannels.Mono);
            }
            sound.Play(sea?.45f:.55f,0,0);
        }catch(NoAudioHardwareException error){_presentationAudioUnavailable=true;RecordFailure(error,"presentation_audio");}
    }

    // Presentation clock only: no battle state, path or random generator is changed.
    private void UpdateSeaPresentation(double elapsed)
    {
        if(_robotSummary || _robotScenes.Length==0 || _robotScenes[_battleWave].Terrain!=BattleTerrain.Sea){_seaSoundAge=0;return;}
        if(!_robotPlaying)return;
        _seaPresentationAge+=elapsed;_seaSoundAge+=elapsed;
        if(_seaSoundAge>=5.6){_seaSoundAge%=5.6;PlayPresentationSound(true);}
    }
    private Matrix SeaBoardTransform(BattleState battle)
    {
        if(battle.Terrain!=BattleTerrain.Sea)return Matrix.Identity;
        float x=1-.024f*(1+(float)Math.Sin(_seaPresentationAge*1.12));
        float y=1-.018f*(1+(float)Math.Sin(_seaPresentationAge*.83+1.2));
        return Matrix.CreateTranslation(-540,-540,0)*Matrix.CreateScale(x,y,1)*Matrix.CreateTranslation(540,540,0);
    }
}

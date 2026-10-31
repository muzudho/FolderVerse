namespace FolderVerse;
using System;
using Microsoft.Xna.Framework.Audio;

// Original ten-second curtain-call: bright bells, bouncing bass, then a held C-major cadence.
public sealed class EndingMusicScore
{
    public const int Rate=22050;
    private long _sample;
    private static readonly int[] Melody={12,16,19,24,23,19,16,19,17,21,24,26,24,21,17,14,19,23,26,28,26,23,19,14};
    private static readonly int[] Roots={0,5,7,0};
    private static double Tone(int note,double age,double decay)
    {
        double phase=Math.Tau*130.81278265*Math.Pow(2,note/12d)*age;
        return (Math.Sin(phase)+.2*Math.Sin(phase*2))*Math.Min(1,age*120)*Math.Exp(-age*decay);
    }
    public byte[] Render(int count=2205)
    {
        if(count<1)throw new ArgumentOutOfRangeException(nameof(count));
        var data=new byte[checked(count*2)];
        for(int i=0;i<count;i++,_sample++){
            double t=_sample/(double)Rate,beat=t/.3,age=t% .3,v=0;
            if(t<7.2){
                int step=(int)beat,root=Roots[Math.Min(2,step/8)];
                v=.22*Tone(Melody[step],age,8)+.17*Tone(root-12+(step%2==0?0:7),age,10);
                if(step%2==0)v+=.055*(Tone(root,age,7)+Tone(root+4,age,7)+Tone(root+7,age,7));
            }else if(t<10){
                double end=t-7.2;
                v=.15*(Tone(0,end,1.1)+Tone(4,end,1.1)+Tone(7,end,1.1)+Tone(24,end,1.5))+.2*Tone(-12,end,1.4);
            }
            short sample=(short)(Math.Tanh(v)*Math.Clamp((10-t)/.4,0,1)*short.MaxValue*.8);
            data[i*2]=(byte)sample;data[i*2+1]=(byte)(sample>>8);
        }
        return data;
    }
}
public sealed class EndingMusic:IDisposable
{
    private readonly DynamicSoundEffectInstance _stream=new(EndingMusicScore.Rate,AudioChannels.Mono);
    private readonly EndingMusicScore _score=new();
    public void Update(bool active)
    {
        if(!active){if(_stream.State==SoundState.Playing)_stream.Pause();return;}
        _stream.Volume=.45f;
        while(_stream.PendingBufferCount<3)_stream.SubmitBuffer(_score.Render());
        if(_stream.State==SoundState.Paused)_stream.Resume();else if(_stream.State!=SoundState.Playing)_stream.Play();
    }
    public void Dispose(){_stream.Stop();_stream.Dispose();}
}

namespace FolderVerse;
using System;
using Microsoft.Xna.Framework.Audio;

// Original upbeat toy-band score: bouncing bass, offbeat chords and rising bell arpeggios.
public sealed class IndustryMusicScore
{
    public const int Rate=22050,Bpm=132;
    public const int LoopBeats=32;
    private long _sample;
    private uint _noise=731;
    private static readonly int[] Roots={0,9,5,7},Arpeggio={0,1,2,3,2,1,2,3,1,2,3,4,3,2,1,2};
    public long CompletedLoops=> (long)(_sample/(double)Rate*Bpm/60/LoopBeats);
    private static double Note(double hz,double age,double decay)=>
        Math.Sin(Math.Tau*hz*age)*Math.Min(1,age*150)*Math.Exp(-age*decay);
    public byte[] Render(int count=2205)
    {
        if(count<1)throw new ArgumentOutOfRangeException(nameof(count));
        var bytes=new byte[checked(count*2)];double beatSeconds=60d/Bpm;
        for(int i=0;i<count;i++,_sample++){
            double t=_sample/(double)Rate,beat=t/beatSeconds;
            int chord=(int)(beat/8)%4,tick=(int)(beat*2),bassBeat=(int)beat;
            double root=130.8128*Math.Pow(2,Roots[chord]/12d),third=chord==1?3:4;
            int arpeggio=Arpeggio[tick%Arpeggio.Length];double interval=arpeggio switch{0=>0,1=>third,2=>7,3=>12,_=>third+12};
            double eighthAge=(beat*2-Math.Floor(beat*2))*beatSeconds/2;
            double quarterAge=(beat-Math.Floor(beat))*beatSeconds;
            double bell=root*4*Math.Pow(2,interval/12);
            double v=(Note(bell,eighthAge,14)+.25*Note(bell*2,eighthAge,22))*.17;
            v+=Note(root/2*Math.Pow(2,bassBeat%2==0?0:7d/12),quarterAge,9)*.21;
            if(tick%2==1)v+=(Note(root,eighthAge,18)+Note(root*Math.Pow(2,third/12),eighthAge,18)+Note(root*1.498307,eighthAge,18))*.045;
            _noise=unchecked(_noise*1664525+1013904223);double noise=(_noise>>8)/8388608d-1;
            v+=noise*Math.Exp(-eighthAge*85)*Math.Min(1,eighthAge*500)*.027;
            if(bassBeat%2==0)v+=Math.Sin(Math.Tau*65*quarterAge)*Math.Exp(-quarterAge*30)*.10;
            double gate=Math.Min(1,(beatSeconds/2-eighthAge)*180);
            short value=(short)(Math.Tanh(v*gate)*Math.Min(1,t/.15)*short.MaxValue*.85);
            bytes[i*2]=(byte)value;bytes[i*2+1]=(byte)(value>>8);
        }
        return bytes;
    }
}
public sealed class IndustryMusic:IDisposable
{
    private readonly DynamicSoundEffectInstance _stream=new(IndustryMusicScore.Rate,AudioChannels.Mono);
    private readonly IndustryMusicScore _score=new();
    public void Update(){
        _stream.Volume=.42f;
        while(_stream.PendingBufferCount<3)_stream.SubmitBuffer(_score.Render());
        if(_stream.State==SoundState.Paused)_stream.Resume();else if(_stream.State!=SoundState.Playing)_stream.Play();
    }
    public void Dispose(){_stream.Stop();_stream.Dispose();}
}

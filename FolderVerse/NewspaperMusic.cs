namespace FolderVerse;
using System;
using Microsoft.Xna.Framework.Audio;
public enum NewspaperMusicMood { BusyMorning,CalmDay,ElegantAfternoon,SleepingNight }
public sealed class NewspaperMusicScore
{
    public const int Rate=22050;
    public const double LoopBeats=96;
    public long CompletedLoops {get;private set;}
    public static readonly string[] Labels={"忙しい朝","穏やかな昼下がり","優雅な午後","寝静まった夜"};
    private readonly double[] _mix=new double[4];
    private double _beat,_tempo;private long _sample;
    public NewspaperMusicMood Mood {get;set;}
    public NewspaperMusicScore(NewspaperMusicMood mood){Mood=mood;_mix[(int)mood]=1;_tempo=Tempo(mood);}
    private static int Tempo(NewspaperMusicMood mood)=>mood switch{NewspaperMusicMood.BusyMorning=>116,NewspaperMusicMood.CalmDay=>76,NewspaperMusicMood.ElegantAfternoon=>92,_=>54};
    private static readonly int[] Melody={4,7,11,9,7,4,2,0,2,4,7,12,11,7,4,2},Roots={0,9,2,7},Triad={0,4,7};
    private static double Pluck(double frequency,double t,double phase,double decay)=>
        (Math.Sin(Math.Tau*frequency*t)+.25*Math.Sin(Math.Tau*frequency*2*t))*Math.Min(1,phase*50)*Math.Exp(-phase*decay)*Math.Min(1,(1-phase)*25);
    private static double Voice(NewspaperMusicMood mood,double t,double beat){
        double meter=mood==NewspaperMusicMood.ElegantAfternoon?3:4;
        int bar=(int)(beat/meter),root=Roots[bar%4];double bass=130.8128*Math.Pow(2,root/12d);
        double pace=mood==NewspaperMusicMood.BusyMorning?2:mood==NewspaperMusicMood.SleepingNight?.5:1;
        double note=beat*pace,phase=note-Math.Floor(note);int degree=Melody[(int)note%Melody.Length];
        double hz=261.6256*Math.Pow(2,degree/12d),quarter=beat-Math.Floor(beat);
        double v=Pluck(hz,t,phase,mood==NewspaperMusicMood.SleepingNight?2:5)*.15;
        v+=Pluck(bass/2,t,quarter,3)*.09;
        double pad=Math.Sin(Math.PI*(beat%meter)/meter);
        v+=(Math.Sin(Math.Tau*bass*t)+Math.Sin(Math.Tau*bass*1.259921*t)+Math.Sin(Math.Tau*bass*1.887749*t))*.022*pad;
        if(mood==NewspaperMusicMood.BusyMorning)v+=Pluck(hz*2,t,phase,9)*.04+(Math.Sin(t*8341)+Math.Sin(t*11931))*Math.Exp(-phase*50)*.012;
        if(mood==NewspaperMusicMood.CalmDay)v+=Pluck(hz/2,t,phase,2)*.045;
        if(mood==NewspaperMusicMood.ElegantAfternoon)v+=Pluck(bass*Math.Pow(2,Triad[(int)beat%3]/12d),t,quarter,4)*.09;
        if(mood==NewspaperMusicMood.SleepingNight)v=v*.62+Pluck(hz*2,t,phase,2)*.035;
        return v;
    }
    public byte[] Render(int count=2205){
        if((int)Mood<0 || (int)Mood>3 || count<1)throw new ArgumentOutOfRangeException();
        var bytes=new byte[count*2];
        for(int i=0;i<count;i++,_sample++){
            double value=0,tempo=0,t=_sample/(double)Rate;
            for(int m=0;m<4;m++){_mix[m]+=(((int)Mood==m?1d:0)-_mix[m])/(Rate*.6);tempo+=_mix[m]*Tempo((NewspaperMusicMood)m);if(_mix[m]>.00001)value+=_mix[m]*Voice((NewspaperMusicMood)m,t,_beat);}
            _tempo+=(tempo-_tempo)/(Rate*.3);_beat+=_tempo/(60*Rate);
            if(_beat>=LoopBeats){_beat-=LoopBeats;CompletedLoops++;}
            short sample=(short)(Math.Tanh(value)*Math.Min(1,t/.7)*short.MaxValue*.85);bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
        }return bytes;
    }
}
public sealed class NewspaperMusic:IDisposable
{
    private readonly DynamicSoundEffectInstance _stream=new(NewspaperMusicScore.Rate,AudioChannels.Mono);
    private readonly NewspaperMusicScore _score;
    public NewspaperMusic(NewspaperMusicMood mood){_score=new(mood);}
    public void Update(NewspaperMusicMood mood,bool active){
        _score.Mood=mood;_stream.Volume=.42f;
        if(!active){if(_stream.State==SoundState.Playing)_stream.Pause();return;}
        while(_stream.PendingBufferCount<2)_stream.SubmitBuffer(_score.Render());
        if(_stream.State==SoundState.Paused)_stream.Resume();else if(_stream.State!=SoundState.Playing)_stream.Play();
    }
    public void Dispose(){_stream.Stop();_stream.Dispose();}
}

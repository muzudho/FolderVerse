namespace FolderVerse;
using System;
using Microsoft.Xna.Framework.Audio;

// Original toy-orchestra theme. All arrangements share a beat clock and motif;
// sample-level mixing lets the situation change without restarting a song.
public sealed class BattleMusicScore
{
    public const int Rate=22050;
    private readonly double[] _weights=new double[12];
    private long _sample;
    private double _beat,_bpm;
    public BattleMusicMood Mood {get;private set;}
    public double Tempo=>_bpm;
    public static int TempoFor(BattleMusicMood mood)=>mood switch{
        BattleMusicMood.Defeat=>60,BattleMusicMood.Losing=>72,BattleMusicMood.Disadvantage=>88,
        BattleMusicMood.Balanced=>104,BattleMusicMood.Advantage=>120,BattleMusicMood.Winning=>138,
        BattleMusicMood.Victory=>148,BattleMusicMood.Comical=>128,BattleMusicMood.Close=>156,
        BattleMusicMood.Decisive=>96,BattleMusicMood.Suspense=>112,_=>140};
    public BattleMusicScore(BattleMusicMood mood=BattleMusicMood.Balanced){SetMood(mood);_weights[(int)mood-1]=1;_bpm=TempoFor(mood);}
    public void SetMood(BattleMusicMood mood){if((int)mood<1 || (int)mood>12)throw new ArgumentOutOfRangeException(nameof(mood));Mood=mood;}
    private static readonly int[] Major={0,2,4,5,7,9,11},Minor={0,2,3,5,7,8,10};
    private static readonly int[] Motif={0,2,4,6,4,2,1,4,0,2,5,4,3,2,1,0},Chords={0,3,4,0};
    private static double Tone(double hz,double t,double brass)=>Math.Sin(Math.Tau*hz*t)+brass*(.32*Math.Sin(Math.Tau*hz*2*t)+.16*Math.Sin(Math.Tau*hz*3*t));
    private static double Envelope(double phase,double decay)=>Math.Min(1,phase*60)*Math.Exp(-phase*decay)*Math.Min(1,(1-phase)*25);
    private static double Voice(BattleMusicMood mood,double t,double beat)
    {
        bool minor=(int)mood<=3 || mood==BattleMusicMood.Suspense;
        var scale=minor?Minor:Major;int bar=(int)(beat/4),step=(int)(beat*2);double eighth=beat*2-step,quarter=beat-Math.Floor(beat);
        int chord=Chords[(bar/2)%4],degree=Motif[step%16];
        double brass=mood is BattleMusicMood.Victory or BattleMusicMood.Resolution or BattleMusicMood.Winning?1:.25;
        double root=130.8128*Math.Pow(2,scale[chord]/12d);
        double lead=261.6256*Math.Pow(2,(scale[degree%7]+(degree/7)*12)/12d);
        double v=Tone(lead,t,brass)*Envelope(eighth,3.8)*.14;
        v+=Tone(root/2,t,.2)*Envelope(quarter,2)*.12;
        // Quiet sustained chord beds bind the different arrangements together.
        v+=(Math.Sin(Math.Tau*root*t)+Math.Sin(Math.Tau*root*(minor?1.189207:1.259921)*t)+Math.Sin(Math.Tau*root*1.498307*t))*.025*Math.Sin(Math.PI*(beat%8)/8);
        double kick=Math.Sin(Math.Tau*(48*t-2*Math.Exp(-quarter*18)))*Math.Exp(-quarter*20);
        double hat=(Math.Sin(t*7291)+Math.Sin(t*10331)+Math.Sin(t*13913))*Math.Exp(-eighth*45);
        v+=kick*.1+hat*.018;
        if(mood==BattleMusicMood.Defeat){
            int note=Minor[6-(bar%7)];double hz=130.8128*Math.Pow(2,note/12d);
            return (Tone(hz,t,.45)*Envelope(quarter,1.2)*.18+Tone(hz/2,t,.2)*.08)*.8;
        }
        if(mood==BattleMusicMood.Losing)v=v*.6+Tone(root/2,t,.5)*Math.Exp(-quarter*6)*.10;
        if(mood==BattleMusicMood.Disadvantage)v=v*.75+Math.Sin(Math.Tau*lead*.5*t)*Envelope(eighth,7)*.045;
        if(mood==BattleMusicMood.Comical)v+=Math.Sin(Math.Tau*lead*2*t+Math.Sin(t*17)*1.5)*Envelope(eighth,9)*.075*(step%3==0?1:-.4);
        if(mood==BattleMusicMood.Close)v+=Tone(lead*2,t,.3)*Envelope(eighth,10)*.065+hat*.018;
        if(mood==BattleMusicMood.Decisive)v=v*.8+Tone(root*2,t,.8)*Envelope(quarter,1.3)*.10;
        if(mood==BattleMusicMood.Suspense)v=v*.4+(Math.Sin(Math.Tau*root*t)+Math.Sin(Math.Tau*root*1.059463*t))*.055*(.5+.5*Math.Sin(beat*Math.Tau*4));
        if(brass==1)v+=Tone(lead*.5,t,1)*Envelope(quarter,1.6)*.11;
        return v;
    }
    public byte[] Render(int samples=2205)
    {
        if(samples<1)throw new ArgumentOutOfRangeException(nameof(samples));var bytes=new byte[samples*2];
        for(int i=0;i<samples;i++, _sample++){
            double targetBpm=0,v=0,t=_sample/(double)Rate;
            for(int m=0;m<12;m++){
                _weights[m]+=(((int)Mood==m+1?1d:0)-_weights[m])/(Rate*.45);
                targetBpm+=_weights[m]*TempoFor((BattleMusicMood)(m+1));
                if(_weights[m]>.00001)v+=_weights[m]*Voice((BattleMusicMood)(m+1),t,_beat);
            }
            _bpm+=(targetBpm-_bpm)/(Rate*.3);_beat+=_bpm/(60*Rate);
            double fade=Math.Min(1,t/.4);
            short value=(short)(Math.Tanh(v*1.2)*fade*short.MaxValue*.85);
            bytes[i*2]=(byte)value;bytes[i*2+1]=(byte)(value>>8);
        }
        return bytes;
    }
}
public sealed class BattleMusic:IDisposable
{
    private readonly DynamicSoundEffectInstance _stream=new(BattleMusicScore.Rate,AudioChannels.Mono);
    private readonly BattleMusicScore _score;
    public BattleMusic(BattleMusicMood mood){_score=new(mood);}
    public void Update(BattleMusicMood mood,bool active){
        _score.SetMood(mood);_stream.Volume=.38f;
        if(!active){if(_stream.State==SoundState.Playing)_stream.Pause();return;}
        while(_stream.PendingBufferCount<2)_stream.SubmitBuffer(_score.Render());
        if(_stream.State==SoundState.Paused)_stream.Resume();else if(_stream.State!=SoundState.Playing)_stream.Play();
    }
    public void Dispose(){_stream.Stop();_stream.Dispose();}
}

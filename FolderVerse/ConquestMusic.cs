namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

// Original procedural score. Terrain weights are independent of future character themes.
public sealed class ConquestMusic:IDisposable
{
    private DynamicSoundEffectInstance _stream;
    private long _sample;
    private uint _noise=173;
    private float _wind;
    private Vector3 _environment;
    private const int Rate=22050;
    public ConquestMusic(){_stream=new(Rate,AudioChannels.Mono);}
    public void Update(Vector3 environment,float volume){
        _environment=Vector3.Lerp(_environment,environment,.08f);_stream.Volume=Math.Clamp(volume,0,.5f);
        while(_stream.PendingBufferCount<3)_stream.SubmitBuffer(Render());
        if(_stream.State!=SoundState.Playing)_stream.Play();
    }
    private byte[] Render(){
        var bytes=new byte[2205*2];int[] melody={0,7,12,14,15,14,12,7,3,10,15,17,19,17,15,10};int[] bass={0,0,3,7};
        for(int i=0;i<bytes.Length/2;i++, _sample++){
            double t=_sample/(double)Rate,beat=t*2,phase=beat-Math.Floor(beat);
            double root=130.8128*Math.Pow(2,bass[(int)(beat/8)%4]/12d);
            double tone=root*Math.Pow(2,melody[(int)beat%melody.Length]/12d);
            double env=Math.Min(1,phase*30)*Math.Exp(-phase*3);
            double v=(Math.Sin(t*tone*Math.Tau)+.25*Math.Sin(t*tone*2*Math.Tau))*.15*env;
            v+=Math.Sin(t*root/2*Math.Tau)*.13+Math.Sin(t*root*1.5*Math.Tau)*.045;
            _noise=unchecked(_noise*1664525+1013904223);float noise=(_noise>>8)/8388608f-1;_wind+=.025f*(noise-_wind);
            v+=_environment.Z*_wind*.5*(.6+.4*Math.Sin(t*.7)); // mountain wind
            v+=_environment.Y*Math.Sin(Math.Tau*(300*t+18*Math.Sin(t*9)))*Math.Pow(Math.Max(0,Math.Sin(t*5)),16)*.12; // bubbles
            v+=_environment.X*(Math.Sin(t*tone*4*Math.Tau)*env*.055+noise*Math.Pow(Math.Max(0,Math.Sin(t*1.7)),120)*.12); // ice bells/cracks
            short sample=(short)(Math.Clamp(v,-.95,.95)*short.MaxValue);bytes[i*2]=(byte)sample;bytes[i*2+1]=(byte)(sample>>8);
        }
        return bytes;
    }
    public static SoundEffect Curtain(){
        var bytes=new byte[Rate/2*2];uint state=37;float low=0;
        for(int i=0;i<bytes.Length/2;i++){
            state=unchecked(state*1664525+1013904223);float n=(state>>8)/8388608f-1;low+=.12f*(n-low);
            short value=(short)((n-low)*Math.Sin(Math.PI*i/(bytes.Length/2))*9000);bytes[i*2]=(byte)value;bytes[i*2+1]=(byte)(value>>8);
        }
        return new(bytes,Rate,AudioChannels.Mono);
    }
    public void Dispose(){_stream?.Stop();_stream?.Dispose();_stream=null;}
}

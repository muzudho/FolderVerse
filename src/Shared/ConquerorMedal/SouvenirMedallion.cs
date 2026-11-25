namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public readonly record struct MedallionPose(bool Reverse,float ScaleX,float Glint);
public static class SouvenirMedallion
{
    public const float HoldSeconds=.3f,FlipSeconds=1.1f;
    public static Color Metal(int rank)=>rank switch{1=>new(239,199,107),2=>new(216,222,228),3=>new(212,143,100),_=>new(164,183,197)};
    public static MedallionPose Pose(float age){
        float t=Math.Clamp((age-HoldSeconds)/FlipSeconds,0,1);float turn=MathHelper.SmoothStep(0,1,t)*MathHelper.Pi;
        return new(t<.5f,Math.Abs(MathF.Cos(turn)),Math.Max(0,1-Math.Abs(age-.36f)/.22f));
    }
    public static byte[] ChimePcm(){
        const int rate=22050;var pcm=new byte[(int)(rate*.65f)*2];
        for(int i=0;i<pcm.Length/2;i++){double t=i/(double)rate,attack=Math.Min(1,t/.004),envelope=Math.Exp(-t*9);double wave=Math.Sin(Math.Tau*1568*t)+.45*Math.Sin(Math.Tau*2349.3*t)+.2*Math.Sin(Math.Tau*3136*t);short value=(short)(wave*6500*attack*envelope);pcm[i*2]=(byte)value;pcm[i*2+1]=(byte)(value>>8);}return pcm;
    }
}

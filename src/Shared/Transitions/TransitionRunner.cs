namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

// Read-only casting and screen-space movement; never move the real inventory.
public static class TransitionRunner
{
    public const float PauseAtSeconds=.42f,PauseSeconds=.7f;
    public const float LeadSeconds=.45f+PauseSeconds,DurationSeconds=FallingTilePattern.ReleaseSeconds+PauseSeconds;
    public static float MotionTime(float age)=>age<PauseAtSeconds?age:age<PauseAtSeconds+PauseSeconds?PauseAtSeconds:age-PauseSeconds;
    public static Robot Select(IEnumerable<Robot> robots,int country,int seed)
    {
        var choices=robots.Where(r=>r.Owner==country && r.Parts==RobotParts.Complete && !r.IsWorkpiece && !r.IsDismantling && r.RecoveredRaw==null).ToArray();
        return choices.Length==0?null:choices[new Random(seed).Next(choices.Length)];
    }
    public static Vector2 Position(float age){float motion=MotionTime(age);return new(MathHelper.Lerp(-100,2020,motion/FallingTilePattern.ReleaseSeconds),760-Math.Abs(MathF.Sin(motion*24))*13);}
    public static byte[] EntryPcm(RobotMaterial material)
    {
        const int rate=22050,count=rate*28/100;var pcm=new byte[count*2];var random=new Random(129);
        double frequency=material switch{RobotMaterial.Maple=>180,RobotMaterial.Tin=>470,RobotMaterial.Paper=>1100,RobotMaterial.Cardboard=>320,RobotMaterial.Foam=>850,RobotMaterial.Polystyrene=>700,RobotMaterial.SoftVinyl=>240,RobotMaterial.Silicone=>130,_=>580};
        for(int i=0;i<count;i++){
            double t=i/(double)rate,noise=random.NextDouble()*2-1;
            double sound=(Math.Sin(t*2*Math.PI*frequency)*.7+noise*.3)*Math.Exp(-t*(material is RobotMaterial.Maple or RobotMaterial.Silicone?35:65));
            if(material==RobotMaterial.Tin)sound+=.16*Math.Sin(t*2*Math.PI*(1700+220*Math.Sin(t*80)))*Math.Exp(-t*15);
            if(material is RobotMaterial.SoftVinyl or RobotMaterial.Silicone)sound+=.15*Math.Sin(t*2*Math.PI*(900-1500*t))*Math.Exp(-t*24);
            short value=(short)(Math.Clamp(sound,-1,1)*13000);pcm[i*2]=(byte)value;pcm[i*2+1]=(byte)(value>>8);
        }
        return pcm;
    }
}
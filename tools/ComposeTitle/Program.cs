// Offline original score: Folder Verse / a toy world unpacking and folding away.
// 48 bars, D major with a B-minor middle, 128 BPM. No runtime synthesis is needed.
const int rate=22050,bpm=128,bars=48;double duration=bars*4*60d/bpm;
var path=Path.GetFullPath(args.Length>0?args[0]:"Folder Verse.wav");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
int frames=(int)Math.Round(rate*duration);var pcm=new byte[frames*4];uint noiseState=32157;
int[][] melody={
 new[]{74,78,81,78,76,74,69,73},new[]{76,73,69,73,76,81,80,76},
 new[]{71,74,78,81,78,76,74,71},new[]{74,79,83,81,79,78,76,74},
 new[]{78,81,86,85,83,81,78,74},new[]{79,83,86,83,81,79,78,76},
 new[]{76,81,85,88,85,81,76,73},new[]{74,78,81,86,81,78,76,74}};
int[][] middle={new[]{71,74,78,81,83,81,78,74},new[]{73,76,80,83,85,83,80,76},new[]{74,78,81,86,85,81,78,76},new[]{73,76,81,85,83,81,76,73}};
int[] roots={50,57,47,55,50,55,57,50};
double Hz(int midi)=>440*Math.Pow(2,(midi-69)/12d);
double Voice(int midi,double age,double length,bool pulse=false){
 double phase=Math.Tau*Hz(midi)*age;
 double tone=pulse?(Math.Sin(phase)+.3*Math.Sin(phase*3)+.13*Math.Sin(phase*5))*.70:Math.Sin(phase)+.12*Math.Sin(phase*2);
 double env=Math.Min(1,age/.004)*Math.Min(1,Math.Max(0,length-age)/.016)*Math.Exp(-age*2.2);
 return tone*env;
}
for(int i=0;i<frames;i++){
 double t=i/(double)rate,beat=t*bpm/60,barBeat=beat%4;int bar=(int)(beat/4),step=(int)(barBeat*2);
 bool intro=bar<4,bridge=bar>=16 && bar<28,fold=bar>=40;
 int sectionBar=intro?bar:bridge?bar-16:fold?bar-40:bar>=28?bar-28:bar-4;
 int root=roots[sectionBar%8];bool minor=root==47;int[] chord={root,root+(minor?3:4),root+7,root+12};
 double eighth=60d/bpm/2,age=(barBeat*2-step)*eighth;
 int note=bridge?middle[sectionBar%4][step]:melody[sectionBar%8][step];
 double lead=Voice(note,age,eighth,true)*(intro?.045:fold?.09:.18);
 if(bar>=28 && bar<40)lead+=Voice(note-12,age,eighth,true)*.045;
 double bassAge=(beat-Math.Floor(beat))*60/bpm;
 double bass=Voice(root-12,bassAge,60d/bpm,true)*.13;
 double arpPhase=beat*4,arpAge=(arpPhase-Math.Floor(arpPhase))*60/bpm/4;
 int arpIndex=(int)arpPhase%8;int foldedIndex=fold?7-arpIndex:arpIndex;
 int arpNote=chord[(foldedIndex<4?foldedIndex:7-foldedIndex)%4]+12;
 double arp=Voice(arpNote,arpAge,60d/bpm/4)*.065;
 double bell=Voice(note+12,age,eighth)*((step is 0 or 4)?.038:0);
 double kickAge=(beat%2)*60/bpm;
 double kick=Math.Sin(Math.Tau*(53*kickAge+1.7*(1-Math.Exp(-kickAge*30))))*Math.Exp(-kickAge*20)*.13;
 noiseState=unchecked(noiseState*1664525+1013904223);double noise=(noiseState>>8)/8388608d-1;
 double snareAge=((beat+1)%2)*60/bpm;
 double snare=noise*Math.Exp(-snareAge*28)*.065;
 double hat=noise*Math.Exp(-age*75)*.027;
 double drums=(kick+snare+hat)*(intro?.45:fold?.6:1);
 double musicBox=Voice(chord[(int)(beat*2)%4]+24,age,eighth)*((intro || fold)?.065:.018);
 double pan=.12*Math.Sin(bar*Math.PI/4),edge=Math.Min(1,Math.Min(t/.02,(duration-t)/.025));
 double left=Math.Tanh((lead*(1-pan)+bass+arp*1.15+bell*.5+drums+musicBox*.7)*1.35)*edge*.88;
 double right=Math.Tanh((lead*(1+pan)+bass+arp*.65+bell+drums+musicBox)*1.35)*edge*.88;
 short l=(short)(left*short.MaxValue),r=(short)(right*short.MaxValue);int p=i*4;pcm[p]=(byte)l;pcm[p+1]=(byte)(l>>8);pcm[p+2]=(byte)r;pcm[p+3]=(byte)(r>>8);
}
using(var output=new BinaryWriter(File.Create(path))){output.Write("RIFF"u8);output.Write(pcm.Length+36);output.Write("WAVEfmt "u8);output.Write(16);output.Write((short)1);output.Write((short)2);output.Write(rate);output.Write(rate*4);output.Write((short)4);output.Write((short)16);output.Write("data"u8);output.Write(pcm.Length);output.Write(pcm);}
Console.WriteLine($"Folder Verse: {duration:0.00}s, {bpm} BPM, stereo PCM / {path}");

using FolderVerse;
using Microsoft.Xna.Framework;
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
BattleState Board(params int[] counts){
    var b=new BattleState(42);long id=0;
    for(int owner=0;owner<counts.Length;owner++){
        b.Armies.Add(new(){Owner=owner});
        for(int i=0;i<counts[owner];i++)b.Units.Add(new(){Id=++id,Owner=owner,Position=new(i%10,i/10),Daggers=0,Rifles=0,Shields=0});
    }
    b.Capture(BattlePhase.Choose);return b;
}
BattleMusicMood Mood(BattleState b,int index=0,int[] players=null)=>BattleMusicSituation.Classify(b,index,players??new[]{0},(a,c)=>a==c);
foreach(var (own,enemy,expected) in new[]{(1,10,BattleMusicMood.Losing),(1,2,BattleMusicMood.Disadvantage),(2,2,BattleMusicMood.Balanced),(2,1,BattleMusicMood.Advantage),(10,1,BattleMusicMood.Winning)})
    Check(Mood(Board(own,enemy))==expected,$"Solo material ratio: {expected}");
var win=Board(2,2);win.Finished=true;win.Winner=0;win.Capture(BattlePhase.Result);
Check(Mood(win)==BattleMusicMood.Balanced && Mood(win,1)==BattleMusicMood.Victory,"Victory is heard only when replay reaches the result, not from future simulation state");
win.Winner=1;Check(Mood(win,1)==BattleMusicMood.Defeat,"Final defeat theme");
Check(Mood(win,1,new[]{0,1})==BattleMusicMood.Resolution,"Multi-player final resolution");
Check(Mood(Board(1,3,9),players:new[]{0,1})==BattleMusicMood.Comical,"Mixed three-army sizes are comical");
Check(Mood(Board(3,3),players:new[]{0,1})==BattleMusicMood.Close,"Close battle is up-tempo");
Check(Mood(Board(1,9),players:new[]{0,1})==BattleMusicMood.Decisive,"Large disparity is broad");
var unresolved=Board(2,2);unresolved.Capture(BattlePhase.Result);Check(Mood(unresolved,1,new[]{0,1})==BattleMusicMood.Suspense,"Unfinished segment ends in suspense");
var stale=Board(2,2);for(int i=1;i<=11;i++){stale.Turn=i;stale.Capture(BattlePhase.Result);}stale.Capture(BattlePhase.Choose);
Check(Mood(stale,stale.Frames.Count-1,new[]{0,1})==BattleMusicMood.Suspense,"Visible prolonged stalemate is suspenseful");
var allied=Board(1,10,10);Check(BattleMusicSituation.Classify(allied,0,new[]{0},(a,b)=>a==b || a is 0 or 2 && b is 0 or 2)==BattleMusicMood.Balanced,"Subordinate and allied strength contributes to the player's side");
Check(Mood(Board(3,3),players:new[]{9})==BattleMusicMood.Close,"Uninvolved player uses ensemble music");
var signatures=new HashSet<string>();
using var pcm=new MemoryStream();var score=new BattleMusicScore(BattleMusicMood.Defeat);
foreach(var mood in Enum.GetValues<BattleMusicMood>()){
    var standalone=new BattleMusicScore(mood);var sample=standalone.Render(BattleMusicScore.Rate*2);
    signatures.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(sample)));
    double energy=0;int peak=0;for(int i=0;i<sample.Length;i+=2){int value=(short)(sample[i]|sample[i+1]<<8);energy+=(double)value*value;peak=Math.Max(peak,Math.Abs(value));}
    Check(energy/sample.Length>10000 && peak<32000,$"Audible, unclipped arrangement: {mood}");
    score.SetMood(mood);var clip=score.Render(BattleMusicScore.Rate*6);pcm.Write(clip);
}
Check(signatures.Count==12,"All twelve arrangements are distinct");
var transition=new BattleMusicScore(BattleMusicMood.Defeat);transition.Render(BattleMusicScore.Rate);transition.SetMood(BattleMusicMood.Close);
var early=transition.Render(2205);Check(transition.Tempo>60 && transition.Tempo<100,"Tempo changes gradually instead of jumping");
transition.Render(BattleMusicScore.Rate*5);Check(Math.Abs(transition.Tempo-156)<1,"New tempo settles smoothly");
var path=Path.Combine(AppContext.BaseDirectory,"battle-music-12-states.wav");
using(var output=new BinaryWriter(File.Create(path))){
    output.Write("RIFF"u8);output.Write((int)pcm.Length+36);output.Write("WAVEfmt "u8);output.Write(16);output.Write((short)1);output.Write((short)1);output.Write(BattleMusicScore.Rate);output.Write(BattleMusicScore.Rate*2);output.Write((short)2);output.Write((short)16);output.Write("data"u8);output.Write((int)pcm.Length);output.Write(pcm.ToArray());
}
Console.WriteLine("PASS: all 12 replay-based moods, no future-result leak, allies, stalemate, distinct audible PCM, unclipped mixing and smooth tempo transitions.");
Console.WriteLine(path);
var newsSignatures=new HashSet<string>();using var newsPcm=new MemoryStream();var reader=new NewspaperMusicScore(NewspaperMusicMood.BusyMorning);
foreach(var mood in Enum.GetValues<NewspaperMusicMood>()){
 reader.Mood=mood;var clip=reader.Render(NewspaperMusicScore.Rate*6);newsPcm.Write(clip);newsSignatures.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(clip)));
 long energy=0;for(int i=0;i<clip.Length;i+=2){int value=(short)(clip[i]|clip[i+1]<<8);energy+=(long)value*value;Check(Math.Abs(value)<32000,"Newspaper music does not clip");}Check(energy/clip.Length>10000,"Every newspaper theme is audible");
}
Check(newsSignatures.Count==4,"Four distinct newspaper themes");
using(var output=new BinaryWriter(File.Create(Path.Combine(AppContext.BaseDirectory,"newspaper-music-4-states.wav")))){
 output.Write("RIFF"u8);output.Write((int)newsPcm.Length+36);output.Write("WAVEfmt "u8);output.Write(16);output.Write((short)1);output.Write((short)1);output.Write(NewspaperMusicScore.Rate);output.Write(NewspaperMusicScore.Rate*2);output.Write((short)2);output.Write((short)16);output.Write("data"u8);output.Write((int)newsPcm.Length);output.Write(newsPcm.ToArray());
}
Console.WriteLine("PASS: four newspaper atmospheres, continuous rendering, audible unclipped PCM and preview WAV.");
var longRead=new NewspaperMusicScore(NewspaperMusicMood.SleepingNight);for(int second=0;second<120;second++)longRead.Render(NewspaperMusicScore.Rate);Check(longRead.CompletedLoops>=1,"Newspaper music repeats even through a two-minute reading session");
var afterLoop=longRead.Render();Check(afterLoop.Any(v=>v!=0),"Newspaper music stays audible after its loop boundary");
var titlePath=Path.Combine(AppContext.BaseDirectory,"Content","Music","folder-verse.wav");var title=File.ReadAllBytes(titlePath);
Check(System.Text.Encoding.ASCII.GetString(title,0,4)=="RIFF" && BitConverter.ToInt16(title,22)==2 && BitConverter.ToInt32(title,24)==22050,"Static title music is playable stereo PCM WAV");
Check(Math.Abs((title.Length-44)/(22050d*4)-90)<.01,"Title theme is ninety seconds");
long titleEnergy=0;int titlePeak=0;for(int i=44;i<title.Length;i+=2){int v=BitConverter.ToInt16(title,i);titleEnergy+=(long)v*v;titlePeak=Math.Max(titlePeak,Math.Abs(v));}
Check(titlePeak<32000 && titleEnergy/((title.Length-44)/2)>100000,"Title track is audible without clipped samples");
Check(Math.Abs(BitConverter.ToInt16(title,44)-BitConverter.ToInt16(title,title.Length-4))<500,"Title loop boundary has no abrupt amplitude jump");
Console.WriteLine("PASS: two-minute newspaper looping and static title WAV format, duration, audio energy, headroom and loop join.");

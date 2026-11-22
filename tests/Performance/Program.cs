using FolderVerse;
void Near(double expected,double actual,string name){if(Math.Abs(expected-actual)>.0001)throw new Exception(name+": "+actual);Console.WriteLine("PASS "+name);}
var p=new FramePerformance();Near(0,p.Current.Fps,"initial reading has no invented FPS");
for(int i=0;i<=60;i++){p.Update(2);p.Draw(i/60d,5,3);}
Near(60,p.Current.Fps,"60 real draw intervals per second");Near(1000d/60,p.Current.FrameMilliseconds,"frame time in ms");Near(1000d/60,p.Current.LongestMilliseconds,"longest interval");Near(2,p.Current.UpdateMilliseconds,"CPU update average");Near(5,p.Current.DrawMilliseconds,"CPU draw average");Near(3,p.Current.GlobeMilliseconds,"globe CPU subset average");
p.Update(1);p.Draw(1.5,4,0);p.Update(1);p.Draw(2,4,0);
Near(2,p.Current.Fps,"stall reduces real FPS");Near(500,p.Current.LongestMilliseconds,"stall retained in max");Near(0,p.Current.GlobeMilliseconds,"no globe scene resets globe reading");Near(1,p.Current.UpdateMilliseconds,"new window clears previous averages");
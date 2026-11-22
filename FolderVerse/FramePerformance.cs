namespace FolderVerse;
using System;

// Wall-clock draw cadence and CPU submission time; these are not GPU timings.
public sealed class FramePerformance
{
    public sealed record Reading(double Fps,double FrameMilliseconds,double LongestMilliseconds,double UpdateMilliseconds,double DrawMilliseconds,double GlobeMilliseconds);
    public Reading Current {get;private set;}=new(0,0,0,0,0,0);
    private double _start=-1,_previous=-1,_intervals,_longest,_updates,_draws,_globes;
    private int _frames,_updateCount,_drawCount;
    public void Update(double milliseconds){_updates+=milliseconds;_updateCount++;}
    public void Draw(double seconds,double milliseconds,double globeMilliseconds){
        if(_start<0)_start=seconds;
        if(_previous>=0){double delta=Math.Max(0,(seconds-_previous)*1000);_intervals+=delta;_longest=Math.Max(_longest,delta);_frames++;}
        _previous=seconds;_draws+=milliseconds;_globes+=globeMilliseconds;_drawCount++;
        double elapsed=seconds-_start;if(elapsed<1)return;
        Current=new(_frames/elapsed,_frames==0?0:_intervals/_frames,_longest,_updateCount==0?0:_updates/_updateCount,_draws/_drawCount,_globes/_drawCount);
        _start=seconds;_intervals=_longest=_updates=_draws=_globes=0;_frames=_updateCount=_drawCount=0;
    }
}
namespace FolderVerse;
using System;
using System.Diagnostics;
public static class RenderFeatureCost
{
    public const int Count=8;
    public static readonly double[] Milliseconds=new double[Count];
    public static void BeginFrame()=>Array.Clear(Milliseconds);
    public readonly struct Scope:IDisposable
    {
        private readonly int _index;private readonly long _start;
        public Scope(int index,bool enabled=true){_index=enabled?index:-1;_start=enabled?Stopwatch.GetTimestamp():0;}
        public void Dispose(){if(_index>=0)Milliseconds[_index]+=Stopwatch.GetElapsedTime(_start).TotalMilliseconds;}
    }
}
public sealed class DisplayBreaker
{
    public const double BudgetMilliseconds=1000d/18;
    public bool Automatic {get;set;}=true;
    public readonly double[] LastCost=new double[8];
    public readonly bool[] Tripped=new bool[8];
    private readonly double[] _protectedUntil=new double[8],_costs=new double[8];
    private double _window=-1,_drawSum,_overSeconds,_nextTrip;private int _frames;
    public void Manual(int index,bool enabled,double now){Tripped[index]=false;if(enabled)_protectedUntil[index]=now+10;_overSeconds=0;}
    public int? Observe(double now,double drawMs,bool[] enabled,double[] costs,bool eligible){
        if(_window<0)_window=now;_drawSum+=drawMs;_frames++;
        for(int i=0;i<8;i++)if(enabled[i])_costs[i]+=costs[i];
        double elapsed=now-_window;if(elapsed<1)return null;
        double draw=_drawSum/_frames;var current=new double[8];
        for(int i=0;i<8;i++){current[i]=_costs[i]/_frames;if(enabled[i])LastCost[i]=current[i];}
        _window=now;_drawSum=0;_frames=0;Array.Clear(_costs);
        if(!Automatic||!eligible){_overSeconds=0;return null;}
        _overSeconds=draw>BudgetMilliseconds*1.1?_overSeconds+Math.Min(elapsed,1):0;
        if(_overSeconds<3||now<_nextTrip)return null;
        int best=-1;double highest=.01;
        for(int i=0;i<7;i++)if(enabled[i]&&now>=_protectedUntil[i]&&current[i]>highest){best=i;highest=current[i];}
        if(best<0)return null;Tripped[best]=true;_overSeconds=0;_nextTrip=now+3;return best;
    }
}
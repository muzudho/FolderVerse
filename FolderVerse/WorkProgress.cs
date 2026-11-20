namespace FolderVerse;
using System;

// Exact bounded fraction. The default value is zero; only rendering converts to floating point.
public readonly record struct WorkProgress
{
    private readonly long _numerator,_denominator;
    public long Numerator=>_numerator;
    public long Denominator=>_denominator==0?1:_denominator;
    public WorkProgress(long done,long total)
    {
        if(total<=0 || done<0 || done>total)throw new ArgumentOutOfRangeException();
        long a=done,b=total;while(b!=0){long r=a%b;a=b;b=r;}
        _numerator=done/a;_denominator=done==0?0:total/a;
    }
    public WorkProgress Remaining=>new(Denominator-Numerator,Denominator);
    public bool Complete=>Numerator==Denominator;
    public override string ToString()=>Denominator==1?Numerator.ToString():$"{Numerator}/{Denominator}";
}

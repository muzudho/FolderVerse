namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public readonly record struct CharacterTile(int Slot,int X,int Y,int Side)
{
    public Rectangle Bounds
    {
        get
        {
            int left=54+X*1806/CharacterSelectionLayout.Columns;
            int top=112+Y*900/CharacterSelectionLayout.Rows;
            return new(left,top,54+(X+Side)*1806/CharacterSelectionLayout.Columns-left,
                112+(Y+Side)*900/CharacterSelectionLayout.Rows-top);
        }
    }
}
public readonly record struct OverviewTile(int Slot,int X,int Y,int Width,int Height)
{
    public Rectangle Bounds=>new(54+X*1806/28,112+Y*900/20,
        (X+Width)*1806/28-X*1806/28,(Y+Height)*900/20-Y*900/20);
}
public static class CharacterSelectionLayout
{
    public const int Columns=28,Rows=20;
    public static CharacterTile[] Create(int[] counts,int activeCount)
    {
        if(activeCount<1 || activeCount>20 || counts.Length<activeCount || counts.Take(activeCount).Any(n=>n<=0))
            throw new ArgumentException("Selection requires allocated territories.");
        int total=counts.Take(activeCount).Sum();
        int[] sides=counts.Take(activeCount).Select(n=>Math.Clamp((int)Math.Sqrt((double)(Columns*Rows-64)*n/total),1,Rows)).Append(8).ToArray();
        return Pack(sides,counts.Take(activeCount).Select(n=>(double)n).Append(double.MaxValue).ToArray(),activeCount);
    }
    // Zero-cell rulers retain a minimal selectable portrait; positive areas use subtree cell totals.
    public static OverviewTile[] CreateOverview(int[] counts,int[] ranks=null){
        if(counts.Length<1 || counts.Length>20 || counts.Any(n=>n<0))throw new ArgumentException("Invalid overview counts.");
        var ranked=ranks==null?Enumerable.Range(0,counts.Length).OrderByDescending(i=>counts[i]).ThenByDescending(i=>i).ToArray():Enumerable.Range(0,counts.Length).OrderBy(i=>ranks[i]).ToArray();
        var weights=ranked.Select(i=>(double)Math.Max(1,counts[i])).ToArray();double total=weights.Sum();
        var targets=weights.Select(w=>4+(400-4*counts.Length)*w/total).ToArray();
        // Partition the entire 20x20 portrait region into rows of rectangles. Search integer
        // row heights while balancing area against cropping, preserving rank by area and position.
        var memo=new Dictionary<(int Start,int Height,int Limit),(double Cost,int Count,int RowHeight,int[] Widths)>();
        int[] Widths(int start,int count){
            double sum=weights.Skip(start).Take(count).Sum();
            var ideal=weights.Skip(start).Take(count).Select(w=>2+(20-2*count)*w/sum).ToArray();
            var widths=ideal.Select(w=>(int)w).ToArray();
            for(int spare=20-widths.Sum();spare>0;spare--){
                int next=Enumerable.Range(0,count).Where(i=>i==0 || widths[i]<widths[i-1]).OrderByDescending(i=>ideal[i]-widths[i]).ThenBy(i=>i).First();
                widths[next]++;
            }
            return widths;
        }
        double Solve(int start,int height,int limit){
            if(start==counts.Length)return height==0?0:double.PositiveInfinity;
            if(height<2)return double.PositiveInfinity;
            var key=(start,height,limit);if(memo.TryGetValue(key,out var cached))return cached.Cost;
            var best=(Cost:double.PositiveInfinity,Count:0,RowHeight:0,Widths:Array.Empty<int>());
            for(int count=1;count<=Math.Min(10,counts.Length-start);count++){
                var widths=Widths(start,count);
                for(int h=2;h<=height;h++){
                    if(widths[0]*h>limit)continue;
                    double cost=0;
                    for(int i=0;i<count;i++){
                        double area=widths[i]*h,target=targets[start+i],ratio=widths[i]*1806d/28/(h*45);
                        double aspect=Math.Max(ratio,1/ratio);
                        cost+=Math.Pow((area-target)/Math.Max(10,target),2)+.15*Math.Pow(Math.Log(ratio),2)+10*Math.Pow(Math.Max(0,aspect-3),2);
                    }
                    cost+=Solve(start+count,height-h,widths[^1]*h);
                    if(cost<best.Cost)best=(cost,count,h,widths);
                }
            }
            memo[key]=best;return best.Cost;
        }
        Solve(0,20,400);
        var tiles=new OverviewTile[counts.Length];int start=0,y=0,limit=400;
        while(start<counts.Length){
            var row=memo[(start,20-y,limit)];int x=8;
            if(row.Count==0)throw new InvalidOperationException("Portrait layout could not fill the calendar.");
            for(int i=0;i<row.Count;i++){int slot=ranked[start+i];tiles[slot]=new(slot,x,y,row.Widths[i],row.RowHeight);x+=row.Widths[i];}
            start+=row.Count;y+=row.RowHeight;limit=row.Widths[^1]*row.RowHeight;
        }
        return tiles;
    }
    public static CharacterTile[] CreateBattles(long[] strengths)
    {
        if(strengths.Length<1 || strengths.Length>10 || strengths.Any(n=>n<=0))throw new ArgumentException("Battle layout requires 1 to 10 battles.");
        double total=strengths.Sum(n=>(double)n);
        var sides=strengths.Select(n=>Math.Clamp((int)Math.Sqrt((Columns*Rows-64)*(double)n/total),4,Rows)).Append(8).ToArray();
        return Pack(sides,strengths.Select(n=>(double)n).Append(double.MaxValue).ToArray(),strengths.Length);
    }
    private static CharacterTile[] Pack(int[] sides,double[] weights,int activeCount)
    {
        while(true)
        {
            bool[,] used=new bool[Columns,Rows];var result=new CharacterTile[activeCount+1];bool fits=true;
            foreach(int slot in Enumerable.Range(0,activeCount+1).OrderByDescending(i=>sides[i]).ThenByDescending(i=>weights[i]).ThenBy(i=>i))
            {
                int size=sides[slot];bool placed=false;
                for(int y=0;y<=Rows-size && !placed;y++)for(int x=0;x<=Columns-size && !placed;x++)
                {
                    bool empty=true;
                    for(int dy=0;dy<size && empty;dy++)for(int dx=0;dx<size;dx++)if(used[x+dx,y+dy]){empty=false;break;}
                    if(!empty)continue;
                    result[slot]=new(slot==activeCount?-1:slot,x,y,size);
                    for(int dy=0;dy<size;dy++)for(int dx=0;dx<size;dx++)used[x+dx,y+dy]=true;
                    placed=true;
                }
                if(!placed){fits=false;break;}
            }
            if(fits)return result;
            // Square area alone does not guarantee geometric packing. Shrink one largest tile and retry.
            int largest=Enumerable.Range(0,activeCount).OrderByDescending(i=>sides[i]).ThenBy(i=>weights[i]).First();
            sides[largest]--;
        }
    }
}

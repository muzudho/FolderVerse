namespace FolderVerse;
using System;
using System.Linq;
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
public static class CharacterSelectionLayout
{
    public const int Columns=28,Rows=20;
    public static CharacterTile[] Create(int[] counts,int activeCount)
    {
        if(activeCount<1 || activeCount>20 || counts.Length<activeCount || counts.Take(activeCount).Any(n=>n<=0))
            throw new ArgumentException("Selection requires allocated territories.");
        int total=counts.Take(activeCount).Sum();
        int[] sides=counts.Take(activeCount).Select(n=>Math.Clamp((int)Math.Sqrt((double)(Columns*Rows-64)*n/total),1,Rows)).Append(8).ToArray();
        while(true)
        {
            bool[,] used=new bool[Columns,Rows];var result=new CharacterTile[activeCount+1];bool fits=true;
            foreach(int slot in Enumerable.Range(0,activeCount+1).OrderByDescending(i=>sides[i]).ThenByDescending(i=>i==activeCount?int.MaxValue:counts[i]).ThenBy(i=>i))
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
            int largest=Enumerable.Range(0,activeCount).OrderByDescending(i=>sides[i]).ThenBy(i=>counts[i]).First();
            sides[largest]--;
        }
    }
}
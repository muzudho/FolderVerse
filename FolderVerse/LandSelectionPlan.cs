namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

public static class LandSelectionPlan
{
    // Exact minimum disjoint rectangle partition. No rectangle includes an unchosen cell.
    public static int[][] Rectangles(IEnumerable<SurfaceCell> selection)
    {
        var result=new List<int[]>();
        foreach(var face in selection.GroupBy(c=>c.Face))
        {
            var cells=face.OrderBy(c=>c.Y).ThenBy(c=>c.X).ToArray();
            var positions=cells.Select((c,i)=>(c,i)).ToDictionary(p=>(p.c.X,p.c.Y),p=>p.i);
            var memo=new Dictionary<BigInteger,int[][]>();
            int[][] Solve(BigInteger remaining)
            {
                if(remaining.IsZero)return Array.Empty<int[]>();
                if(memo.TryGetValue(remaining,out var cached))return cached;
                int first=0;while((remaining&(BigInteger.One<<first)).IsZero)first++;
                var origin=cells[first];var candidates=new List<(BigInteger Mask,int[] Ids)>();
                int maxWidth=int.MaxValue;
                for(int y=origin.Y;;y++)
                {
                    int width=0;
                    while(width<maxWidth && positions.TryGetValue((origin.X+width,y),out int i) && !(remaining&(BigInteger.One<<i)).IsZero)width++;
                    if(width==0)break;maxWidth=width;
                    for(int w=1;w<=width;w++)
                    {
                        BigInteger mask=0;var ids=new List<int>();
                        for(int yy=origin.Y;yy<=y;yy++)for(int x=origin.X;x<origin.X+w;x++)
                        {int i=positions[(x,yy)];mask|=BigInteger.One<<i;ids.Add(cells[i].Id);}
                        candidates.Add((mask,ids.ToArray()));
                    }
                }
                int[][] best=null;
                foreach(var candidate in candidates.OrderByDescending(c=>c.Ids.Length))
                {
                    var tail=Solve(remaining^candidate.Mask);
                    if(best==null || tail.Length+1<best.Length)best=new[]{candidate.Ids}.Concat(tail).ToArray();
                    if(best.Length==1)break;
                }
                return memo[remaining]=best;
            }
            result.AddRange(Solve((BigInteger.One<<cells.Length)-1));
        }
        return result.ToArray();
    }
}

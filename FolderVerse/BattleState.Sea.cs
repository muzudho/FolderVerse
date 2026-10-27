namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public sealed partial class BattleState
{
    public HashSet<Point> SeaLogs {get;}=new();
    public HashSet<Point> SeaClearWater {get;}=new();
    public void BuildSeaLayout(int pattern,int rotation=0)
    {
        Terrain=BattleTerrain.Sea;Walls.Clear();WallShotDirections.Clear();SeaLogs.Clear();SeaClearWater.Clear();
        if(BoardSize==Size){
            LayoutName="中央船・四方接舷";EntrySides=new[]{0,1,2,3};Flag=new(6,6);
            var deck13=Enumerable.Range(0,5).SelectMany(Region).ToHashSet();
            // One open boarding lane joins each attacking ship to the central defending deck.
            for(int i=2;i<=10;i++){deck13.Add(new(6,i));deck13.Add(new(i,6));}
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)if(!deck13.Contains(new(x,y))){Walls.Add(new(x,y));SeaClearWater.Add(new(x,y));}
            return;
        }
        rotation=((rotation%4)+4)%4;
        Point Rotate(Point p){for(int i=0;i<rotation;i++)p=new(9-p.Y,p.X);return p;}
        var deck=new HashSet<Point>();
        if(pattern==1)
        {
            LayoutName="接舷・丸太渡り";EntrySides=new[]{(3+rotation)%4,(1+rotation)%4};
            for(int y=0;y<10;y++)
            {
                int first=y is 0 or 9?2:y is 1 or 8?1:0;
                int last=y is 0 or 9?2:3;
                for(int x=first;x<=last;x++){deck.Add(Rotate(new(x,y)));deck.Add(Rotate(new(9-x,y)));}
            }
            SeaLogs.Add(Rotate(new(4,4)));SeaLogs.Add(Rotate(new(5,4)));
            // Both ships run vertically: their complete forward/backward lanes stay open water.
            for(int y=0;y<10;y++)for(int x=0;x<10;x++)
                if(!new[]{new Point(4,1),new Point(5,7),new Point(4,8)}.Contains(new Point(x,y)))SeaClearWater.Add(Rotate(new(x,y)));
        }
        else
        {
            LayoutName="船首衝突・丸太渡り";
            int[] sides=Armies.Count is >0 and <=2?new[]{0,2}:Armies.Count==3?new[]{0,1,2}:new[]{0,1,2,3};
            EntrySides=sides.Select(s=>(s+rotation)%4).ToArray();
            foreach(int side in EntrySides)
                for(int y=0;y<10;y++)for(int x=2;x<=7;x++)
                {var p=new Point(x,y);for(int i=0;i<side;i++)p=new(9-p.Y,p.X);SeaClearWater.Add(p);}
            foreach(int side in EntrySides)
                for(int y=0;y<4;y++)for(int x=0;x<10;x++)
                {
                    int inset=y<2?2:y==2?3:4;if(x<inset || x>9-inset)continue;
                    var p=new Point(x,y);for(int i=0;i<side;i++)p=new(9-p.Y,p.X);deck.Add(p);
                }
            // The central junction connects ships through bridges only one cell wide.
            foreach(var p in new[]{new Point(4,4),new Point(4,5),new Point(5,4)})
                if(!deck.Contains(p))SeaLogs.Add(p);
        }
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
            if(!deck.Contains(new(x,y)) && !SeaLogs.Contains(new(x,y)))Walls.Add(new(x,y));
        Flag=deck.OrderBy(p=>p.Y).ThenBy(p=>Math.Abs(p.X-4)).First();
    }
    private void PlaceSeaFlag()
    {
        var defender=Armies.FirstOrDefault(a=>a.Owner==Defender)??Armies.FirstOrDefault();
        if(defender==null)return;
        Flag=Region(defender.Side).Where(Walkable).Where(p=>!SeaLogs.Contains(p))
            .OrderBy(p=>Math.Abs((defender.Side%2==0?p.X:p.Y)-4.5))
            .ThenBy(p=>defender.Side switch{0=>Math.Abs(p.Y-1),1=>Math.Abs(p.X-8),2=>Math.Abs(p.Y-8),_=>Math.Abs(p.X-1)})
            .First();
    }
}

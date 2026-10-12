namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class BattleState
{
    public string LayoutName {get;private set;}="標準";
    public int[] EntrySides {get;private set;}={0,1,2,3};
    public int EntryCapacity=>EntrySides.Length;
    public void BuildLayout(int pattern,int rotation=0)
    {
        Walls.Clear();rotation=((rotation%4)+4)%4;
        Point Rotate(Point p){for(int i=0;i<rotation;i++)p=new(9-p.Y,p.X);return p;}
        LayoutName=pattern switch{1=>"渓谷",2=>"Ｔ字路",3=>"片側峡谷",4=>"中央衝突",_=>"標準"};
        int[] sides=pattern==1?new[]{0,2}:pattern==2?new[]{2,1,3}:new[]{0,1,2,3};
        EntrySides=sides.Select(s=>(s+rotation)%4).ToArray();
        for(int y=0;y<10;y++)for(int x=0;x<10;x++)
        {
            bool blocked=pattern switch {
                1=>x<2 || x>7,
                2=>!(y>=3 && y<=6 || y>=3 && x>=2 && x<=7),
                3=>y<4 && (x<2 || x>7),
                4=>(x is 0 or 9) && (y is 0 or 9),
                _=>false};
            if(blocked)Walls.Add(Rotate(new(x,y)));
        }
        Flag=Rotate(new(5,5));
    }
    public void ChooseLayout()
    {
        if(Terrain==BattleTerrain.Sea){BuildSeaLayout(Armies.Count<=2?Random.Next(2):0,Random.Next(4));return;}
        // Restrict narrow arenas to armies that fit their entrances.
        int[] patterns=Armies.Count<=2?new[]{0,1,2,3,4}:Armies.Count==3?new[]{0,2,3,4}:new[]{0,3,4};
        int pattern=patterns[Random.Next(patterns.Length)];
        if(pattern!=0)BuildLayout(pattern,Random.Next(pattern==1?2:4));
    }
}


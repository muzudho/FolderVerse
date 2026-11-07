namespace FolderVerse;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public sealed record SeaWhirlpool(int Cell,Vector2 Center,float Radius);
public sealed partial class TerrainRoutes
{
    private SeaWhirlpool[] _whirlpools=Array.Empty<SeaWhirlpool>();
    public SeaWhirlpool[] Whirlpools=>_whirlpools??Array.Empty<SeaWhirlpool>();
    public bool WhirlpoolAt(int cell,Point point)
    {
        var sample=Position(cell,point);
        return Whirlpools.Any(w=>Vector3.Dot(_world.Cells[cell].Normal,_world.Cells[w.Cell].Normal)>.99f &&
            Vector3.DistanceSquared(sample,WhirlpoolCenter(w))<w.Radius*w.Radius/100);
    }
    public Vector3 WhirlpoolCenter(SeaWhirlpool whirl){var cell=_world.Cells[whirl.Cell];return cell.Origin+cell.U*((whirl.Center.X+.5f)/10)+cell.V*((whirl.Center.Y+.5f)/10);}
    private void BuildWhirlpools()
    {
        _whirlpools=Array.Empty<SeaWhirlpool>();if(_world.TutorialStep!=0)return;
        var candidates=new List<(int First,int Second)>();
        foreach(var cell in _world.Cells){
            var next=_world.Cells.FirstOrDefault(c=>c.Face==cell.Face && Vector3.DistanceSquared(c.Center,cell.Center+cell.U)<.001f);
            if(next==null)continue;
            var center=cell.Origin+cell.U+cell.V*.5f;
            bool ocean=new[]{cell.Id,next.Id}.All(id=>Enumerable.Range(0,100).Where(i=>Vector3.DistanceSquared(Position(id,PointAt(i)),center)<.42f*.42f).All(i=>_heights[id][i]<.52f));
            if(ocean)candidates.Add((cell.Id,next.Id));
        }
        var random=new SeedRandom(_world.WorldSeed^0x4e414441);var shuffled=candidates.ToArray();random.Shuffle(shuffled);
        var selected=new List<SeaWhirlpool>();var occupied=new HashSet<int>();
        int limit=candidates.Count==0?0:Math.Max(1,candidates.Count/20);
        foreach(var pair in shuffled){
            if(occupied.Contains(pair.First) || occupied.Contains(pair.Second))continue;
            selected.Add(new(pair.First,new Vector2(9.5f,4.5f),3.3f));occupied.Add(pair.First);occupied.Add(pair.Second);
            if(selected.Count>=limit)break;
        }
        // Each disk touches at most half of either cell, and no cell receives two.
        // The sea ring supplies alternate paths without placing a Node in the disk.
        _whirlpools=selected.ToArray();
    }
}

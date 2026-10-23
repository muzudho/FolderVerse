namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

public enum LandmarkKind { House,Windmill,Well,Tree,Rock,Flowers,Grove,Lighthouse,Pier,Wreck,Reef,Buoy,Gulls,RookTower,Castle,JapaneseCastle,Arch }
public sealed record NodeLandmark(int Node,int Cell,Vector3 Position,LandmarkKind[] Objects,string Direction)
{
    public string Signature=>string.Join(",",Objects.Select(k=>(int)k));
    public string Description=>Direction+"側の"+string.Join("と",Objects.GroupBy(k=>k).Select(g=>NodeLandmarks.Name(g.Key)+(g.Count()>1?$"×{g.Count()}":"")));
}
public static class NodeLandmarks
{
    private static readonly ConditionalWeakTable<Node[],NodeLandmark[]> Cache=new();
    private static readonly LandmarkKind[] Land={LandmarkKind.House,LandmarkKind.Windmill,LandmarkKind.Well,LandmarkKind.Tree,LandmarkKind.Rock,LandmarkKind.Flowers,LandmarkKind.Grove,LandmarkKind.RookTower,LandmarkKind.Castle,LandmarkKind.JapaneseCastle,LandmarkKind.Arch};
    private static readonly LandmarkKind[] Sea={LandmarkKind.Reef,LandmarkKind.Buoy,LandmarkKind.Gulls};
    public static string Name(LandmarkKind kind)=>kind switch {
        LandmarkKind.House=>"赤い家",LandmarkKind.Windmill=>"風車",LandmarkKind.Well=>"井戸",LandmarkKind.Tree=>"大木",LandmarkKind.Rock=>"岩",LandmarkKind.Flowers=>"花畑",LandmarkKind.Grove=>"三本杉",
        LandmarkKind.Lighthouse=>"灯台",LandmarkKind.Pier=>"桟橋",LandmarkKind.Wreck=>"難破船",LandmarkKind.Reef=>"岩礁",LandmarkKind.Buoy=>"ブイ",LandmarkKind.RookTower=>"ルックの塔",LandmarkKind.Castle=>"西洋の城",LandmarkKind.JapaneseCastle=>"日本の城",LandmarkKind.Arch=>"凱旋門",_=>"カモメ"};
    public static IReadOnlyList<NodeLandmark> All(WorldSetup world)=>Cache.GetValue(world.Nodes.All,_=>Build(world));
    public static NodeLandmark At(WorldSetup world,int node)=>All(world)[node];
    public static string Label(WorldSetup world,Node node)=>world.Nodes.Label(node)+" / "+At(world,node.Id).Description;
    // Combinations with repetition expand the vocabulary without adding art. Sorting is canonical.
    private static IEnumerable<LandmarkKind[]> Combinations(LandmarkKind[] palette,int count,int start=0)
    {
        if(count==0){yield return Array.Empty<LandmarkKind>();yield break;}
        for(int i=start;i<palette.Length;i++)foreach(var rest in Combinations(palette,count-1,i))yield return new[]{palette[i]}.Concat(rest).ToArray();
    }
    private static NodeLandmark[] Build(WorldSetup world)
    {
        var nodes=world.Nodes.All;var result=new NodeLandmark[nodes.Length];
        var neighbors=nodes.Select(_=>new HashSet<int>()).ToArray();
        foreach(var node in nodes)foreach(var route in world.Routes.Neighbors(node)){
            var next=world.Nodes.At(route.Target,route.Entry);if(next==null || next.Id==node.Id)continue;
            neighbors[node.Id].Add(next.Id);neighbors[next.Id].Add(node.Id);
        }
        var occupied=nodes.GroupBy(n=>n.Cell).ToDictionary(g=>g.Key,g=>g.Select(n=>n.Center).ToHashSet());
        foreach(var node in nodes){
            bool land=world.Routes.Land(node.Cell,node.Center);
            var candidates=Enumerable.Range(0,100).Select(i=>new Point(i%10,i/10))
                .Where(p=>!occupied[node.Cell].Contains(p) && world.Routes.Land(node.Cell,p)==land && Math.Abs(p.X-node.Center.X)+Math.Abs(p.Y-node.Center.Y)<=3)
                .OrderBy(p=>Math.Abs(Math.Abs(p.X-node.Center.X)+Math.Abs(p.Y-node.Center.Y)-2))
                .ThenBy(p=>unchecked((uint)(world.WorldSeed*397+node.Id*7919+p.X*73+p.Y*137))).ToArray();
            var point=candidates.Length>0?candidates[0]:node.Center;
            if(candidates.Length>0)occupied[node.Cell].Add(point);
            bool coast=Enumerable.Range(0,4).Select(i=>point+(i switch{0=>new Point(1,0),1=>new Point(-1,0),2=>new Point(0,1),_=>new Point(0,-1)}))
                .Any(p=>p.X>=0 && p.X<10 && p.Y>=0 && p.Y<10 && world.Routes.Land(node.Cell,p)!=land);
            var palette=(coast?(land?Land.Concat(new[]{LandmarkKind.Lighthouse,LandmarkKind.Pier}):Sea.Concat(new[]{LandmarkKind.Wreck,LandmarkKind.Pier})):land?Land:Sea).OrderBy(k=>k).ToArray();
            var used=neighbors[node.Id].Where(n=>result[n]!=null).Select(n=>result[n].Signature).ToHashSet();
            LandmarkKind[] chosen=null;
            for(int count=2;chosen==null;count++){
                var options=Combinations(palette,count).ToArray();int start=(int)(unchecked((uint)(world.WorldSeed*31+node.Id*101))%(uint)options.Length);
                for(int i=0;i<options.Length;i++){var option=options[(start+i)%options.Length];if(!used.Contains(string.Join(",",option.Select(k=>(int)k)))){chosen=option;break;}}
            }
            var cell=world.Cells[node.Cell];var position=world.Routes.Position(node.Cell,point);
            if(point==node.Center)position+=cell.U*.035f+cell.V*.035f;
            var normal=Vector3.Normalize(world.Geographic(cell.Normal));var north=Vector3.UnitY-normal*normal.Y;
            if(north.LengthSquared()<.001f)north=-world.Geographic(cell.V);
            north.Normalize();var east=Vector3.Cross(north,normal);east.Normalize();
            var delta=world.Geographic(position-world.Routes.Position(node.Cell,node.Center));float x=Vector3.Dot(delta,east),y=Vector3.Dot(delta,north);
            string direction=Math.Abs(x)>Math.Abs(y)?x>=0?"東":"西":y>=0?"北":"南";
            result[node.Id]=new(node.Id,node.Cell,position,chosen,direction);
        }
        return result;
    }
}

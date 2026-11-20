namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

public enum BattleSiteKind { Standard,Open,Narrow,MountainPass,Valley,Coast,Shoal }
public sealed record BattleSiteProfile(BattleSiteKind Kind,int Orientation,int Area,int LocalWater,int LocalMountains)
{
    public string Name=>Kind switch{BattleSiteKind.Open=>"開けた平地",BattleSiteKind.Narrow=>"狭い通路",BattleSiteKind.MountainPass=>"山の通り道",BattleSiteKind.Valley=>"谷の水路",BattleSiteKind.Coast=>"海岸",BattleSiteKind.Shoal=>"浅瀬",_=>"標準"};
    public string Description=>Kind switch{
        BattleSiteKind.Open=>"岩が少なく、壁の間隔も広い戦場。",
        BattleSiteKind.Narrow=>"岩で横幅が狭く、中央に通り道が残ります。",
        BattleSiteKind.MountainPass=>"岩の間を通り抜ける戦場。",
        BattleSiteKind.Valley=>"水路と岩を避けて進む戦場。",
        BattleSiteKind.Coast=>"海側に水面、旗は陸側へ寄っています。",
        BattleSiteKind.Shoal=>"近くの陸に向いた側に岩があります。",
        _=>"標準の戦場配置です。"};
    public static BattleSiteProfile FromWorld(WorldSetup w,Node n)
    {
        int water=0,mountains=0,land=0;var waterDirection=Point.Zero;var landDirection=Point.Zero;
        // Read the original region grid; never alter its 10x10 dots or route graph.
        for(int y=0;y<10;y++)for(int x=0;x<10;x++){
            if(Math.Abs(x-n.Center.X)>2||Math.Abs(y-n.Center.Y)>2)continue;
            var p=new Point(x,y);var type=w.Routes.Terrain(n.Cell,p);
            if(type==TravelTerrain.Sea){water++;waterDirection+=p-n.Center;}
            else {land++;landDirection+=p-n.Center;if(type==TravelTerrain.Mountain)mountains++;}
        }
        int width=n.Points.Max(p=>p.X)-n.Points.Min(p=>p.X)+1,height=n.Points.Max(p=>p.Y)-n.Points.Min(p=>p.Y)+1;
        int orientation=width<height?0:1;
        BattleSiteKind kind;
        if(n.Terrain==TravelTerrain.Sea){kind=land>=4?BattleSiteKind.Shoal:BattleSiteKind.Standard;orientation=(int)BattleAi.Face(landDirection);}
        else if(n.IsHarbor||water>=4){kind=BattleSiteKind.Coast;orientation=(int)BattleAi.Face(waterDirection);}
        else if(n.Terrain==TravelTerrain.Mountain||mountains>=4)kind=BattleSiteKind.MountainPass;
        else if(WorldTerrain.IsValley(w,w.Cells[n.Cell],w.Routes.Position(n.Cell,n.Center),w.Terrain.Elevation(w.Routes.Position(n.Cell,n.Center))))kind=BattleSiteKind.Valley;
        else if(n.Points.Length<=12||Math.Min(width,height)<=2)kind=BattleSiteKind.Narrow;
        else if(n.Points.Length>=60&&(uint)(w.WorldSeed^n.Cell*7919^n.Center.X*31^n.Center.Y)%3==0)kind=BattleSiteKind.Open;
        else kind=BattleSiteKind.Standard;
        return new(kind,orientation,n.Points.Length,water,mountains);
    }
}
public sealed partial class BattleState
{
    public BattleSiteProfile SiteProfile;
    public bool SiteLayoutApplied;
    public HashSet<Point> SiteWater=new();
    public HashSet<Point> SiteRocks=new();
    public void PrepareSiteFlag()
    {
        if(SiteProfile?.Kind==BattleSiteKind.Coast&&Kind==BattleKind.Node&&!SplitBoard&&BoardSize==Size&&Terrain!=BattleTerrain.Sea){
            var at=new Point(6,6)-Forward((BattleFacing)SiteProfile.Orientation);
            if(Walkable(at))Flag=at;
        }
    }
    public void ApplySiteLayout()
    {
        if(SiteProfile==null||SiteLayoutApplied||Kind!=BattleKind.Node||SplitBoard||BoardSize!=Size)return;
        SiteLayoutApplied=true;LayoutName=SiteProfile.Name;
        if(SiteProfile.Kind==BattleSiteKind.Standard)return;
        var keep=Armies.SelectMany(a=>ArmyRegion(a).Concat(ArmyEntrance(a)).Concat(ApproachPath(a)).Concat(RetreatPath(a)))
            .Concat(Units.Where(u=>u.Active).Select(u=>u.Position)).Append(Flag).Concat(Fortifications.Select(f=>f.Position)).ToHashSet();
        if(SiteProfile.Kind==BattleSiteKind.Open){
            foreach(var p in Walls.Where(p=>!Fortifications.Any(f=>f.Position==p)).ToArray()){Walls.Remove(p);WallShotDirections.Remove(p);}return;
        }
        Point Rotate(Point p){for(int i=0;i<SiteProfile.Orientation;i++)p=new(12-p.Y,p.X);return p;}
        bool Connected(){
            var seen=new HashSet<Point>{Flag};var q=new Queue<Point>();q.Enqueue(Flag);
            while(q.TryDequeue(out var p))foreach(BattleFacing f in Enum.GetValues<BattleFacing>()){var next=p+Forward(f);if(Walkable(next)&&seen.Add(next))q.Enqueue(next);}
            return seen.Count==Enumerable.Range(0,Size).Sum(y=>Enumerable.Range(0,Size).Count(x=>Walkable(new(x,y))));
        }
        int placed=0;
        for(int y=2;y<=10;y++)for(int x=2;x<=10;x++){
            bool water=SiteProfile.Kind==BattleSiteKind.Coast||SiteProfile.Kind==BattleSiteKind.Valley&&x==4;
            bool candidate=SiteProfile.Kind switch{
                BattleSiteKind.Narrow=>(x==3||x==9)&&y>=3&&y<=9&&y!=6,
                BattleSiteKind.MountainPass=>(y==4||y==8)&&(x<=4||x>=8),
                BattleSiteKind.Valley=>x==4&&y!=5&&y!=8||x==8&&(y==3||y==4||y==8||y==9),
                BattleSiteKind.Coast=>y==2||y==3&&x%3==0,
                BattleSiteKind.Shoal=>y<=3&&x%2==0,
                _=>false};
            if(!candidate||placed>=12)continue;var at=Rotate(new(x,y));if(keep.Contains(at))continue;
            if(Terrain==BattleTerrain.Sea){if(!SeaClearWater.Contains(at))continue;SeaClearWater.Remove(at);SiteRocks.Add(at);placed++;continue;}
            if(!Walkable(at))continue;Walls.Add(at);if(!Connected()){Walls.Remove(at);continue;}
            if(water)SiteWater.Add(at);else SiteRocks.Add(at);placed++;
        }
    }
}

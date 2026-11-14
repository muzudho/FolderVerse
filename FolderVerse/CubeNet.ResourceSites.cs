namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal enum ResourceSiteKind { Salt,Oil,Gas,Quartz }
    internal sealed record ResourceSite(ResourceSiteKind Kind,Vector3 Position,float Size);
    private (int Seed,SurfaceCell[] Cells,TerrainSampler Terrain) _resourceSiteKey;
    private ResourceSite[][] _resourceSites;

    private void DrawResourceSites(UiPainter ui,WorldSetup world,int cellId,Func<Vector3,Vector2> project,bool globe)
    {
        if(world.TutorialStep!=0 || world.EquipmentMode!=RobotEquipmentMode.Materials)return;
        var key=(world.WorldSeed,world.Cells,world.Terrain);
        if(_resourceSites==null || _resourceSiteKey!=key)
        {
            _resourceSiteKey=key;
            _resourceSites=world.Cells.Select(cell=>ResourceSites(world,cell,key.Item3)).ToArray();
        }
        var cell=world.Cells[cellId];
        foreach(var site in _resourceSites[cellId].OrderBy(s=>project(s.Position).Y))
        {
            var origin=site.Position;
            if(globe)origin+=cell.Normal*WorldTerrain.ReliefAt(world,cell,origin,key.Item3);
            Vector2 Point(float x,float y,float z)=>project(origin+site.Size*(cell.U*x+cell.V*y+(globe?cell.Normal*z:Vector3.Zero)));
            var node=RobotWorld.SiteNode(world,cell,site.Position);
            int remaining=node==null || !node.Functional || node.Purpose==NodePurpose.Relay?0:
                world.Robots.RawDeposit(node.Id,RobotWorld.SiteMaterial(site.Kind));
            DrawResourceSite(ui,site.Kind,Point,globe,world.Robots.OtherRawSupplyEnabled && remaining==0);
            if(world.Robots.OtherRawSupplyEnabled && remaining>0 && Vector2.Distance(Point(-.6f,0,0),Point(.6f,0,0))>=34)
            {
                var label=Point(0,.65f,0);
                var bounds=new Rectangle((int)label.X-25,(int)label.Y,50,16);
                ui.Box(bounds,new Color(62,37,30));
                ui.Center($"残り{remaining}荷",bounds,.24f,new Color(255,236,181));
            }
        }
    }

    internal static ResourceSite[] ResourceSites(WorldSetup world,SurfaceCell cell,TerrainSampler terrain)
    {
        var random=new SeedRandom(world.WorldSeed^cell.Id*7919^0x524553);
        var sites=new System.Collections.Generic.List<ResourceSite>();
        var swirl=world.Routes.Whirlpools.Select(w=>(Center:world.Routes.WhirlpoolCenter(w),Radius:w.Radius/10f+.40f)).ToArray();
        // Low-frequency field stands in for neighboring outcrops of the same old strata.
        float basin=WorldTerrain.Elevation(cell.Center*.32f,terrain.Offset+new Vector3(19,7,31));
        var scores=new float[4];
        var positions=new Vector3[4];
        for(int y=1;y<9;y++)for(int x=1;x<9;x++)
        {
            var point=world.Routes.Position(cell.Id,new Point(x,y));
            float height=terrain.Elevation(point);
            if(height<.52f)continue;
            int water=0;
            float low=height,high=height;
            foreach(var direction in new[]{cell.U,-cell.U,cell.V,-cell.V})
            {
                float neighbor=terrain.Elevation(point+direction*.1f);
                if(neighbor<.52f)water++;
                else {low=Math.Min(low,neighbor);high=Math.Max(high,neighbor);}
            }
            float grade=high-low;
            if(height<.56f && water is 1 or 2 && grade<.038f && swirl.All(w=>Vector3.DistanceSquared(w.Center,point)>w.Radius*w.Radius))
            {
                int sheltered=0;
                foreach(var direction in new[]{cell.U,-cell.U,cell.V,-cell.V})
                    if(terrain.Elevation(point+direction*.24f)>=.52f)sheltered++;
                Candidate(ResourceSiteKind.Salt,point,.05f-grade+.016f*sheltered);
            }
            if(basin>.53f && height is >=.64f and <.76f && water==0)
                Candidate(ResourceSiteKind.Oil,point,basin-.5f+grade);
            if(height is >=.55f and <.68f && water==0 && grade<.023f)
                Candidate(ResourceSiteKind.Gas,point,.024f-grade);
            if(height>=.62f && water==0 && grade>.042f)
                Candidate(ResourceSiteKind.Quartz,point,grade);
        }
        Add(ResourceSiteKind.Salt,3);
        Add(ResourceSiteKind.Oil,3);
        Add(ResourceSiteKind.Gas,4);
        Add(ResourceSiteKind.Quartz,4);
        return sites.ToArray();

        void Candidate(ResourceSiteKind kind,Vector3 point,float score)
        {
            int i=(int)kind;
            if(score<=scores[i])return;
            scores[i]=score;
            positions[i]=point;
        }

        void Add(ResourceSiteKind kind,int odds)
        {
            int i=(int)kind;
            if(scores[i]<=0 || random.Next(odds)!=0)return;
            if(sites.Any(s=>Vector3.DistanceSquared(s.Position,positions[i])<.19f*.19f))return;
            float size=kind==ResourceSiteKind.Salt?Math.Clamp(.085f+scores[i],.14f,.21f):.15f;
            sites.Add(new(kind,positions[i],size));
        }
    }

    private static void DrawResourceSite(UiPainter ui,ResourceSiteKind kind,Func<float,float,float,Vector2> point,bool globe,bool depleted)
    {
        var sand=new Color(197,154,101);
        var rock=new Color(78,76,83);
        var white=new Color(247,243,220);
        switch(kind)
        {
            case ResourceSiteKind.Salt:
                Box(-.67f,-.50f,1.34f,1,0,.055f,sand);
                for(int row=0;row<2;row++)for(int col=0;col<2;col++)
                    Box(-.59f+col*.59f,-.43f+row*.43f,.51f,.36f,.055f,.025f,
                        (row+col)%2==0?new Color(232,137,164):new Color(108,207,197));
                Boulder(.51f,.31f,.21f,.27f,white);
                break;
            case ResourceSiteKind.Oil:
                Box(-.65f,-.37f,1.30f,.74f,0,.06f,rock);
                Box(-.13f,-.15f,.26f,.3f,.06f,.51f,new Color(186,124,25));
                Box(-.55f,-.10f,1.0f,.20f,.55f,.12f,new Color(255,196,50));
                Cylinder(.47f,0,.15f,.34f,.39f,new Color(213,64,43));
                Box(.43f,-.035f,.07f,.07f,.06f,.35f,white);
                break;
            case ResourceSiteKind.Gas:
                Box(-.41f,-.05f,.82f,.10f,.02f,.09f,new Color(162,170,167));
                Cylinder(-.33f,.16f,.29f,0,.34f,white);
                Cylinder(.28f,-.20f,.33f,0,.49f,white);
                Cylinder(-.33f,.16f,.296f,.30f,.065f,new Color(29,139,150));
                Cylinder(.28f,-.20f,.336f,.45f,.065f,new Color(29,139,150));
                break;
            case ResourceSiteKind.Quartz:
                Boulder(-.28f,0,.49f,.63f,rock);
                Boulder(.29f,0,.43f,.83f,new Color(87,85,92));
                Face(white,point(-.64f,.27f,.23f),point(-.37f,.30f,.31f),point(.53f,-.23f,.52f),point(.33f,-.31f,.59f));
                Boulder(.49f,.30f,.13f,.12f,white);
                break;
        }

        void Face(Color color,params Vector2[] vertices)=>FillProjectedFace(ui,vertices,
            depleted?Color.Lerp(color,new Color(78,76,74),.65f):color);

        void Box(float x,float y,float w,float d,float z,float h,Color color)
        {
            Vector2[] top={point(x,y,z+h),point(x+w,y,z+h),point(x+w,y+d,z+h),point(x,y+d,z+h)};
            if(globe)Sides(top,new[]{point(x,y,z),point(x+w,y,z),point(x+w,y+d,z),point(x,y+d,z)},color);
            Face(color,top);
        }

        void Cylinder(float x,float y,float radius,float z,float h,Color color)
        {
            var top=Ring(x,y,radius,z+h,16);
            if(globe)Sides(top,Ring(x,y,radius,z,16),color);
            Face(color,top);
        }

        void Boulder(float x,float y,float radius,float height,Color color)
        {
            var rim=Ring(x,y,radius,.06f,7);
            var peak=point(x-radius*.18f,y-radius*.12f,height);
            for(int i=0;i<rim.Length;i++)
                Face(Shade(color,i%3),peak,rim[i],rim[(i+1)%rim.Length]);
        }

        Vector2[] Ring(float x,float y,float radius,float z,int count)=>
            Enumerable.Range(0,count).Select(i=>
            {
                float angle=i*MathHelper.TwoPi/count;
                return point(x+MathF.Cos(angle)*radius,y+MathF.Sin(angle)*radius,z);
            }).ToArray();

        void Sides(Vector2[] top,Vector2[] bottom,Color color)
        {
            float winding=Cross(top[1]-top[0],top[2]-top[1]);
            for(int i=0;i<top.Length;i++)
            {
                int j=(i+1)%top.Length;
                if(Cross(top[j]-top[i],bottom[i]-top[i])*winding<0)
                    Face(Shade(color,i%3),top[i],top[j],bottom[j],bottom[i]);
            }
        }

        static Color Shade(Color color,int side)=>(color*(.62f+side*.13f)) with {A=255};
    }
}

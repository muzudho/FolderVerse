namespace FolderVerse;
using System;
using System.Linq;
using Microsoft.Xna.Framework;

public sealed partial class CubeNet
{
    internal enum ResourceSiteKind { Salt,Oil,Gas,Quartz,OffshoreGas }
    internal sealed record ResourceSite(ResourceSiteKind Kind,Vector3 Position,float Size);
    private (int Seed,SurfaceCell[] Cells,TerrainSampler Terrain,bool Offshore,bool UseLargestWaterBody) _resourceSiteKey;
    private ResourceSite[][] _resourceSites;
    private readonly System.Collections.Generic.Dictionary<Node,Point> _grovePoints=new();

    private ResourceSite[] DisplayedResourceSites(WorldSetup world,int cellId)
    {
        if(world.TutorialStep!=0 || world.EquipmentMode!=RobotEquipmentMode.Materials)return Array.Empty<ResourceSite>();
        var key=(world.WorldSeed,world.Cells,world.Terrain,ShowOffshoreSites(world),UseLargestWaterBody(world));
        if(_resourceSites==null || _resourceSiteKey!=key)
        {
            _resourceSiteKey=key;
            _resourceSites=world.Cells.Select(cell=>ResourceSites(world,cell,key.Item3)).ToArray();
            _grovePoints.Clear();
        }
        return _resourceSites[cellId];
    }

    private void DrawResourceSites(UiPainter ui,WorldSetup world,int cellId,Func<Vector3,Vector2> project,bool globe)
    {
        var cell=world.Cells[cellId];
        foreach(var site in DisplayedResourceSites(world,cellId).OrderBy(s=>project(s.Position).Y))
        {
            var origin=site.Position;
            if(globe)origin+=cell.Normal*WorldTerrain.ReliefAt(world,cell,origin,world.Terrain);
            Vector2 Point(float x,float y,float z)=>project(origin+site.Size*(cell.U*x+cell.V*y+(globe?cell.Normal*z:Vector3.Zero)));
            var node=RobotWorld.SiteNode(world,cell,site.Position);
            int remaining=node==null || !node.Functional || node.Purpose==NodePurpose.Relay?0:
                world.Robots.RawDeposit(node.Id,RobotWorld.SiteMaterial(site.Kind));
            bool depleted=world.Robots.OtherRawSupplyEnabled && remaining==0;
            if(!globe && site.Kind==ResourceSiteKind.OffshoreGas)
                DrawRecycledPPMapArt(ui,Point(0,0,0),Vector2.Distance(Point(-.9f,0,0),Point(.9f,0,0)),depleted);
            else DrawResourceSite(site.Kind,globe,(color,vertices)=>FillProjectedFace(ui,
                vertices.Select(ProjectVertex).ToArray(),depleted?Color.Lerp(color,new Color(78,76,74),.65f):color),
                globe?ProjectVertex:null);
            Vector2 ProjectVertex(Vector3 p)=>Point(p.X,p.Y,p.Z);
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
        AddOffshoreGas();
        return sites.ToArray();

        void AddOffshoreGas()
        {
            if(!ShowOffshoreSites(world) || world.IsPolarFace(cell.Face) ||
                new SeedRandom(world.WorldSeed^cell.Id*7919^0x505047).Next(3)!=0)return;
            var candidates=Enumerable.Range(0,64).Select(i=>new Point(1+i%8,1+i/8))
                .Where(p=>world.Routes.IsOpenOcean(cell.Id,p,UseLargestWaterBody(world)) && world.Routes.Walkable(cell.Id,p) &&
                    world.Nodes.At(cell.Id,p) is {Terrain:TravelTerrain.Sea} node && !world.Routes.NearWhirlpool(node))
                .Select(p=>world.Routes.Position(cell.Id,p))
                .Where(p=>sites.All(s=>Vector3.DistanceSquared(s.Position,p)>=.28f*.28f))
                .OrderBy(p=>MathF.Abs(terrain.Elevation(p)-.45f));
            foreach(var location in candidates.Take(1))
                sites.Add(new(ResourceSiteKind.OffshoreGas,location,.18f));
        }

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

    private static bool ShowOffshoreSites(WorldSetup world)=>
        !world.Robots.OtherRawSupplyEnabled || world.Robots.OffshoreRawSupplyEnabled;

    private static bool UseLargestWaterBody(WorldSetup world)=>
        world.Robots.OtherRawSupplyEnabled && !world.Robots.OffshoreUsesSeaAreaRule;

    internal static void DrawResourceSite(ResourceSiteKind kind,bool globe,Action<Color,Vector3[]> emit,Func<Vector3,Vector2> project=null)
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
            case ResourceSiteKind.OffshoreGas:
                // Keep the existing offshore PP deposit identity and amount. Its
                // visible source is now collected/reprocessed PP, not a gas tank.
                var hull=new Color(31,75,93);var pipe=new Color(26,151,143);
                var trim=new Color(249,151,49);var glass=new Color(139,212,218);
                Box(-.88f,-.54f,1.76f,1.08f,0,.12f,hull);
                Box(-.91f,-.57f,1.82f,1.14f,.10f,.10f,white);
                for(int side=0;side<2;side++)for(int end=0;end<2;end++)
                    Box(-.91f+end*1.58f,-.58f+side*.98f,.24f,.18f,.12f,.10f,trim);
                // Sloping mesh elevator brings recovered fragments up to sorting.
                Face(pipe,point(-.88f,.96f,.04f),point(-.48f,.96f,.04f),point(-.48f,.03f,.45f),point(-.88f,.03f,.45f));
                for(int step=0;step<8;step++){
                    float t=step/8f;Box(-.89f,.12f+t*.83f,.43f,.025f,.41f-t*.37f,.025f,white);
                    if(step%2==0)Box(-.79f+(step%3)*.07f,.14f+t*.78f,.08f,.06f,.44f-t*.37f,.035f,step%4==0?trim:new Color(68,120,213));
                }
                Box(-.86f,-.38f,.44f,.42f,.20f,.20f,pipe);
                Box(-.85f,-.36f,.42f,.37f,.40f,.025f,glass);
                for(int bin=0;bin<2;bin++){
                    Box(-.82f+bin*.22f,.18f,.18f,.22f,.20f,.12f,white);
                    Box(-.80f+bin*.22f,.20f,.14f,.18f,.32f,.02f,bin==0?trim:rock);
                }
                // Closed washing drum, recirculation tank and connecting pipes.
                Drum(-.34f,-.12f,.43f,.66f,.21f,glass);
                Cylinder(-.11f,-.30f,.19f,.20f,.43f,glass);
                Cylinder(-.11f,-.30f,.20f,.61f,.045f,white);
                Box(-.13f,-.34f,.48f,.055f,.65f,.055f,pipe);
                Box(.30f,-.34f,.055f,.34f,.40f,.30f,pipe);
                Box(.30f,-.24f,.29f,.47f,.20f,.30f,white);
                Box(.32f,-.22f,.25f,.43f,.49f,.04f,pipe);
                Cylinder(.70f,.05f,.15f,.22f,.48f,glass);
                Cylinder(.70f,.05f,.155f,.69f,.035f,white);
                for(int pellet=0;pellet<9;pellet++)
                    Cylinder(.64f+(pellet%3)*.057f,.00f+(pellet/3)*.05f,.027f,.53f,.04f,white);
                Box(.55f,.015f,.12f,.06f,.41f,.06f,trim);
                // Railings and mooring bollards retain the toy harbor vocabulary.
                for(int post=0;post<5;post++){
                    Box(-.83f+post*.40f,-.51f,.025f,.025f,.20f,.27f,white);
                    Cylinder(-.82f+post*.40f,.48f,.035f,.20f,.08f,hull);
                }
                Box(-.84f,-.52f,1.64f,.035f,.46f,.03f,white);
                break;
            case ResourceSiteKind.Quartz:
                Boulder(-.28f,0,.49f,.63f,rock);
                Boulder(.29f,0,.43f,.83f,new Color(87,85,92));
                Face(white,point(-.64f,.27f,.23f),point(-.37f,.30f,.31f),point(.53f,-.23f,.52f),point(.33f,-.31f,.59f));
                Boulder(.49f,.30f,.13f,.12f,white);
                break;
        }

        static Vector3 point(float x,float y,float z)=>new(x,y,z);
        void Face(Color color,params Vector3[] vertices)=>emit(color,vertices);


        void Box(float x,float y,float w,float d,float z,float h,Color color)
        {
            Vector3[] top={point(x,y,z+h),point(x+w,y,z+h),point(x+w,y+d,z+h),point(x,y+d,z+h)};
            if(globe)Sides(top,new[]{point(x,y,z),point(x+w,y,z),point(x+w,y+d,z),point(x,y+d,z)},color);
            Face(color,top);
        }

        void Cylinder(float x,float y,float radius,float z,float h,Color color)
        {
            var top=Ring(x,y,radius,z+h,16);
            if(globe)Sides(top,Ring(x,y,radius,z,16),color);
            Face(color,top);
        }

        void Drum(float x,float y,float z,float length,float radius,Color color)
        {
            Vector3[] End(float at,float r)=>Enumerable.Range(0,16).Select(i=>{
                float angle=i*MathHelper.TwoPi/16;return point(at,y+MathF.Cos(angle)*r,z+MathF.Sin(angle)*r);
            }).ToArray();
            var a=End(x,radius);var b=End(x+length,radius);
            Sides(b,a,color);Face(new Color(26,151,143),b);Face(new Color(26,151,143),a.Reverse().ToArray());
            for(int band=0;band<3;band++)Sides(End(x+band*length/2+.025f,radius+.012f),End(x+band*length/2,radius+.012f),white);
        }

        void Boulder(float x,float y,float radius,float height,Color color)
        {
            var rim=Ring(x,y,radius,.06f,7);
            var peak=point(x-radius*.18f,y-radius*.12f,height);
            for(int i=0;i<rim.Length;i++)
                Face(Shade(color,i%3),peak,rim[i],rim[(i+1)%rim.Length]);
        }

        Vector3[] Ring(float x,float y,float radius,float z,int count)=>
            Enumerable.Range(0,count).Select(i=>
            {
                float angle=i*MathHelper.TwoPi/count;
                return point(x+MathF.Cos(angle)*radius,y+MathF.Sin(angle)*radius,z);
            }).ToArray();

        void Sides(Vector3[] top,Vector3[] bottom,Color color)
        {
            var screenTop=project==null?null:top.Select(project).ToArray();
            float winding=screenTop==null?0:Cross(screenTop[1]-screenTop[0],screenTop[2]-screenTop[1]);
            for(int i=0;i<top.Length;i++)
            {
                int j=(i+1)%top.Length;
                if(screenTop==null || Cross(screenTop[j]-screenTop[i],project(bottom[i])-screenTop[i])*winding<0)
                    Face(Shade(color,i%3),top[i],top[j],bottom[j],bottom[i]);
            }
        }

        static Color Shade(Color color,int side)=>(color*(.62f+side*.13f)) with {A=255};
    }
}

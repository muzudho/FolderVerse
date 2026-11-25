namespace FolderVerse;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public void DrawGlobeLayers(UiPainter ui,WorldSetup setup,WorldPreview globe,Rectangle panel,float yaw,float pitch,float pulse)
    {
        using var measure=new WorldPreview.DrawCpuScope();
        AppearanceTime=pulse;
        RefreshScenicRoutes(setup);
        var offset=setup.Terrain;
        var joinedPortals=new HashSet<(int First,int Second,Vector3 Position)>();
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p)
            {
                // Sample just inside this face when the route ends exactly on its edge.
                float relief=WorldTerrain.ReliefAt(setup,cell,Vector3.Lerp(p,cell.Center,.001f),offset);
                globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result,relief);
                return result;
            }
            DrawCellRoutes(ui,setup,cell.Id,Project,pulse,drawNodes:false,globeRibbon:true,
                joinPortal:(portal,dashed,danger,dark,tint)=>
                {
                    var other=setup.Cells[portal.Target];
                    if(!globe.ProjectVisible(other.Center,other.Normal,yaw,pitch,panel,out _))return;
                    var key=(Math.Min(cell.Id,other.Id),Math.Max(cell.Id,other.Id),portal.Position);
                    if(!joinedPortals.Add(key))return;
                    var edge=portal.Position;
                    var a=edge+cell.Normal*WorldTerrain.ReliefAt(setup,cell,Vector3.Lerp(edge,cell.Center,.001f),offset);
                    var b=edge+other.Normal*WorldTerrain.ReliefAt(setup,other,Vector3.Lerp(edge,other.Center,.001f),offset);
                    if(Vector3.DistanceSquared(a,b)<.000001f)return;
                    // The route lifts away from the corner before touching the opposite face.
                    var crown=edge+(cell.Normal+other.Normal)*(.035f+Math.Max(Vector3.Distance(a,edge),Vector3.Distance(b,edge)));
                    var sea=portal.Terrain==TravelTerrain.Sea;
                    var previous=ProjectBridge(a);float dangerPhase=0;
                    for(int i=1;i<=12;i++)
                    {
                        float t=i/12f;
                        var next=ProjectBridge(Vector3.Lerp(Vector3.Lerp(a,crown,t),Vector3.Lerp(crown,b,t),t));
                        if(danger)DrawDangerRoad(ui,previous,next,ref dangerPhase,true); else if(!dashed || (i-1)/2%2==0)DrawBridgeSegment(previous,next,sea,dashed,dark,tint);
                        previous=next;
                    }
                    Vector2 ProjectBridge(Vector3 p)
                    {
                        globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var at);
                        return at;
                    }
                });
        }

        void DrawBridgeSegment(Vector2 a,Vector2 b,bool sea,bool dashed,bool dark,Func<Color,Vector2,Color> tint)
        {
            var delta=b-a;
            if(delta.LengthSquared()<.001f)return;
            var center=(a+b)/2;
            float angle=MathF.Atan2(delta.Y,delta.X);
            if(!dark)ui.Tile(center+new Vector2(0,2),new Vector2(delta.Length()+2,dashed?4:7),angle,
                tint(sea?new Color(39,107,137):new Color(145,99,33),center));
            ui.Tile(center,new Vector2(delta.Length()+2,dashed?3:6),angle,
                tint(sea?new Color(108,213,245):new Color(255,216,99),center));
        }
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result);return result;}
            if (ShowRoutes && DirectoryOnly) continue;
            if (ShowRoutes) DrawResourceSites(ui,setup,cell.Id,Project,globe:true);
            if (ShowRoutes) DrawLandmarks(ui,setup,cell.Id,p=>Project(p),pulse);
            var obstacles=ResourceClearance(setup,cell.Id);
            foreach (var node in setup.Nodes.InCell(cell.Id))
            {
                if (!NodeVisible(setup,node)) continue;
                if(ShowRoutes)DrawMapleDeposit(ui,setup,node,Project,globe:true);
                var position = SiteDisplayPosition(setup,node,obstacles);
                position+=cell.Normal*WorldTerrain.ReliefAt(setup,cell,position,offset);
                var at = Project(position);
                Color Tint(Color color) => NodeTint(node.Id,color);
                float scale = MathF.Max(Vector2.Distance(at,Project(position+cell.U)),Vector2.Distance(at,Project(position+cell.V)));
                float radius = GlobeNodeRadius(node,scale,globe.GlobeZoom);
                if (ShowRoutes) DrawSurfacePuck(ui,setup,node,cell,position,radius,pulse,Project,Tint);
                var roof=ShowRoutes?PurposeFlagAnchor(node,FacilityDevelopment.Grade(setup,node)):null;
                if(roof is Vector3 anchor)
                    position+=radius*GlobePurposeBuildingScale*(cell.U*anchor.X+cell.V*anchor.Y+cell.Normal*anchor.Z);
                bool centered=ShowRoutes && HasFacilityPlatform(node);
                float flagOffset=HasGlobeBuilding(node)?GlobePurposeBuildingScale:1;
                DrawSurfaceFlag(ui,setup,DisplayedNodeOwner(node),cell,position,
                    ShowRoutes && roof==null ? radius*flagOffset : 0,Project,c=>NodeFlagTint(node.Id,c),roof!=null,centered,AnimateFlags?pulse:0);
            }
        }
        DrawMapSymbols(ui,setup,p=>
        {
            if(!globe.ProjectVisible(p.Cell.Center,p.Cell.Normal,yaw,pitch,panel,out _))return null;
            globe.ProjectVisible(p.A,p.Cell.Normal,yaw,pitch,panel,out var a);
            globe.ProjectVisible(p.B,p.Cell.Normal,yaw,pitch,panel,out var b);
            return (a,b);
        });
        DrawGlobeRobotOverflow(ui);
    }
    // HUD is composited after atmosphere, so clouds cannot wash out the ring.
    public void DrawGlobeHud(UiPainter ui,WorldSetup setup,WorldPreview globe,Rectangle panel,float yaw,float pitch,float time){
        AppearanceTime=time;
        foreach(var node in setup.Nodes.All)
            if(NodeVisible(setup,node) && NodeNeedsRing(node.Id) && TryGlobeHudRing(setup,node,globe,panel,yaw,pitch,out var ring))DrawNodeHud(ui,node.Id,ring);
    }

    public bool TryGlobeHudRing(WorldSetup setup,Node node,WorldPreview globe,Rectangle panel,float yaw,float pitch,out MapHudRing ring){
        var cell=setup.Cells[node.Cell];var point=SiteDisplayPosition(setup,node,ResourceClearance(setup,node.Cell));
        point+=cell.Normal*WorldTerrain.ReliefAt(setup,cell,point,setup.Terrain);
        ring=default;
        if(!globe.ProjectVisible(point,cell.Normal,yaw,pitch,panel,out var at))return false;
        Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result);return result;}
        float unit=Math.Max(Vector2.Distance(at,Project(point+cell.U)),Vector2.Distance(at,Project(point+cell.V)));
        float radius=GlobeNodeRadius(node,unit,globe.GlobeZoom);
        ring=NodeHudEnvelope(node,(x,y,z)=>Project(point+radius*(cell.U*x+cell.V*y+cell.Normal*z)));
        return true;
    }
    // Enclose the building silhouette, not only the foot of its flagpole.
    private static MapHudRing NodeHudEnvelope(Node node,Func<float,float,float,Vector2> project){
        float extent=HasFacilityPlatform(node)?FacilityPlatformScale:PurposeBuildingScale*1.5f;
        float height=HasFacilityPlatform(node)?FacilityModelScale*1.4f:PurposeBuildingScale*1.8f;
        var min=new Vector2(float.PositiveInfinity);var max=new Vector2(float.NegativeInfinity);
        foreach(float x in new[]{-extent,extent})foreach(float y in new[]{-extent,extent})foreach(float z in new[]{0f,height}){
            var at=project(x,y,z);min=Vector2.Min(min,at);max=Vector2.Max(max,at);
        }
        return MapHud.Enclose(min,max);
    }
    public MapHudRing NetHudRing(WorldSetup setup,Node node,Rectangle panel){
        var at=AnimatedNodePosition(setup,node,panel);float radius=MapPuckRadius(node,Fit(panel).Scale);
        // Flat building artwork uses the same local x/y/z basis as DrawFlatPuck.
        return NodeHudEnvelope(node,(x,y,z)=>at+new Vector2(x,y*.68f-z)*radius);
    }

    internal float MapPuckRadius(Node node,float projectedUnit)
    {
        float radius = MathHelper.Clamp(projectedUnit*.06f,4,10);
        if (node.IsHarbor) radius = MathHelper.Clamp(radius*1.3f,7,13);
        return EmphasizedNode(node.Id) ? radius*1.4f : radius;
    }
    private const float PurposeBuildingScale=1.7f;
    private const float GlobePurposeBuildingScale=PurposeBuildingScale;
    private const float GlobeObjectHeight=.20f;
    private const float RoofFlagPoleHeight=.065f;
    private static bool HasPurposeBuilding(Node node)=>node.Functional && node.Purpose!=NodePurpose.General;
    private static bool HasGlobeBuilding(Node node)=>HasCivilianSettlement(node);
    private static bool HasFacilityPlatform(Node node)=>node.Functional && node.Purpose==NodePurpose.General && !node.IsHarbor;
    private const float FacilityPlatformScale=2.15f*.90f;
    private const float FacilityModelScale=.85f*2;

    internal float GlobeNodeRadius(Node node,float projectedUnit,float zoom)
    {
        // Size buildings at the reference zoom, then let projection scale them with the globe.
        float referenceUnit=HasGlobeBuilding(node)?projectedUnit/zoom:projectedUnit;
        float radius=MapPuckRadius(node,referenceUnit)/MathF.Max(referenceUnit,1);
        if(!HasGlobeBuilding(node))return radius;
        // The factory chimney is the tallest of the remaining building models.
        const float maxModelHeight=1.8f;
        radius=Math.Min(radius,GlobeObjectHeight/(GlobePurposeBuildingScale*maxModelHeight));
        return PurposeFlagAnchor(node,2) is Vector3 roof
            ? Math.Min(radius,(GlobeObjectHeight-RoofFlagPoleHeight)/(GlobePurposeBuildingScale*roof.Z))
            : radius;
    }

    internal float GlobeNodeHitRadius(Node node,float projectedUnit,float zoom)
        => GlobeNodeRadius(node,projectedUnit,zoom)*projectedUnit*
            (HasFacilityPlatform(node)?FacilityPlatformScale:HasGlobeBuilding(node)?GlobePurposeBuildingScale:1)+4;

    private static void DrawNodeFacilities(UiPainter ui,WorldSetup world,Node node,
        Func<float,float,float,Vector2> project,Func<Color,Color> tint)
    {
        if(HasFacilityPlatform(node)){
            DrawDevelopedFacility(ui,NodePurpose.General,node.Terrain==TravelTerrain.Sea,FacilityDevelopment.Grade(world,node),
                (x,y,z)=>project(x*FacilityModelScale,y*FacilityModelScale,z*FacilityModelScale),tint);
            return;
        }
        DrawFacilities(ui,world,node,true,(x,y,z)=>project(-.95f+x*.85f,y*.85f,z*.85f),tint);
        if(!node.Functional)return;
        if(node.IsHarbor || RelocatedFacilityGroups(node)>0)
            DrawFacilities(ui,world,node,false,(x,y,z)=>project(.85f+x*.66f,.78f+y*.66f,z*.66f),tint);
    }

    private void DrawFlatPuck(UiPainter ui,WorldSetup setup,Node node,Vector2 at,float radius,float pulse,Func<Color,Color> tint,Func<Vector3,Vector2> project,Func<Color,Color> flagTint=null)
    {
        bool sea = !setup.Routes.Land(node.Cell,node.Center);
        var top = sea ? new Color(164,226,242) : new Color(255,232,153);
        var side = sea ? new Color(52,110,135) : new Color(152,111,53);
        var rim = new Color(24,44,51);
        float depth = MathHelper.Clamp(radius*.38f,2,5);
        void Face(Vector2 center,float r,Color color)
        {
            const float squash = .68f;
            float height = r*squash;
            for (float y=-height+.5f;y<height;y++)
            {
                float offset=y/squash;
                float width=2*MathF.Sqrt(Math.Max(0,r*r-offset*offset));
                ui.Tile(center+new Vector2(0,y),new(width,1.1f),0,tint(color));
            }
        }
        bool target = MovementTargets.Contains(node.Id) && (!RestrictNodeActions || ActionTargetNodes.Contains(node.Id));
        float platformRadius=HasFacilityPlatform(node)?radius*FacilityPlatformScale:radius;
        bool civilian=HasCivilianSettlement(node);
        bool purpose=HasPurposeBuilding(node) || node.IsHarbor && node.Functional;
        if(node.IsHarbor && purpose && TryHarborFrame(setup,node,out var shore,out var seaward)){
            var harborAt=project(shore);
            var forward=Vector2.Normalize(project(shore+seaward*.1f)-harborAt);
            var across=new Vector2(forward.Y,-forward.X);
            DrawHarborPier(ui,(x,y,z)=>FlatHarborPoint(harborAt,forward,across,radius,x,y),tint,FacilityDevelopment.Grade(setup,node));
            DrawHarborBadge(ui,harborAt-across*radius*1.45f,radius,tint,ScreenAngle);
        }
        else if(purpose)
            DrawPurposeBuilding(ui,node.Purpose,sea,(x,y,z)=>at+new Vector2(x,y*.68f-z)*radius*PurposeBuildingScale,tint,FacilityDevelopment.Grade(setup,node));
        else if(HasFacilityPlatform(node) || !civilian){
            Face(at+new Vector2(0,depth),platformRadius+1,rim);
            Face(at+new Vector2(0,depth),platformRadius,side);
            Face(at,platformRadius+1,rim);
            Face(at,platformRadius,node.Functional ? top : Color.Lerp(top,rim,.35f));
        }
        if(purpose)DrawFactorySmoke(ui,node,FacilityDevelopment.Grade(setup,node),pulse,(x,y,z)=>at+new Vector2(x,y*.68f-z)*radius*PurposeBuildingScale,tint);
        if(civilian)DrawNodeFacilities(ui,setup,node,(x,y,z)=>
            at+new Vector2(x+y*.34f,y*.58f-x*.14f-z)*radius,tint);
        var flagFoot=PurposeFlagAnchor(node,FacilityDevelopment.Grade(setup,node)) is Vector3 roof
            ? at+new Vector2(roof.X,roof.Y*.68f-roof.Z)*radius*PurposeBuildingScale
            : HasFacilityPlatform(node)?at:at+new Vector2(radius*PurposeBuildingScale+4,-radius*.68f);
        CountryFlagArt.Marker(ui,setup,DisplayedNodeOwner(node),flagFoot,
            Math.Clamp((int)(radius*3),24,42),flagTint??tint,geographic:true,wave:AnimateFlags?pulse:0);
        if (AttackTargetMarkers && SelectedTarget==node.Id)
            foreach (float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4})
                ui.Tile(at,new(radius*2,2),angle,tint(rim));
    }

    private void DrawSurfacePuck(UiPainter ui,WorldSetup setup,Node node,SurfaceCell cell,Vector3 position,float radius,float pulse,
        Func<Vector3,Vector2> project,Func<Color,Color> tint)
    {
        // Buildings and flags use EnabledColor/DisabledColor, never the day/night tint.
        var originalTint=tint;
        bool sea = !setup.Routes.Land(node.Cell,node.Center);
        var top = sea ? new Color(164,226,242) : new Color(255,232,153);
        var side = sea ? new Color(52,110,135) : new Color(152,111,53);
        var rim = new Color(24,44,51);
        bool civilian=HasCivilianSettlement(node);
        bool purpose=HasPurposeBuilding(node) || node.IsHarbor && node.Functional;
        bool platform=HasFacilityPlatform(node);
        float platformRadius=platform?radius*FacilityPlatformScale:radius;
        const int sides = 12;
        Span<Vector2> lower = stackalloc Vector2[sides];
        Span<Vector2> upper = stackalloc Vector2[sides];
        Span<Vector2> ring = stackalloc Vector2[sides];
        for (int i=0;i<sides;i++)
        {
            float angle = i*MathHelper.TwoPi/sides;
            var radial = cell.U*MathF.Cos(angle)+cell.V*MathF.Sin(angle);
            lower[i] = project(position+radial*platformRadius-(platform?cell.Normal*radius*.20f:Vector3.Zero));
            upper[i] = project(position+radial*platformRadius+(platform?Vector3.Zero:cell.Normal*radius*.34f));
            ring[i] = project(position+radial*Math.Max(platformRadius,purpose?radius*GlobePurposeBuildingScale:radius)*1.32f);
        }
        bool selected = SelectedTarget==node.Id || EmphasizedNode(node.Id);
        bool target = MovementTargets.Contains(node.Id) && (!RestrictNodeActions || ActionTargetNodes.Contains(node.Id));
        if(node.IsHarbor && purpose && TryHarborFrame(setup,node,out var shore,out var seaward)){
            var across=Vector3.Cross(seaward,cell.Normal);
            var footing=HarborFooting(setup,cell,shore);
            DrawHarborPier(ui,(x,y,z)=>project(footing+radius*GlobePurposeBuildingScale*(across*x+seaward*y+cell.Normal*z)),tint,FacilityDevelopment.Grade(setup,node));
            DrawHarborBadge(ui,project(footing)+new Vector2(-Math.Clamp(radius*Vector2.Distance(project(position),project(position+cell.U))*1.65f,12,44),-12),9,tint);
        }
        else if(purpose)
            DrawPurposeBuilding(ui,node.Purpose,sea,(x,y,z)=>project(position+radius*GlobePurposeBuildingScale*(cell.U*x+cell.V*y+cell.Normal*z)),tint,FacilityDevelopment.Grade(setup,node));
        else if(platform || !civilian){
            float winding = Cross(upper[1]-upper[0],upper[2]-upper[1]);
            Span<Vector2> wall = stackalloc Vector2[4];
            for (int i=0;i<sides;i++)
            {
                int next = (i+1)%sides;
                if (Cross(upper[next]-upper[i],lower[i]-upper[i])*winding>=0) continue;
                wall[0]=upper[i];wall[1]=upper[next];wall[2]=lower[next];wall[3]=lower[i];
                FillProjectedFace(ui,wall,tint(Color.Lerp(side,top,.12f*(1+MathF.Cos(i*MathHelper.TwoPi/sides)))));
            }
            FillProjectedFace(ui,upper,tint(node.Functional ? top : Color.Lerp(top,rim,.35f)));
        }
        if(purpose)DrawFactorySmoke(ui,node,FacilityDevelopment.Grade(setup,node),pulse,(x,y,z)=>project(position+radius*GlobePurposeBuildingScale*(cell.U*x+cell.V*y+cell.Normal*z)),tint);
        if(civilian)DrawNodeFacilities(ui,setup,node,(x,y,z)=>
            project(position+radius*(cell.U*x+cell.V*y+cell.Normal*z)),tint);
        float hour=DawnMeridian is {} dawn?WorldDayCycle.Hour(setup,cell,dawn):12;
        if(DawnMeridian.HasValue && WorldDayCycle.Daylight(hour)<.5f && (purpose || civilian || platform)){
            float scale=purpose?GlobePurposeBuildingScale:1;
            for(int i=0;i<3;i++){
                Vector2 Window(float x,float z)=>project(position+radius*scale*(cell.U*x-cell.V*.31f+cell.Normal*z));
                float x=-.22f+i*.18f;
                FillProjectedFace(ui,new[]{Window(x,.22f),Window(x+.09f,.22f),Window(x+.09f,.35f),Window(x,.35f)},originalTint(new Color(255,222,128)));
            }
        }
        var center = project(position+cell.Normal*radius*(platform?0:purpose?GlobePurposeBuildingScale*.5f:.34f));
        if (AttackTargetMarkers && SelectedTarget==node.Id)
            foreach (float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4})
                ui.Tile(center,new(Vector2.Distance(center,upper[0])*2,2),angle,tint(rim));
    }

    private static void DrawSurfaceFlag(UiPainter ui,WorldSetup world,int owner,SurfaceCell cell,Vector3 position,float radius,
        Func<Vector3,Vector2> project,Func<Color,Color> tint,bool roofMounted=false,bool centered=false,float flagTime=0)
    {
        using var cost=new RenderFeatureCost.Scope(6,flagTime!=0);
        if (owner<0 || owner>=world.ActiveCount) return;
        var vertical = project(position+cell.Normal)-project(position);
        var across = MathF.Abs(Cross(project(position+cell.U)-project(position),vertical)) >=
                     MathF.Abs(Cross(project(position+cell.V)-project(position),vertical)) ? cell.U : cell.V;
        if (Cross(project(position+across)-project(position),vertical)>0) across=-across;
        var foot=roofMounted || centered?position:position+across*(radius+.025f);
        float poleHeight=roofMounted?RoofFlagPoleHeight:GlobeObjectHeight;
        float clothHeight=roofMounted?.046f:.115f;
        var apex=foot+cell.Normal*poleHeight;
        var a=project(foot);var b=project(apex);var pole=b-a;
        ui.Tile((a+b)/2,new(pole.Length(),2),MathF.Atan2(pole.Y,pole.X),tint(new Color(232,218,177)));

        int shape=world.CountryFlagShape(CountryFlagArt.GeographicBoss(world,owner));
        float width=(shape==2 ? .115f : shape==1 ? .23f : .173f)*(roofMounted?.65f:1);
        var origin=apex+across*.008f;
        // A shallow fold gives the flag two planar facets; the shared artwork follows both.
        var crease=Vector3.Normalize(Vector3.Cross(across,cell.Normal))*.022f;
        Vector2 Point(int x,int y)
        {
            float u=x/36f;
            return project(origin+across*(width*u)-cell.Normal*(clothHeight*y/24f)+crease*((1-MathF.Abs(2*u-1))+(flagTime==0?0:MathF.Sin(flagTime*1.7f+u*5)*u*.8f)));
        }
        foreach (var (box,ink) in CountryFlagArt.GeographicPatches(world,owner))
        {
            Patch(box.Left,Math.Min(box.Right,18),box,tint(ink));
            Patch(Math.Max(box.Left,18),box.Right,box,tint(Color.Lerp(ink,Color.Black,.12f)));
        }
        void Patch(int left,int right,Rectangle box,Color color)
        {
            if (left>=right) return;
            Span<Vector2> face=stackalloc Vector2[4];
            face[0]=Point(left,box.Top);face[1]=Point(right,box.Top);
            face[2]=Point(right,box.Bottom);face[3]=Point(left,box.Bottom);
            FillProjectedFace(ui,face,color);
        }
    }

    private static float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;

    private static void FillProjectedFace(UiPainter ui,ReadOnlySpan<Vector2> face,Color color)
    {
        float min=float.PositiveInfinity,max=float.NegativeInfinity;
        foreach (var p in face) { min=MathF.Min(min,p.Y);max=MathF.Max(max,p.Y); }
        for (float y=MathF.Ceiling(min-.5f)+.5f;y<max;y++)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            for (int i=0;i<face.Length;i++)
            {
                var a=face[i];var b=face[(i+1)%face.Length];
                if ((a.Y>y)==(b.Y>y)) continue;
                float x=a.X+(b.X-a.X)*(y-a.Y)/(b.Y-a.Y);
                left=MathF.Min(left,x);right=MathF.Max(right,x);
            }
            if (left<right) ui.Tile(new((left+right)/2,y),new(right-left,1),0,color);
        }
    }
}

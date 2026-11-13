namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public void DrawGlobeLayers(UiPainter ui,WorldSetup setup,WorldPreview globe,Rectangle panel,float yaw,float pitch,float pulse)
    {
        RefreshScenicRoutes(setup);
        var offset=WorldTerrain.Offset(setup.WorldSeed);
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result,WorldTerrain.ReliefAt(cell,p,offset));return result;}
            DrawCellRoutes(ui,setup,cell.Id,Project,pulse,drawNodes:false,globeRibbon:true);
        }
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result);return result;}
            if (ShowRoutes && DirectoryOnly) continue;
            if (ShowRoutes) DrawLandmarks(ui,setup,cell.Id,p=>Project(p),pulse);
            foreach (var node in setup.Nodes.InCell(cell.Id))
            {
                if (!NodeVisible(setup,node)) continue;
                var position = setup.Routes.Position(node.Cell,node.Center);
                position+=cell.Normal*WorldTerrain.ReliefAt(cell,position,offset);
                var at = Project(position);
                Color Tint(Color color) => NodeTint(node.Id,color);
                float scale = MathF.Max(Vector2.Distance(at,Project(position+cell.U)),Vector2.Distance(at,Project(position+cell.V)));
                float radius = MapPuckRadius(node,scale)/MathF.Max(scale,1);
                if (ShowRoutes) DrawSurfacePuck(ui,setup,node,cell,position,radius,pulse,Project,Tint);
                DrawSurfaceFlag(ui,setup,DisplayedNodeOwner(node),cell,position,ShowRoutes ? radius*(HasPurposeBuilding(node)?PurposeBuildingScale:1) : 0,Project,Tint);
            }
        }
        DrawMapSymbols(ui,setup,p=>
        {
            if(!globe.ProjectVisible(p.Cell.Center,p.Cell.Normal,yaw,pitch,panel,out _))return null;
            globe.ProjectVisible(p.A,p.Cell.Normal,yaw,pitch,panel,out var a);
            globe.ProjectVisible(p.B,p.Cell.Normal,yaw,pitch,panel,out var b);
            return (a,b);
        });
    }

    internal float MapPuckRadius(Node node,float projectedUnit)
    {
        float radius = MathHelper.Clamp(projectedUnit*.06f,4,10);
        if (node.IsHarbor) radius = MathHelper.Clamp(radius*1.3f,7,13);
        return EmphasizedNode(node.Id) ? radius*1.4f : radius;
    }
    private const float PurposeBuildingScale=1.7f;
    private static bool HasPurposeBuilding(Node node)=>node.Functional && node.Purpose!=NodePurpose.General;

    private void DrawFlatPuck(UiPainter ui,WorldSetup setup,Node node,Vector2 at,float radius,float pulse,Func<Color,Color> tint)
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
        bool target = MovementTargets.Contains(node.Id) && (!AttackTargetSpotlight || AttackHighlightTargets.Contains(node.Id));
        if (SelectedTarget==node.Id || EmphasizedNode(node.Id) || target && MathF.Sin(pulse*6)>=0)
            Face(at+new Vector2(0,depth),radius+3,SelectedTarget==node.Id || EmphasizedNode(node.Id) ? Color.White : new Color(255,219,96));
        bool building=HasPurposeBuilding(node);
        if(building)
            DrawPurposeBuilding(ui,node.Purpose,(x,y,z)=>at+new Vector2(x,y*.68f-z)*radius*PurposeBuildingScale,tint);
        else{
            Face(at+new Vector2(0,depth),radius+1,rim);
            Face(at+new Vector2(0,depth),radius,side);
            Face(at,radius+1,rim);
            Face(at,radius,node.Functional ? top : Color.Lerp(top,rim,.35f));
            if(node.IsHarbor && node.Functional)
                DrawHarbor(ui,at,radius*.65f,tint(new Color(230,247,250)),DimNode(node.Id));
        }
        CountryFlagArt.Marker(ui,setup,DisplayedNodeOwner(node),at+new Vector2(radius*(building?PurposeBuildingScale:1)+4,-radius*.68f),
            Math.Clamp((int)(radius*3),24,42),tint);
        if (AttackTargetMarkers && SelectedTarget==node.Id)
            foreach (float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4})
                ui.Tile(at,new(radius*2,2),angle,tint(rim));
    }

    private void DrawSurfacePuck(UiPainter ui,WorldSetup setup,Node node,SurfaceCell cell,Vector3 position,float radius,float pulse,
        Func<Vector3,Vector2> project,Func<Color,Color> tint)
    {
        bool sea = !setup.Routes.Land(node.Cell,node.Center);
        var top = sea ? new Color(164,226,242) : new Color(255,232,153);
        var side = sea ? new Color(52,110,135) : new Color(152,111,53);
        var rim = new Color(24,44,51);
        bool building=HasPurposeBuilding(node);
        const int sides = 12;
        Span<Vector2> lower = stackalloc Vector2[sides];
        Span<Vector2> upper = stackalloc Vector2[sides];
        Span<Vector2> ring = stackalloc Vector2[sides];
        for (int i=0;i<sides;i++)
        {
            float angle = i*MathHelper.TwoPi/sides;
            var radial = cell.U*MathF.Cos(angle)+cell.V*MathF.Sin(angle);
            lower[i] = project(position+radial*radius);
            upper[i] = project(position+radial*radius+cell.Normal*radius*.55f);
            ring[i] = project(position+radial*radius*(building?PurposeBuildingScale:1)*1.32f);
        }
        bool selected = SelectedTarget==node.Id || EmphasizedNode(node.Id);
        bool target = MovementTargets.Contains(node.Id) && (!AttackTargetSpotlight || AttackHighlightTargets.Contains(node.Id));
        if (selected || target && MathF.Sin(pulse*6)>=0)
            FillProjectedFace(ui,ring,tint(selected ? Color.White : new Color(255,219,96)));
        if(building)
            DrawPurposeBuilding(ui,node.Purpose,(x,y,z)=>project(position+radius*PurposeBuildingScale*(cell.U*x+cell.V*y+cell.Normal*z)),tint);
        else{
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
            if(node.IsHarbor && node.Functional){
                var puckCenter=project(position+cell.Normal*radius*.55f);
                DrawHarbor(ui,puckCenter,MathF.Max(4,Vector2.Distance(puckCenter,upper[0])*.65f),tint(rim),DimNode(node.Id));
            }
        }
        var center = project(position+cell.Normal*radius*(building?.85f:.55f));
        if (AttackTargetMarkers && SelectedTarget==node.Id)
            foreach (float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4})
                ui.Tile(center,new(Vector2.Distance(center,upper[0])*2,2),angle,tint(rim));
    }

    private static void DrawSurfaceFlag(UiPainter ui,WorldSetup world,int owner,SurfaceCell cell,Vector3 position,float radius,
        Func<Vector3,Vector2> project,Func<Color,Color> tint)
    {
        if (owner<0 || owner>=world.ActiveCount) return;
        var vertical = project(position+cell.Normal)-project(position);
        var across = MathF.Abs(Cross(project(position+cell.U)-project(position),vertical)) >=
                     MathF.Abs(Cross(project(position+cell.V)-project(position),vertical)) ? cell.U : cell.V;
        if (Cross(project(position+across)-project(position),vertical)>0) across=-across;
        var foot=position+across*(radius+.025f);
        const float poleHeight=.28f;
        var apex=foot+cell.Normal*poleHeight;
        var a=project(foot);var b=project(apex);var pole=b-a;
        ui.Tile((a+b)/2,new(pole.Length(),2),MathF.Atan2(pole.Y,pole.X),tint(new Color(232,218,177)));

        int shape=world.CountryFlagShape(owner);
        float width=shape==2 ? .115f : shape==1 ? .23f : .173f;
        var origin=apex+across*.008f;
        // A shallow fold gives the flag two planar facets; the shared artwork follows both.
        var crease=Vector3.Normalize(Vector3.Cross(across,cell.Normal))*.022f;
        Vector2 Point(int x,int y)
        {
            float u=x/36f;
            return project(origin+across*(width*u)-cell.Normal*(.115f*y/24f)+crease*(1-MathF.Abs(2*u-1)));
        }
        foreach (var (box,ink) in CountryFlagArt.Patches(world,owner))
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

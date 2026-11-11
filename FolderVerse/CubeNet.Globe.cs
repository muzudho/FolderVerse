namespace FolderVerse;
using System;
using Microsoft.Xna.Framework;
public sealed partial class CubeNet
{
    public void DrawGlobeLayers(UiPainter ui,WorldSetup setup,WorldPreview globe,Rectangle panel,float yaw,float pitch,float pulse)
    {
        foreach(var cell in setup.Cells)
        {
            if(!globe.ProjectVisible(cell.Center,cell.Normal,yaw,pitch,panel,out _))continue;
            Vector2 Project(Vector3 p){globe.ProjectVisible(p,cell.Normal,yaw,pitch,panel,out var result);return result;}
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
                var at = Project(position);
                Color Tint(Color color) => NodeTint(node.Id,color);
                if (!ShowRoutes)
                {
                    CountryFlagArt.Marker(ui,setup,DisplayedNodeOwner(node),at,28,Tint);
                    continue;
                }
                float radius = GlobePuckRadius(node, Vector2.Distance(at,Project(position+cell.U)));
                DrawGlobePuck(ui,setup,node,at,radius,pulse,Tint);
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

    internal float GlobePuckRadius(Node node,float projectedUnit)
    {
        float radius = MathHelper.Clamp(projectedUnit*.06f,4,10);
        if (node.IsHarbor) radius = MathHelper.Clamp(radius*1.3f,7,13);
        return EmphasizedNode(node.Id) ? radius*1.4f : radius;
    }

    private void DrawGlobePuck(UiPainter ui,WorldSetup setup,Node node,Vector2 at,float radius,float pulse,Func<Color,Color> tint)
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
        Face(at+new Vector2(0,depth),radius+1,rim);
        Face(at+new Vector2(0,depth),radius,side);
        Face(at,radius+1,rim);
        Face(at,radius,node.Functional ? top : Color.Lerp(top,rim,.35f));
        if (node.IsHarbor && node.Functional)
            DrawHarbor(ui,at,radius*.65f,tint(new Color(230,247,250)),DimNode(node.Id));
        CountryFlagArt.Marker(ui,setup,DisplayedNodeOwner(node),at+new Vector2(radius+4,-radius*.68f),
            Math.Clamp((int)(radius*3),24,42),tint);
        if (AttackTargetMarkers && SelectedTarget==node.Id)
            foreach (float angle in new[]{MathHelper.PiOver4,-MathHelper.PiOver4})
                ui.Tile(at,new(radius*2,2),angle,tint(rim));
    }
}

namespace FolderVerse;
using Microsoft.Xna.Framework;
using System;

public partial class Game1
{
    private static Rectangle RobotCard(int index)=>new(54+index%6*149,220+index/6*153,137,141);
    private static string PartsLabel(RobotParts parts)=>((parts&RobotParts.Head)!=0?"頭":"")+((parts&RobotParts.Body)!=0?"胴":"")+((parts&RobotParts.Legs)!=0?"脚":"");
    private static string IntegratedLabel(RobotParts parts)=>parts==(RobotParts.Head|RobotParts.Body)?"頭／胴一体型":parts==(RobotParts.Body|RobotParts.Legs)?"胴／脚一体型":"";
    private static string PartsLabel(Robot robot)=>robot.IntegratedParts==0?PartsLabel(robot.Parts):IntegratedLabel(robot.IntegratedParts)+((robot.Parts&~robot.IntegratedParts)!=0?"＋"+PartsLabel(robot.Parts&~robot.IntegratedParts):"");
    private void DrawRobotGlyph(Rectangle r,int owner,RobotParts parts,bool captain=false,float opacity=1,bool whiteOutline=false,bool damaged=false,bool cheap=false,RobotParts integrated=0)
        =>DrawRobotGlyph(r,new Robot(0,owner,parts,captain?RobotRole.Captain:RobotRole.Soldier){IntegratedParts=integrated,DamagedParts=damaged?parts:0,CheapParts=cheap?parts:0},whiteOutline,opacity);
    private void DrawRobotGlyph(Rectangle r,Robot robot,bool whiteOutline=false,float opacity=1,Func<RobotParts,float> partOpacity=null)
    {
        // Outline only the exterior, including for translucent reservations and delivery ghosts.
        var shapes=new System.Collections.Generic.List<(Rectangle Bounds,RobotParts Part,bool Face)>();
        void Shape(Rectangle bounds,RobotParts part,bool face=false){if((robot.Parts&part)!=0)shapes.Add((bounds,part,face));}
        bool headBody=(robot.IntegratedParts&robot.Parts)==(RobotParts.Head|RobotParts.Body);
        bool bodyLegs=(robot.IntegratedParts&robot.Parts)==(RobotParts.Body|RobotParts.Legs);
        int bodyTop=headBody?r.Height/4:r.Height/3,bodyBottom=r.Height/3+r.Height/3;
        int legsTop=bodyLegs?bodyBottom:r.Height*3/4,legsBottom=bodyLegs?r.Height:r.Height*3/4+r.Height/4;
        Shape(new(r.X+r.Width/4,r.Y,r.Width/2,r.Height/4),RobotParts.Head,true);
        Shape(new(r.X+r.Width/5,r.Y,r.Width*3/5,robot.Role==RobotRole.Captain?r.Height/7:r.Height/12),RobotParts.Head);
        Shape(new(r.X+r.Width/6,r.Y+bodyTop,r.Width*2/3,bodyBottom-bodyTop),RobotParts.Body);
        Shape(new(r.X+r.Width/6,r.Y+legsTop,r.Width/4,legsBottom-legsTop),RobotParts.Legs);
        Shape(new(r.X+r.Width*7/12,r.Y+legsTop,r.Width/4,legsBottom-legsTop),RobotParts.Legs);
        float Alpha(RobotParts part)=>opacity*(partOpacity?.Invoke(part)??1);
        if(whiteOutline)foreach(var shape in shapes){
            if(robot.IsCheap(shape.Part)){
                for(int y=shape.Bounds.Top;y<shape.Bounds.Bottom;y++){
                    var row=RobotPartSilhouette.Row(shape.Bounds,y,true);row.Inflate(1,1);
                    _ui.Box(row,ControlEdge*Alpha(shape.Part));
                }
                continue;
            }
            var bounds=shape.Bounds;bounds.Inflate(2,2);
            var edges=new System.Collections.Generic.List<Rectangle>{bounds};
            foreach(var filled in shapes){
                var next=new System.Collections.Generic.List<Rectangle>();
                foreach(var edge in edges){
                    var cut=Rectangle.Intersect(edge,filled.Bounds);
                    if(cut.Width<=0 || cut.Height<=0){next.Add(edge);continue;}
                    if(edge.Top<cut.Top)next.Add(new(edge.X,edge.Y,edge.Width,cut.Top-edge.Top));
                    if(cut.Bottom<edge.Bottom)next.Add(new(edge.X,cut.Bottom,edge.Width,edge.Bottom-cut.Bottom));
                    if(edge.Left<cut.Left)next.Add(new(edge.X,cut.Top,cut.Left-edge.Left,cut.Height));
                    if(cut.Right<edge.Right)next.Add(new(cut.Right,cut.Top,edge.Right-cut.Right,cut.Height));
                }
                edges=next;
            }
            foreach(var edge in edges)_ui.Box(edge,ControlEdge*Alpha(shape.Part));
        }
        foreach(var shape in shapes){
            DrawRobotPartFill(shape.Bounds,r,shape.Bounds,robot,shape.Part,robot.Origin(shape.Part),
                shape.Face?Cream:_setup.OwnerColor(robot.Origin(shape.Part)),Alpha(shape.Part));
        }
        foreach(var part in new[]{RobotParts.Head,RobotParts.Body,RobotParts.Legs})if((robot.Parts&part)!=0){
            float alpha=Alpha(part);bool damaged=robot.IsDamaged(part);
            if(damaged){
                var metal=new Color(185,180,164)*alpha;
                int y=part==RobotParts.Head?r.Y+r.Height/12:part==RobotParts.Body?r.Y+r.Height/3:r.Y+r.Height*3/4;
                _ui.Box(new(r.X+r.Width/4,y+2,Math.Max(2,r.Width/5),Math.Max(2,r.Height/28)),metal);
                _ui.Box(new(r.X+r.Width/2,y+Math.Max(4,r.Height/12),Math.Max(2,r.Width/7),Math.Max(2,r.Height/28)),metal);
            }
        }
        int workOwner=WorkReservationOwner(robot);
        if(workOwner>=0 && (robot.Parts&RobotParts.Legs)!=0)RobotWorkArt.DrawShoes(_ui,r,_setup.OwnerColor(workOwner),opacity*(partOpacity?.Invoke(RobotParts.Legs)??1),robot.ShoeOwner>=0?DefenseShoeProgress(robot):WorkShoeProgress(robot));
        DrawWorkReservationEffect(r,robot,opacity);
    }
    private void DrawRobotPartFill(Rectangle area,Rectangle robotArea,Rectangle damageArea,Robot robot,RobotParts part,int owner,Color primary,float opacity)
    {
        int pattern=_setup.CountryPattern(owner),unit=Math.Max(1,robotArea.Width/16);
        var secondary=pattern==0?primary:WorldSetup.Colors[_setup.CountrySecondaryColor(owner)];
        for(int y=area.Top;y<area.Bottom;y++){
            var row=RobotPartSilhouette.Row(area,y,robot.IsCheap(part));
            int start=row.Left;Color prior=default;
            for(int x=row.Left;x<=row.Right;x++){
                Color ink=default;
                if(x<row.Right){
                    ink=pattern!=0 && WorldSetup.CountryPatternSecondary(pattern,(x-robotArea.X)/unit,(y-robotArea.Y)/unit)?secondary:primary;
                    if(robot.IsDamaged(part))ink=RobotDamageArt.Tint(damageArea,x,y,ink);
                    ink*=opacity;
                }
                if(x==row.Left)prior=ink;
                if(x==row.Right || ink!=prior){_ui.Box(new(start,y,x-start,1),prior);start=x;prior=ink;}
            }
        }
    }
}
